using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Positioni;

// The window retains its native size. The content lays out in unscaled units
// before the transform maps it back to the available viewport.
public sealed class UiScalePanel : Panel
{
    private readonly ScaleTransform transform = new() { ScaleX = 1, ScaleY = 1 };

    private double zoomFactor = 1;

    public double ZoomFactor
    {
        get => zoomFactor;
        set
        {
            if (ZoomFactor == value) return;
            zoomFactor = value;
            transform.ScaleX = value;
            transform.ScaleY = value;
            InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0) return new Size();
        var content = Children[0];
        double scale = ZoomFactor;
        content.Measure(new Size(availableSize.Width / scale, availableSize.Height / scale));
        return new Size(content.DesiredSize.Width * scale, content.DesiredSize.Height * scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var content = Children[0];
        double scale = ZoomFactor;
        content.Arrange(new Rect(0, 0, finalSize.Width / scale, finalSize.Height / scale));
        content.RenderTransform = scale == 1 ? null : transform;
        return finalSize;
    }
}
