using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Lastfm.Configuration;
using Jellyfin.Plugin.Lastfm.Resources;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Lastfm;

/// <summary>
/// The main plugin entry point for the Last.fm integration.
/// Extends BasePlugin for Jellyfin plugin recognition and configuration management.
/// Implements IHasWebPages for the configuration page in the dashboard.
/// Implements IPluginServiceRegistrator for DI service registration.
/// </summary>
public class LastfmPlugin : BasePlugin<PluginConfiguration>, IHasWebPages, IPluginServiceRegistrator
{
    /// <summary>
    /// Gets the static instance of the plugin for access from other services.
    /// </summary>
    public static LastfmPlugin? Instance { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="xmlSerializer">The XML serializer.</param>
    public LastfmPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
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

    /// <summary>
    /// Gets the pages for this plugin. The configuration page is shown as a Settings
    /// button on the plugin card in the Jellyfin dashboard.
    /// </summary>
    /// <returns>The plugin pages.</returns>
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "Lastfm",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
            }
        };
    }

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<LastfmScrobbler>();
        serviceCollection.AddHostedService<LastfmScrobblerStartup>();
    }

    /// <summary>
    /// Startup service that forces the scrobbler to be resolved at application start,
    /// so it can subscribe to playback events immediately.
    /// </summary>
    private class LastfmScrobblerStartup : Microsoft.Extensions.Hosting.IHostedService
    {
        private readonly LastfmScrobbler _scrobbler;

        public LastfmScrobblerStartup(LastfmScrobbler scrobbler)
        {
            _scrobbler = scrobbler;
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
