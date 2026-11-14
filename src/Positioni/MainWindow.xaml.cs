using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using static Positioni.Core.UiText;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using Microsoft.Win32;
using Windows.Storage.Pickers;
using Windows.System;
using Positioni.Core;

namespace Positioni;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<Preset> presets = [];
    private readonly string dataPath;
    private readonly nint hwnd;
    private readonly DesktopHost host;
    private Preset? selected;
    private bool IsNewProfile => selected is not null && !presets.Contains(selected);
    private uint editModifiers, editKey;
    private (uint Modifiers, uint Key)? removedShortcut;
    private bool loading, recording, pausedBeforeRecording, busy;
    private bool paused, profileDialogOpen;
    private ContentDialog? activeDialog;
    private Task<ContentDialogResult>? activeDialogResult;
    private Task<bool>? unsavedProfileDecision;

    private bool OptionsOpen => OptionsOverlay.Visibility == Visibility.Visible;

    private bool applyingLanguage;
    private string[] placements = [];
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "WindowPositioner";
    private string? otherStartupCopy, startupSettingError;
    private static readonly string[] ImportSources = ["Positioni (.json)", "Sizer (.xml)", "WindowSizer (.json)", "FancyZones (.json)", "DisplayFusion (.json, .reg)", "AutoHotkey v2 (.ahk)"];
    private static readonly string[] LanguageNames = ["English", "Español", "Português", "Italiano", "Français", "Deutsch"];

    public MainWindow()
    {
        InitializeComponent();
        TrackProfileEdits();
        TrackScrollShadows(EditorScroll, EditorTopShadow, EditorBottomShadow);
        TrackScrollShadows(OptionsScroll, OptionsTopShadow, OptionsBottomShadow);
        PresetList.Loaded += PresetList_Loaded;

        hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        InitializeAlignmentEditor();
        AppWindow.SetIcon(Native.AppIconPath);
        RestoreWindowSize();
        statusTimer.Tick += (_, _) => DismissToast();
        LanguageInput.ItemsSource = LanguageNames;
        ImportSource.ItemsSource = ImportSources;
        ImportSource.SelectedIndex = 0;
        PresetList.ItemsSource = presets;
        ApplyLanguage();
        dataPath = Path.Combine(LanguagePreference.DataDirectory, "presets.json");
        string? loadError = null;
        try
        {
            if (File.Exists(dataPath)) foreach (var p in PresetFiles.Read(dataPath).Presets) presets.Add(p);

        }
        catch (Exception e)
        {
            loadError = Get("LoadError", e is System.Text.Json.JsonException ? Get("InvalidFormat") : e.Message);
        }
        host = new DesktopHost(hwnd);
        host.Pressed = p => { if(!busy) ApplyToActive(p); };
        host.Open = ShowEditor;
        Root.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(RecordShortcutKey), true);
        host.Toggle = TogglePause;
        host.ResetSize = () => DispatcherQueue.TryEnqueue(ResetWindowSize);
        host.Resized = RememberWindowSize;
        host.MonitorLayoutChanged = () => DispatcherQueue.TryEnqueue(UpdateMonitorPreviews);
        host.Quit = () => { host.Quitting = true; Close(); };
        Closed += (_, _) => { ClearStatus(); host.Dispose(); };
        LoadStartupSetting();
        InitializeCloseBehavior();
        InitializeTheme();
        InitializeUiScale();
        InitializeSpellCheck();
        InitializeUpdates();
        InitializeLogoMotion();
        RefreshCount();
        PresetList.SelectedIndex = presets.Count>0 ? 0 : -1;
        Editor.IsEnabled = selected != null;
        var errors=RefreshHotkeys();
        if (loadError != null) Notify(loadError, true);
        else if (errors.Count > 0) Notify(string.Join("\n", errors), true);
        if(!host.TrayAvailable) Notify(Get("TrayError"),true);
        if (loadError is null && !File.Exists(dataPath)) OpenStarterProfiles(StarterPickerEntry.Welcome);
    }


    private void Options_Click(object sender, RoutedEventArgs e)
    {
        if (busy || OptionsOpen) return;
        LoadStartupSetting();
        ShowOptionsOverlay();
    }

    private void ShowOptionsOverlay()
    {
        OptionsOverlay.Visibility = Visibility.Visible;
        SyncUiSizeSelection();
        MainSurface.IsEnabled = false;
        CloseOptionsButton.Focus(FocusState.Programmatic);
        UpdateStudioGlow();
    }

    private void CloseOptions_Click(object sender, RoutedEventArgs e) => CloseOptions();

    private void CloseOptionsAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = CloseOptions();
    }

    private bool CloseOptions()
    {
        if (!OptionsOpen || busy || applyingUpdate) return false;
        DismissOptions();
        return true;
    }

    private void DismissOptions()
    {
        if (!OptionsOpen) return;
        bool fromEmptyState = OptionsStarterPicker.Visibility == Visibility.Visible &&
            StarterBackButton.Visibility == Visibility.Collapsed;
        if (OptionsStarterPicker.Visibility == Visibility.Visible) ShowOptionsOverview();
        if (toastInOptions) DismissToast();
        OptionsOverlay.Visibility = Visibility.Collapsed;
        studioLogoClicks = 0;
        DeveloperModePanel.Visibility = Visibility.Collapsed;
        UpdateStudioGlow();
        MainSurface.IsEnabled = true;
        (fromEmptyState && presets.Count == 0 ? EmptyChooseStarterProfilesButton : OptionsButton)
            .Focus(FocusState.Programmatic);
    }

    private static void FitComboBoxToItems(ComboBox combo, IEnumerable<string> items)
    {
        var text = new TextBlock
        {
            FontFamily = combo.FontFamily,
            FontSize = combo.FontSize,
            FontWeight = combo.FontWeight,
            FontStretch = combo.FontStretch,
            FontStyle = combo.FontStyle,
            Language = combo.Language
        };
        double widest = 0;
        foreach (string item in items)
        {
            text.Text = item;
            text.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            widest = Math.Max(widest, text.DesiredSize.Width);
        }

        const double chevronAndEdgeWidth = 34;
        combo.Width = Math.Ceiling(widest + combo.Padding.Left + combo.Padding.Right + chevronAndEdgeWidth);
    }

    private void TransferLayout_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ImportCard is null || ExportCard is null) return;
        bool stacked = e.NewSize.Width < 530;
        var columns = ((Grid)sender).ColumnDefinitions;
        columns[0].Width = new GridLength(1, stacked ? GridUnitType.Star : GridUnitType.Auto);
        columns[1].Width = stacked ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetColumnSpan(ImportCard, stacked ? 2 : 1);
        Grid.SetColumn(ExportCard, stacked ? 0 : 1);
        Grid.SetColumnSpan(ExportCard, stacked ? 2 : 1);
        Grid.SetRow(ExportCard, stacked ? 1 : 0);
        ExportCard.Margin = stacked ? new Thickness(0, 12, 0, 0) : new Thickness(0);
    }



    private void Language_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (applyingLanguage || LanguageInput.SelectedIndex < 0) return;
        string language = LanguageCodes[LanguageInput.SelectedIndex];
        if (language == Language) return;

        try
        {
            LanguagePreference.Save(language);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            applyingLanguage = true;
            LanguageInput.SelectedIndex = LanguageCodes.ToList().IndexOf(Language);
            applyingLanguage = false;
            Notify(Get("LanguageError", ex.Message), true);
            return;
        }

        Language = language;
        ClearStatus();
        ApplyLanguage();
        host.UpdateTrayText();
    }

    private void ApplyLanguage()
    {
        applyingLanguage = loading = true;
        Root.Language = Language;
        LanguageInput.SelectedIndex = LanguageCodes.ToList().IndexOf(Language);
        LanguageLabel.Text = Get("Language");
        AutomationProperties.SetName(LanguageInput, LanguageLabel.Text);
        ThemeTitle.Text = Get("Theme");
        SystemThemeRadio.Content = Get("ThemeSystem");
        LightThemeRadio.Content = Get("ThemeLight");
        DarkThemeRadio.Content = Get("ThemeDark");
        UiScaleTitle.Text = Get("UiScale");
        SmallUiRadio.Content = Get("UiScaleSmall");
        DefaultUiRadio.Content = Get("UiScaleDefault");
        LargeUiRadio.Content = Get("UiScaleLarge");
        CloseBehaviorTitle.Text = Get("OnClose");
        CloseBehaviorHint.Text = Get("OnCloseHint");
        CloseToTrayLabel.Text = Get("CloseToTray");
        ExitOnCloseLabel.Text = Get("CloseApplication");
        AutomationProperties.SetName(CloseToTrayRadio, CloseToTrayLabel.Text);
        AutomationProperties.SetName(ExitOnCloseRadio, ExitOnCloseLabel.Text);
        ImportSourceLabel.Text = Get("ImportSource");
        AutomationProperties.SetName(ImportSource, ImportSourceLabel.Text);
        FitComboBoxToItems(ImportSource, ImportSources);
        FitComboBoxToItems(LanguageInput, LanguageNames);
        DescriptionLabel.Text = Get("Description");
        UpdatePauseButton();
        AddButton.Content = EmptyAddProfileText.Text = Get("Add");
        AutomationProperties.SetName(EmptyAddProfileButton, EmptyAddProfileText.Text);
        EmptyChooseStarterProfilesText.Text = Get("StarterChoose");
        AutomationProperties.SetName(EmptyChooseStarterProfilesButton, EmptyChooseStarterProfilesText.Text);
        EmptyProfilesTitle.Text = Get("EmptyProfilesTitle");
        EmptyProfilesHint.Text = Get("EmptyProfilesHint");
        OptionsButton.Content = OptionsTitle.Text = Get("Options");
        WelcomeTitle.Text = Get("StarterWelcome");
        WelcomeDescription.Text = Get("StarterWelcomeDescription");
        WelcomeHint.Text = Get("StarterWelcomeHint");
        StarterProfilesTitle.Text = Get("StarterProfiles");
        StarterProfilesHint.Text = Get("StarterProfilesHint");
        ChooseStarterProfilesButton.Content = Get("StarterChoose");
        StarterBackText.Text = Get("StarterBack");
        var closeOptions = Get("CloseOptions");
        AutomationProperties.SetName(CloseOptionsButton, closeOptions);
        ToolTipService.SetToolTip(CloseOptionsButton, closeOptions);
        SystemStartupLabel.Text = Get("SystemStartup");
        RefreshUpdateUi();
        Startup.Content = Get("Startup");
        UseStartupCopyButton.Content = Get("StartupUseThisCopy");
        UpdateStartupConflictText();
        BackupsLabel.Text = Get("Backups");
        BackupHintLabel.Text = Get("BackupHint");
        DeleteProfilesLabel.Text = Get("DeleteProfiles");
        DeleteProfilesHint.Text = Get("DeleteProfilesHint");
        DeleteAllProfilesButton.Content = Get("DeleteAllProfiles");
        ImportButton.Content = Get("Import");
        ExportButton.Content = ExportLabel.Text = Get("Export");
        OtherSettingsTitle.Text = Get("OtherSettings");
        DisableSpellCheck.Content = Get("DisableSpellCheck");
        SpellCheckHint.Text = Get("SpellCheckHint");
        AutomationProperties.SetHelpText(DisableSpellCheck, SpellCheckHint.Text);
        AboutLabel.Text = Get("About");
        VersionLabel.Text = Get("Version", typeof(MainWindow).Assembly.GetName().Version!.ToString(3));
        CreatedByLabel.Text = Get("CreatedBy");
        EasterEggTitle.Text = Get("EasterEggTitle");
        EasterEggDescription.Text = Get("EasterEggDescription");
        RunOnboardingButton.Content = Get("RunOnboarding");
        RepositoryLinkLabel.Text = Get("Repository");
        AutomationProperties.SetName(RepositoryLink, RepositoryLinkLabel.Text);
        DuplicateButton.Content = Get("Duplicate");
        AutomationProperties.SetName(UpButton, Get("MoveUp"));
        AutomationProperties.SetName(DownButton, Get("MoveDown"));
        DeleteEditorButton.Content = DeleteButton.Content = Get("Delete");
        ResetProfileButton.Content = Get("ResetProfile");
        EditLabel.Text = Get("Edit");
        ProfileNameTitle.Text = Get("Name");
        ProfileNotesTitle.Text = Get("Notes");
        AutomationProperties.SetName(PresetName, ProfileNameTitle.Text);
        AutomationProperties.SetName(PresetNotes, ProfileNotesTitle.Text);
        WidthLabel.Text = Get("Width");
        HeightLabel.Text = Get("Height");
        AutomationProperties.SetName(WidthInput, WidthLabel.Text);
        AutomationProperties.SetName(HeightInput, HeightLabel.Text);
        AutomationProperties.SetName(WidthUnit, Get("WidthUnit"));
        AutomationProperties.SetName(HeightUnit, Get("HeightUnit"));
        PrimaryInput.Content = Get("Primary");
        KeepSizeInput.Content = Get("KeepSize");
        OutsideInput.Content = Get("Outside");
        ShortcutLabel.Text = Get("Shortcut");
        ShortcutEmptyLabel.Text = Get("ShortcutUnset");
        AutomationProperties.SetName(ShortcutEmptyLabel, Get("NoShortcut"));
        ClearShortcutButton.Content = Get("ClearShortcut");
        UndoShortcutButton.Content = Get("UndoShortcut");
        UpdateShortcutControls();
        EnabledInput.Content = Get("Enabled");

        CaptureButton.Content = Get("Capture");
        MorePresetOptionsLabel.Text = Get("MorePresetOptions");
        AutomationProperties.SetName(MorePresetOptionsButton, MorePresetOptionsLabel.Text);
        PrimaryHintLabel.Text = Get("PrimaryPresetHint");
        KeepSizeHintLabel.Text = Get("KeepSizePresetHint");
        OutsideHintLabel.Text = Get("OutsidePresetHint");
        CaptureHintLabel.Text = Get("CapturePresetHint");


        ApplyPositionLabels();
        foreach (var profile in presets)
            if (string.IsNullOrWhiteSpace(profile.Name))
                profile.RefreshDisplayName();

        foreach (var presenter in profileListShortcuts)
            if (presenter.DataContext is Preset profile)
                presenter.Content = CreateCompactShortcut(profile);

        foreach (var thumbnail in profileThumbnails)
            UpdateProfileThumbnail(thumbnail);
        RefreshCount();
        applyingLanguage = loading = false;
        UpdateProfileEditorState();
    }


    private void UpdatePauseButton()
    {
        PauseIcon.Glyph = paused ? "\uE768" : "\uE769";
        PauseLabel.Text = Get(paused ? "Resume" : "Pause");
        AutomationProperties.SetName(PauseButton, PauseLabel.Text);
    }

    internal void ShowEditor()
    {

        Native.ShowWindow(hwnd, 9);
        Activate();
        Native.SetForegroundWindow(hwnd);
    }
    private void RefreshCount()
    {
        PresetsHeading.Text = Get("Presets");
        CountLabel.Text = presets.Count.ToString();
        bool hasProfiles = presets.Count > 0;
        DeleteAllProfilesButton.IsEnabled = hasProfiles;
        int selectedIndex = selected is null ? -1 : presets.IndexOf(selected);
        DuplicateButton.IsEnabled = DeleteButton.IsEnabled = selectedIndex >= 0;
        UpButton.IsEnabled = selectedIndex > 0;
        DownButton.IsEnabled = selectedIndex >= 0 && selectedIndex < presets.Count - 1;
        bool hasEditor = hasProfiles || IsNewProfile;
        Editor.Visibility = hasEditor ? Visibility.Visible : Visibility.Collapsed;
        EmptyProfilesPanel.Visibility = hasEditor ? Visibility.Collapsed : Visibility.Visible;
    }
    private List<string> RefreshHotkeys() => host.Register(presets,paused);
    private void Persist(List<Preset>? snapshot = null)
    {
        PresetFiles.Save(dataPath, new Settings { Presets = snapshot ?? presets.ToList() });
        RefreshCount();
    }
    private async void Selection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (loading) return;
        var requested = PresetList.SelectedItem as Preset;
        if (requested == selected) return;

        if (HasProfileChanges() || profileDialogOpen)
        {
            SetListSelection(IsNewProfile ? null : selected);
            if (!await ConfirmProfileChangeAsync()) return;
        }

        SelectProfile(requested);
    }

    private void SetListSelection(Preset? profile)
    {
        bool wasLoading = loading;
        loading = true;
        PresetList.SelectedItem = profile;
        loading = wasLoading;
    }

    private void SelectProfile(Preset? profile)
    {
        bool changed = selected?.Id != profile?.Id;
        SetListSelection(profile is not null && presets.Contains(profile) ? profile : null);
        LoadProfile(profile);
        if (changed && profile is not null)
            EditorScroll.ChangeView(null, 0, null, disableAnimation: true);
    }

    private void LoadProfile(Preset? profile)
    {
        loading = true;
        StopRecording();
        removedShortcut = null;
        selected = profile;
        foreach (var marker in profileSelectionMarkers) UpdateProfileSelectionMarker(marker);
        RefreshCount();
        Editor.IsEnabled = profile != null;
        if (profile != null)
        {
            PresetName.Text = profile.Name;
            PresetNotes.Text = profile.Notes;
            SetDimension(WidthInput, WidthUnit, profile.Width == 0 ? double.NaN : profile.WidthPercent ?? profile.Width,
                profile.WidthPercent.HasValue);
            SetDimension(HeightInput, HeightUnit, profile.Height == 0 ? double.NaN : profile.HeightPercent ?? profile.Height,
                profile.HeightPercent.HasValue);
            XInput.Value = profile.X == Preset.UnsetCoordinate ? double.NaN : profile.X;
            YInput.Value = profile.Y == Preset.UnsetCoordinate ? double.NaN : profile.Y;
            lastAnchor = profile.Placement == UnsetPlacement ? UnsetPlacement : CenterAnchor;
            SetEditorPlacement(profile.Placement);
            SetPositionOptionsExpanded(profile.Placement == ExactCoordinates);
            PrimaryInput.IsChecked = profile.PrimaryMonitor;
            KeepSizeInput.IsChecked = profile.KeepSize;
            OutsideInput.IsChecked = profile.AllowOutside;
            EnabledInput.IsChecked = profile.Enabled;
            editModifiers = profile.Modifiers;
            editKey = profile.Key;
            UpdateShortcutControls();
        }
        loading = false;
        UpdateProfileEditorState();
    }

    private async Task<bool> ConfirmProfileChangeAsync()
    {
        var decision = unsavedProfileDecision ??= ResolveProfileChangesAsync();
        try { return await decision; }
        finally { if (unsavedProfileDecision == decision) unsavedProfileDecision = null; }
    }

    private async Task<bool> ResolveProfileChangesAsync()
    {
        if (profileDialogOpen) return false;
        if (!HasProfileChanges())
        {
            if (IsNewProfile) SelectProfile(presets.FirstOrDefault());
            return true;
        }
        profileDialogOpen = true;
        try
        {
            bool draft = IsNewProfile;
            var result = await ShowDestructiveDialogAsync(Get("UnsavedProfileTitle"),
                draft ? Get("UnsavedNewProfileQuestion") : Get("UnsavedProfileQuestion", selected!.DisplayName),
                Get("DiscardChanges"), draft ? Get("SaveNewProfile") : Get("SaveChanges"), Get("KeepEditing"));

            if (result == ContentDialogResult.Secondary) return TrySaveProfile();
            if (result != ContentDialogResult.Primary) return false;
            if (draft) SelectProfile(presets.FirstOrDefault());
            return true;
        }
        catch (Exception ex)
        {
            Notify(ex.Message, true);
            return false;
        }
        finally { profileDialogOpen = false; }
    }

    private Preset ReadEditor()
    {
        if (selected == null) throw new InvalidOperationException(Get("SelectPreset"));

        int Number(NumberBox input, int empty)
        {
            double value = DraftNumber(input);
            if (double.IsNaN(value) && string.IsNullOrWhiteSpace(input.Text)) return empty;
            return double.IsFinite(value) ? checked((int)Math.Round(value)) : throw new InvalidDataException(Get("FillNumbers"));
        }
        var p=selected.Copy(); p.Name=PresetName.Text.Trim(); p.Notes=PresetNotes.Text.Trim();
        int width = Number(WidthInput, 0), height = Number(HeightInput, 0);
        p.WidthPercent = WidthUnit.SelectedIndex == PercentUnitIndex ? width : null;
        p.HeightPercent = HeightUnit.SelectedIndex == PercentUnitIndex ? height : null;
        p.Width = width;
        p.Height = height;
        if (p.WidthPercent > 0 || p.HeightPercent > 0)
        {
            var work = Native.WorkArea(hwnd, PrimaryInput.IsChecked == true);
            // Keep pixel equivalents for older versions reading the same profile file.
            if (p.WidthPercent > 0)
                p.Width = Math.Clamp(Geometry.PercentToPixels(width, work.Width), 100, 32768);
            if (p.HeightPercent > 0)
                p.Height = Math.Clamp(Geometry.PercentToPixels(height, work.Height), 80, 32768);
        }
        p.X=Number(XInput, Preset.UnsetCoordinate); p.Y=Number(YInput, Preset.UnsetCoordinate); p.Placement=editPlacement; p.PrimaryMonitor=PrimaryInput.IsChecked==true;
        p.KeepSize=KeepSizeInput.IsChecked==true; p.AllowOutside=OutsideInput.IsChecked==true; p.Enabled=EnabledInput.IsChecked==true;
        p.Modifiers=editModifiers; p.Key=editKey;
        if(p.Validate() is string error) throw new InvalidDataException(error);
        return p;
    }

    private void Save_Click(object sender, RoutedEventArgs e) => TrySaveProfile();

    private bool TrySaveProfile()
    {
        try
        {
            var p = ReadEditor();
            var previous = selected!;
            string? shortcutWarning = null;
            if (p.CanRegisterShortcut && (!previous.CanRegisterShortcut ||
                p.Key != previous.Key || p.Modifiers != previous.Modifiers))
            {
                var conflict = ShortcutConflict(p.Modifiers, p.Key, p.Id, presets);
                if (conflict is not null)
                {
                    if (IsNewProfile || previous.IsComplete)
                    {
                        Notify(conflict.Message(Hotkey.Format(p.Modifiers, p.Key)), true);
                        return false;
                    }

                    p.Enabled = false;
                    shortcutWarning = conflict.Message(p.Shortcut);
                }
            }
            int index = presets.IndexOf(selected!);
            var snapshot = presets.ToList();
            if (IsNewProfile && presets.Count >= ProfileLimit) throw new InvalidDataException(Get("Limit"));
            if (index < 0) snapshot.Add(p);
            else snapshot[index] = p;
            Persist(snapshot);
            loading = true;
            if (index < 0)
            {
                animateInactiveProfile = null;
                presets.Add(p);
                SelectProfile(p);
            }
            else
            {
                // Keep the existing list item so saving does not replay its entry transition.
                animateInactiveProfile = previous.Enabled && !p.Enabled ? p.Id : null;
                bool shortcutChanged = previous.Key != p.Key || previous.Modifiers != p.Modifiers;
                previous.ApplyEdits(p);
                if (shortcutChanged)
                {
                    foreach (var presenter in profileListShortcuts)
                        if (ReferenceEquals(presenter.DataContext, previous))
                            presenter.Content = CreateCompactShortcut(previous);
                }
                foreach (var thumbnail in profileThumbnails)
                    if (ReferenceEquals(thumbnail.DataContext, previous))
                        UpdateProfileThumbnail(thumbnail, force: true);
                LoadProfile(previous);
            }
            var errors = RefreshHotkeys();
            if (shortcutWarning is not null) errors.Insert(0, shortcutWarning);
            Notify(errors.Count > 0 ? Get("SavedWarnings") + "\n" + string.Join("\n", errors) : Get("Saved"), errors.Count > 0);
            return true;
        }
        catch (Exception ex)
        {
            Notify(ex.Message, true);
            return false;
        }
    }

    private void ResetProfile_Click(object sender, RoutedEventArgs e)
    {
        DeleteEditorButton.Focus(FocusState.Programmatic);
        LoadProfile(selected);
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        if (!await ConfirmProfileChangeAsync()) return;
        if (presets.Count >= ProfileLimit) { Notify(Get("Limit"), true); return; }
        SelectProfile(new Preset { Width = 0, Height = 0, WidthPercent = 0, HeightPercent = 0, Placement = UnsetPlacement });
    }

    private async void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        if (selected == null || IsNewProfile || presets.Count >= ProfileLimit || !await ConfirmProfileChangeAsync()) return;
        var p = selected.Copy();
        p.Id = Guid.NewGuid();
        if (!string.IsNullOrWhiteSpace(p.Name)) p.Name += $" ({Get("Copy")})";
        p.Modifiers = 0;
        p.Key = 0;
        InsertProfile(p, presets.IndexOf(selected) + 1);
    }

    private void InsertProfile(Preset profile, int index)
    {
        try
        {
            var snapshot = presets.ToList();
            snapshot.Insert(index, profile);
            Persist(snapshot);
            presets.Insert(index, profile);
            RefreshCount();
            SelectProfile(profile);
        }
        catch (Exception ex) { Notify(Get("SaveError", ex.Message), true); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (selected == null || profileDialogOpen || busy) return;
        var victim = selected;
        profileDialogOpen = true;
        try
        {
            bool draft = IsNewProfile;
            if ((!draft || HasProfileChanges()) &&
                await ShowDestructiveDialogAsync(Get("DeleteTitle"),
                    draft ? Get("DeleteNewProfileQuestion") : Get("DeleteQuestion", victim.DisplayName),
                    Get("Delete")) != ContentDialogResult.Primary) return;
            if (draft)
            {
                SelectProfile(presets.FirstOrDefault());
                return;
            }
            int index = presets.IndexOf(victim);
            var snapshot = presets.ToList();
            snapshot.RemoveAt(index);
            Persist(snapshot);
            loading = true;
            presets.RemoveAt(index);
            RefreshCount();
            SelectProfile(presets.Count == 0 ? null : presets[Math.Min(index, presets.Count - 1)]);
            RefreshHotkeys();
        }
        catch (Exception ex) { Notify(Get("SaveError", ex.Message), true); }
        finally { profileDialogOpen = false; }
    }

    private void PresetList_Loaded(object sender, RoutedEventArgs e)
    {
        if (FindListScrollViewer(PresetList) is not { } scroll) return;
        PresetList.Loaded -= PresetList_Loaded;
        TrackScrollShadows(scroll, PresetListTopShadow, PresetListBottomShadow);
    }

    private static ScrollViewer? FindListScrollViewer(DependencyObject element)
    {
        for (int index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element); index++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, index);
            if (child is ScrollViewer scroll) return scroll;
            if (FindListScrollViewer(child) is { } nested) return nested;
        }

        return null;
    }

    private void Up_Click(object sender, RoutedEventArgs e) => Move(-1);
    private void Down_Click(object sender, RoutedEventArgs e) => Move(1);
    private void Move(int delta)
    {
        if (selected == null || IsNewProfile) return;
        int index = presets.IndexOf(selected), destination = index + delta;
        if (index < 0 || destination < 0 || destination >= presets.Count) return;
        var moved = selected;
        try
        {
            var snapshot = presets.ToList();
            snapshot.RemoveAt(index);
            snapshot.Insert(destination, moved);
            Persist(snapshot);

            loading = true;
            try
            {
                presets.Move(index, destination);
                SetListSelection(moved);
            }
            finally { loading = false; }

            RefreshCount();
            // Let the list lay out the reordered rows before requesting any scroll.
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (PresetList.SelectedItem != moved || presets.IndexOf(moved) != destination) return;
                PresetList.ScrollIntoView(moved, destination == 0
                    ? ScrollIntoViewAlignment.Leading
                    : ScrollIntoViewAlignment.Default);
            });
        }
        catch (Exception ex) { Notify(Get("SaveError", ex.Message), true); }
    }
    private void Pause_Click(object sender,RoutedEventArgs e) => TogglePause();
    private void TogglePause()
    {
        paused=!paused; var errors=RefreshHotkeys(); UpdatePauseButton();
        if (errors.Count > 0) Notify(string.Join("\n", errors), true);
    }
    private void Record_Click(object sender,RoutedEventArgs e)
    {
        if(recording) { StopRecording(); return; }
        recording=true; pausedBeforeRecording=paused; host.Register(presets,true);
        UpdateShortcutControls();
        RecordButton.Focus(FocusState.Programmatic);
        host.BeginRecording((key, modifiers) =>
            DispatcherQueue.TryEnqueue(() => TryRecordShortcut(key, modifiers)));
    }
    private void StopRecording()
    {
        RecordConflictAlert.IsOpen = false;
        if(!recording) return; recording=false; host.EndRecording(); host.Register(presets,pausedBeforeRecording);
        UpdateShortcutControls();
    }
    private void Record_LostFocus(object sender,RoutedEventArgs e) => StopRecording();
    private void RecordShortcutKey(object sender, KeyRoutedEventArgs e)
    {
        if (!recording) return;
        e.Handled = true;
        TryRecordShortcut((uint)e.Key, Native.Modifiers());
    }

    private void TryRecordShortcut(uint key, uint mods)
    {
        if (!recording) return;
        if (key == (uint)VirtualKey.Escape) { StopRecording(); return; }
        if (!Hotkey.IsRecordableKey(key) || mods == 0) return;
        var conflict = ShortcutConflict(mods, key, selected?.Id, presets);
        if (conflict is not null)
        {
            RecordConflictAlert.Message = conflict.Message(Hotkey.Format(mods, key)) + " " + Get("ShortcutTryAnother");
            RecordConflictAlert.IsOpen = true;
            return;
        }
        editKey = key;
        editModifiers = mods;
        removedShortcut = null;
        StopRecording();
    }
    private sealed record ShortcutIssue(string? OtherProfile, bool AlreadyInUse)
    {
        public string Message(string shortcut) => OtherProfile is not null
            ? Get("ShortcutUsedByProfile", shortcut, OtherProfile)
            : Get(AlreadyInUse ? "ShortcutAlreadyInUse" : "ShortcutRegistrationUnavailable", shortcut);

        public string ImportReason => OtherProfile is not null
            ? Get("ImportShortcutUsedByProfile", OtherProfile)
            : Get(AlreadyInUse ? "ImportShortcutAlreadyInUse" : "ImportShortcutUnavailable");
    }

    private ShortcutIssue? ShortcutConflict(uint modifiers, uint key, Guid? excludedId, IEnumerable<Preset> profiles)
    {
        var other = profiles.FirstOrDefault(p => p.Id != excludedId && p.CanRegisterShortcut &&
            p.Modifiers == modifiers && p.Key == key);
        if (other is not null) return new(other.DisplayName, true);

        const int hotkeyAlreadyRegistered = 1409;
        int error = host.CheckShortcut(modifiers, key);
        return error == 0 ? null : new(null, error == hotkeyAlreadyRegistered);
    }


    private void ClearShortcut_Click(object sender, RoutedEventArgs e)
    {
        if (editKey == 0) return;
        StopRecording();
        removedShortcut = (editModifiers, editKey);
        editKey = editModifiers = 0;
        UpdateShortcutControls();
        UndoShortcutButton.Focus(FocusState.Programmatic);
    }

    private void UndoShortcut_Click(object sender, RoutedEventArgs e)
    {
        if (removedShortcut is not { } shortcut) return;
        (editModifiers, editKey) = shortcut;
        removedShortcut = null;
        UpdateShortcutControls();
        RecordButton.Focus(FocusState.Programmatic);
    }

    private void UpdateShortcutControls()
    {
        bool hasShortcut = editKey != 0;
        string shiftLabel = Get("Shift");
        var labels = Hotkey.GetKeys(editModifiers, editKey);
        ShortcutKeys.ItemsSource = labels.Select((label, index) => index == labels.Count - 1
            ? new ShortcutKeycap(label, false, false, editKey)
            : new ShortcutKeycap(label, label == "Win", label == shiftLabel)).ToArray();
        ShortcutKeys.Visibility = hasShortcut ? Visibility.Visible : Visibility.Collapsed;
        ShortcutEmptyState.Visibility = hasShortcut ? Visibility.Collapsed : Visibility.Visible;

        string label = recording ? Get("PressCombination") :
            hasShortcut ? Get("ReassignShortcut") : Get("AssignShortcut");
        RecordButton.Content = label;
        AutomationProperties.SetName(RecordButton, label);
        ClearShortcutButton.Visibility = !recording && hasShortcut ? Visibility.Visible : Visibility.Collapsed;
        UndoShortcutButton.Visibility = !recording && !hasShortcut && removedShortcut.HasValue
            ? Visibility.Visible : Visibility.Collapsed;
        RecordHelpLabel.Text = recording ? Get("RecordHelp") : "";
        RecordHelpLabel.Visibility = recording ? Visibility.Visible : Visibility.Collapsed;
        UpdateProfileEditorState();
    }
    private async void Capture_Click(object sender,RoutedEventArgs e)
    {
        if(busy||selected==null) return; busy=true;
        var profile = selected;
        int generation = closeGeneration;
        try
        {
            Native.ShowWindow(hwnd,6); await Task.Delay(4000);
            if (generation != closeGeneration || selected != profile) return;
            var target=Native.GetForegroundWindow(); if(target==0||target==hwnd) throw new InvalidOperationException(Get("SelectCapture"));
            var r = Native.Bounds(target);
            loading = true;
            XInput.Value = r.X;
            YInput.Value = r.Y;
            SetDimension(WidthInput, WidthUnit, r.Width, percent: false);
            SetDimension(HeightInput, HeightUnit, r.Height, percent: false);
            KeepSizeInput.IsChecked = false;
            loading = false;
            SetEditorPlacement(ExactCoordinates);
            SetPositionOptionsExpanded(true);
            UpdateProfileEditorState();
            Notify(Get("CapturedWindow"));
        }
        catch(Exception ex) { Notify(ex.Message,true); }
        finally { if (generation == closeGeneration) ShowEditor(); busy=false; }
    }
    private async void ApplyToActive(Preset p)
    {
        try
        {
            nint target=Native.GetForegroundWindow();
            if(target==0||target==hwnd) throw new InvalidOperationException(Get("SelectApply"));
            var expected=Native.Apply(target,p); await Task.Delay(150); var actual=Native.Bounds(target);
            bool limited=Math.Abs(actual.Width-expected.Width)>3||Math.Abs(actual.Height-expected.Height)>3||Math.Abs(actual.X-expected.X)>3||Math.Abs(actual.Y-expected.Y)>3;
            if (limited) Notify(Get("Limited", p.DisplayName), true);
        }
        catch(Exception ex) { Notify(ex.Message,true); ShowEditor(); }
    }
    private Task<ContentDialogResult> ShowDestructiveDialogAsync(string title, string message, string primary,
        string? secondary = null, string? close = null) =>
        ShowDialogAsync(new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = primary,
            PrimaryButtonStyle = (Style)Root.Resources["DestructiveDialogButtonStyle"],
            SecondaryButtonText = secondary ?? "",
            SecondaryButtonStyle = (Style)Root.Resources["WrapDialogButtonStyle"],
            CloseButtonText = close ?? Get("Cancel"),
            DefaultButton = ContentDialogButton.Close
        });

    private async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        dialog.XamlRoot = Root.XamlRoot;
        dialog.RequestedTheme = Root.RequestedTheme;
        var accentAction = dialog.DefaultButton;
        if (accentAction != ContentDialogButton.None)
        {
            // Keep the accent independent of WinUI's focus-based default button styling.
            var accentStyle = new Style(typeof(Button)) { BasedOn = AddButton.Style };
            switch (accentAction)
            {
                case ContentDialogButton.Primary: dialog.PrimaryButtonStyle = accentStyle; break;
                case ContentDialogButton.Secondary: dialog.SecondaryButtonStyle = accentStyle; break;
                case ContentDialogButton.Close: dialog.CloseButtonStyle = accentStyle; break;
            }
        }

        activeDialog = dialog;
        try
        {
            DialogBackdrop.Visibility = Visibility.Visible;
            if (UiViewport.ZoomFactor != 1)
            {
                DialogHost.Visibility = Visibility.Visible;
                DialogHost.Children.Add(dialog);
            }
            activeDialogResult = dialog.ShowAsync(ContentDialogPlacement.InPlace).AsTask();
            return await activeDialogResult;
        }
        finally
        {
            DialogHost.Children.Remove(dialog);
            DialogHost.Visibility = Visibility.Collapsed;
            DialogBackdrop.Visibility = Visibility.Collapsed;
            activeDialog = null;
            activeDialogResult = null;
        }
    }


    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        busy = true;
        try
        {
            if (await ExportProfilesAsync()) Notify(Get("Exported"));
        }
        catch (Exception ex) { Notify(Get("ExportError", ex.Message), true); }
        finally { busy = false; }
    }

    private async Task<bool> ExportProfilesAsync()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH'h'-mm'm'-ss's'", System.Globalization.CultureInfo.InvariantCulture);
        var picker = new FileSavePicker { SuggestedFileName = $"positioni-preset-export-{timestamp}" };
        picker.FileTypeChoices.Add(Get("ExportType"), [".json"]);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSaveFileAsync();
        if (file == null) return false;

        if (string.Equals(Path.GetFullPath(file.Path), Path.GetFullPath(dataPath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(Get("ExportChooseAnotherFile"));

        PresetFiles.Save(file.Path, new Settings { Presets = presets.ToList() });
        return true;
    }

    private async void DeleteAllProfiles_Click(object sender, RoutedEventArgs e)
    {
        if (busy || profileDialogOpen || presets.Count == 0) return;
        busy = true;
        profileDialogOpen = true;
        try
        {
            var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
            AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
            var content = new StackPanel { Spacing = 12 };
            content.Children.Add(new TextBlock { Text = Get("DeleteAllProfilesWarning"), TextWrapping = TextWrapping.Wrap });
            content.Children.Add(status);

            var dialog = new ContentDialog
            {
                XamlRoot = Root.XamlRoot,
                RequestedTheme = Root.RequestedTheme,
                Title = Get("DeleteAllProfilesTitle"),
                Content = content,
                PrimaryButtonText = Get("DeleteAllProfiles"),
                SecondaryButtonText = Get("ExportProfiles"),
                PrimaryButtonStyle = (Style)Root.Resources["DestructiveDialogButtonStyle"],
                SecondaryButtonStyle = (Style)Root.Resources["WrapDialogButtonStyle"],
                CloseButtonText = Get("Cancel"),
                DefaultButton = ContentDialogButton.Close
            };

            dialog.Resources["ContentDialogMaxWidth"] = 640d;

            dialog.SecondaryButtonClick += async (_, args) =>
            {
                args.Cancel = true;
                var deferral = args.GetDeferral();
                try
                {
                    if (!await ExportProfilesAsync()) return;
                    status.Text = Get("ProfilesExported");
                    status.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    status.Text = Get("ExportError", ex.Message);
                    status.Visibility = Visibility.Visible;
                }
                finally { deferral.Complete(); }
            };

            if (await ShowDialogAsync(dialog) != ContentDialogResult.Primary) return;

            PresetFiles.Save(dataPath, new Settings(), keepBackup: false);
            loading = true;
            presets.Clear();
            SelectProfile(null);
            RefreshHotkeys();
            DismissOptions();
            Notify(Get("AllProfilesDeleted"), persistent: true);
        }
        catch (Exception ex) { Notify(Get("SaveError", ex.Message), true); }
        finally
        {

            profileDialogOpen = false;
            busy = false;
        }
    }

    private static string StartupExecutable
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            if (directory.Name.Equals("Files", StringComparison.OrdinalIgnoreCase) && directory.Parent != null)
            {
                string launcher = Path.Combine(directory.Parent.FullName, "Positioni.exe");
                if (File.Exists(launcher)) return launcher;
            }
            return Environment.ProcessPath!;
        }
    }

    private static string StartupCommand => $"\"{StartupExecutable}\" {Program.StartupArgument}";

    private static string? GetStartupPath(string command)
    {
        command = command.Trim();
        bool quoted = command.StartsWith('"');
        int end = quoted ? command.IndexOf('"', 1) : command.IndexOfAny([' ', '\t']);
        if (quoted && end < 0) return null;
        string path = quoted ? command[1..end] : end < 0 ? command : command[..end];
        if (!Path.IsPathFullyQualified(path)) return null;

        string fileName = Path.GetFileName(path);
        return fileName.Equals("Positioni.exe", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("WindowPositioner.exe", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFullPath(path) : null;
    }

    private static bool StartupFileIsMissing(string path)
    {
        try { File.GetAttributes(path); }
        catch (IOException ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // A disconnected drive or share may contain a working copy.
            return Directory.Exists(Path.GetPathRoot(path));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return false;
    }

    private void LoadStartupSetting()
    {
        otherStartupCopy = startupSettingError = null;
        StartupConflict.Visibility = Visibility.Collapsed;
        Startup.IsChecked = false;
        Startup.IsEnabled = true;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key?.GetValue(RunValue) is not string command || string.IsNullOrWhiteSpace(command)) return;

            string? registeredPath = GetStartupPath(command);
            bool currentCopy = string.Equals(registeredPath, StartupExecutable, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(registeredPath, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
            if (currentCopy || (registeredPath != null && StartupFileIsMissing(registeredPath)))
            {
                if (!string.Equals(command, StartupCommand, StringComparison.OrdinalIgnoreCase))
                    key.SetValue(RunValue, StartupCommand);
                Startup.IsChecked = true;
                return;
            }

            otherStartupCopy = registeredPath;
            Startup.IsEnabled = false;
            UpdateStartupConflictText();
            StartupConflict.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Startup.IsEnabled = false;
            startupSettingError = ex.Message;
            UpdateStartupConflictText();
            StartupConflict.Visibility = Visibility.Visible;
        }
    }

    private void UpdateStartupConflictText()
    {
        StartupConflictText.Text = startupSettingError is { } error ? Get("StartupError", error)
            : otherStartupCopy is { } path ? Get("StartupOtherCopy", path) : Get("StartupUnknownCopy");
        UseStartupCopyButton.Visibility = startupSettingError is null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Startup_Click(object sender, RoutedEventArgs e) => SetStartup(Startup.IsChecked == true);

    private void UseStartupCopy_Click(object sender, RoutedEventArgs e) => SetStartup(true);

    private void SetStartup(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled) key.SetValue(RunValue, StartupCommand);
            else key.DeleteValue(RunValue, false);
            LoadStartupSetting();
        }
        catch (Exception ex)
        {
            LoadStartupSetting();
            Notify(Get("StartupError", ex.Message), true);
        }
    }
}
