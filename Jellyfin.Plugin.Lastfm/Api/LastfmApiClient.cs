using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Lastfm.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Lastfm.Api;

/// <summary>
/// Client for the Last.fm API.
/// Handles all HTTP communication with the Last.fm service including
/// scrobbling, now playing, love/ban, and authentication.
/// </summary>
public class LastfmApiClient
{
    private const string BaseUrl = "https://ws.audioscrobbler.com/2.0/";

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    private string _apiKey;
    private string _apiSecret;
    private string _sessionKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmApiClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="logger">The logger.</param>
    public LastfmApiClient(IHttpClientFactory httpClientFactory, PluginConfiguration config, ILogger logger)
    {
        _httpClient = httpClientFactory.CreateClient("Lastfm");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Jellyfin-Lastfm/1.0.0");
        _apiKey = config.ApiKey;
        _apiSecret = config.ApiSecret;
        _sessionKey = config.SessionKey;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the API key used for Last.fm requests.
    /// </summary>
    public string ApiKey { get => _apiKey; set => _apiKey = value; }

    /// <summary>
    /// Gets or sets the API secret used for request signing.
    /// </summary>
    public string ApiSecret { get => _apiSecret; set => _apiSecret = value; }

    /// <summary>
    /// Gets or sets the session key for authenticated requests.
    /// </summary>
    public string SessionKey { get => _sessionKey; set => _sessionKey = value; }

    /// <summary>
    /// Gets the Last.fm authentication URL for the user to authorize the application.
    /// </summary>
    /// <returns>The authorization URL.</returns>
    public string GetAuthUrl()
    {
        return $"https://www.last.fm/api/auth/?api_key={_apiKey}";
    }

    /// <summary>
    /// Gets a session token from Last.fm using the authentication token.
    /// </summary>
    /// <param name="authToken">The authentication token from the callback.</param>
    /// <returns>The session key and username, or null if authentication failed.</returns>
    public async Task<LastfmSession?> GetSessionAsync(string authToken)
    {
        var parameters = new Dictionary<string, string>
        {
            { "method", "auth.getSession" },
            { "api_key", _apiKey },
            { "token", authToken }
        };

        var response = await SignedPostAsync(parameters);
        var doc = await response.Content.ReadAsStringAsync();

        try
        {
            using var json = JsonDocument.Parse(doc);
            var session = json.RootElement.GetProperty("session");
            var username = session.GetProperty("name").GetString();
            var key = session.GetProperty("key").GetString();

            if (username != null && key != null)
            {
                _logger.LogInformation("Authenticated with Last.fm as user {Username}", username);
                return new LastfmSession(username, key);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Last.fm auth response: {Response}", doc);
        }

        return null;
    }

    /// <summary>
    /// Updates the now playing status on Last.fm.
    /// </summary>
    /// <param name="artist">The track artist.</param>
    /// <param name="title">The track title.</param>
    /// <param name="album">The album name (optional).</param>
    public async Task<HttpResponseMessage> UpdateNowPlayingAsync(string artist, string title, string? album = null)
    {
        if (string.IsNullOrEmpty(_sessionKey))
        {
            _logger.LogWarning("Last.fm: UpdateNowPlaying skipped - no session key");
            return new HttpResponseMessage(System.Net.HttpStatusCode.PreconditionFailed);
        }

        var parameters = new Dictionary<string, string>
        {
            { "method", "track.updateNowPlaying" },
            { "artist", artist },
            { "track", title },
            { "sk", _sessionKey }
        };

        if (!string.IsNullOrEmpty(album))
        {
            parameters["album"] = album;
        }

        var response = await SignedPostAsync(parameters);
        if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Last.fm UpdateNowPlaying failed ({StatusCode}): {Body}", response.StatusCode, body);
            }

        return response;
    }

    /// <summary>
    /// Scrobbles a track to Last.fm.
    /// </summary>
    /// <param name="artist">The track artist.</param>
    /// <param name="title">The track title.</param>
    /// <param name="album">The album name (optional).</param>
    /// <param name="timestamp">The Unix timestamp when the track started playing.</param>
    public async Task<HttpResponseMessage> ScrobbleAsync(string artist, string title, string? album = null, long timestamp = 0)
    {
        if (string.IsNullOrEmpty(_sessionKey))
        {
            _logger.LogWarning("Last.fm: Scrobble skipped - no session key");
            return new HttpResponseMessage(System.Net.HttpStatusCode.PreconditionFailed);
        }

        if (timestamp == 0)
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        var parameters = new Dictionary<string, string>
        {
            { "method", "track.scrobble" },
            { "artist", artist },
            { "track", title },
            { "timestamp", timestamp.ToString(CultureInfo.InvariantCulture) },
            { "sk", _sessionKey }
        };

        if (!string.IsNullOrEmpty(album))
        {
            parameters["album"] = album;
        }

        var response = await SignedPostAsync(parameters);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Last.fm Scrobble failed ({StatusCode}): {Body}", response.StatusCode, body);
        }

        return response;
    }

    /// <summary>
    /// Loves a track on Last.fm.
    /// </summary>
    /// <param name="artist">The track artist.</param>
    /// <param name="title">The track title.</param>
    public async Task LoveTrackAsync(string artist, string title)
    {
        if (string.IsNullOrEmpty(_sessionKey))
        {
            return;
        }

        var parameters = new Dictionary<string, string>
        {
            { "method", "track.love" },
            { "artist", artist },
            { "track", title },
            { "sk", _sessionKey }
        };

        await SignedPostAsync(parameters);
        _logger.LogInformation("Last.fm loved: {Artist} - {Title}", artist, title);
    }

    /// <summary>
    /// Un-loves a track on Last.fm.
    /// </summary>
    /// <param name="artist">The track artist.</param>
    /// <param name="title">The track title.</param>
    public async Task UnloveTrackAsync(string artist, string title)
    {
        if (string.IsNullOrEmpty(_sessionKey))
        {
            return;
        }

        var parameters = new Dictionary<string, string>
        {
            { "method", "track.unlove" },
            { "artist", artist },
            { "track", title },
            { "sk", _sessionKey }
        };

        await SignedPostAsync(parameters);
        _logger.LogInformation("Last.fm unloved: {Artist} - {Title}", artist, title);
    }

    /// <summary>
    /// Bans a track on Last.fm.
    /// </summary>
    /// <param name="artist">The track artist.</param>
    /// <param name="title">The track title.</param>
    public async Task BanTrackAsync(string artist, string title)
    {
        if (string.IsNullOrEmpty(_sessionKey))
        {
            return;
        }

        var parameters = new Dictionary<string, string>
        {
            { "method", "track.ban" },
            { "artist", artist },
            { "track", title },
            { "sk", _sessionKey }
        };

        await SignedPostAsync(parameters);
        _logger.LogInformation("Last.fm banned: {Artist} - {Title}", artist, title);
    }

    /// <summary>
    /// Sends an unsigned GET request to the Last.fm API.
    /// </summary>
    /// <param name="parameters">The request parameters.</param>
    /// <returns>The HTTP response message.</returns>
    public async Task<HttpResponseMessage> GetAsync(Dictionary<string, string> parameters)
    {
        parameters["api_key"] = _apiKey;
        parameters["format"] = "json";

        var queryString = string.Join("&", parameters.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var url = $"{BaseUrl}?{queryString}";
        return await _httpClient.GetAsync(url);
    }

    /// <summary>
    /// Sends a signed POST request to the Last.fm API.
    /// All write operations require an API signature.
    /// </summary>
    /// <param name="parameters">The request parameters (must include method).</param>
    /// <returns>The HTTP response message.</returns>
    private async Task<HttpResponseMessage> SignedPostAsync(Dictionary<string, string> parameters)
    {
        parameters["api_key"] = _apiKey;

        // Generate API signature
        var signature = GenerateApiSignature(parameters, _apiSecret);
        parameters["api_sig"] = signature;
        parameters["format"] = "json";

        var content = new FormUrlEncodedContent(parameters);
        return await _httpClient.PostAsync(BaseUrl, content);
    }

    /// <summary>
    /// Generates an MD5 API signature for a Last.fm API call.
    /// The signature is created by alphabetically sorting all parameters (except format and callback),
    /// concatenating them as keyvalue pairs, appending the API secret, and MD5-hashing the result.
    /// </summary>
    /// <param name="parameters">The API call parameters.</param>
    /// <param name="secret">The API shared secret.</param>
    /// <returns>The MD5 signature string in lowercase hexadecimal.</returns>
    private static string GenerateApiSignature(Dictionary<string, string> parameters, string secret)
    {
        var sorted = parameters
            .Where(p => p.Key != "format" && p.Key != "callback")
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{p.Key}{p.Value}");

        var signatureBase = string.Concat(sorted) + secret;

        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(signatureBase);
        var hashBytes = md5.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

/// <summary>
/// Represents a Last.fm authenticated session.
/// </summary>
/// <param name="Username">The authenticated username.</param>
/// <param name="SessionKey">The session key for API calls.</param>
public record LastfmSession(string Username, string SessionKey);
