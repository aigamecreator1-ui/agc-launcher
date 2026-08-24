using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AGC.Launcher.Controls;

/// <summary>
/// Game-HUD style progress indicator: a row of lit/unlit blocks rather than a
/// smooth flat bar, with a brighter "leading edge" block.
/// </summary>
public class SegmentedBar : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<SegmentedBar, double>(nameof(Value));

    public static readonly StyledProperty<int> SegmentCountProperty =
        AvaloniaProperty.Register<SegmentedBar, int>(nameof(SegmentCount), 28);

    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<SegmentedBar, double>(nameof(Gap), 3);

    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<SegmentedBar, IBrush?>(nameof(TrackBrush));

    public static readonly StyledProperty<IBrush?> FillBrushProperty =
        AvaloniaProperty.Register<SegmentedBar, IBrush?>(nameof(FillBrush));

    public static readonly StyledProperty<IBrush?> LeadBrushProperty =
        AvaloniaProperty.Register<SegmentedBar, IBrush?>(nameof(LeadBrush));

    static SegmentedBar()
    {
        AffectsRender<SegmentedBar>(
            ValueProperty, SegmentCountProperty, GapProperty, TrackBrushProperty, FillBrushProperty, LeadBrushProperty);
        HeightProperty.OverrideDefaultValue<SegmentedBar>(10);
    }

    /// <summary>Progress from 0.0 to 1.0.</summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int SegmentCount
    {
        get => GetValue(SegmentCountProperty);
        set => SetValue(SegmentCountProperty, value);
    }

    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? FillBrush
    {
        get => GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    public IBrush? LeadBrush
    {
        get => GetValue(LeadBrushProperty);
        set => SetValue(LeadBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        var count = Math.Max(1, SegmentCount);
        var gap = Gap;
        var totalGap = gap * (count - 1);
        var segmentWidth = (size.Width - totalGap) / count;
        if (segmentWidth <= 0)
        {
            return;
        }

        var value = Math.Clamp(Value, 0, 1);
        var litCount = (int)Math.Round(value * count);

        var track = TrackBrush ?? Brushes.DimGray;
        var fill = FillBrush ?? Brushes.Cyan;
        var lead = LeadBrush ?? fill;

        for (var i = 0; i < count; i++)
        {
            var x = i * (segmentWidth + gap);
            var rect = new Rect(x, 0, segmentWidth, size.Height);
            var brush = i < litCount ? (i == litCount - 1 ? lead : fill) : track;
            context.FillRectangle(brush, rect);
        }
    }
}
