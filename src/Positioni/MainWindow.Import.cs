using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Positioni.Core;
using Windows.Storage.Pickers;
using static Positioni.Core.UiText;

namespace Positioni;

public sealed partial class MainWindow
{
    private const int ProfileLimit = 500;

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        busy = true;
        int generation = closeGeneration;
        try
        {
            var source = (ImportFormat)ImportSource.SelectedIndex;
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(source switch
            {
                ImportFormat.Sizer => ".xml",
                ImportFormat.AutoHotkey => ".ahk",
                _ => ".json"
            });
            if (source == ImportFormat.DisplayFusion) picker.FileTypeFilter.Add(".reg");
            var file = await picker.PickSingleFileAsync();
            if (file == null || generation != closeGeneration || requestingClose) return;

            var (incoming, warnings) = source switch
            {
                ImportFormat.Positioni => (PresetFiles.Read(file.Path).Presets, new List<string>()),
                ImportFormat.Sizer => PresetFiles.ReadSizer(file.Path),
                _ => ExternalPresets.Read(file.Path, source, source == ImportFormat.FancyZones ? Native.WorkArea(hwnd, false) : null)
            };
            if (incoming.Count == 0)
            {
                Notify(Get("NoCompatible"), true);
                return;
            }

            var chosen = await ReviewImportAsync(incoming, warnings);
            if (chosen.Count == 0 || generation != closeGeneration || requestingClose) return;
            if (!await ConfirmProfileChangeAsync() || generation != closeGeneration || requestingClose) return;
            if (presets.Count + chosen.Count > ProfileLimit) throw new InvalidDataException(Get("ImportLimit"));

            var (affected, errors) = AddImportedProfiles(chosen);
            DismissOptions();
            if (affected.Count > 0)
                await ShowImportShortcutWarningsAsync(affected, errors, starterProfiles: false);
            else
                NotifyImportResult(Get("Imported", chosen.Count), errors);
        }
        catch (Exception ex)
        {
            if (generation == closeGeneration)
                Notify(Get("ImportError", ex is System.Xml.XmlException or System.Text.Json.JsonException
                    ? Get("InvalidFormat") : ex.Message), true);
        }
        finally { busy = false; }
    }

    private (List<(Preset Profile, ShortcutIssue Issue)> Affected, List<string> Errors) AddImportedProfiles(List<Preset> chosen)
    {
        var affected = new List<(Preset Profile, ShortcutIssue Issue)>();
        var occupied = presets.Where(p => p.CanRegisterShortcut).ToList();
        foreach (var profile in chosen)
        {
            profile.Id = Guid.NewGuid();
            if (!profile.CanRegisterShortcut) continue;

            var conflict = ShortcutConflict(profile.Modifiers, profile.Key, profile.Id, occupied);
            if (conflict is null)
            {
                occupied.Add(profile);
                continue;
            }

            affected.Add((profile.Copy(), conflict));
            profile.Key = profile.Modifiers = 0;
        }

        Persist([..presets, ..chosen]);
        foreach (var profile in chosen) presets.Add(profile);
        RefreshCount();
        if (chosen.Count > 0) SelectProfile(chosen[0]);
        return (affected, RefreshHotkeys());
    }

    private void NotifyImportResult(string message, List<string> errors)
    {
        if (errors.Count > 0) message += "\n" + string.Join("\n", errors);
        Notify(message, errors.Count > 0, persistent: true);
    }

    private Task<ContentDialogResult> ShowImportShortcutWarningsAsync(
        List<(Preset Profile, ShortcutIssue Issue)> affected, List<string> registrationErrors, bool starterProfiles)
    {
        var list = CreateShortcutWarnings(affected, registrationErrors, starterProfiles);
        list.Width = Math.Clamp(Root.ActualWidth - 80, 0, 498);
        list.MaxHeight = Math.Clamp(Root.ActualHeight - 200, 80, 440);
        var dialog = new ContentDialog
        {
            Title = Get(starterProfiles ? "StarterAddedWithWarnings" : "ImportCompletedWithWarnings"),
            Content = list,
            CloseButtonText = Get("Close"),
            DefaultButton = ContentDialogButton.Close
        };
        dialog.Resources["ContentDialogMinWidth"] = 0d;
        return ShowDialogAsync(dialog);
    }

    private Grid CreateShortcutWarnings(
        List<(Preset Profile, ShortcutIssue Issue)> affected, List<string> registrationErrors, bool starterProfiles)
    {
        string title = starterProfiles
            ? affected.Count == 1 ? "StarterMissingShortcutOne" : "StarterMissingShortcuts"
            : affected.Count == 1 ? "ImportMissingShortcutOne" : "ImportMissingShortcuts";
        var content = new StackPanel { Spacing = 12, Padding = new Thickness(0, 0, 18, 0) };
        content.Children.Add(new InfoBar
        {
            Severity = InfoBarSeverity.Warning,
            Title = Get(title),
            Message = Get(affected.Count == 1 ? "ImportAssignShortcutOne" : "ImportAssignShortcuts"),
            IsClosable = false,
            IsOpen = true
        });

        var rows = new StackPanel { Spacing = 10 };
        foreach (var (profile, issue) in affected)
        {
            var row = new StackPanel { Spacing = 3 };
            row.Children.Add(new TextBlock
            {
                Text = profile.DisplayName,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            row.Children.Add(CreateCompactShortcut(profile));
            row.Children.Add(ImportDetail(issue.ImportReason));
            rows.Children.Add(row);
        }
        if (registrationErrors.Count > 0) rows.Children.Add(ImportDetail(string.Join("\n", registrationErrors)));

        content.Children.Add(rows);
        var scroll = new ScrollViewer
        {
            Content = content,
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var list = new Grid();
        var top = new Border { Style = (Style)Root.Resources["ScrollTopShadowStyle"] };
        var bottom = new Border { Style = (Style)Root.Resources["ScrollBottomShadowStyle"] };
        TrackScrollShadows(scroll, top, bottom);
        list.Children.Add(scroll);
        list.Children.Add(top);
        list.Children.Add(bottom);
        return list;
    }

    private async Task<List<Preset>> ReviewImportAsync(List<Preset> incoming, List<string> warnings)
    {
        var dialog = new ContentDialog
        {
            Title = Get("ReviewImport"),
            KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden
        };
        dialog.Resources["ContentDialogMinWidth"] = 0d;
        bool accepted = false;
        var cancelButton = new Button { Content = Get("Cancel"), HorizontalAlignment = HorizontalAlignment.Stretch };
        var importButton = new Button
        {
            Content = new TextBlock { Text = Get("ImportSelected"), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center },
            Style = (Style)AddButton.Style,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(importButton, Get("ImportSelected"));
        cancelButton.Click += (_, _) => dialog.Hide();
        importButton.Click += (_, _) =>
        {
            accepted = true;
            dialog.Hide();
        };
        var escape = new KeyboardAccelerator { Key = Windows.System.VirtualKey.Escape };
        escape.Invoked += (_, args) =>
        {
            dialog.Hide();
            args.Handled = true;
        };
        dialog.KeyboardAccelerators.Add(escape);

        var actions = new Grid { ColumnSpacing = 8, Margin = new Thickness(0, 12, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        actions.Children.Add(cancelButton);
        Grid.SetColumn(importButton, 1);
        actions.Children.Add(importButton);

        var count = new TextBlock { Style = (Style)Root.Resources["ProfileCountTextStyle"] };
        var countBadge = new Border { Style = (Style)Root.Resources["ProfileCountBadgeStyle"], Child = count };
        var heading = new Grid { ColumnSpacing = 8 };
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        heading.Children.Add(new TextBlock { Text = Get("ImportSelectionCount"), TextWrapping = TextWrapping.Wrap });
        Grid.SetColumn(countBadge, 1);
        heading.Children.Add(countBadge);

        var selectAll = new CheckBox { Content = Get("ImportSelectAll"), IsChecked = true, IsThreeState = false };
        var limit = ImportDetail(Get("ImportLimit"));
        var rows = new StackPanel { Spacing = 4 };
        var items = new List<(Preset Profile, CheckBox Check, TextBlock Detail)>();
        foreach (var profile in incoming)
        {
            var title = new TextBlock
            {
                Text = profile.DisplayName,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            var detail = ImportDetail("");
            detail.FontSize = 12;
            const double labelIndent = 28; // WinUI checkbox square (20) plus its label gap (8).
            detail.Margin = new Thickness(labelIndent, 0, 0, 0);
            var shortcut = CreateCompactShortcut(profile);
            shortcut.Margin = new Thickness(labelIndent, 4, 0, 6);
            var check = new CheckBox
            {
                Content = title,
                IsChecked = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 0, 0, 0)
            };
            AutomationProperties.SetName(check, profile.DisplayName);
            var row = new StackPanel();
            row.Children.Add(check);
            row.Children.Add(detail);
            row.Children.Add(shortcut);
            rows.Children.Add(row);
            items.Add((profile, check, detail));
        }

        var listContent = new StackPanel { Spacing = 8, Padding = new Thickness(0, 0, 24, 0) };
        listContent.Children.Add(rows);
        if (warnings.Count > 0)
            listContent.Children.Add(ImportDetail(Get("Warnings") + "\n" + string.Join("\n", warnings)));
        var scroll = new ScrollViewer
        {
            Content = listContent,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = Math.Clamp(Root.ActualHeight - 260, 80, 425)
        };
        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(heading);
        content.Children.Add(ImportDetail(Get("ImportKeepExisting")));
        content.Children.Add(selectAll);
        content.Children.Add(limit);
        var topShadow = new Border { Style = (Style)Root.Resources["ScrollTopShadowStyle"] };
        var bottomShadow = new Border { Style = (Style)Root.Resources["ScrollBottomShadowStyle"] };
        TrackScrollShadows(scroll, topShadow, bottomShadow);
        var list = new Grid();
        list.Children.Add(scroll);
        list.Children.Add(topShadow);
        list.Children.Add(bottomShadow);
        content.Children.Add(list);
        content.Children.Add(actions);
        dialog.Content = content;

        List<Preset> Selected() => items.Where(item => item.Check.IsChecked == true).Select(item => item.Profile).ToList();

        bool updatingSelection = false;
        void RefreshSelection()
        {
            var chosen = Selected();
            count.Text = chosen.Count.ToString();
            selectAll.IsChecked = chosen.Count == 0 ? false : chosen.Count == items.Count ? true : null;
            bool overLimit = presets.Count + chosen.Count > ProfileLimit;
            limit.Visibility = overLimit ? Visibility.Visible : Visibility.Collapsed;
            importButton.IsEnabled = chosen.Count > 0 && !overLimit;
            foreach (var (profile, check, detail) in items)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(profile.Group)) parts.Add(profile.Group);
                if (profile.Width != 0 && profile.Height != 0 && !string.IsNullOrWhiteSpace(profile.Name))
                    parts.Add(profile.Dimensions);
                if (profile.Placement != UnsetPlacement) parts.Add(placements[profile.Placement]);
                if (!profile.IsComplete) parts.Add(Get("IncompleteProfile"));
                else if (!profile.Enabled) parts.Add(Get("Inactive"));
                detail.Text = string.Join(" · ", parts);
                AutomationProperties.SetHelpText(check, detail.Text + "\n" + profile.Shortcut);
            }
        }

        void RowSelectionChanged(object sender, RoutedEventArgs args)
        {
            if (!updatingSelection) RefreshSelection();
        }

        foreach (var (_, check, _) in items)
        {
            check.Checked += RowSelectionChanged;
            check.Unchecked += RowSelectionChanged;
        }
        selectAll.Click += (_, _) =>
        {
            updatingSelection = true;
            foreach (var (_, check, _) in items) check.IsChecked = selectAll.IsChecked == true;
            updatingSelection = false;
            RefreshSelection();
        };
        RefreshSelection();

        void FitDialog()
        {
            content.Width = Math.Clamp(Root.ActualWidth - 80, 0, 498);
            scroll.MaxHeight = Math.Clamp(Root.ActualHeight - 260, 80, 425);
        }
        void WindowSizeChanged(object sender, SizeChangedEventArgs args) => FitDialog();

        FitDialog();
        Root.SizeChanged += WindowSizeChanged;
        try { await ShowDialogAsync(dialog); }
        finally { Root.SizeChanged -= WindowSizeChanged; }
        return accepted ? Selected() : [];
    }

    private TextBlock ImportDetail(string text) => new()
    {
        Text = text,
        Style = (Style)Root.Resources["ImportDetailTextStyle"]
    };
}
