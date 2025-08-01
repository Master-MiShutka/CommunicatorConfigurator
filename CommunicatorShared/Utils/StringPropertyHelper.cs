namespace TMP.Work.CommunicatorPSDTU.Common.Utils;

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

internal static class StringPropertyHelper
{
    /// <summary>
    /// Преобразовывает массив байт в строку
    /// У свойства объекта должен быть аттрибут <see cref="System.ComponentModel.DataAnnotations.MaxLengthAttribute"/>
    /// </summary>
    /// <param name="bytes">Массив байт, первый байт - длина значения</param>
    /// <param name="type">Тип объекта, содержащий данное свойство</param>
    /// <param name="propertyName">Имя свойства объекта</param>
    /// <param name="logger">Ссылка на журнал</param>
    /// <param name="modifyFunction">Функция для модификации байтов значения</param>
    /// <returns>Значение свойства. Возвращает <see cref="string.Empty">, если не найден аттрибут <see cref="System.ComponentModel.DataAnnotations.MaxLengthAttribute"/></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetValueFromBytes(ReadOnlySpan<byte> bytes, Type type, string propertyName, ILogger logger, Func<byte, byte>? modifyFunction = null)
    {
        logger.LogTrace("* parse {Property}", propertyName);

        System.Reflection.PropertyInfo? property = type.GetProperty(propertyName) ?? throw new ArgumentNullException(nameof(propertyName));

        int maxLength = property.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? -1;

        if (maxLength == -1)
        {
            logger.LogError("Attribute 'MaxLengthAttribute' not found for property '{PropertyName}' object '{MaxLength}'!", propertyName, type);

            return string.Empty;
        }

        if (bytes.IsEmpty)
        {
            logger.LogError("Empty byte array!");

            return string.Empty;
        }

        byte valueLength = bytes[0];

        if (valueLength > maxLength)
        {
            logger.LogError("Length {Length} of field '{PropertyName}' is more than {MaxLength} chars!", valueLength, propertyName, maxLength);

            return string.Empty;
        }

        if (valueLength > bytes.Length)
        {
            logger.LogError("The read length ({Length}) of field '{PropertyName}' is greater than the number of bytes ({MaxLength}) of the array!", valueLength, propertyName, bytes.Length);
        }

        ReadOnlySpan<byte> valueBytes = bytes.Slice(1, valueLength);

        logger.LogTrace(":: byte array: [{Bytes}]", Convert.ToHexString(valueBytes));

        if (modifyFunction != null)
        {
            logger.LogTrace("Modification of data.");

            byte[] newBytes = bytes.Slice(1, valueLength).ToArray();

            for (int i = 0; i < newBytes.Length; i++)
            {
                newBytes[i] = modifyFunction(newBytes[i]);
            }

            logger.LogTrace(":: new byte array: [{Bytes}]", Convert.ToHexString(newBytes));

            string result = Encoding.ASCII.GetString(newBytes);

            return result;
        }
        else
        {
            string result = Encoding.ASCII.GetString(valueBytes);

            return result;
        }
    }
}
