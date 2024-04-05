# Name of Viewer Here

[Viewer Name Here] is a viewer for Second Life and OpenSimulator. It is heavily inspired by Radegast, using the Avalonia UI framework and .NET Core. It should work on Windows, Linux, and Android. iOS and macOS may be supported in the future.

Features unique to this viewer (compared to the stock viewer) will include:

- Popout windows for chat, inventory, etc.
- World View is optional, you can use a purely text-based interface if you prefer.
- Automation support (e.g. for bots)

## Planned Features

- [ ] User Authentication

  - [ ] Login
  - [ ] MFA Support

- [ ] Communication

  - [ ] Chat
  - [ ] Voice Chat

- [ ] User Interaction

  - [ ] Friends
  - [ ] Groups
  - [ ] Profiles
  - [ ] Search

- [ ] World Interaction

  - [ ] Teleporting
  - [ ] 3D World View
  - [ ] Camera Controls
  - [ ] World Map
  - [ ] Mini Map
  - [ ] Radar
  - [ ] Nearby People

- [ ] Content Creation and Management

  - [ ] Inventory
  - [ ] Appearance Editor
  - [ ] Scripting
  - [ ] Building
  - [ ] Uploading Textures
  - [ ] Uploading PBR Materials
  - [ ] Uploading Sounds
  - [ ] Uploading Meshes
  - [ ] Uploading Animations

- [ ] User Interface

  - [ ] Light and Dark Modes
  - [ ] Customizable UI
  - [ ] Customizable Keybinds
  - [ ] Customizable Notifications

- [ ] Grid Manager
- [ ] Preferences
- [ ] Plugin System
- [ ] RLV Support
- [ ] Automation Support
- [ ] Accessibility
- [ ] Discord Rich Presence

## Installation

To be added...

## Building

Make sure you have the [.NET Core SDK](https://dotnet.microsoft.com/download) installed. We use .NET 8.0.

Install The Avalonia Project too: `dotnet new install Avalonia.Templates` (if you haven't already).

Also install the .NET things for android, wasm, etc. if you want to build for those platforms.
`dotnet workload install android`, `dotnet workload install wasm-tools`, etc.

Clone the repository and navigate to the project directory.

Run `dotnet build` to build the project.

## Contributing

Contributions are welcome! Please see the [CONTRIBUTING.md](CONTRIBUTING.md) file for more information.

Also, please note the [Code of Conduct](CODE_OF_CONDUCT.md) for this project.

## Support and Contact

Please note that while the viewer is in the early development stage, it is not recommended for everyday use and no support will be given. This ReadMe will be updated with support information once the viewer is in a more stable state.

If you have any questions or concerns, please contact [Galaxy Littlepaws](mailto:galaxylittlepaws@proton.me) and include "Viewer Name Here" in the subject line.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

- Radegast
- OpenMetaverse
- Avalonia
- .NET Core
- OpenSimulator
- Second Life

## Disclaimer

This viewer is not affiliated with, endorsed by, or sponsored by Linden Research, Inc. or any of its affiliates. Second Life and Linden Lab are trademarks or registered trademarks of Linden Research, Inc. All rights reserved. No infringement is intended.
