namespace TMP.Work.CommunicatorPSDTU.UI.Wpf.Controls;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PropertyChanged.SourceGenerator;

/// <summary>
/// Interaction logic for SignalIndicator.xaml
/// </summary>
public partial class SignalIndicator : UserControl
{
    private const float GoodSignalThreshold = 0.8f;
    private const float PoorSignalThreshold = 0.5f;
    private const float WeakSignalThreshold = 0.2f;
    private const float NoSignalThreshold = 0.0f;

    private bool isBuilded;

    public SignalIndicator()
    {
        this.InitializeComponent();

        this.Bars = [new() { Height = this.ActualHeight * 0.5, Width = this.ActualWidth * 0.1 }];

        this.SizeChanged += this.SignalIndicator_SizeChanged;
    }

    private void SignalIndicator_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        this.isBuilded = false;

        this.RefreshBars();
    }

    [Bindable(true)]
    public ObservableCollection<BarItem> Bars { get; private set; }

    public Brush OffBrush
    {
        get => (Brush)this.GetValue(OffBrushProperty); set => this.SetValue(OffBrushProperty, value);
    }

    // Using a DependencyProperty as the backing store for OffBrush.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty OffBrushProperty =
        DependencyProperty.Register("OffBrush", typeof(Brush), typeof(SignalIndicator), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnBrushChanged));

    public Brush OnBrush
    {
        get => (Brush)this.GetValue(OnBrushProperty); set => this.SetValue(OnBrushProperty, value);
    }

    // Using a DependencyProperty as the backing store for OffBrush.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty OnBrushProperty =
        DependencyProperty.Register("OnBrush", typeof(Brush), typeof(SignalIndicator), new FrameworkPropertyMetadata(SystemColors.AccentColorBrush, FrameworkPropertyMetadataOptions.AffectsRender, OnBrushChanged));

    public Brush StrokeBrush
    {
        get => (Brush)this.GetValue(StrokeBrushProperty); set => this.SetValue(StrokeBrushProperty, value);
    }

    // Using a DependencyProperty as the backing store for StrokeBrush.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty StrokeBrushProperty =
        DependencyProperty.Register("StrokeBrush", typeof(Brush), typeof(SignalIndicator), new FrameworkPropertyMetadata(SystemColors.AccentColorDark2Brush, FrameworkPropertyMetadataOptions.AffectsRender, OnBrushChanged));

    private static void OnBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignalIndicator signalIndicator)
        {
            signalIndicator.isBuilded = false;

            signalIndicator.RefreshBars();
        }
    }

    public byte NumberOfBars
    {
        get => (byte)this.GetValue(NumberOfBarsProperty); set => this.SetValue(NumberOfBarsProperty, value);
    }

    // Using a DependencyProperty as the backing store for NumberOfBars.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty NumberOfBarsProperty =
        DependencyProperty.Register("NumberOfBars", typeof(byte), typeof(SignalIndicator), new FrameworkPropertyMetadata((byte)6, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange, BarsCountChanged));

    private static void BarsCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignalIndicator signalIndicator)
        {
            byte newValue = (byte?)e.NewValue ?? 1;

            if (newValue == 0)
            {
                newValue = 1;
            }

            signalIndicator.isBuilded = false;

            signalIndicator.NumberOfBars = newValue;

            signalIndicator.RebuildBars();

            signalIndicator.RefreshBars();
        }
    }

    public byte MaxLevel
    {
        get => (byte)this.GetValue(MaxLevelProperty); set => this.SetValue(MaxLevelProperty, value);
    }

    // Using a DependencyProperty as the backing store for MaxLevel.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MaxLevelProperty =
        DependencyProperty.Register("MaxLevel", typeof(byte), typeof(SignalIndicator), new FrameworkPropertyMetadata((byte)31, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange, MaxLevelChanged));

    private static void MaxLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignalIndicator signalIndicator)
        {
            signalIndicator.isBuilded = false;
            signalIndicator.RefreshBars();
        }
    }

    public byte Level
    {
        get => (byte)this.GetValue(LevelProperty); set => this.SetValue(LevelProperty, value);
    }

    // Using a DependencyProperty as the backing store for Level.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register("Level", typeof(byte), typeof(SignalIndicator), new FrameworkPropertyMetadata((byte)0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange, LevelChanged));

    private static void LevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignalIndicator signalIndicator)
        {
            signalIndicator.ToolTip = e.NewValue.ToString();
            signalIndicator.RefreshBars();
        }
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        this.isBuilded = false;

        Size size = base.ArrangeOverride(arrangeBounds);

        this.RefreshBars();

        return size;
    }

    private void RebuildBars()
    {
        if (this.Bars.Count != this.NumberOfBars)
        {
            this.Bars.Clear();
            for (byte i = 0; i < this.NumberOfBars; i++)
            {
                this.Bars.Add(new BarItem());
            }
        }
    }

    private void RefreshBars()
    {
        if (this.isBuilded)
        {
            return;
        }

        if (this.ActualWidth == 0d || this.ActualHeight == 0d)
        {
            this.InvalidateMeasure();
            return;
        }

        this.RebuildBars();

        double controlWidth = this.ActualWidth, controlHeight = this.ActualHeight;

        double spaceBetweenBars = controlWidth * 0.05;

        double barHeightStep = controlHeight / this.NumberOfBars;

        double barWidth = (controlWidth - (spaceBetweenBars * (this.NumberOfBars - 1))) / this.NumberOfBars;

        if (barWidth <= 3d)
        {
            barWidth = 3d;
        }

        double levelPerBar = 1d * this.MaxLevel / this.NumberOfBars;

        for (byte i = 0; i < this.NumberOfBars; i++)
        {
            BarItem bar = this.Bars[i];

            bar.Width = barWidth;

            bar.Height = (i + 1) * barHeightStep;

            bar.Margin = i == (this.NumberOfBars - 1) ? new Thickness(0) : new Thickness(0, 0, spaceBetweenBars, 0);

            bar.Fill = (this.Level >= levelPerBar * (i + 1)) ? this.OnBrush : this.OffBrush;
        }

        this.isBuilded = true;
    }
}

public sealed partial class BarItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    [Notify] private Brush fill = SystemColors.AccentColorDark1Brush;
    [Notify] private double height;
    [Notify] private double width;
    [Notify] private Thickness margin;

}
