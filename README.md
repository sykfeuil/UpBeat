# UpBeat

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

**UpBeat** is an open-source Android music streaming app built with **.NET MAUI**.
Search for any song on YouTube and listen to the audio only, ad-free, with no limits, even in the background.

> 🚧 **Work in progress** — the project is in its early stages and not usable yet.

## Features

### Planned for v1
- [x] Search for music on YouTube
- [x] Audio-only streaming
- [x] Background playback with notification and lock screen controls

### Also available
- [x] Mini player on every page, with a progress bar to seek within the track
- [x] Local playlists: create, rename, delete, add tracks (long press a search result), remove tracks, reorder by drag and drop
- [x] Search while typing
- [x] YouTube thumbnails in lists, mini player and media notification
- [x] Import a YouTube playlist (search it from the Playlists tab)
- [x] Listening history (last 100 tracks, no duplicates)
- [x] Pixel-art look: icons, staircase frames, segmented progress bar, block loader, JetBrains Mono font

### Coming later
- [ ] Playback queue (play a whole playlist), shuffle and repeat
- [ ] Favorites
- [ ] Offline downloads

## Tech stack

| Area | Technology |
|---|---|
| Framework | [.NET MAUI](https://learn.microsoft.com/dotnet/maui/) (C#), .NET 10 |
| Platform | Android |
| YouTube extraction | [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) |
| Audio playback | [CommunityToolkit.Maui.MediaElement](https://learn.microsoft.com/dotnet/communitytoolkit/maui/views/mediaelement) |
| Architecture | MVVM with [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) |
| Local storage | SQLite via [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net) |
| UI helpers | [CommunityToolkit.Maui](https://learn.microsoft.com/dotnet/communitytoolkit/maui/) (long press, toasts) |

All processing happens on the device: there is no server and no account. Your data stays on your phone.

## Getting started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- .NET MAUI Android workload:
  ```bash
  dotnet workload install maui-android
  ```
- An Android emulator or a physical device with USB debugging enabled
- Recommended: [Visual Studio Code](https://code.visualstudio.com/) with the [.NET MAUI extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui), or Visual Studio 2026

- Android SDK and a JDK 21 (e.g. [Microsoft Build of OpenJDK](https://learn.microsoft.com/java/openjdk/download)). Missing Android components can be installed with:
  ```bash
  dotnet build src/UpBeat/UpBeat.csproj -t:InstallAndroidDependencies -f net10.0-android \
    -p:AndroidSdkDirectory="<path-to-android-sdk>" -p:JavaSdkDirectory="<path-to-jdk>" \
    -p:AcceptAndroidSDKLicenses=True
  ```

### Build and run
```bash
git clone https://github.com/sykfeuil/UpBeat.git
cd UpBeat

# Build
dotnet build src/UpBeat/UpBeat.csproj

# Build, deploy and launch on a connected device or running emulator
dotnet build src/UpBeat/UpBeat.csproj -t:Run -f net10.0-android
```

## Disclaimer

UpBeat is a personal, educational project. It is **not affiliated with, endorsed by, or sponsored by YouTube or Google**.
Using this app may go against [YouTube's Terms of Service](https://www.youtube.com/t/terms). You are solely responsible for how you use it.
The app does not host or distribute any content.

## License

UpBeat is free software, licensed under the [GNU General Public License v3.0](LICENSE).
You can use, study, modify and share it, but any distributed modified version must also be released under the GPL-3.0.

### Third-party assets
- [JetBrains Mono](https://github.com/JetBrains/JetBrainsMono) font, licensed under the [SIL Open Font License 1.1](src/UpBeat/Resources/Fonts/JetBrainsMono-OFL.txt).
