using System;
using System.Globalization;
using System.Net.Http;
using Jellyfin.Plugin.Lastfm.Api;
using Jellyfin.Plugin.Lastfm.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Lastfm;

/// <summary>
/// Handles playback event tracking and Last.fm API communication.
/// Registered as a singleton DI service, this class subscribes to Jellyfin's
/// session manager events for automatic scrobbling and now-playing notifications.
/// </summary>
public class LastfmScrobbler : IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<LastfmScrobbler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private LastfmApiClient _apiClient;

    private readonly Dictionary<string, PlaybackTracker> _activeTrackers = new();
    private readonly object _trackerLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmScrobbler"/> class.
    /// Subscribes to playback events immediately on construction.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="sessionManager">The session manager for playback events.</param>
    /// <param name="logger">The logger instance.</param>
    public LastfmScrobbler(IHttpClientFactory httpClientFactory, ISessionManager sessionManager, ILogger<LastfmScrobbler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sessionManager = sessionManager;
        _logger = logger;

        var config = GetCurrentConfig();
        _apiClient = new LastfmApiClient(httpClientFactory, config, logger);

        _sessionManager.PlaybackStart += OnPlaybackStarted;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;

        _logger.LogInformation("Last.fm scrobbler initialized and listening for playback events");
    }

    /// <summary>
    /// Gets the current plugin configuration.
    /// </summary>
    private PluginConfiguration GetCurrentConfig()
    {
        return LastfmPlugin.Instance?.Configuration ?? new PluginConfiguration();
    }

    /// <summary>
    /// Refreshes the API client with the latest configuration.
    /// </summary>
    private void RefreshApiClient()
    {
        var config = GetCurrentConfig();
        _apiClient = new LastfmApiClient(_httpClientFactory, config, _logger);
    }

    private async void OnPlaybackStarted(object? sender, PlaybackProgressEventArgs e)
    {
        if (e.Item is not Audio audio)
        {
            return;
        }

        RefreshApiClient();
        var config = GetCurrentConfig();

        if (string.IsNullOrEmpty(config.SessionKey))
        {
            _logger.LogWarning("Last.fm session key not configured. Skipping playback tracking.");
            return;
        }

        var tracker = new PlaybackTracker(audio, e.PlaybackPositionTicks ?? 0);
        lock (_trackerLock)
        {
            _activeTrackers[e.DeviceId] = tracker;
        }

        if (config.NowPlayingEnabled)
        {
            try
            {
                var artist = GetArtistName(audio);
                var album = audio.Album;
                var title = audio.Name ?? string.Empty;

                await _apiClient.UpdateNowPlayingAsync(artist, title, album);
                _logger.LogInformation("Last.fm now playing: {Artist} - {Title}", artist, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send now playing notification to Last.fm");
            }
        }
    }

    private async void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs e)
    {
        if (e.Item is not Audio audio)
        {
            return;
        }

        var config = GetCurrentConfig();
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
            return;
        }

        var positionSeconds = positionTicks / 10_000_000;

        // Last.fm scrobble rules: 50% of track OR 4 minutes, whichever comes first
        var minScrobbleSeconds = Math.Min((int)(durationSeconds * config.ScrobblePercent / 100.0), 240);

        if (positionSeconds >= minScrobbleSeconds)
        {
            tracker.Scrobbled = true;
            RefreshApiClient();

            try
            {
                var artist = GetArtistName(audio);
                var album = audio.Album;
                var title = audio.Name ?? string.Empty;
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - positionSeconds;

                await _apiClient.ScrobbleAsync(artist, title, album, timestamp);
                _logger.LogInformation("Last.fm scrobbled: {Artist} - {Title}", artist, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scrobble to Last.fm");
            }
        }
    }

    private async void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        if (e.Item is not Audio audio)
        {
            return;
        }

        lock (_trackerLock)
        {
            _activeTrackers.Remove(e.DeviceId);
        }

        var config = GetCurrentConfig();
        if (!config.ScrobblingEnabled || string.IsNullOrEmpty(config.SessionKey))
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
            RefreshApiClient();

            try
            {
                var artist = GetArtistName(audio);
                var album = audio.Album;
                var title = audio.Name ?? string.Empty;
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - positionSeconds;

                await _apiClient.ScrobbleAsync(artist, title, album, timestamp);
                _logger.LogInformation("Last.fm scrobbled on stop: {Artist} - {Title}", artist, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scrobble on stop to Last.fm");
            }
        }
    }

    /// <summary>
    /// Gets the artist name for scrobbling, respecting configuration.
    /// </summary>
    private string GetArtistName(Audio audio)
    {
        var config = GetCurrentConfig();

        if (config.UseAlbumArtist)
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
    /// Disposes the scrobbler and unsubscribes from events.
    /// </summary>
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
