namespace Jellyfin.Plugin.Lastfm.Resources;

/// <summary>
/// Localized strings for the Last.fm plugin.
/// </summary>
public static class Strings
{
    /// <summary>
    /// Gets the plugin name.
    /// </summary>
    public const string PluginName = "Last.fm";

    /// <summary>
    /// Gets the plugin description.
    /// </summary>
    public const string Description = "Scrobble your music to Last.fm, update now playing status, and love/ban tracks directly from Jellyfin.";

    /// <summary>
    /// Gets the plugin category.
    /// </summary>
    public const string PluginCategory = "General";

    /// <summary>
    /// Gets the tab header for Last.fm settings.
    /// </summary>
    public const string SettingsTabHeader = "Last.fm Settings";

    /// <summary>
    /// Gets the label for the API key field.
    /// </summary>
    public const string ApiKeyLabel = "Last.fm API Key";

    /// <summary>
    /// Gets the help text for the API key field.
    /// </summary>
    public const string ApiKeyHelp = "Enter your Last.fm API key. You can get one at last.fm/api/account/create.";

    /// <summary>
    /// Gets the label for the API secret field.
    /// </summary>
    public const string ApiSecretLabel = "Last.fm API Secret";

    /// <summary>
    /// Gets the help text for the API secret field.
    /// </summary>
    public const string ApiSecretHelp = "Enter your Last.fm API shared secret.";

    /// <summary>
    /// Gets the label for the connect button.
    /// </summary>
    public const string ConnectLabel = "Connect to Last.fm";

    /// <summary>
    /// Gets the label for the connected status.
    /// </summary>
    public const string ConnectedLabel = "Connected as";

    /// <summary>
    /// Gets the label for the scrobbling toggle.
    /// </summary>
    public const string ScrobblingEnabledLabel = "Enable Scrobbling";

    /// <summary>
    /// Gets the label for the now playing toggle.
    /// </summary>
    public const string NowPlayingEnabledLabel = "Enable Now Playing Notifications";

    /// <summary>
    /// Gets the label for the scrobble percentage field.
    /// </summary>
    public const string ScrobblePercentLabel = "Scrobble at";

    /// <summary>
    /// Gets the label for the minimum duration field.
    /// </summary>
    public const string MinDurationLabel = "Minimum track duration (seconds)";

    /// <summary>
    /// Gets the label for the auto-love toggle.
    /// </summary>
    public const string AutoLoveLabel = "Auto-love liked tracks";

    /// <summary>
    /// Gets the label for the use album artist toggle.
    /// </summary>
    public const string UseAlbumArtistLabel = "Use Album Artist for scrobbling";

    /// <summary>
    /// Gets the save button text.
    /// </summary>
    public const string SaveLabel = "Save";
}
