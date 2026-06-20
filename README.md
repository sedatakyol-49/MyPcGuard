# MyPcGuard

MyPcGuard is a local PC Health & Security Assistant built with Avalonia UI, .NET 10 and MVVM. It helps users understand startup load, suspicious processes, Windows Defender status, quarantine items, reports and action history.

## Not Antivirus

MyPcGuard is not antivirus software and does not replace antivirus protection. It does not automatically delete files, quarantine files or change system settings without user confirmation.

## Safety Principles

- All analyses run locally on the device.
- No files are uploaded automatically.
- System changes require explicit confirmation.
- Autostart disable/enable operations use backups.
- Action history is written to a local JSON file.
- System-critical Microsoft, Windows, driver and security components are protected from aggressive actions.
- Non-reversible destructive actions are disabled or treated as dangerous.

## Privacy

MyPcGuard is designed local-first. Optional future online checks should use file hashes only, not full file uploads.

## Languages

The app supports:

- Deutsch (`de`) as default
- English (`en`)
- Türkçe (`tr`)

Language selection is stored locally in `%LocalAppData%\MyPcGuard\user-settings.json`.

## Platform Targets

- Windows: primary implementation for system scanning, Defender status, startup registry entries and safe actions.
- Linux/macOS: cross-platform architecture is preserved; unsupported actions return clear NotSupported results in this product stage.

## MVP Features

- Dashboard status
- Startup recommendations and safe actions
- Process and finding overview
- Defender status
- Quarantine index and restore-ready model
- Action history
- HTML report export
- Localized UI foundation with RESX resources

## Roadmap

- Code signing
- Installer
- Auto updater
- VirusTotal hash lookup
- Scheduled scan
- PDF report
- License activation
- Deeper macOS/Linux support
