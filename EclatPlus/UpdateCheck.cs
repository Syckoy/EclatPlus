using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EclatPlus;

internal sealed record UpdateInfo(Version Remote, string Url);

/// <summary>
/// Vérifie GitHub une seule fois au lancement. Ne lit ni n’écrit les réglages utilisateur.
/// </summary>
internal static class UpdateCheck
{
    public const string RepoUrl = "https://github.com/Syckoy/EclatPlus";

    private const string ReleasesUrl = "https://api.github.com/repos/Syckoy/EclatPlus/releases/latest";
    private const string VersionFileUrl = "https://raw.githubusercontent.com/Syckoy/EclatPlus/main/version.json";

    private static readonly HttpClient Http = CreateClient();

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

            var url = string.IsNullOrWhiteSpace(release.HtmlUrl) ? RepoUrl : release.HtmlUrl;
            return new UpdateInfo(version, url);
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

            var url = string.IsNullOrWhiteSpace(file.Url) ? RepoUrl : file.Url;
            return new UpdateInfo(version, url);
        }
        catch
        {
            return null;
        }
    }

    private static Version? ParseVersion(string raw)
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

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
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
    }

    private sealed class VersionFile
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
