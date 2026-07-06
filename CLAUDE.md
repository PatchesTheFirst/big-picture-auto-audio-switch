# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Windows-only WPF system tray application (.NET 8, `net8.0-windows10.0.17763.0`) that automatically switches the default audio output device when Steam Big Picture Mode opens, and restores the previous device when it closes. There is no main window — the app lives entirely in the tray.

## Commands

```powershell
dotnet build                                    # Build solution
dotnet test                                     # Run all tests
dotnet test --filter "FullyQualifiedName~AudioServiceTests"   # Run one test class
dotnet test --filter "FullyQualifiedName~AudioServiceTests.MethodName"  # Run one test
dotnet run --project src/BigPictureAutoAudioSwitch             # Run the app

# Publish (self-contained single file, matches the release pipeline)
dotnet publish src/BigPictureAutoAudioSwitch -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

Everything is Windows-only (WPF, Win32 P/Invoke, COM interop) — build and tests require a Windows machine. CI (`.github/workflows/ci.yml`) builds and tests Release on every push/PR; tagging `v*` triggers `publish.yml`, which stamps the assembly version from the tag and creates a GitHub release.

## Architecture

**Startup flow:** `Program.Main` enforces a single instance via a named mutex; a second launch signals a named `EventWaitHandle` that tells the running instance to open its Settings window. `App.xaml.cs` builds a Generic Host (`Microsoft.Extensions.Hosting`) that registers all services by interface, configures Serilog, applies the theme, then starts `AudioService` and `BigPictureDetector` and creates the tray icon (H.NotifyIcon). Other code resolves dependencies via the static `App.Services` provider.

**Layers (MVVM via CommunityToolkit.Mvvm):**
- `Services/` — all logic, each behind an `I*` interface (this is what makes the ViewModels testable with Moq)
- `ViewModels/` — `TrayIconViewModel` (singleton, owns tray menu), `SettingsViewModel` and `AboutViewModel` (transient)
- `Views/` — XAML windows plus `Views/Themes/ThemeColors.{Light,Dark}.xaml`; `ThemeService` swaps these resource dictionaries at runtime to follow the Windows light/dark setting

**Detection (`BigPictureDetector`):** uses `SetWinEventHook` (create/destroy/show/hide) — no polling. A switch only triggers when all three checks pass: window class `SDL_app`, title exactly "Steam Big Picture Mode", and owning process `steam` or `steamwebhelper` (many games also use the `SDL_app` class). A 1-second deactivation cooldown debounces window flicker. If the initial switch fails (e.g., HDMI audio needs init time), a cancellable background retry loop takes over.

**Audio (`AudioService`):** NAudio for device enumeration/notifications; switching the default device uses the undocumented `IPolicyConfig` COM interface (interop declared inside `AudioService.cs`) and sets all three roles — Multimedia, Console, Communications — so voice apps follow the switch. It stores the previous device ID before switching and restores it on deactivation.

**Settings & logging:** JSON settings and Serilog logs live under `%LOCALAPPDATA%\BigPictureAutoAudioSwitch\`. Verbose logging is controlled at runtime through a shared `LoggingLevelSwitch` and auto-disables after 48 hours (`LoggingService.CheckAutoDisableAsync`).

**Tuning constants** (retry counts/delays, cooldown, log limits, mutex/event names, paths) are centralized in `AppConstants.cs` — change them there, not inline.

## Gotchas

- The release build is published **trimmed** (`PublishTrimmed=true`, partial mode). Reflection-heavy additions can break the published exe even when `dotnet build`/`dotnet run` work; assemblies that don't survive trimming must be added as `TrimmerRootAssembly` in the csproj.
- The solution file is the XML-based `BigPictureAutoAudioSwitch.slnx` (not `.sln`).
- Tests (xUnit + Moq + FluentAssertions) mirror the source layout under `tests/BigPictureAutoAudioSwitch.Tests/{Services,ViewModels}/`.
- Notable user-facing changes should be recorded in `CHANGELOG.md` (Keep a Changelog format).
