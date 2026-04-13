using System;
using System.Globalization;
using System.Net.Http;
using Jellyfin.Plugin.Lastfm.Api;
using Jellyfin.Plugin.Lastfm.Configuration;
using Jellyfin.Plugin.Lastfm.Resources;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Lastfm;

/// <summary>
/// The main plugin entry point for the Last.fm integration.
/// Implements IPluginServiceRegistrator for DI, IHasWebPages for config page,
/// and IScheduledTask for background task integration.
/// </summary>
public class LastfmPlugin : IPluginServiceRegistrator, IHasWebPages, IScheduledTask
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<LastfmPlugin> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private PluginConfiguration _config = null!;
    private LastfmApiClient _apiClient = null!;

    private readonly Dictionary<string, PlaybackTracker> _activeTrackers = new();
    private readonly object _trackerLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmPlugin"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="logger">The logger.</param>
    public LastfmPlugin(
        IHttpClientFactory httpClientFactory,
        ILibraryManager libraryManager,
        ISessionManager sessionManager,
        ILogger<LastfmPlugin> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sessionManager = sessionManager;
        _logger = logger;

        _config = new PluginConfiguration();
        _apiClient = new LastfmApiClient(httpClientFactory, _config, logger);

        _sessionManager.PlaybackStart += OnPlaybackStarted;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;

        _logger.LogInformation("Last.fm plugin initialized and listening for playback events");
    }

    /// <summary>
    /// Parameterless constructor required for IHasWebPages instantiation by Jellyfin.
    /// </summary>
    public LastfmPlugin()
    {
        _sessionManager = null!;
        _logger = null!;
        _httpClientFactory = null!;
        _config = new PluginConfiguration();
    }

    /// <summary>
    /// Gets or sets the plugin configuration.
    /// </summary>
    public PluginConfiguration Config
    {
        get => _config;
        set
        {
            _config = value;
            if (_httpClientFactory != null && _logger != null)
            {
                _apiClient = new LastfmApiClient(_httpClientFactory, _config, _logger);
            }
        }
    }

    /// <summary>
    /// Gets the Last.fm API client.
    /// </summary>
    public LastfmApiClient ApiClient => _apiClient;

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "Lastfm",
                DisplayName = "Last.fm",
                EmbeddedResourcePath = typeof(LastfmPlugin).Namespace + ".Configuration.config.html",
                EnableInMainMenu = false
            }
        };
    }

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<LastfmPlugin>();
    }

    private async void OnPlaybackStarted(object? sender, PlaybackProgressEventArgs e)
    {
        if (e.Item is not Audio audio)
        {
            return;
        }

        if (string.IsNullOrEmpty(_config.SessionKey))
        {
            _logger.LogWarning("Last.fm session key not configured. Skipping playback tracking.");
            return;
        }

        var tracker = new PlaybackTracker(audio, e.PlaybackPositionTicks ?? 0);
        lock (_trackerLock)
        {
            _activeTrackers[e.DeviceId] = tracker;
        }

        if (_config.NowPlayingEnabled)
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

        if (!_config.ScrobblingEnabled || string.IsNullOrEmpty(_config.SessionKey))
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
        if (durationSeconds < _config.MinDurationSeconds)
        {
            return;
        }

        var positionSeconds = positionTicks / 10_000_000;

        // Last.fm scrobble rules: 50% of track OR 4 minutes, whichever comes first
        var minScrobbleSeconds = Math.Min((int)(durationSeconds * _config.ScrobblePercent / 100.0), 240);

        if (positionSeconds >= minScrobbleSeconds)
        {
            tracker.Scrobbled = true;

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

        if (!_config.ScrobblingEnabled || string.IsNullOrEmpty(_config.SessionKey))
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
        if (durationSeconds < _config.MinDurationSeconds)
        {
            return;
        }

        var positionSeconds = positionTicks / 10_000_000;
        var minScrobbleSeconds = Math.Min((int)(durationSeconds * _config.ScrobblePercent / 100.0), 240);

        if (positionSeconds >= minScrobbleSeconds)
        {
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
    /// <param name="audio">The audio item.</param>
    /// <returns>The artist name.</returns>
    private string GetArtistName(Audio audio)
    {
        if (_config.UseAlbumArtist)
        {
            var albumArtist = audio.AlbumArtists?.FirstOrDefault();
            if (!string.IsNullOrEmpty(albumArtist))
            {
                return albumArtist;
            }
        }

        return audio.Artists?.FirstOrDefault() ?? audio.GetTopParent()?.Name ?? "Unknown Artist";
    }

    /// <inheritdoc />
    public string Name => Strings.PluginName;

    /// <inheritdoc />
    public string Description => Strings.Description;

    /// <inheritdoc />
    public string Category => Strings.PluginCategory;

    /// <inheritdoc />
    public string Key => "LastfmPluginSync";

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Last.fm sync task executed");
        progress.Report(100);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }
}

/// <summary>
/// Tracks playback state for scrobble detection.
/// </summary>
public class PlaybackTracker
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
