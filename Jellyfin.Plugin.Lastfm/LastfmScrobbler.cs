using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Lastfm.Api;
using Jellyfin.Plugin.Lastfm.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Lastfm;

/// <summary>
/// Handles playback event subscription and scrobbling logic.
/// Registered as an IHostedService via IPluginServiceRegistrator to ensure
/// automatic instantiation when the Jellyfin server starts.
/// </summary>
public class LastfmScrobbler : IHostedService, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LastfmScrobbler> _logger;
    private LastfmApiClient? _apiClient;

    private readonly Dictionary<string, PlaybackTracker> _activeTrackers = new();
    private readonly object _trackerLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmScrobbler"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public LastfmScrobbler(
        ISessionManager sessionManager,
        IHttpClientFactory httpClientFactory,
        ILogger<LastfmScrobbler> logger)
    {
        _sessionManager = sessionManager;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStarted;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;

        _logger.LogInformation("Last.fm scrobbler started - listening for playback events");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStarted;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _sessionManager.PlaybackProgress -= OnPlaybackProgress;

        _logger.LogInformation("Last.fm scrobbler stopped");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets or creates the API client, always refreshing credentials from current config.
    /// </summary>
    private LastfmApiClient? ApiClient
    {
        get
        {
            var config = LastfmPlugin.Instance?.Configuration;
            if (config == null)
            {
                _logger.LogWarning("Last.fm: Cannot access plugin configuration (plugin instance is null)");
                return null;
            }

            if (_apiClient == null)
            {
                _apiClient = new LastfmApiClient(_httpClientFactory, config, _logger);
                _logger.LogInformation("Last.fm: API client created with session key: {HasSessionKey}",
                    !string.IsNullOrEmpty(config.SessionKey) ? "YES" : "NO");
            }
            else
            {
                // Always refresh credentials from config in case they changed after authentication
                _apiClient.ApiKey = config.ApiKey;
                _apiClient.ApiSecret = config.ApiSecret;
                _apiClient.SessionKey = config.SessionKey;
            }

            return _apiClient;
        }
    }

    private PluginConfiguration? Config => LastfmPlugin.Instance?.Configuration;

    private async void OnPlaybackStarted(object? sender, PlaybackProgressEventArgs e)
    {
        try
        {
            var config = Config;

            if (e.Item is not Audio audio || config == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(config.SessionKey))
            {
                _logger.LogDebug("Last.fm: Skipping playback start - no session key configured");
                return;
            }

            var tracker = new PlaybackTracker(audio, e.PlaybackPositionTicks ?? 0);
            lock (_trackerLock)
            {
                _activeTrackers[e.DeviceId] = tracker;
            }

            if (config.NowPlayingEnabled)
            {
                var apiClient = ApiClient;
                if (apiClient != null)
                {
                    var artist = GetArtistName(audio);
                    var album = audio.Album;
                    var title = audio.Name ?? string.Empty;

                    var response = await apiClient.UpdateNowPlayingAsync(artist, title, album);
                    _logger.LogInformation("Last.fm now playing sent: {Artist} - {Title} (response: {StatusCode})",
                        artist, title, response.StatusCode);
                }
                else
                {
                    _logger.LogWarning("Last.fm: Cannot send now playing - API client is null");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle playback start for Last.fm");
        }
    }

    private async void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs e)
    {
        try
        {
            var config = Config;

            if (e.Item is not Audio audio || config == null)
            {
                return;
            }

            if (!config.ScrobblingEnabled || string.IsNullOrEmpty(config.SessionKey))
            {
                return;
            }

            PlaybackTracker? tracker;
            lock (_trackerLock)
            {
                if (!_activeTrackers.TryGetValue(e.DeviceId, out tracker))
                {
                    return;
                }
            }

            if (tracker.Scrobbled || tracker.TrackId != audio.Id.ToString("N", CultureInfo.InvariantCulture))
            {
                return;
            }

            var positionTicks = e.PlaybackPositionTicks ?? 0;
            var durationTicks = audio.RunTimeTicks ?? 0;

            if (durationTicks == 0)
            {
                return;
            }

            var durationSeconds = durationTicks / 10_000_000;
            if (durationSeconds < config.MinDurationSeconds)
            {
                _logger.LogDebug("Last.fm: Track too short ({Duration}s < {MinDuration}s): {Title}",
                    durationSeconds, config.MinDurationSeconds, audio.Name);
                return;
            }

            var positionSeconds = positionTicks / 10_000_000;

            // Last.fm scrobble rules: configured percent OR 4 minutes, whichever comes first
            var minScrobbleSeconds = Math.Min((int)(durationSeconds * config.ScrobblePercent / 100.0), 240);

            _logger.LogDebug("Last.fm: Progress {Position}s / {Duration}s (threshold: {Threshold}s): {Title}",
                positionSeconds, durationSeconds, minScrobbleSeconds, audio.Name);

            if (positionSeconds >= minScrobbleSeconds)
            {
                tracker.Scrobbled = true;

                var apiClient = ApiClient;
                if (apiClient != null)
                {
                    var artist = GetArtistName(audio);
                    var album = audio.Album;
                    var title = audio.Name ?? string.Empty;
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - positionSeconds;

                    var response = await apiClient.ScrobbleAsync(artist, title, album, timestamp);
                    _logger.LogInformation("Last.fm scrobble sent: {Artist} - {Title} (response: {StatusCode})",
                        artist, title, response.StatusCode);
                }
                else
                {
                    _logger.LogWarning("Last.fm: Cannot scrobble - API client is null");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle playback progress for Last.fm");
        }
    }

    private async void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        try
        {
            var config = Config;
            bool wasScrobbled = false;

            if (e.Item is Audio audio)
            {
                lock (_trackerLock)
                {
                    if (_activeTrackers.TryGetValue(e.DeviceId, out var tracker))
                    {
                        wasScrobbled = tracker.Scrobbled;
                    }

                    _activeTrackers.Remove(e.DeviceId);
                }
            }
            else
            {
                return;
            }

            if (config == null || !config.ScrobblingEnabled || string.IsNullOrEmpty(config.SessionKey))
            {
                return;
            }

            // Skip if already scrobbled during playback progress
            if (wasScrobbled)
            {
                return;
            }

            var positionTicks = e.PlaybackPositionTicks ?? 0;
            var durationTicks = audio.RunTimeTicks ?? 0;

            if (durationTicks == 0)
            {
                return;
            }

            var durationSeconds = durationTicks / 10_000_000;
            if (durationSeconds < config.MinDurationSeconds)
            {
                return;
            }

            var positionSeconds = positionTicks / 10_000_000;
            var minScrobbleSeconds = Math.Min((int)(durationSeconds * config.ScrobblePercent / 100.0), 240);

            if (positionSeconds >= minScrobbleSeconds)
            {
                var apiClient = ApiClient;
                if (apiClient != null)
                {
                    var artist = GetArtistName(audio);
                    var album = audio.Album;
                    var title = audio.Name ?? string.Empty;
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - positionSeconds;

                    var response = await apiClient.ScrobbleAsync(artist, title, album, timestamp);
                    _logger.LogInformation("Last.fm scrobble sent on stop: {Artist} - {Title} (response: {StatusCode})",
                        artist, title, response.StatusCode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle playback stop for Last.fm");
        }
    }

    /// <summary>
    /// Gets the artist name for scrobbling, respecting configuration.
    /// </summary>
    /// <param name="audio">The audio item.</param>
    /// <returns>The artist name.</returns>
    private string GetArtistName(Audio audio)
    {
        var config = Config;
        if (config != null && config.UseAlbumArtist)
        {
            var albumArtist = audio.AlbumArtists?.FirstOrDefault();
            if (!string.IsNullOrEmpty(albumArtist))
            {
                return albumArtist;
            }
        }

        return audio.Artists?.FirstOrDefault() ?? audio.GetTopParent()?.Name ?? "Unknown Artist";
    }

    /// <summary>
    /// Invalidates the API client so it will be recreated with fresh config on next use.
    /// </summary>
    public void InvalidateApiClient()
    {
        _apiClient = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sessionManager.PlaybackStart -= OnPlaybackStarted;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _sessionManager.PlaybackProgress -= OnPlaybackProgress;
    }
}

/// <summary>
/// Tracks playback state for scrobble detection.
/// </summary>
internal class PlaybackTracker
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackTracker"/> class.
    /// </summary>
    /// <param name="audio">The audio item being played.</param>
    /// <param name="positionTicks">The starting position in ticks.</param>
    public PlaybackTracker(Audio audio, long positionTicks)
    {
        TrackId = audio.Id.ToString("N", CultureInfo.InvariantCulture);
        DurationTicks = audio.RunTimeTicks ?? 0;
        StartPositionTicks = positionTicks;
        Scrobbled = false;
    }

    /// <summary>
    /// Gets the unique track identifier.
    /// </summary>
    public string TrackId { get; }

    /// <summary>
    /// Gets the total track duration in ticks.
    /// </summary>
    public long DurationTicks { get; }

    /// <summary>
    /// Gets or sets the starting position in ticks.
    /// </summary>
    public long StartPositionTicks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this track has already been scrobbled.
    /// </summary>
    public bool Scrobbled { get; set; }
}
