# Jellyfin Last.fm Plugin

> Scrobble your music to Last.fm, update now playing status, and love/ban tracks directly from Jellyfin.

## Features

- **Automatic Scrobbling**: Tracks are scrobbled to Last.fm when you reach 50% of playback or 4 minutes, whichever comes first
- **Now Playing Notifications**: Updates your Last.fm profile with what you're currently listening to
- **Love/Ban Tracks**: Mark tracks as loved or banned on Last.fm via the plugin API
- **Auto-Love**: Optionally auto-love tracks that you mark as favorites in Jellyfin
- **Configurable**: Fine-tune scrobble percentage, minimum duration, and artist source
- **Multi-User**: Supports multiple Jellyfin sessions simultaneously

## Installation

### From Jellyfin Plugin Repository

1. Navigate to **Dashboard > Plugins > Catalog**
2. Search for **Last.fm**
3. Click **Install**

### Manual Installation

1. Download the latest release from [Releases](../../releases)
2. Extract the ZIP file
3. Copy the `Jellyfin.Plugin.Lastfm` folder to your Jellyfin plugins directory:
   - **Linux**: `~/.config/jellyfin/plugins/`
   - **Windows**: `%LocalAppData%\Jellyfin\plugins\`
   - **macOS**: `~/.local/share/jellyfin/plugins/`
4. Restart Jellyfin

## Configuration

### Prerequisites

You need a [Last.fm API account](https://www.last.fm/api/account/create) to use this plugin:

1. Go to [last.fm/api/account/create](https://www.last.fm/api/account/create)
2. Fill in the application details (any name and description work)
3. Copy your **API Key** and **Shared Secret**

### Setup

1. Navigate to **Dashboard > Plugins > Last.fm**
2. Enter your **API Key** and **API Secret**
3. Click **Connect to Last.fm**
4. Authorize the application on the Last.fm page that opens
5. Enter the token from the callback URL if needed
6. Save your configuration

### Options

| Option | Default | Description |
|--------|---------|-------------|
| Scrobbling Enabled | Yes | Enable/disable scrobbling to Last.fm |
| Now Playing Enabled | Yes | Enable/disable now playing notifications |
| Scrobble at | 50% | Percentage of track to play before scrobbling |
| Minimum Duration | 30s | Minimum track length to be eligible for scrobbling |
| Auto-love Liked Tracks | Yes | Automatically love tracks liked in Jellyfin |
| Use Album Artist | No | Use album artist instead of track artist for scrobbling |

## Building

### Requirements

- .NET SDK 9.0+
- NuGet sources configured:
  - `https://api.nuget.org/v3/index.json`
  - `https://nuget.jellyfin.org/v3/index.json`

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build in Debug mode
dotnet build

# Build in Release mode
dotnet build -c Release

# Publish artifacts
dotnet publish -c Release -o artifacts
```

### NuGet Configuration

Create a `nuget.config` at the repository root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="jellyfin" value="https://nuget.jellyfin.org/v3/index.json" />
  </packageSources>
</configuration>
```

## API Endpoints

The plugin exposes the following API endpoints under `/Lastfm/`:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Lastfm/AuthUrl` | Get Last.fm authorization URL |
| POST | `/Lastfm/Authenticate` | Authenticate with a token |
| POST | `/Lastfm/Love` | Love a track |
| POST | `/Lastfm/Unlove` | Un-love a track |
| POST | `/Lastfm/Ban` | Ban a track |
| GET | `/Lastfm/Status` | Get connection status |

## Troubleshooting

### Scrobbles not appearing

- Verify your session key is configured (check the Status endpoint)
- Ensure tracks are at least 30 seconds long (Last.fm requirement)
- Check that you've reached the scrobble threshold (50% or 4 minutes)
- Check Jellyfin logs for any error messages from the Last.fm plugin

### Connection issues

- Ensure `https://ws.audioscrobbler.com` is accessible from your Jellyfin server
- Verify your API key and secret are correct
- Try re-authenticating by connecting to Last.fm again

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
