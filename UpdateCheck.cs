using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EclatPlus;

internal sealed record UpdateInfo(Version Remote, string PageUrl, string? DownloadUrl);

/// <summary>
/// Vérifie GitHub une seule fois au lancement. Ne lit ni n’écrit les réglages utilisateur.
/// </summary>
internal static class UpdateCheck
{
    public const string RepoUrl = "https://github.com/Syckoy/EclatPlus";

    private const string ReleasesUrl = "https://api.github.com/repos/Syckoy/EclatPlus/releases/latest";
    private const string VersionFileUrl = "https://raw.githubusercontent.com/Syckoy/EclatPlus/main/version.json";

    public static HttpClient Http { get; } = CreateClient(TimeSpan.FromSeconds(8));
    public static HttpClient DownloadHttp { get; } = CreateClient(TimeSpan.FromMinutes(3));

    public static Version LocalVersion { get; } = ReadLocalVersion();

    public static async Task<UpdateInfo?> TryGetUpdateAsync(CancellationToken cancellationToken = default)
    {
        var remote = await TryFromReleaseAsync(cancellationToken).ConfigureAwait(false)
                     ?? await TryFromVersionFileAsync(cancellationToken).ConfigureAwait(false);

        if (remote is null || remote.Remote <= LocalVersion)
        {
            return null;
        }

        return remote;
    }

    private static async Task<UpdateInfo?> TryFromReleaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Http.GetAsync(ReleasesUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, JsonOptions(), cancellationToken)
                .ConfigureAwait(false);
            if (release?.TagName is null)
            {
                return null;
            }

            var version = ParseVersion(release.TagName);
            if (version is null)
            {
                return null;
            }

            var page = string.IsNullOrWhiteSpace(release.HtmlUrl) ? RepoUrl : release.HtmlUrl;
            return new UpdateInfo(version, page, PickAsset(release.Assets));
        }
        catch
        {
            return null;
        }
    }

    private static async Task<UpdateInfo?> TryFromVersionFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Http.GetAsync(VersionFileUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var file = await JsonSerializer.DeserializeAsync<VersionFile>(stream, JsonOptions(), cancellationToken)
                .ConfigureAwait(false);
            if (file?.Version is null)
            {
                return null;
            }

            var version = ParseVersion(file.Version);
            if (version is null)
            {
                return null;
            }

            var page = string.IsNullOrWhiteSpace(file.Url) ? RepoUrl : file.Url;
            return new UpdateInfo(version, page, string.IsNullOrWhiteSpace(file.Download) ? null : file.Download);
        }
        catch
        {
            return null;
        }
    }

    private static string? PickAsset(List<GitHubAsset>? assets)
    {
        if (assets is null || assets.Count == 0)
        {
            return null;
        }

        var zipNamed = assets.FirstOrDefault(a =>
            a.Name is not null && a.Name.Equals("EclatPlus.zip", StringComparison.OrdinalIgnoreCase));
        if (zipNamed?.BrowserDownloadUrl is not null)
        {
            return zipNamed.BrowserDownloadUrl;
        }

        var zip = assets.FirstOrDefault(a =>
            a.Name is not null && a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        if (zip?.BrowserDownloadUrl is not null)
        {
            return zip.BrowserDownloadUrl;
        }

        var exe = assets.FirstOrDefault(a =>
            a.Name is not null && a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        return exe?.BrowserDownloadUrl;
    }

    internal static Version? ParseVersion(string raw)
    {
        var text = raw.Trim();
        if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            text = text[1..];
        }

        return Version.TryParse(text, out var version) ? version : null;
    }

    private static Version ReadLocalVersion()
    {
        var info = Application.ProductVersion;
        var plus = info.IndexOf('+');
        if (plus >= 0)
        {
            info = info[..plus];
        }

        return Version.TryParse(info, out var version) ? version : new Version(1, 0, 0);
    }

    private static HttpClient CreateClient(TimeSpan timeout)
    {
        var client = new HttpClient { Timeout = timeout };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("EclatPlus", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }

    private sealed class VersionFile
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("download")]
        public string? Download { get; set; }
    }
}
