using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Lastfm;

/// <summary>
/// Registers plugin services with the Jellyfin dependency injection container.
/// This class must be separate from LastfmPlugin for Jellyfin to properly
/// discover and register the services.
/// </summary>
public class LastfmPluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <summary>
    /// Registers the Last.fm scrobbler as an IHostedService.
    /// IHostedService is automatically started by the .NET runtime when the
    /// server starts, ensuring the scrobbler subscribes to playback events.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="applicationHost">The application host.</param>
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHostedService<LastfmScrobbler>();
    }
}
