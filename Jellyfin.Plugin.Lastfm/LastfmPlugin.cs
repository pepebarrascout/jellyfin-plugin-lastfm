using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Lastfm.Configuration;
using Jellyfin.Plugin.Lastfm.Resources;

namespace Jellyfin.Plugin.Lastfm;

/// <summary>
/// The main plugin entry point for the Last.fm integration.
/// Extends BasePlugin for proper Jellyfin plugin recognition and config page access.
/// </summary>
public class LastfmPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Static instance for access from other services.
    /// </summary>
    public static LastfmPlugin? Instance { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="xmlSerializer">The XML serializer.</param>
    /// <param name="logger">The logger.</param>
    public LastfmPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<LastfmPlugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => Strings.PluginName;

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("f3c07757-5b42-442e-a37a-f49355b6a7c0");

    /// <inheritdoc />
    public override string Description => Strings.Description;

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0}.Configuration.config.html",
                    GetType().Namespace)
            }
        ];
    }
}
