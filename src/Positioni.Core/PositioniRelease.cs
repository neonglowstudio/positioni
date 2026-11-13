using System.Text.Json;

namespace Positioni.Core;

public sealed record ReleaseAsset(Uri Download, long Size, string? Sha256);

public sealed record PositioniRelease(Version Version, Uri Notes, ReleaseAsset? Asset)
{
    public static readonly Uri Latest = new("https://api.github.com/repos/neonglowstudio/positioni/releases/latest");
    public static readonly Uri Releases = new("https://github.com/neonglowstudio/positioni/releases");

    public static PositioniRelease? Parse(string json, Version current, bool portable)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) return null;
        string tag = root.GetProperty("tag_name").GetString() ?? "";
        string text = tag.StartsWith('v') || tag.StartsWith('V') ? tag[1..] : tag;
        if (!Version.TryParse(text, out var version) || version.Build < 0 || version.Revision != -1 ||
            version.ToString(3) != text)
            throw new InvalidDataException("The GitHub release tag is not a three-part version.");
        if (version <= current) return null;

        string fileName = portable ? $"positioni-{text}-win-x64-portable.zip" : $"positioni-{text}-win-x64-setup.exe";
        ReleaseAsset? asset = null;
        foreach (var item in root.GetProperty("assets").EnumerateArray())
        {
            string? assetName = item.GetProperty("name").GetString();
            if (assetName is null || !assetName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                continue;

            string download = $"{Releases}/download/{tag}/{assetName}";
            if (item.GetProperty("browser_download_url").GetString() != download ||
                !item.GetProperty("size").TryGetInt64(out long size) || size is <= 0 or > 350_000_000)
                continue;

            string? digest = item.TryGetProperty("digest", out var digestValue) ? digestValue.GetString() : null;
            string? hash = digest is { Length: 71 } && digest.StartsWith("sha256:", StringComparison.Ordinal)
                && digest.AsSpan(7).ToString().All(Uri.IsHexDigit) ? digest[7..] : null;
            asset = new(new Uri(download), size, hash);
            break;
        }
        return new(version, new Uri($"{Releases}/tag/{tag}"), asset);
    }
}
