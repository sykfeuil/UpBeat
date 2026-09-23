# UpBeat

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

**UpBeat** is an open-source Android music streaming app built with **.NET MAUI**.
Search for any song on YouTube and listen to the audio only, ad-free, with no limits, even in the background.

> 🚧 **Work in progress** — the project is in its early stages and not usable yet.

## Features

### Planned for v1
- [ ] Search for music on YouTube
- [ ] Audio-only streaming
- [ ] Background playback with notification and lock screen controls

### Coming later
- [ ] Local playlists (create, edit, reorder)
- [ ] Playback queue, shuffle and repeat
- [ ] History and favorites
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

### Build and run
_Instructions will be added once the project is created._

## Disclaimer

UpBeat is a personal, educational project. It is **not affiliated with, endorsed by, or sponsored by YouTube or Google**.
Using this app may go against [YouTube's Terms of Service](https://www.youtube.com/t/terms). You are solely responsible for how you use it.
The app does not host or distribute any content.

## License

UpBeat is free software, licensed under the [GNU General Public License v3.0](LICENSE).
You can use, study, modify and share it, but any distributed modified version must also be released under the GPL-3.0.
