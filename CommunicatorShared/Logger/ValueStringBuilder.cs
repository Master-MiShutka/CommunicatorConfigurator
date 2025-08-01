// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TMP.Work.CommunicatorPSDTU.Common.Logger;

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal ref partial struct ValueStringBuilder
{
    private char[]? arrayToReturnToPool;
    private Span<char> rawChars;
    private int length;

    public ValueStringBuilder(Span<char> initialBuffer)
    {
        this.arrayToReturnToPool = null;
        this.rawChars = initialBuffer;
        this.length = 0;
    }

    public ValueStringBuilder(int initialCapacity)
    {
        this.arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
        this.rawChars = this.arrayToReturnToPool;
        this.length = 0;
    }

    public int Length
    {
        readonly get => this.length;
        set
        {
            Debug.Assert(value >= 0);
            Debug.Assert(value <= this.rawChars.Length);
            this.length = value;
        }
    }

    public readonly int Capacity => this.rawChars.Length;

    public void EnsureCapacity(int capacity)
    {
        // This is not expected to be called this with negative capacity
        Debug.Assert(capacity >= 0);

        // If the caller has a bug and calls this with negative capacity, make sure to call Grow to throw an exception.
        if ((uint)capacity > (uint)this.rawChars.Length)
        {
            this.Grow(capacity - this.length);
        }
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// Does not ensure there is a null char after <see cref="Length"/>
    /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
    /// the explicit method call, and write eg "fixed (char* c = builder)"
    /// </summary>
    public readonly ref char GetPinnableReference()
    {
        return ref MemoryMarshal.GetReference(this.rawChars);
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ref char GetPinnableReference(bool terminate)
    {
        if (terminate)
        {
            this.EnsureCapacity(this.Length + 1);
            this.rawChars[this.Length] = '\0';
        }
        return ref MemoryMarshal.GetReference(this.rawChars);
    }

    public ref char this[int index]
    {
        get
        {
            Debug.Assert(index < this.length);
            return ref this.rawChars[index];
        }
    }

    public override string ToString()
    {
        string s = this.rawChars[..this.length].ToString();
        this.Dispose();
        return s;
    }

    /// <summary>Returns the underlying storage of the builder.</summary>
    public readonly Span<char> RawChars => this.rawChars;

    /// <summary>Returns a span representing the remaining space available in the underlying storage of the builder.</summary>
    public readonly Span<char> RemainingRawChars => this.rawChars[this.length..];

    /// <summary>
    /// Returns a span around the contents of the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ReadOnlySpan<char> AsSpan(bool terminate)
    {
        if (terminate)
        {
            this.EnsureCapacity(this.Length + 1);
            this.rawChars[this.Length] = '\0';
        }
        return this.rawChars[..this.length];
    }

    public readonly ReadOnlySpan<char> AsSpan() => this.rawChars[..this.length];
    public readonly ReadOnlySpan<char> AsSpan(int start) => this.rawChars[start..this.length];
    public readonly ReadOnlySpan<char> AsSpan(int start, int length) => this.rawChars.Slice(start, length);

    public bool TryCopyTo(Span<char> destination, out int charsWritten)
    {
        if (this.rawChars[..this.length].TryCopyTo(destination))
        {
            charsWritten = this.length;
            this.Dispose();
            return true;
        }
        else
        {
            charsWritten = 0;
            this.Dispose();
            return false;
        }
    }

    public void Insert(int index, char value, int count)
    {
        if (this.length > this.rawChars.Length - count)
        {
            this.Grow(count);
        }

        int remaining = this.length - index;
        this.rawChars.Slice(index, remaining).CopyTo(this.rawChars[(index + count)..]);
        this.rawChars.Slice(index, count).Fill(value);
        this.length += count;
    }

    public void Insert(int index, string? s)
    {
        if (s == null)
        {
            return;
        }

        int count = s.Length;

        if (this.length > (this.rawChars.Length - count))
        {
            this.Grow(count);
        }

        int remaining = this.length - index;
        this.rawChars.Slice(index, remaining).CopyTo(this.rawChars[(index + count)..]);
        s
#if !NET
				.AsSpan()
#endif
            .CopyTo(this.rawChars[index..]);
        this.length += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c)
    {
        int pos = this.length;
        Span<char> chars = this.rawChars;
        if ((uint)pos < (uint)chars.Length)
        {
            chars[pos] = c;
            this.length = pos + 1;
        }
        else
        {
            this.GrowAndAppend(c);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string? s)
    {
        if (s == null)
        {
            return;
        }

        int pos = this.length;
        if (s.Length == 1 && (uint)pos < (uint)this.rawChars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        {
            this.rawChars[pos] = s[0];
            this.length = pos + 1;
        }
        else
        {
            this.AppendSlow(s);
        }
    }

    private void AppendSlow(string s)
    {
        int pos = this.length;
        if (pos > this.rawChars.Length - s.Length)
        {
            this.Grow(s.Length);
        }

        s
#if !NET
				.AsSpan()
#endif
            .CopyTo(this.rawChars[pos..]);
        this.length += s.Length;
    }

    public void Append(char c, int count)
    {
        if (this.length > this.rawChars.Length - count)
        {
            this.Grow(count);
        }

        Span<char> dst = this.rawChars.Slice(this.length, count);
        for (int i = 0; i < dst.Length; i++)
        {
            dst[i] = c;
        }
        this.length += count;
    }

    public void Append(scoped ReadOnlySpan<char> value)
    {
        int pos = this.length;
        if (pos > this.rawChars.Length - value.Length)
        {
            this.Grow(value.Length);
        }

        value.CopyTo(this.rawChars[this.length..]);
        this.length += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AppendSpan(int length)
    {
        int origPos = this.length;
        if (origPos > this.rawChars.Length - length)
        {
            this.Grow(length);
        }

        this.length = origPos + length;
        return this.rawChars.Slice(origPos, length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        this.Grow(1);
        this.Append(c);
    }

    /// <summary>
    /// Resize the internal buffer either by doubling current buffer size or
    /// by adding <paramref name="additionalCapacityBeyondPos"/> to
    /// <see cref="length"/> whichever is greater.
    /// </summary>
    /// <param name="additionalCapacityBeyondPos">
    /// Number of chars requested beyond current position.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondPos)
    {
        Debug.Assert(additionalCapacityBeyondPos > 0);
        Debug.Assert(this.length > this.rawChars.Length - additionalCapacityBeyondPos, "Grow called incorrectly, no resize is needed.");

        const uint arrayMaxLength = 0x7FFFFFC7; // same as Array.MaxLength

        // Increase to at least the required size (_pos + additionalCapacityBeyondPos), but try
        // to double the size if possible, bounding the doubling to not go beyond the max array length.
        int newCapacity = (int)Math.Max(
            (uint)(this.length + additionalCapacityBeyondPos),
            Math.Min((uint)this.rawChars.Length * 2, arrayMaxLength));

        // Make sure to let Rent throw an exception if the caller has a bug and the desired capacity is negative.
        // This could also go negative if the actual required length wraps around.
        char[] poolArray = ArrayPool<char>.Shared.Rent(newCapacity);

        this.rawChars[..this.length].CopyTo(poolArray);

        char[]? toReturn = this.arrayToReturnToPool;
        this.rawChars = this.arrayToReturnToPool = poolArray;
        if (toReturn != null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        char[]? toReturn = this.arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn != null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }
}
