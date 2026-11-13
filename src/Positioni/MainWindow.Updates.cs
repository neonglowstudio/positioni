using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Positioni.Core;
using static Positioni.Core.UiText;

namespace Positioni;

public sealed partial class MainWindow
{
    private enum UpdateState { Idle, Checking, Current, Available, Error, Downloading, Unsafe, DemoComplete }

    private static readonly Version CurrentVersion = new(typeof(MainWindow).Assembly.GetName().Version!.ToString(3));
    private static readonly HttpClient UpdateClient = CreateUpdateClient();
    private static readonly string UpdatePreference = Path.Combine(LanguagePreference.DataDirectory, "updates-auto.txt");
    private readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromHours(24) };
    private readonly CancellationTokenSource updateCancellation = new();
    private readonly string? updateDemo = Environment.GetCommandLineArgs()
        .FirstOrDefault(arg => arg is "--demo-update=available" or "--demo-update=current" or "--demo-update=portable");
    private bool automaticUpdates;
    private bool checkingUpdates;
    private bool applyingUpdate;
    private bool showUpdateBadge;
    private bool portableUpdate;
    private byte[]? updateSigner;
    private bool signerChecked;
    private PositioniRelease? offeredUpdate;
    private UpdateState updateState;

    private static HttpClient CreateUpdateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20), MaxResponseContentBufferSize = 2_000_000 };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Positioni/" + CurrentVersion);
        return client;
    }

    private void InitializeUpdates()
    {
        portableUpdate = updateDemo == "--demo-update=portable" ||
            (updateDemo == null && !Directory.EnumerateFiles(AppContext.BaseDirectory, "unins*.exe").Any());
        try { automaticUpdates = updateDemo != null || (File.Exists(UpdatePreference) && File.ReadAllText(UpdatePreference) == "1"); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        AutoUpdateCheck.IsChecked = automaticUpdates;
        updateTimer.Tick += async (_, _) => await CheckForUpdatesAsync(false);
        Closed += (_, _) =>
        {
            updateTimer.Stop();
            updateCancellation.Cancel();
        };
        if (automaticUpdates) updateTimer.Start();
        if (updateDemo != null || automaticUpdates) DispatcherQueue.TryEnqueue(async () => await CheckForUpdatesAsync(false));
    }

    private void RefreshUpdateUi()
    {
        UpdatesTitle.Text = Get("UpdatesTitle");
        AutoUpdateCheck.Content = Get("UpdatesAuto");
        CheckUpdateButton.Content = Get("UpdatesCheck");
        UpdateAvailableButton.Content = Get("UpdatesAvailableButton");
        UpdateNotesLabel.Text = Get(updateDemo == null ? "UpdatesNotes" : "UpdatesDemoNotes");
        AutomationProperties.SetName(UpdateNotesButton, UpdateNotesLabel.Text);
        UpdateDemoLabel.Text = Get("UpdatesDemo");
        UpdateDemoLabel.Visibility = updateDemo != null ? Visibility.Visible : Visibility.Collapsed;

        bool canInstall = !portableUpdate && (updateDemo != null || updateSigner != null);
        ApplyUpdateLabel.Text = Get(portableUpdate ? "UpdatesZip" : canInstall ? "UpdatesInstall" : "UpdatesDownload");
        AutomationProperties.SetName(ApplyUpdateButton, ApplyUpdateLabel.Text);
        ApplyUpdateExternalIcon.Visibility = canInstall ? Visibility.Collapsed : Visibility.Visible;
        ApplyUpdateButton.Visibility = offeredUpdate?.Asset != null ? Visibility.Visible : Visibility.Collapsed;
        UpdateNotesButton.Visibility = offeredUpdate != null ? Visibility.Visible : Visibility.Collapsed;
        UpdateAvailableButton.Visibility = showUpdateBadge && offeredUpdate != null ? Visibility.Visible : Visibility.Collapsed;
        CheckUpdateButton.IsEnabled = !checkingUpdates && !applyingUpdate;
        ApplyUpdateButton.IsEnabled = !checkingUpdates && !applyingUpdate && !busy;
        UpdateMessage.Text = updateState switch
        {
            UpdateState.Checking => Get("UpdatesChecking"),
            UpdateState.Current => Get("UpdatesCurrent", CurrentVersion),
            UpdateState.Available when offeredUpdate is { } offer => Get("UpdatesFound", offer.Version, CurrentVersion),
            UpdateState.Error => Get("UpdatesError"),
            UpdateState.Downloading => Get("UpdatesDownloading"),
            UpdateState.Unsafe => Get("UpdatesUnsafe"),
            UpdateState.DemoComplete => Get(portableUpdate ? "UpdatesDemoPortable" : "UpdatesDemoComplete"),
            _ => ""
        };
        UpdateMessage.Visibility = updateState != UpdateState.Idle ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void AutoUpdateCheck_Click(object sender, RoutedEventArgs e)
    {
        bool enabled = AutoUpdateCheck.IsChecked == true;
        try
        {
            if (updateDemo == null)
            {
                Directory.CreateDirectory(LanguagePreference.DataDirectory);
                File.WriteAllText(UpdatePreference, enabled ? "1" : "0");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AutoUpdateCheck.IsChecked = automaticUpdates;
            Notify(Get("UpdatesPreferenceError"), true);
            return;
        }
        automaticUpdates = enabled;
        if (enabled)
        {
            updateTimer.Start();
            await CheckForUpdatesAsync(false);
        }
        else
        {
            updateTimer.Stop();
            showUpdateBadge = false;
            RefreshUpdateUi();
        }
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e) => await CheckForUpdatesAsync(true);

    private void UpdateAvailable_Click(object sender, RoutedEventArgs e)
    {
        Options_Click(sender, e);
        if (!OptionsOpen) return;
        UpdatesTitle.StartBringIntoView();
        CheckUpdateButton.Focus(FocusState.Programmatic);
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (checkingUpdates || applyingUpdate) return;
        checkingUpdates = true;
        if (manual)
        {
            updateState = UpdateState.Checking;
            RefreshUpdateUi();
        }
        try
        {
            if (updateDemo != null)
            {
                await Task.Delay(manual ? 450 : 1, updateCancellation.Token);
                offeredUpdate = updateDemo == "--demo-update=current" ? null : new(
                    new Version(CurrentVersion.Major, CurrentVersion.Minor + 1, 0),
                    new Uri($"{PositioniRelease.Releases}/tag/v0.1.0"), new(PositioniRelease.Releases, 1, null));
            }
            else
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, PositioniRelease.Latest);
                request.Headers.Accept.ParseAdd("application/vnd.github+json");
                using var response = await UpdateClient.SendAsync(request, updateCancellation.Token);
                offeredUpdate = response.StatusCode == HttpStatusCode.NotFound
                    ? null
                    : PositioniRelease.Parse(await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(updateCancellation.Token),
                        CurrentVersion, portableUpdate);
                if (!portableUpdate && offeredUpdate?.Asset != null && !signerChecked)
                {
                    updateSigner = await Task.Run(() => Authenticode.TrustedSigner(Environment.ProcessPath!), updateCancellation.Token);
                    signerChecked = true;
                }
            }
            updateState = offeredUpdate == null ? UpdateState.Current : UpdateState.Available;
            showUpdateBadge = automaticUpdates && offeredUpdate != null;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or
            System.Text.Json.JsonException or InvalidDataException or InvalidOperationException or KeyNotFoundException)
        {
            if (manual) updateState = UpdateState.Error;
        }
        finally
        {
            checkingUpdates = false;
            if (!updateCancellation.IsCancellationRequested) RefreshUpdateUi();
        }
    }

    private async void UpdateNotes_Click(object sender, RoutedEventArgs e)
    {
        if (offeredUpdate is not { } release) return;
        try { await Windows.System.Launcher.LaunchUriAsync(release.Notes); }
        catch (Exception) { Notify(Get("UpdatesError"), true); }
    }

    private async void ApplyUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (applyingUpdate || checkingUpdates || busy || offeredUpdate is not { Asset: { } asset } release) return;
        if (updateDemo != null)
        {
            updateState = UpdateState.DemoComplete;
            RefreshUpdateUi();
            return;
        }

        if (portableUpdate || updateSigner == null)
        {
            try { await Windows.System.Launcher.LaunchUriAsync(asset.Download); }
            catch (Exception) { Notify(Get("UpdatesError"), true); }
            return;
        }

        applyingUpdate = true;
        updateState = UpdateState.Downloading;
        RefreshUpdateUi();
        string? staging = null;
        bool handedOff = false;
        try
        {
            staging = Directory.CreateTempSubdirectory("Positioni-Update-").FullName;
            string setup = Path.Combine(staging, "Positioni-setup.exe");
            using var downloadTimeout = CancellationTokenSource.CreateLinkedTokenSource(updateCancellation.Token);
            downloadTimeout.CancelAfter(TimeSpan.FromMinutes(10));
            await DownloadInstallerAsync(asset, setup, downloadTimeout.Token);
            var versionInfo = FileVersionInfo.GetVersionInfo(setup);
            if (versionInfo.ProductName?.Trim() != "Positioni" ||
                !Version.TryParse(versionInfo.ProductVersion?.Trim(), out var setupVersion) ||
                setupVersion.Build < 0 || setupVersion.ToString(3) != release.Version.ToString(3))
                throw new CryptographicException();

            var signer = await Task.Run(() => Authenticode.TrustedSigner(setup), updateCancellation.Token);
            if (signer == null || !CryptographicOperations.FixedTimeEquals(signer, updateSigner))
                throw new CryptographicException();
            string helper = Path.Combine(staging, "Positioni.Update.exe");
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Positioni.Update.exe"), helper);
            var start = new ProcessStartInfo(helper) { UseShellExecute = false, WorkingDirectory = staging };
            start.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            start.ArgumentList.Add(Environment.ProcessPath!);
            using var process = Process.Start(start) ?? throw new IOException();
            handedOff = true;
            host.Quitting = true;
            Close();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or
            OperationCanceledException or CryptographicException or System.ComponentModel.Win32Exception)
        {
            updateState = ex is CryptographicException ? UpdateState.Unsafe : UpdateState.Error;
        }
        finally
        {
            if (!handedOff && staging != null)
            {
                try { Directory.Delete(staging, recursive: true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            applyingUpdate = false;
            if (!updateCancellation.IsCancellationRequested) RefreshUpdateUi();
        }
    }

    private static async Task DownloadInstallerAsync(ReleaseAsset asset, string path, CancellationToken token)
    {
        using var response = await UpdateClient.GetAsync(asset.Download, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync(token);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[81920];
        long length = 0;
        int count;
        while ((count = await source.ReadAsync(buffer, token)) != 0)
        {
            length += count;
            if (length > asset.Size) throw new CryptographicException();
            hash.AppendData(buffer, 0, count);
            await target.WriteAsync(buffer.AsMemory(0, count), token);
        }
        if (length != asset.Size || (asset.Sha256 != null &&
            !Convert.ToHexString(hash.GetHashAndReset()).Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase)))
            throw new CryptographicException();
    }
}
