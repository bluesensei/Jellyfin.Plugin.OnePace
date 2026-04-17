# Jellyfin.Plugin.OnePace

A Jellyfin plugin that provides metadata for [One Pace](https://onepace.net), a fan project that recuts the One Piece anime to remove filler and match the pacing of the original manga by Eiichiro Oda.

Works in shared Anime libraries alongside other shows — no separate library needed.

## Features

- **Series metadata** — title, description, genres, rating, premiere date, artwork
- **Season/arc metadata** — all 36 One Pace arcs with correct names and ordering
- **Episode metadata** — titles, descriptions, air dates for 464+ episodes
- **Artwork** — poster, logo, and multiple backdrops for the series; per-arc season posters
- **Override protection** — prevents TMDB, AniList, AniDB, and other providers from overwriting One Pace metadata with wrong anime matches
- **Flexible file naming** — supports both `[One Pace]` tagged files and plain folder names
- **Configurable library path** — auto-identify One Pace content without filename tags

## Installation

### From Release

1. Download `Jellyfin.Plugin.OnePace-v1.0.0.zip` from the [latest release](https://github.com/bluesensei/Jellyfin.Plugin.OnePace/releases)
2. Extract to your Jellyfin plugins folder as `OnePace_1.0.0.0/`
   - **Linux:** `~/.local/share/jellyfin/plugins/OnePace_1.0.0.0/`
   - **macOS:** `~/Library/Application Support/jellyfin/plugins/OnePace_1.0.0.0/`
   - **Windows:** `%LOCALAPPDATA%\jellyfin\plugins\OnePace_1.0.0.0\`
3. Download the `One Pace/` data folder from [tissla/one-pace-jellyfin](https://github.com/tissla/one-pace-jellyfin) and place it inside the plugin folder:
   ```
   OnePace_1.0.0.0/
   ├── Jellyfin.Plugin.OnePace.dll
   ├── meta.json
   └── One Pace/
       ├── tvshow.nfo
       ├── poster.png
       ├── logo.png
       ├── backdrop.jpg
       ├── Season 1/
       │   ├── season.nfo
       │   ├── poster.png
       │   └── *.nfo (episode files)
       ├── Season 2/
       │   └── ...
       └── ...
   ```
4. Restart Jellyfin

### From Source

```bash
git clone https://github.com/bluesensei/Jellyfin.Plugin.OnePace.git
cd Jellyfin.Plugin.OnePace
dotnet publish Jellyfin.Plugin.OnePace -c Release -o publish
```

Copy the contents of `publish/` to your plugin folder as described above.

## Configuration

After installation, go to **Dashboard → Plugins → One Pace** to configure:

| Setting | Description |
|---------|-------------|
| **Library Path** | Full server path to your One Pace media folder (e.g. `/Volumes/Media/Anime/One Pace/`). Items under this path auto-identify as One Pace content without needing `[One Pace]` in filenames. |
| **Preferred Language** | Language for metadata (en, ja, fr, de, es, pt, it, ru, zh, ko, ar) |

## Supported File Naming

The plugin recognizes the standard file and folder naming that One Pace releases use — no renaming needed after download.

### Tagged filenames (recommended)
```
[One Pace][237-238] Skypiea 01 [1080p][En Dub][5B9B150E].mkv
[One Pace][1-7] Romance Dawn [1080p][C7CA5080].mkv
```

### Plain folder names
```
One Pace/
├── [237-303] Skypiea [En Dub][1080p]/
│   ├── [One Pace][237-238] Skypiea 01 [720p][En Dub][5B9B150E].mkv
│   ├── [One Pace][239-240] Skypiea 02 [720p][En Dub][A1B2C3D4].mkv
│   └── ...
└── [304-322] Long Ring Long Land [1080p]/
    └── ...
```

### Library path matching
Set the library path in settings and any files under that path will be identified as One Pace content regardless of naming.

## How It Works

The plugin uses `ICustomMetadataProvider<T>` which runs **after** all remote providers (TMDB, AniList, AniDB, etc.), giving it the final word on metadata. It:

1. Identifies One Pace content by provider ID, filename pattern, or library path
2. Looks up metadata from bundled NFO files
3. Clears all remote provider IDs (TMDB, AniList, etc.) to prevent their image providers from downloading wrong artwork
4. Sets correct metadata and artwork from the local data folder

## Requirements

- Jellyfin **10.11.0** or later
- .NET 9.0 runtime (included with Jellyfin)

## Credits

- [One Pace](https://onepace.net) — the fan project
- [tissla/one-pace-jellyfin](https://github.com/tissla/one-pace-jellyfin) — NFO metadata and artwork

## License

MIT
