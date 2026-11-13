using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Globalization.NumberFormatting;
using Positioni.Core;
using static Positioni.Core.UiText;

namespace Positioni;

public sealed record ShortcutKeycap(string Label, bool IsWindows, bool IsShift, uint Key = 0)
{
    public Visibility WindowsIcon => IsWindows ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TextFace => IsWindows ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ShiftIcon => IsShift ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TabIcon => Key == 0x09 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility LabelVisibility => Key is >= 0x25 and <= 0x28 ? Visibility.Collapsed : Visibility.Visible;
    public double KeyWidth => Key switch { 0x20 => 112, 0x0D => 82, 0x09 => 66, _ => 47 };
    public string Glyph => Key switch
    {
        0x08 => "\uE750", // BackSpaceQWERTY
        0x0D => "\uE751", // ReturnKey
        0x25 => "\uF0B0", // ArrowLeft8
        0x26 => "\uF0AD", // ArrowUp8
        0x27 => "\uF0AF", // ArrowRight8
        0x28 => "\uF0AE", // ArrowDown8
        _ => ""
    };
    public Visibility GlyphVisibility => Glyph.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
}

public sealed partial class MainWindow
{
    private void ProfileListShortcut_Loaded(object sender, RoutedEventArgs args)
    {
        var presenter = (ContentPresenter)sender;
        if (presenter.Content is null && presenter.DataContext is Positioni.Core.Preset profile)
            presenter.Content = CreateCompactShortcut(profile);
    }

    private void ProfileListShortcut_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        var presenter = (ContentPresenter)sender;
        presenter.Content = args.NewValue is Positioni.Core.Preset profile ? CreateCompactShortcut(profile) : null;
    }

    private FrameworkElement CreateCompactShortcut(Positioni.Core.Preset profile)
    {
        if (profile.Key == 0)
            return new TextBlock
            {
                Text = profile.Shortcut,
                FontSize = 12,
                Style = (Style)Root.Resources["ImportDetailTextStyle"]
            };

        var keys = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        AutomationProperties.SetAccessibilityView(keys, AccessibilityView.Raw);
        foreach (string key in Positioni.Core.Hotkey.GetKeys(profile.Modifiers, profile.Key))
        {
            keys.Children.Add(new Border
            {
                Style = (Style)Root.Resources["CompactKeycapStyle"],
                Child = new TextBlock
                {
                    Text = key,
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }
        var shortcut = new Viewbox
        {
            Child = keys,
            StretchDirection = StretchDirection.DownOnly,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        AutomationProperties.SetName(shortcut, profile.Shortcut);
        return shortcut;
    }

    private void MorePresetOptions_Click(object sender, RoutedEventArgs args)
    {
        bool expand = MorePresetOptionsContent.Visibility == Visibility.Collapsed;
        MorePresetOptionsContent.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
        MorePresetOptionsChevron.Glyph = expand ? "\uE70E" : "\uE70D";
    }

    private void TrackProfileEdits()
    {
        Root.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(BlurFieldOnBackground), true);
        BackgroundFocusTarget.LostFocus += (_, _) => BackgroundFocusTarget.IsTabStop = false;
        PresetName.TextChanged += (_, _) => UpdateProfileEditorState();
        PresetNotes.TextChanged += (_, _) => UpdateProfileEditorState();
        foreach (var input in new[] { WidthInput, HeightInput, XInput, YInput })
        {
            input.ValueChanged += (_, _) => UpdateProfileEditorState();
            input.RegisterPropertyChangedCallback(NumberBox.TextProperty, (_, _) => UpdateProfileEditorState());
        }
        foreach (var input in new[] { PrimaryInput, KeepSizeInput, OutsideInput, EnabledInput })
        {
            input.Checked += (_, _) => UpdateProfileEditorState();
            input.Unchecked += (_, _) => UpdateProfileEditorState();
        }
    }

    private static void TrackScrollShadows(ScrollViewer scroll, Border top, Border bottom)
    {
        void Update()
        {
            const double tolerance = 0.5;
            double offset = scroll.VerticalOffset;
            top.Visibility = offset > tolerance ? Visibility.Visible : Visibility.Collapsed;
            bottom.Visibility = scroll.ScrollableHeight - offset > tolerance ? Visibility.Visible : Visibility.Collapsed;
        }

        scroll.ViewChanged += (_, _) => Update();
        scroll.RegisterPropertyChangedCallback(ScrollViewer.ScrollableHeightProperty, (_, _) => Update());
        Update();
    }

    private void BlurFieldOnBackground(object sender, PointerRoutedEventArgs args)
    {
        if (args.GetCurrentPoint(Root).Properties.IsLeftButtonPressed && IsEditorBackground(args.OriginalSource))
            ClearEditorFieldFocus();
    }

    private void EditorBackground_Tapped(object sender, TappedRoutedEventArgs args)
    {
        if (!IsEditorBackground(args.OriginalSource)) return;

        // Consume background taps before ScrollViewer can focus its first child and scroll to it.
        args.Handled = true;
        ClearEditorFieldFocus();
    }

    private void EditorBackground_DoubleTapped(object sender, DoubleTappedRoutedEventArgs args)
    {
        if (!IsEditorBackground(args.OriginalSource)) return;

        args.Handled = true;
        ClearEditorFieldFocus();
    }

    private void EditorBackground_PointerReleased(object sender, PointerRoutedEventArgs args)
    {
        if (args.GetCurrentPoint(Root).Properties.PointerUpdateKind != Microsoft.UI.Input.PointerUpdateKind.LeftButtonReleased
            || !IsEditorBackground(args.OriginalSource)) return;

        // ScrollViewer can transfer focus before the double-tap event is raised.
        args.Handled = true;
        ClearEditorFieldFocus();
    }

    private static bool IsEditorBackground(object source)
    {
        for (var element = source as DependencyObject; element != null; element = VisualTreeHelper.GetParent(element))
        {
            if (element is TextBox or NumberBox or ButtonBase or Selector or SelectorItem or ScrollBar)
                return false;
        }

        return true;
    }

    private void ClearEditorFieldFocus()
    {
        if (FocusManager.GetFocusedElement(Root.XamlRoot) is not TextBox and not NumberBox) return;

        BackgroundFocusTarget.IsTabStop = true;
        if (!BackgroundFocusTarget.Focus(FocusState.Programmatic))
            BackgroundFocusTarget.IsTabStop = false;
    }
    private bool HasProfileChanges() =>
        selected is { } profile &&
            (PresetName.Text.Trim() != profile.Name.Trim() ||
             PresetNotes.Text.Trim() != profile.Notes.Trim() ||
             DimensionChanged(WidthInput, WidthUnit, profile.WidthPercent, profile.Width) ||
             DimensionChanged(HeightInput, HeightUnit, profile.HeightPercent, profile.Height) ||
             NumberChanged(XInput, profile.X, Preset.UnsetCoordinate) ||
             NumberChanged(YInput, profile.Y, Preset.UnsetCoordinate) ||
             editPlacement != profile.Placement ||
             (PrimaryInput.IsChecked == true) != profile.PrimaryMonitor ||
             (KeepSizeInput.IsChecked == true) != profile.KeepSize ||
             (OutsideInput.IsChecked == true) != profile.AllowOutside ||
             (EnabledInput.IsChecked == true) != profile.Enabled ||
             editModifiers != profile.Modifiers ||
             editKey != profile.Key);

    private void UpdateProfileEditorState()
    {
        if (loading || SaveButton == null) return;
        bool changed = HasProfileChanges();
        bool canSave = changed;
        SaveButton.Content = Get(IsNewProfile ? "SaveNewProfile" : "SaveChanges");
        if (canSave != SaveButton.IsEnabled)
        {
            if (canSave) SaveButton.Style = AddButton.Style;
            else SaveButton.ClearValue(FrameworkElement.StyleProperty);
            SaveButton.IsEnabled = canSave;
        }
        ResetProfileButton.IsEnabled = changed;
        UpdatePositionPreview();
    }

    private static bool DimensionChanged(NumberBox input, ComboBox unit, int? percent, int pixels) =>
        (unit.SelectedIndex == PercentUnitIndex) != percent.HasValue || NumberChanged(input, percent ?? pixels, 0);

    private bool HasEditorGeometry() =>
        editPlacement != UnsetPlacement && ValidNumber(WidthInput) && ValidNumber(HeightInput) &&
        (editPlacement != ExactCoordinates || (ValidNumber(XInput) && ValidNumber(YInput)));

    private static bool ValidNumber(NumberBox input)
    {
        double value = DraftNumber(input);
        return value >= input.Minimum && value <= input.Maximum;
    }

    private static bool NumberChanged(NumberBox input, int original, int empty) =>
        double.IsNaN(DraftNumber(input)) && string.IsNullOrWhiteSpace(input.Text)
            ? original != empty : Math.Round(DraftNumber(input)) != original;

    private static double DraftNumber(NumberBox input)
    {
        double value = input.Value;
        if (input.XamlRoot is { } root && input.NumberFormatter is INumberParser parser)
        {
            var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
            for (var element = focused; element != null; element = VisualTreeHelper.GetParent(element))
            {
                if (element != input) continue;
                value = parser.ParseDouble(input.Text ?? "") ?? double.NaN;
                break;
            }
        }
        return value;
    }
}