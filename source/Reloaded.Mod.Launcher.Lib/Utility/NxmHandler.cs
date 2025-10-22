namespace Reloaded.Mod.Launcher.Lib.Utility;

using System.Web;
using System.Net.Http.Headers;

/// <summary>
/// Class for handling Nexus Mods nxm links
/// </summary>
public static class NxmHandler
{
    /// <summary>
    /// Represents a parsed NXM link.
    /// </summary>
    public class NxmLink
    {
        public string Game { get; set; } = string.Empty;
        public int ModId { get; set; }
        public int FileId { get; set; }
        public string? Key { get; set; }
        public long Expires { get; set; }
        public int? UserId { get; set; }
    }

    private static readonly Regex NxmRegex = new(
        @"^nxm://([a-z0-9]+)/mods/(\d+)/files/(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    /// <summary>
    /// Parses an nxm:// URL into a structured <see cref="NxmLink"/>.
    /// </summary>
    /// <param name="url">The NXM link string.</param>
    /// <returns>An <see cref="NxmLink"/> object containing parsed data.</returns>
    /// <exception cref="ArgumentException">Thrown if the URL is invalid or cannot be parsed.</exception>
    public static NxmLink Parse(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("NXM URL cannot be empty.", nameof(url));

        var match = NxmRegex.Match(url);
        if (!match.Success)
            throw new ArgumentException($"Invalid NXM URL format: {url}", nameof(url));

        var game = match.Groups[1].Value;
        var modId = int.Parse(match.Groups[2].Value);
        var fileId = int.Parse(match.Groups[3].Value);

        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);

        var link = new NxmLink
        {
            Game = game,
            ModId = modId,
            FileId = fileId,
            Key = query["key"],
            UserId = TryParseInt(query["user_id"]),
            Expires = TryParseLong(query["expires"])
        };

        return link;
    }

    /// <summary>
    /// Represents a Nexus Mods download link entry.
    /// </summary>
    public class NexusDownloadLink
    {
        public string Name { get; set; } = string.Empty;
        public string Short_Name { get; set; } = string.Empty;
        public string URI { get; set; } = string.Empty;
    }

    /// <summary>
    /// Retrieves the downloadable file URLs from the Nexus Mods API
    /// for a given NXM link.
    /// </summary>
    /// <param name="link">Parsed <see cref="NxmLink"/> structure.</param>
    /// <param name="apiKey">Your Nexus Mods API key (from user or app authentication).</param>
    /// <returns>A list of available download links (may contain multiple mirrors).</returns>
    /// <exception cref="HttpRequestException">Thrown if the API call fails.</exception>
    public static async Task<List<NexusDownloadLink>> GetDownloadLinksAsync(NxmLink link, string apiKey)
    {
        if (link == null)
            throw new ArgumentNullException(nameof(link));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key must be provided.", nameof(apiKey));

        // Base API endpoint
        var baseUrl = $"https://api.nexusmods.com/v1/games/{link.Game}/mods/{link.ModId}/files/{link.FileId}/download_link.json";

        var query = new List<string>();
        if (!string.IsNullOrEmpty(link.Key))
            query.Add($"key={Uri.EscapeDataString(link.Key)}");
        if (link.Expires > 0)
            query.Add($"expires={link.Expires}");
        if (query.Count > 0)
            baseUrl += "?" + string.Join("&", query);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("apikey", apiKey);

        var response = await client.GetAsync(baseUrl).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"NexusMods API request failed ({response.StatusCode}): {content}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var links = JsonSerializer.Deserialize<List<NexusDownloadLink>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<NexusDownloadLink>();

        return links;
    }

    private static int? TryParseInt(string? value)
            => int.TryParse(value, out var result) ? result : null;

    private static long TryParseLong(string? value)
        => long.TryParse(value, out var result) ? result : 0;

}