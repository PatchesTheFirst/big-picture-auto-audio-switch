# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed
- Verbose-logging 48-hour auto-disable timer no longer resets when re-saving settings
- Stale "launch on startup" registry entries pointing at a missing executable are cleaned up automatically
- Released binaries are now stamped with the git tag version
- Per-Monitor V2 DPI awareness — crisp rendering on mixed-DPI multi-monitor setups

### Added
- CI build and test on every push and pull request
- Prompt before closing the Settings window with unsaved changes
- Launching a second instance opens the Settings window of the running instance
- The "Test" button restores the previous audio device after the dialog closes

### Pending
- TODO: capture Settings window and tray menu screenshots for the README

## [1.0.0] - 2026-06

### Added
- Initial release: automatic audio device switching on Steam Big Picture Mode detection
