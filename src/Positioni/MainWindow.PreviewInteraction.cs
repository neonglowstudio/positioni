using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Positioni.Core;
using Point = Windows.Foundation.Point;
using PreviewRect = Windows.Foundation.Rect;

namespace Positioni;

public sealed partial class MainWindow
{
    private sealed record PreviewDrag(uint PointerId, Point Start, PreviewRect Window, int Placement,
        int EdgeX, int EdgeY, Rect Work, Rect Target, double WidthValue, double HeightValue)
    {
        public bool IsMove => EdgeX == 0 && EdgeY == 0;
        public bool HasMoved { get; set; }
    }

    private sealed record PreviewDraw(uint PointerId, Point Start);
    private PreviewDraw? previewDraw;
    private PreviewDrag? previewDrag;
    private PreviewRect previewWorkBounds;
    private Rect? previewWorkArea;
    private Rect? previewTarget;

    private void InitializePreviewInteraction()
    {
        for (int index = 0; index < 9; index++)
            PreviewSnapGuides.Children.Add(new Ellipse { Width = 5, Height = 5 });

        PreviewArea.PointerEntered += PreviewPointerMoved;
        PreviewArea.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(PreviewPointerMoved), true);
        PreviewArea.PointerExited += (_, _) =>
        {
            if (previewDrag is null && previewDraw is null) SetPreviewCursor(0, 0, false);
        };
        PreviewArea.PointerPressed += PreviewPointerPressed;
        PreviewArea.PointerReleased += PreviewPointerReleased;
        PreviewArea.PointerCaptureLost += PreviewPointerCanceled;
        PreviewArea.PointerCanceled += PreviewPointerCanceled;
    }

    private (int X, int Y, bool Inside) PreviewHit(Point point)
    {
        if (PreviewWindow.Visibility != Visibility.Visible) return default;
        var bounds = previewDestination;
        bool inside = bounds.Contains(point);
        const double tolerance = 6;
        if (KeepSizeInput.IsChecked == true ||
            point.X < bounds.Left - tolerance || point.X > bounds.Right + tolerance ||
            point.Y < bounds.Top - tolerance || point.Y > bounds.Bottom + tolerance)
            return (0, 0, inside);

        int placement = editPlacement == ResizeOnly ? CenterAnchor : editPlacement;
        int column = placement == ExactCoordinates ? 0 : (placement - 1) % 3;
        int row = placement == ExactCoordinates ? 0 : (placement - 1) / 3;
        int Edge(double value, double start, double end, int anchor)
        {
            double fromStart = anchor != 0 ? Math.Abs(value - start) : double.PositiveInfinity;
            double fromEnd = anchor != 2 ? Math.Abs(value - end) : double.PositiveInfinity;
            if (Math.Min(fromStart, fromEnd) > tolerance) return 0;
            return fromStart <= fromEnd ? -1 : 1;
        }
        return (Edge(point.X, bounds.Left, bounds.Right, column),
            Edge(point.Y, bounds.Top, bounds.Bottom, row), inside);
    }

    private void SetPreviewCursor(int x, int y, bool inside, bool drawing = false)
    {
        PreviewArea.SetCursor(drawing ? InputSystemCursorShape.Cross : (x, y) switch
        {
            (0, 0) => inside ? InputSystemCursorShape.SizeAll : InputSystemCursorShape.Arrow,
            (_, 0) => InputSystemCursorShape.SizeWestEast,
            (0, _) => InputSystemCursorShape.SizeNorthSouth,
            _ => x == y ? InputSystemCursorShape.SizeNorthwestSoutheast : InputSystemCursorShape.SizeNortheastSouthwest
        });
        PreviewWindowFill.Opacity = inside || x != 0 || y != 0 ? 0.28 : 0.16;
    }

    private void PreviewPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        if (previewDrag is not null || previewDraw is not null || previewWorkArea is not { } work) return;
        var point = args.GetCurrentPoint(PreviewArea);
        if (args.Pointer.PointerDeviceType != PointerDeviceType.Mouse || !point.Properties.IsLeftButtonPressed) return;
        if (previewTarget is not { } target)
        {
            if (HasEditorGeometry() || !previewWorkBounds.Contains(point.Position)) return;
            ClearEditorFieldFocus();
            if (!PreviewArea.CapturePointer(args.Pointer)) return;
            previewDraw = new(args.Pointer.PointerId, point.Position);
            SetPreviewCursor(0, 0, false, drawing: true);
            args.Handled = true;
            return;
        }
        var hit = PreviewHit(point.Position);
        if (!hit.Inside && hit.X == 0 && hit.Y == 0) return;

        ClearEditorFieldFocus();
        if (!PreviewArea.CapturePointer(args.Pointer)) return;
        previewDrag = new(args.Pointer.PointerId, point.Position, previewDestination, editPlacement,
            hit.X, hit.Y, work, target, WidthInput.Value, HeightInput.Value);
        MovePreviewWindow(previewDestination, animate: false);
        SetPreviewCursor(hit.X, hit.Y, hit.Inside);
        args.Handled = true;
    }

    private void PreviewPointerMoved(object sender, PointerRoutedEventArgs args)
    {
        var point = args.GetCurrentPoint(PreviewArea).Position;
        if (previewDraw is { } draw && draw.PointerId == args.Pointer.PointerId)
        {
            if (DrawnPreviewRect(draw.Start, point) is { } bounds)
            {
                PreviewWindow.Visibility = Visibility.Visible;
                MovePreviewWindow(bounds, animate: false);
            }
            else HidePreviewWindow();
            args.Handled = true;
            return;
        }
        if (previewDrag is not { } drag)
        {
            UpdatePreviewHover(point);
            return;
        }
        if (args.Pointer.PointerId != drag.PointerId) return;
        args.Handled = true;
        UpdatePreviewDrag(drag, point);
    }

    private void UpdatePreviewHover(Point point)
    {
        bool drawing = selected is not null && previewWorkArea is not null && previewTarget is null
            && previewWorkBounds.Contains(point);
        var hit = PreviewHit(point);
        SetPreviewCursor(hit.X, hit.Y, hit.Inside, drawing);
    }

    private void UpdatePreviewDrag(PreviewDrag drag, Point point)
    {
        double dx = point.X - drag.Start.X, dy = point.Y - drag.Start.Y;
        if (!drag.HasMoved && Math.Max(Math.Abs(dx), Math.Abs(dy)) < 3) return;
        drag.HasMoved = true;

        if (drag.IsMove)
        {
            var work = previewWorkBounds;
            var window = drag.Window;
            double x = Math.Clamp(window.X + dx, work.Left, Math.Max(work.Left, work.Right - window.Width));
            double y = Math.Clamp(window.Y + dy, work.Top, Math.Max(work.Top, work.Bottom - window.Height));
            MovePreviewWindow(new PreviewRect(x, y, window.Width, window.Height), animate: false);
            ShowPreviewSnapGuides(PreviewAnchor(point));
            return;
        }

        int anchor = drag.Placement == ResizeOnly ? CenterAnchor : drag.Placement;
        int column = anchor == ExactCoordinates ? 0 : (anchor - 1) % 3;
        int row = anchor == ExactCoordinates ? 0 : (anchor - 1) / 3;
        bool allowOutside = OutsideInput.IsChecked == true;
        double width = drag.Target.Width + dx * drag.EdgeX * (column == 1 ? 2 : 1)
            * drag.Work.Width / previewWorkBounds.Width;
        double height = drag.Target.Height + dy * drag.EdgeY * (row == 1 ? 2 : 1)
            * drag.Work.Height / previewWorkBounds.Height;

        loading = true;
        try
        {
            if (drag.EdgeX != 0)
                SetPreviewDimension(WidthInput, WidthUnit, width, drag.Work.Width, allowOutside);
            if (drag.EdgeY != 0)
                SetPreviewDimension(HeightInput, HeightUnit, height, drag.Work.Height, allowOutside);
        }
        finally { loading = false; }
        UpdateProfileEditorState();
    }

    private static void SetPreviewDimension(NumberBox input, ComboBox unit, double pixels, int extent, bool allowOutside)
    {
        bool percent = unit.SelectedIndex == PercentUnitIndex;
        double value = percent ? pixels * 100 / extent : pixels;
        double maximum = percent || allowOutside ? input.Maximum : Math.Min(input.Maximum, extent);
        input.Value = Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero),
            input.Minimum, Math.Max(input.Minimum, maximum));
    }

    private int PreviewAnchor(Point point)
    {
        var work = previewWorkBounds;
        int column = Math.Clamp((int)((point.X - work.X) * 3 / work.Width), 0, 2);
        int row = Math.Clamp((int)((point.Y - work.Y) * 3 / work.Height), 0, 2);
        return row * 3 + column + 1;
    }

    private void ShowPreviewSnapGuides(int anchor)
    {
        var work = previewWorkBounds;
        for (int index = 0; index < PreviewSnapGuides.Children.Count; index++)
        {
            var dot = (Ellipse)PreviewSnapGuides.Children[index];
            dot.Fill = index + 1 == anchor ? PreviewWindow.Stroke : PreviewMonitor.Stroke;
            Canvas.SetLeft(dot, work.X + work.Width * (index % 3 + 0.5) / 3 - dot.Width / 2);
            Canvas.SetTop(dot, work.Y + work.Height * (index / 3 + 0.5) / 3 - dot.Height / 2);
        }
        PreviewSnapGuides.Visibility = Visibility.Visible;
    }

    private PreviewRect? DrawnPreviewRect(Point start, Point end)
    {
        var work = previewWorkBounds;
        double x = Math.Clamp(end.X, work.Left, work.Right);
        double y = Math.Clamp(end.Y, work.Top, work.Bottom);
        double width = Math.Abs(start.X - x);
        double height = Math.Abs(start.Y - y);
        return width >= 3 && height >= 3
            ? new PreviewRect(Math.Min(start.X, x), Math.Min(start.Y, y), width, height)
            : null;
    }

    private void PreviewPointerReleased(object sender, PointerRoutedEventArgs args)
    {
        if (previewDraw is { } draw && draw.PointerId == args.Pointer.PointerId)
        {
            var release = args.GetCurrentPoint(PreviewArea);
            if (release.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased) return;
            previewDraw = null;
            PreviewArea.ReleasePointerCapture(args.Pointer);
            if (selected is not null && previewWorkArea is { } work && DrawnPreviewRect(draw.Start, release.Position) is { } bounds)
            {
                var area = previewWorkBounds;
                loading = true;
                SetPreviewDimension(WidthInput, WidthUnit, bounds.Width * work.Width / area.Width, work.Width, false);
                SetPreviewDimension(HeightInput, HeightUnit, bounds.Height * work.Height / area.Height, work.Height, false);
                KeepSizeInput.IsChecked = false;
                loading = false;
                int Anchor(double offset, double slack) => slack <= 0 ? 1 : Math.Clamp((int)Math.Round(2 * offset / slack), 0, 2);
                SetEditorPlacement(1 + Anchor(bounds.Left - area.Left, area.Width - bounds.Width)
                    + 3 * Anchor(bounds.Top - area.Top, area.Height - bounds.Height));
            }
            else UpdatePositionPreview();
            UpdatePreviewHover(release.Position);
            args.Handled = true;
            return;
        }

        if (previewDrag is not { } drag || args.Pointer.PointerId != drag.PointerId) return;
        var point = args.GetCurrentPoint(PreviewArea);
        if (point.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased) return;
        UpdatePreviewDrag(drag, point.Position);
        previewDrag = null;
        PreviewSnapGuides.Visibility = Visibility.Collapsed;
        PreviewArea.ReleasePointerCapture(args.Pointer);
        if (drag.IsMove && drag.HasMoved) SetEditorPlacement(PreviewAnchor(point.Position));
        var hit = PreviewHit(point.Position);
        SetPreviewCursor(hit.X, hit.Y, hit.Inside);
        args.Handled = true;
    }

    private void PreviewPointerCanceled(object sender, PointerRoutedEventArgs args)
    {
        if (previewDraw?.PointerId == args.Pointer.PointerId)
        {
            previewDraw = null;
            UpdatePositionPreview();
            return;
        }
        if (previewDrag is not { } drag || args.Pointer.PointerId != drag.PointerId) return;
        previewDrag = null;
        PreviewSnapGuides.Visibility = Visibility.Collapsed;
        SetPreviewCursor(0, 0, false);
        if (!drag.IsMove && drag.HasMoved)
        {
            loading = true;
            WidthInput.Value = drag.WidthValue;
            HeightInput.Value = drag.HeightValue;
            loading = false;
        }
        UpdateProfileEditorState();
    }
}
