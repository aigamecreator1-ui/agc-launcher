using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AGC.Launcher.Controls;

/// <summary>
/// A single-child container that renders and clips to a corner-cut (chamfered)
/// shape instead of a rectangle, for the sci-fi "angular panel" look used by
/// buttons and cards throughout the app.
/// </summary>
public class AngularBorder : Decorator
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<AngularBorder, IBrush?>(nameof(Background));

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<AngularBorder, IBrush?>(nameof(BorderBrush));

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        AvaloniaProperty.Register<AngularBorder, Thickness>(nameof(BorderThickness), new Thickness(1.5));

    public static readonly StyledProperty<double> CornerCutProperty =
        AvaloniaProperty.Register<AngularBorder, double>(nameof(CornerCut), 10);

    static AngularBorder()
    {
        AffectsRender<AngularBorder>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerCutProperty);
        AffectsArrange<AngularBorder>(CornerCutProperty);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>Length, in pixels, of each corner chamfer.</summary>
    public double CornerCut
    {
        get => GetValue(CornerCutProperty);
        set => SetValue(CornerCutProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var child = Child;
        var padding = Padding;
        if (child is null)
        {
            return new Size(padding.Left + padding.Right, padding.Top + padding.Bottom);
        }

        child.Measure(availableSize.Deflate(padding));
        return child.DesiredSize.Inflate(padding);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var padding = Padding;
        Child?.Arrange(new Rect(new Point(padding.Left, padding.Top), finalSize.Deflate(padding)));
        Clip = BuildGeometry(finalSize);
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        var geometry = BuildGeometry(size);

        if (Background is { } background)
        {
            context.DrawGeometry(background, null, geometry);
        }

        var thickness = BorderThickness.Left;
        if (BorderBrush is { } borderBrush && thickness > 0)
        {
            context.DrawGeometry(null, new Pen(borderBrush, thickness), geometry);
        }
    }

    private StreamGeometry BuildGeometry(Size size)
    {
        var width = size.Width;
        var height = size.Height;
        var cut = Math.Max(0, Math.Min(CornerCut, Math.Min(width, height) / 2));

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();

        ctx.BeginFigure(new Point(cut, 0), true);
        ctx.LineTo(new Point(width - cut, 0));
        ctx.LineTo(new Point(width, cut));
        ctx.LineTo(new Point(width, height - cut));
        ctx.LineTo(new Point(width - cut, height));
        ctx.LineTo(new Point(cut, height));
        ctx.LineTo(new Point(0, height - cut));
        ctx.LineTo(new Point(0, cut));
        ctx.EndFigure(true);

        return geometry;
    }
}
