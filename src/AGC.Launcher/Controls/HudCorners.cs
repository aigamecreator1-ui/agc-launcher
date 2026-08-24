using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AGC.Launcher.Controls;

/// <summary>
/// Decorative, non-interactive overlay that draws four targeting-reticle style
/// corner brackets around whatever it's stacked on top of (in the same Grid/Panel
/// cell). Purely visual HUD accent.
/// </summary>
public class HudCorners : Control
{
    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<HudCorners, IBrush?>(nameof(Brush));

    public static readonly StyledProperty<double> BracketLengthProperty =
        AvaloniaProperty.Register<HudCorners, double>(nameof(BracketLength), 14);

    public static readonly StyledProperty<double> BracketThicknessProperty =
        AvaloniaProperty.Register<HudCorners, double>(nameof(BracketThickness), 2);

    public static readonly StyledProperty<double> InsetProperty =
        AvaloniaProperty.Register<HudCorners, double>(nameof(Inset), -1);

    static HudCorners()
    {
        AffectsRender<HudCorners>(BrushProperty, BracketLengthProperty, BracketThicknessProperty, InsetProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<HudCorners>(false);
    }

    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public double BracketLength
    {
        get => GetValue(BracketLengthProperty);
        set => SetValue(BracketLengthProperty, value);
    }

    public double BracketThickness
    {
        get => GetValue(BracketThicknessProperty);
        set => SetValue(BracketThicknessProperty, value);
    }

    public double Inset
    {
        get => GetValue(InsetProperty);
        set => SetValue(InsetProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        var brush = Brush ?? Brushes.Cyan;
        var pen = new Pen(brush, BracketThickness);
        var len = Math.Max(0, Math.Min(BracketLength, Math.Min(size.Width, size.Height) / 2));
        var i = Inset;
        var w = size.Width;
        var h = size.Height;

        // Top-left
        context.DrawLine(pen, new Point(i, i + len), new Point(i, i));
        context.DrawLine(pen, new Point(i, i), new Point(i + len, i));

        // Top-right
        context.DrawLine(pen, new Point(w - i - len, i), new Point(w - i, i));
        context.DrawLine(pen, new Point(w - i, i), new Point(w - i, i + len));

        // Bottom-right
        context.DrawLine(pen, new Point(w - i, h - i - len), new Point(w - i, h - i));
        context.DrawLine(pen, new Point(w - i, h - i), new Point(w - i - len, h - i));

        // Bottom-left
        context.DrawLine(pen, new Point(i + len, h - i), new Point(i, h - i));
        context.DrawLine(pen, new Point(i, h - i), new Point(i, h - i - len));
    }
}
