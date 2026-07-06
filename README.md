# Big Picture Auto Audio Switch

A modern Windows system tray application that automatically switches your default audio output device when Steam Big Picture Mode is detected.

## Features

- **Automatic Detection**: Uses Windows event hooks to efficiently detect Steam Big Picture Mode without polling
- **Automatic Audio Switching**: Switches to your configured audio device when Big Picture starts, restores the original device when it closes
- **Smart Retry Logic**: Automatically retries device switching for devices that need initialization time (e.g., HDMI audio)
- **Dynamic Theme Support**: Automatically follows Windows light/dark mode settings
- **Modern UI**: WPF-based settings window with clean, themed interface
- **Toast Notifications**: Optional notifications when audio is switched
- **Device Validation**: Warns you if a previously configured device is no longer available
- **Launch on Startup**: Configure the app to start with Windows
- **System Tray**: Runs silently in the system tray with quick access menu
- **Verbose Logging**: Optional debug logging for troubleshooting (auto-disables after 48 hours)

## Requirements

- Windows 10 version 1809 (build 17763) or later
- No additional runtime required (self-contained)

## Installation

### Portable (Recommended)

1. Download the latest release from the [Releases](https://github.com/PatchesTheFirst/big-picture-auto-audio-switch/releases) page
2. Extract to a folder of your choice (e.g., `C:\Tools\BigPictureAutoAudioSwitch`)
3. Run `BigPictureAutoAudioSwitch.exe`

> **Note:** Windows SmartScreen may show a warning on first run since the app is not code-signed. Click "More info" → "Run anyway" to proceed.

> **Startup Note:** If you enable "Launch on Windows startup" and later move the app to a different folder, re-enable the startup option in Settings. The app detects the stale entry, cleans it up automatically, and shows the checkbox as unchecked.

## Usage

1. **First Run**: The app will appear in your system tray (notification area)
2. **Configure**: Right-click the tray icon and select "Settings..."
3. **Select Device**: Choose the audio device you want to switch to when Big Picture Mode is detected
4. **Optional**: Enable "Launch on Windows startup" and "Show notifications"
5. **Save**: Click Save to apply your settings

The app will now automatically:
- Switch to your selected audio device when Steam Big Picture Mode opens
- Switch back to your previous audio device when Big Picture Mode closes

> **Note:** Only one instance of the app runs at a time. Launching the executable again while it is already running opens the Settings window of the running instance.

## Building from Source

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build

```powershell
# Clone the repository
git clone https://github.com/PatchesTheFirst/big-picture-auto-audio-switch.git
cd big-picture-auto-audio-switch

# Build
dotnet build

# Run
dotnet run --project src/BigPictureAutoAudioSwitch
```

### Run Tests

```powershell
dotnet test
```

### Publish (Self-Contained)

```powershell
dotnet publish src/BigPictureAutoAudioSwitch -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

## Project Structure

```
big-picture-auto-audio-switch/
├── src/
│   └── BigPictureAutoAudioSwitch/     # Main WPF application
│       ├── Services/                   # Core services (Audio, Detection, Settings, Theme)
│       ├── ViewModels/                 # MVVM ViewModels
│       ├── Views/                      # WPF Windows and Resources
│       │   └── Themes/                 # Light/Dark theme color definitions
│       ├── Converters/                 # Value Converters
│       └── Helpers/                    # Helper classes (BindingProxy)
├── tests/
│   └── BigPictureAutoAudioSwitch.Tests/  # Unit tests (xUnit + Moq)
└── README.md
```

## How It Works

1. **Detection**: The app uses `SetWinEventHook` to listen for window create, destroy, show, and hide events — no polling
2. **Filtering**: A switch is only triggered when a window passes all three checks: window class is `SDL_app`, the title is exactly "Steam Big Picture Mode", and the owning process is `steam` or `steamwebhelper`. Other SDL applications (many games use the same window class) will not trigger a switch. A 1-second cooldown prevents rapid re-triggering when the window flickers
3. **Audio Control**: Uses the Windows Core Audio APIs (NAudio for device enumeration, `IPolicyConfig` COM interop for switching) to set the default playback device for **all three audio roles** — Multimedia, Console, and Communications — so voice apps like Discord follow the switch too
4. **Restoration**: When Big Picture closes, the device that was active before the switch is restored

## Configuration

Settings are stored in `%LOCALAPPDATA%\BigPictureAutoAudioSwitch\settings.json`:

```json
{
  "targetDeviceId": "...",
  "launchOnStartup": false,
  "showNotifications": true,
  "verboseLogging": false,
  "verboseLoggingEnabledAt": null
}
```

## Logs

Application logs are stored in `%LOCALAPPDATA%\BigPictureAutoAudioSwitch\logs\`.

Logs roll over daily, and at most 7 log files are kept (max 50 MB each), so disk usage stays bounded.

To enable verbose (debug) logging for troubleshooting:
1. Open Settings from the tray icon
2. Check "Enable verbose logging (for troubleshooting)"
3. Click Save

Verbose logging automatically disables after 48 hours to prevent excessive disk usage. You can also click "Open Logs Folder" in Settings to access the log files directly.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. Notable changes are tracked in [CHANGELOG.md](CHANGELOG.md).

## Support

If you find this project helpful, consider buying me a coffee at [ko-fi](https://ko-fi.com/patchesthefirst87940)

## Acknowledgments

- Inspired by the original [BigPictureAudioSwitch](https://github.com/cinterre/BigPictureAudioSwitch)
- Uses [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) for system tray functionality
- Uses [NAudio](https://github.com/naudio/NAudio) for audio device management
- Uses [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) for MVVM support
