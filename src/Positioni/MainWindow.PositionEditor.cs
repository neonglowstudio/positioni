using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Positioni.Core;
using static Positioni.Core.UiText;

namespace Positioni;

public sealed partial class MainWindow
{
    private const int UnsetPlacement = Preset.UnsetPlacement;
    private const int ResizeOnly = 0;
    private const int CenterAnchor = 5;
    private const int ExactCoordinates = 10;
    private readonly RadioButton[] alignmentPoints = new RadioButton[9];
    private int editPlacement = CenterAnchor;
    private int lastAnchor = CenterAnchor;
    private bool syncingPlacement;
    private Guid? previewProfile;
    private Rect? previewScreen;
    private Windows.Foundation.Rect previewDestination;
    private double previewScale;
    private Storyboard? previewMotion;

    private void InitializeAlignmentEditor()
    {
        var style = (Style)AlignmentGrid.Resources["AlignmentPointStyle"];
        for (int index = 0; index < alignmentPoints.Length; index++)
        {
            int placement = index + 1;
            var point = new RadioButton { Style = style, GroupName = "ProfileAlignment" };
            point.Checked += (_, _) =>
            {
                if (!syncingPlacement) SetEditorPlacement(placement);
            };
            point.Loaded += (_, _) => UpdateAlignmentPoint(point);
            Grid.SetRow(point, index / 3);
            Grid.SetColumn(point, index % 3);
            alignmentPoints[index] = point;
            AlignmentGrid.Children.Add(point);
        }

        MorePositionOptions.Click += (_, _) => SetPositionOptionsExpanded(!positionOptionsExpanded);
        PositionOptionsViewport.SizeChanged += (_, args) =>
            PositionOptionsClip.Rect = new Windows.Foundation.Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
        PositionOptionsContent.SizeChanged += (_, args) =>
        {
            if (positionOptionsExpanded) ResizePositionOptions(args.NewSize.Height);
        };
        InitializePreviewInteraction();
        PreviewArea.SizeChanged += (_, _) => UpdatePositionPreview();
        AppWindow.Changed += (_, args) =>
        {
            if (args.DidPositionChange) UpdateMonitorPreviews();
        };
    }

    private void ApplyPositionLabels()
    {

        GeometryTitle.Text = Get("GeometryTitle");
        PreviewLabel.Text = Get("PreviewLabel");
        PreviewInteractionHint.Text = Get("PreviewInteractionHint");
        DimensionsLabel.Text = Get("DimensionsLabel");
        PositionLabel.Text = Get("Position");
        MorePositionOptionsLabel.Text = Get("MorePresetOptions");
        AutomationProperties.SetName(MorePositionOptions, MorePositionOptionsLabel.Text);
        ExactPositionInput.Content = Get("ExactPosition");
        KeepPositionInput.Content = Get("Place0");
        placements = Enumerable.Range(0, 11).Select(i => Get($"Place{i}")).ToArray();
        for (int index = 0; index < alignmentPoints.Length; index++)
        {
            string label = placements[index + 1];
            AutomationProperties.SetName(alignmentPoints[index], label);
            ToolTipService.SetToolTip(alignmentPoints[index], label);
        }
        SizePositionSelector();
        SyncPositionControls();
    }

    private void SizePositionSelector()
    {
        var measureLabel = new TextBlock
        {
            FontFamily = SelectedPositionLabel.FontFamily,
            FontSize = SelectedPositionLabel.FontSize,
            FontWeight = SelectedPositionLabel.FontWeight
        };
        const double minimumSelectorWidth = 114;
        const double labelPadding = 20;
        double width = minimumSelectorWidth;
        foreach (string label in placements.Take(ExactCoordinates).Append(Get("ExactPosition")).Append(Get("UnsetPosition")))
        {
            measureLabel.Text = label;
            measureLabel.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            width = Math.Max(width, Math.Ceiling(measureLabel.DesiredSize.Width + labelPadding));
        }
        PositionSelector.Width = width;
    }

    private void SetEditorPlacement(int placement)
    {
        editPlacement = placement;
        if (placement is >= 1 and <= 9) lastAnchor = placement;
        SyncPositionControls();
        UpdateProfileEditorState();
    }

    private void SyncPositionControls()
    {
        syncingPlacement = true;
        for (int index = 0; index < alignmentPoints.Length; index++)
        {
            var point = alignmentPoints[index];
            point.IsChecked = editPlacement == index + 1;
            UpdateAlignmentPoint(point);
        }
        ExactPositionInput.IsChecked = editPlacement == ExactCoordinates;
        KeepPositionInput.IsChecked = editPlacement == ResizeOnly;
        Coordinates.Visibility = editPlacement == ExactCoordinates ? Visibility.Visible : Visibility.Collapsed;
        SelectedPositionLabel.Text = editPlacement switch
        {
            UnsetPlacement => Get("UnsetPosition"),
            ExactCoordinates => Get("ExactPosition"),
            _ => placements[editPlacement]
        };
        syncingPlacement = false;
    }

    private static void UpdateAlignmentPoint(RadioButton point) =>
        VisualStateManager.GoToState(point, point.IsChecked == true ? "SelectedPoint" : "IdlePoint", false);

    private void ExactPosition_Click(object sender, RoutedEventArgs args) =>
        SetEditorPlacement(ExactPositionInput.IsChecked == true ? ExactCoordinates : lastAnchor);

    private void KeepPosition_Click(object sender, RoutedEventArgs args) =>
        SetEditorPlacement(KeepPositionInput.IsChecked == true ? ResizeOnly : lastAnchor);

    private Storyboard? positionOptionsMotion;
    private double positionOptionsHeight;
    private bool positionOptionsExpanded;

    private void SetPositionOptionsExpanded(bool expanded)
    {
        positionOptionsExpanded = expanded;
        PositionOptionsChevron.Glyph = expanded ? "\uE70E" : "\uE70D";
        if (expanded)
        {
            PositionOptionsViewport.Visibility = Visibility.Visible;
            PositionOptionsContent.Measure(new Windows.Foundation.Size(PositionOptionsContent.Width, double.PositiveInfinity));
        }

        ResizePositionOptions(expanded ? PositionOptionsContent.DesiredSize.Height : 0);
    }

    private void ResizePositionOptions(double height)
    {
        bool animate = !loading && PositionOptionsViewport.IsLoaded && motionSettings.AnimationsEnabled;
        if (animate && positionOptionsHeight == height) return;
        positionOptionsHeight = height;

        double from = PositionOptionsViewport.Height;
        positionOptionsMotion?.Stop();
        positionOptionsMotion = null;
        PositionOptionsViewport.Height = height;

        if (!animate || from == height)
        {
            if (height == 0) PositionOptionsViewport.Visibility = Visibility.Collapsed;
            return;
        }

        var storyboard = new Storyboard();
        var resize = new DoubleAnimation
        {
            From = from,
            To = height,
            Duration = new Duration(TimeSpan.FromMilliseconds(180)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true,
            FillBehavior = FillBehavior.Stop
        };
        Storyboard.SetTarget(resize, PositionOptionsViewport);
        Storyboard.SetTargetProperty(resize, nameof(FrameworkElement.Height));
        storyboard.Children.Add(resize);
        storyboard.Completed += (_, _) =>
        {
            if (positionOptionsMotion != storyboard) return;
            positionOptionsMotion = null;
            if (height == 0) PositionOptionsViewport.Visibility = Visibility.Collapsed;
        };
        positionOptionsMotion = storyboard;
        storyboard.Begin();
    }

    private void UpdatePositionPreview()
    {
        if (loading || selected is null)
        {
            if (!loading) ClearPositionPreview();
            return;
        }

        if (previewDrag is { IsMove: true, HasMoved: true } || previewDraw is not null) return;

        double width = PreviewArea.ActualWidth;
        double height = PreviewArea.ActualHeight;
        if (width <= 24 || height <= 16) return;

        bool primary = PrimaryInput.IsChecked == true;
        Rect screen, work;
        try
        {
            (screen, work) = Native.MonitorAreas(hwnd, primary);
        }
        catch (InvalidOperationException)
        {
            ClearPositionPreview();
            return;
        }

        double scale = Math.Min((width - 24) / screen.Width, (height - 16) / screen.Height);
        PreviewClip.Rect = new Windows.Foundation.Rect(0, 0, width, height);
        PreviewMonitor.Width = screen.Width * scale;
        PreviewMonitor.Height = screen.Height * scale;
        double left = (width - PreviewMonitor.Width) / 2;
        double top = (height - PreviewMonitor.Height) / 2;
        Canvas.SetLeft(PreviewMonitor, left);
        Canvas.SetTop(PreviewMonitor, top);
        PreviewMonitor.Visibility = Visibility.Visible;

        // Project both the window and taskbar inside the monitor outline.
        double gap = Math.Min(PreviewMonitor.StrokeThickness + 1, Math.Min(PreviewMonitor.Width, PreviewMonitor.Height) / 4);
        double scaleX = (PreviewMonitor.Width - 2 * gap) / screen.Width;
        double scaleY = (PreviewMonitor.Height - 2 * gap) / screen.Height;
        double originX = left + gap, originY = top + gap;
        var taskbar = Geometry.ReservedMonitorEdge(screen, work);
        bool horizontalBar = taskbar?.Width == screen.Width;
        double originalThickness = taskbar is { } strip
            ? horizontalBar ? strip.Height * scaleY : strip.Width * scaleX
            : 0;
        double thickness = originalThickness >= 1 ? originalThickness + 2.5 : 0;
        bool taskbarAtStart = taskbar is { } edge && (horizontalBar ? edge.Y == screen.Y : edge.X == screen.X);
        if (thickness > 0)
        {
            double outline = PreviewMonitor.StrokeThickness;
            UpdatePreviewTaskbar(horizontalBar, taskbarAtStart, left + outline, top + outline,
                PreviewMonitor.Width - 2 * outline, PreviewMonitor.Height - 2 * outline,
                thickness + gap - outline);
        }
        else
        {
            PreviewTaskbar.Visibility = Visibility.Collapsed;
        }

        double Project(double coordinate, double extent, bool xAxis)
        {
            if (thickness == 0 || xAxis == horizontalBar) return coordinate;
            // Match the gap from the window to the bar with the gap to the monitor outline.
            double reserved = thickness + gap - PreviewMonitor.StrokeThickness;
            if (!taskbarAtStart)
                return extent - ProjectFromStart(extent - coordinate, extent, originalThickness, reserved);
            return ProjectFromStart(coordinate, extent, originalThickness, reserved);
        }

        double innerWidth = screen.Width * scaleX, innerHeight = screen.Height * scaleY;
        double workLeft = Project((work.X - screen.X) * scaleX, innerWidth, xAxis: true);
        double workTop = Project((work.Y - screen.Y) * scaleY, innerHeight, xAxis: false);
        double workRight = Project((work.X + work.Width - screen.X) * scaleX, innerWidth, xAxis: true);
        double workBottom = Project((work.Y + work.Height - screen.Y) * scaleY, innerHeight, xAxis: false);
        previewWorkBounds = new Windows.Foundation.Rect(originX + workLeft, originY + workTop,
            workRight - workLeft, workBottom - workTop);

        previewWorkArea = work;
        bool needsGeometry = !HasEditorGeometry();
        PreviewInteractionHint.Text = Get(needsGeometry ? "PreviewDrawHint" : "PreviewInteractionHint");
        if (needsGeometry)
        {
            previewTarget = null;
            HidePreviewWindow();
            SetPreviewCaption();
            return;
        }

        int ReadNumber(NumberBox input, int saved)
        {
            double value = DraftNumber(input);
            return double.IsFinite(value) && value >= input.Minimum && value <= input.Maximum
                ? (int)Math.Round(value) : saved;
        }

        var draft = new Preset
        {
            Placement = editPlacement == ResizeOnly ? CenterAnchor : editPlacement,
            Width = ReadNumber(WidthInput, selected.Width),
            Height = ReadNumber(HeightInput, selected.Height),
            WidthPercent = WidthUnit.SelectedIndex == PercentUnitIndex ? ReadNumber(WidthInput, selected.WidthPercent ?? 50) : null,
            HeightPercent = HeightUnit.SelectedIndex == PercentUnitIndex ? ReadNumber(HeightInput, selected.HeightPercent ?? 75) : null,
            X = ReadNumber(XInput, selected.X),
            Y = ReadNumber(YInput, selected.Y),
            AllowOutside = OutsideInput.IsChecked == true,
        };
        // The actual size of another window is unknown until the profile is applied.
        var target = Geometry.Resolve(draft, work, work);
        previewTarget = target;
        int visibleLeft = Math.Max(target.X, screen.X) - screen.X;
        int visibleTop = Math.Max(target.Y, screen.Y) - screen.Y;
        int visibleRight = Math.Min(target.X + target.Width, screen.X + screen.Width) - screen.X;
        int visibleBottom = Math.Min(target.Y + target.Height, screen.Y + screen.Height) - screen.Y;
        if (visibleLeft >= visibleRight || visibleTop >= visibleBottom)
        {
            HidePreviewWindow();
            SetPreviewCaption("PreviewOutside");
            return;
        }

        SetPreviewCaption(KeepSizeInput.IsChecked == true ? "PreviewKeepSize" : null);
        double x = Project(visibleLeft * scaleX, innerWidth, xAxis: true);
        double y = Project(visibleTop * scaleY, innerHeight, xAxis: false);
        double previewWidth = Project(visibleRight * scaleX, innerWidth, xAxis: true) - x;
        double previewHeight = Project(visibleBottom * scaleY, innerHeight, xAxis: false) - y;
        double inset = Math.Min(2, Math.Min(previewWidth, previewHeight) / 4);
        bool animate = previewDrag is null && PreviewWindow.Visibility == Visibility.Visible && previewProfile == selected.Id
            && previewScreen == screen && previewScale == scale;
        PreviewWindow.Visibility = Visibility.Visible;
        previewProfile = selected.Id;
        previewScreen = screen;
        previewScale = scale;
        MovePreviewWindow(new Windows.Foundation.Rect(
            originX + x + inset, originY + y + inset, previewWidth - 2 * inset, previewHeight - 2 * inset), animate);
    }

    private static double ProjectFromStart(double coordinate, double extent, double oldThickness, double newThickness) =>
        coordinate <= oldThickness
            ? coordinate * newThickness / oldThickness
            : newThickness + (coordinate - oldThickness) * (extent - newThickness) / (extent - oldThickness);

    private void UpdatePreviewTaskbar(bool horizontal, bool atStart, double x, double y, double width, double height, double thickness)
    {
        PreviewTaskbar.Width = horizontal ? width : thickness;
        PreviewTaskbar.Height = horizontal ? thickness : height;
        Canvas.SetLeft(PreviewTaskbar, x + (!horizontal && !atStart ? width - thickness : 0));
        Canvas.SetTop(PreviewTaskbar, y + (horizontal && !atStart ? height - thickness : 0));

        double radius = Math.Min(thickness, PreviewMonitor.RadiusX - PreviewMonitor.StrokeThickness);
        PreviewTaskbar.CornerRadius = (horizontal, atStart) switch
        {
            (true, true) => new CornerRadius(radius, radius, 0, 0),
            (true, false) => new CornerRadius(0, 0, radius, radius),
            (false, true) => new CornerRadius(radius, 0, 0, radius),
            _ => new CornerRadius(0, radius, radius, 0)
        };
        PreviewTaskbarIcons.Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical;
        double dotSize = Math.Min(3.2, thickness / 2);
        foreach (var icon in PreviewTaskbarIcons.Children.OfType<FrameworkElement>())
            icon.Width = icon.Height = dotSize;
        PreviewTaskbar.Visibility = Visibility.Visible;
    }

    private void ClearPositionPreview()
    {
        HidePreviewWindow();
        PreviewMonitor.Visibility = Visibility.Collapsed;
        PreviewTaskbar.Visibility = Visibility.Collapsed;
        SetPreviewCaption();
    }

    private void SetPreviewCaption(string? message = null)
    {
        PreviewCaption.Text = message is null ? "" : Get(message);
        PreviewCaption.Visibility = message is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void HidePreviewWindow()
    {
        previewMotion?.Stop();
        previewMotion = null;
        PreviewWindow.Visibility = Visibility.Collapsed;
        if (previewDrag is null && previewDraw is null) SetPreviewCursor(0, 0, false);
    }

    private void MovePreviewWindow(Windows.Foundation.Rect destination, bool animate)
    {
        if (animate && previewDestination.Equals(destination)) return;
        previewDestination = destination;

        double startX = PreviewWindowOffset.X;
        double startY = PreviewWindowOffset.Y;
        double startWidth = PreviewWindow.Width;
        double startHeight = PreviewWindow.Height;
        previewMotion?.Stop();
        previewMotion = null;
        PreviewWindowOffset.X = destination.X;
        PreviewWindowOffset.Y = destination.Y;
        PreviewWindow.Width = destination.Width;
        PreviewWindow.Height = destination.Height;
        if (!animate || !motionSettings.AnimationsEnabled) return;

        var storyboard = new Storyboard();
        void Animate(DependencyObject target, string property, double from, double to)
        {
            if (from == to) return;
            var motion = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                EnableDependentAnimation = true,
                FillBehavior = FillBehavior.Stop
            };
            Storyboard.SetTarget(motion, target);
            Storyboard.SetTargetProperty(motion, property);
            storyboard.Children.Add(motion);
        }
        Animate(PreviewWindowOffset, "X", startX, destination.X);
        Animate(PreviewWindowOffset, "Y", startY, destination.Y);
        // Use the same timing for position and size so the outline stays inside the monitor while moving.
        Animate(PreviewWindow, nameof(FrameworkElement.Width), startWidth, destination.Width);
        Animate(PreviewWindow, nameof(FrameworkElement.Height), startHeight, destination.Height);
        if (storyboard.Children.Count == 0) return;
        storyboard.Completed += (_, _) =>
        {
            if (previewMotion == storyboard) previewMotion = null;
        };
        previewMotion = storyboard;
        storyboard.Begin();
    }
}