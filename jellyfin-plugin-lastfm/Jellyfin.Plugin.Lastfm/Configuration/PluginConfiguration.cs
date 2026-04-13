using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Lastfm.Configuration;

/// <summary>
/// Plugin configuration for the Last.fm plugin.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the Last.fm API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Last.fm shared secret.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Last.fm session key for the authenticated user.
    /// </summary>
    public string SessionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username of the authenticated Last.fm user.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether scrobbling is enabled.
    /// </summary>
    public bool ScrobblingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the "Now Playing" notification is enabled.
    /// </summary>
    public bool NowPlayingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the percentage of playback required before scrobbling.
    /// Default is 50% of the track duration or 4 minutes, whichever comes first.
    /// </summary>
    public int ScrobblePercent { get; set; } = 50;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically love tracks that are liked in Jellyfin.
    /// </summary>
    public bool AutoLoveLikedTracks { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum duration in seconds before a track can be scrobbled.
    /// Last.fm requires tracks to be at least 30 seconds long.
    /// </summary>
    public int MinDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to check for album artist before scrobbling.
    /// </summary>
    public bool CheckAlbumArtist { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use album artist instead of track artist for scrobbling.
    /// </summary>
    public bool UseAlbumArtist { get; set; } = false;
}
