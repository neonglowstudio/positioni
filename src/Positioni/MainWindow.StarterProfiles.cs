using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Positioni.Core;
using static Positioni.Core.UiText;

namespace Positioni;

public sealed partial class MainWindow
{
    private static readonly (string Label, int Width, int Height, int Placement, uint Key)[] StarterProfiles =
    [
        ("StarterWideLeft", 70, 100, 1, '1'),
        ("StarterSidebar", 30, 100, 3, '2'),
        ("StarterFull", 100, 100, 5, '3'),
        ("StarterSmall", 40, 40, 5, '4'),
        ("StarterMedium", 50, 50, 5, '5'),
        ("StarterLarge", 75, 75, 5, '6'),
        ("StarterLeftHalf", 50, 100, 1, 'Q'),
        ("StarterRightHalf", 50, 100, 3, 'W'),
        ("StarterPipLarge", 70, 70, 1, 'E'),
        ("StarterPipSmall", 40, 40, 9, 'R')
    ];

    private enum StarterPickerEntry { Welcome, Options, EmptyState }

    private void ChooseStarterProfiles_Click(object sender, RoutedEventArgs e) =>
        OpenStarterProfiles(StarterPickerEntry.Options);

    private void EmptyChooseStarterProfiles_Click(object sender, RoutedEventArgs e)
    {
        if (busy || OptionsOpen) return;
        OpenStarterProfiles(StarterPickerEntry.EmptyState);
    }

    private void StarterBack_Click(object sender, RoutedEventArgs e) => ShowOptionsOverview();

    private void ShowOptionsOverview()
    {
        OptionsStarterPicker.Visibility = Visibility.Collapsed;
        OptionsStarterPicker.Content = null;
        StarterBackButton.Visibility = Visibility.Collapsed;
        OptionsTitle.Margin = new Thickness(0);
        OptionsTitle.Text = Get("Options");
        OptionsScroll.Visibility = Visibility.Visible;
        ChooseStarterProfilesButton.Focus(FocusState.Programmatic);
    }

    private void OpenStarterProfiles(StarterPickerEntry entry)
    {
        bool welcome = entry == StarterPickerEntry.Welcome;
        var target = welcome ? WelcomePicker : OptionsStarterPicker;
        target.Content = CreateStarterPicker(entry);
        if (welcome)
        {
            MainSurface.Visibility = Visibility.Collapsed;
            WelcomeSurface.Visibility = Visibility.Visible;
            return;
        }

        OptionsTitle.Text = Get("StarterProfiles");
        bool fromOptions = entry == StarterPickerEntry.Options;
        OptionsTitle.Margin = new Thickness(0, fromOptions ? 42 : 0, 0, 0);
        StarterBackButton.VerticalAlignment = VerticalAlignment.Top;
        StarterBackButton.Visibility = fromOptions ? Visibility.Visible : Visibility.Collapsed;
        OptionsScroll.Visibility = Visibility.Collapsed;
        OptionsTopShadow.Visibility = OptionsBottomShadow.Visibility = Visibility.Collapsed;
        target.Visibility = Visibility.Visible;
        if (fromOptions) StarterBackButton.Focus(FocusState.Programmatic);
        else ShowOptionsOverlay();
    }

    private Grid CreateStarterPicker(StarterPickerEntry entry)
    {
        bool welcome = entry == StarterPickerEntry.Welcome;
        var rows = new StackPanel { Spacing = 5, Padding = new Thickness(0, 0, 18, 0) };
        var choices = new List<(Preset Profile, CheckBox Check)>();
        foreach (var starter in StarterProfiles)
        {
            var profile = new Preset
            {
                Name = Get(starter.Label),
                Notes = starter.Key switch
                {
                    '1' => Get("StarterWideLeftNotes", Get("StarterSidebar")),
                    '2' => Get("StarterSidebarNotes", Get("StarterWideLeft")),
                    'E' or 'R' => Get("StarterPipNotes"),
                    _ => ""
                },
                WidthPercent = starter.Width,
                HeightPercent = starter.Height,
                Placement = starter.Placement,
                Modifiers = 6,
                Key = starter.Key
            };
            bool alreadyAdded = presets.Any(p => MatchesStarter(p, profile));
            var title = new TextBlock
            {
                Text = profile.DisplayName,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            var details = new StackPanel { Spacing = 4, Margin = new Thickness(28, 0, 0, 0) };
            details.Children.Add(CreateCompactShortcut(profile));
            if (alreadyAdded) details.Children.Add(ImportDetail(Get("StarterAlreadyAdded")));

            var check = new CheckBox
            {
                Content = title,
                IsChecked = !alreadyAdded,
                IsEnabled = !alreadyAdded,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 0, 0, 0)
            };
            AutomationProperties.SetName(check, profile.DisplayName);
            AutomationProperties.SetHelpText(check, alreadyAdded ? Get("StarterAlreadyAdded") : profile.Shortcut);

            var preview = new ProfileThumbnail { DataContext = profile, VerticalAlignment = VerticalAlignment.Top };
            preview.Loaded += ProfileThumbnail_Loaded;
            preview.Unloaded += ProfileThumbnail_Unloaded;
            var row = new Grid { ColumnSpacing = 10, Padding = new Thickness(10, 7, 10, 7) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var info = new StackPanel();
            info.Children.Add(check);
            info.Children.Add(details);
            row.Children.Add(info);
            Grid.SetColumn(preview, 1);
            row.Children.Add(preview);
            rows.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(6),
                Style = (Style)Root.Resources["StarterRowStyle"],
                Child = row
            });
            choices.Add((profile, check));
        }

        var selectAll = new CheckBox
        {
            Content = Get("ImportSelectAll"),
            Margin = new Thickness(11, 0, 0, 0),
            Padding = new Thickness(8, 0, 0, 0),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        var skip = new Button { Content = Get(welcome ? "StarterSkip" : "Cancel") };
        var add = new Button { Style = AddButton.Style };
        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = welcome ? new Thickness(0, 18, 0, 0) : new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        footer.Children.Add(skip);
        footer.Children.Add(add);
        var scroll = new ScrollViewer
        {
            Content = rows,
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var viewport = new Grid();
        var top = new Border { Style = (Style)Root.Resources["ScrollTopShadowStyle"] };
        var bottom = new Border { Style = (Style)Root.Resources["ScrollBottomShadowStyle"] };
        viewport.Children.Add(scroll);
        viewport.Children.Add(top);
        viewport.Children.Add(bottom);
        TrackScrollShadows(scroll, top, bottom);
        var limit = ImportDetail(Get("ImportLimit"));
        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, rows.Spacing) };
        if (welcome)
        {
            var title = new TextBlock
            {
                Text = Get("StarterProfiles"),
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(11, 0, 0, 4)
            };
            AutomationProperties.SetHeadingLevel(title, Microsoft.UI.Xaml.Automation.Peers.AutomationHeadingLevel.Level2);
            heading.Children.Add(title);
        }
        heading.Children.Add(selectAll);
        heading.Children.Add(limit);
        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition());
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.Children.Add(heading);
        Grid.SetRow(viewport, 1);
        Grid.SetRow(footer, 2);
        layout.Children.Add(viewport);
        layout.Children.Add(footer);

        var available = choices.Where(choice => choice.Check.IsEnabled).ToArray();
        bool updating = false;
        void Refresh()
        {
            int count = available.Count(choice => choice.Check.IsChecked == true);
            bool overLimit = presets.Count + count > ProfileLimit;
            selectAll.IsEnabled = available.Length > 0;
            selectAll.IsChecked = count == 0 ? false : count == available.Length ? true : null;
            limit.Visibility = overLimit ? Visibility.Visible : Visibility.Collapsed;
            add.Content = Get(welcome ? "StarterContinue" : "StarterAdd");
            add.IsEnabled = count > 0 && !overLimit;
        }

        foreach (var choice in available)
        {
            choice.Check.Checked += (_, _) => { if (!updating) Refresh(); };
            choice.Check.Unchecked += (_, _) => { if (!updating) Refresh(); };
        }
        selectAll.Click += (_, _) =>
        {
            updating = true;
            foreach (var choice in available) choice.Check.IsChecked = selectAll.IsChecked == true;
            updating = false;
            Refresh();
        };
        skip.Click += async (_, _) =>
        {
            if (welcome) await CommitStarterProfilesAsync([], true);
            else if (entry == StarterPickerEntry.Options) ShowOptionsOverview();
            else CloseOptions();
        };
        add.Click += async (_, _) => await CommitStarterProfilesAsync(
            available.Where(choice => choice.Check.IsChecked == true).Select(choice => choice.Profile).ToList(), welcome);
        Refresh();
        return layout;
    }

    private static bool MatchesStarter(Preset existing, Preset starter) =>
        existing.Name == starter.Name &&
        (starter.Key is '1' or '2' || existing.Notes == starter.Notes) &&
        string.IsNullOrEmpty(existing.Group) &&
        existing.WidthPercent == starter.WidthPercent && existing.HeightPercent == starter.HeightPercent &&
        existing.Placement == starter.Placement && !existing.PrimaryMonitor && !existing.KeepSize && !existing.AllowOutside &&
        (existing.Key == 0 || (existing.Key == starter.Key && existing.Modifiers == starter.Modifiers));

    private async Task CommitStarterProfilesAsync(List<Preset> chosen, bool welcome)
    {
        if (busy) return;
        busy = true;
        int generation = closeGeneration;
        try
        {
            if (!await ConfirmProfileChangeAsync() || generation != closeGeneration || requestingClose) return;
            if (presets.Count + chosen.Count > ProfileLimit) throw new InvalidDataException(Get("ImportLimit"));
            var (affected, errors) = AddImportedProfiles(chosen);
            if (welcome)
            {
                if (affected.Count > 0) ShowWelcomeShortcutWarnings(affected, errors);
                else FinishWelcome();
                if (affected.Count == 0 && errors.Count > 0)
                    Notify(string.Join("\n", errors), true, persistent: true);
                return;
            }

            DismissOptions();
            if (affected.Count > 0)
                await ShowImportShortcutWarningsAsync(affected, errors, starterProfiles: true);
            else
                NotifyImportResult(Get(chosen.Count == 1 ? "StarterImportedOne" : "StarterImported", chosen.Count), errors);
        }
        catch (Exception ex) { Notify(Get("SaveError", ex.Message), true); }
        finally { busy = false; }
    }

    private void ShowWelcomeShortcutWarnings(
        List<(Preset Profile, ShortcutIssue Issue)> affected, List<string> registrationErrors)
    {
        WelcomeHint.Visibility = Visibility.Collapsed;
        var title = new TextBlock
        {
            Text = Get("StarterAddedWithWarnings"),
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        AutomationProperties.SetHeadingLevel(title, Microsoft.UI.Xaml.Automation.Peers.AutomationHeadingLevel.Level2);
        var warnings = CreateShortcutWarnings(affected, registrationErrors, starterProfiles: true);
        var next = new Button
        {
            Content = Get("Continue"),
            Style = AddButton.Style,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        next.Click += (_, _) => FinishWelcome();

        var layout = new Grid { RowSpacing = 12 };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition());
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(warnings, 1);
        Grid.SetRow(next, 2);
        layout.Children.Add(title);
        layout.Children.Add(warnings);
        layout.Children.Add(next);
        WelcomePicker.Content = layout;
        next.Focus(FocusState.Programmatic);
    }

    private void FinishWelcome()
    {
        WelcomeSurface.Visibility = Visibility.Collapsed;
        WelcomePicker.Content = null;
        MainSurface.Visibility = Visibility.Visible;
        (presets.Count > 0 ? (Control)PresetList : EmptyAddProfileButton).Focus(FocusState.Programmatic);
    }
}
