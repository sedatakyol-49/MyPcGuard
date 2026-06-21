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

## Agentic Architecture

MyPcGuard includes an agent layer on top of the classic scanner and service modules. Agents do not replace the scanners; they interpret local scan results, explain why something matters, and prepare conservative action plans.

Current agent foundation:

- Startup Optimization Agent
- Security Agent
- Program Uninstall Agent
- Driver Check Agent
- Web Research Agent placeholder
- Official Source Verifier
- Agent policy, memory and orchestration services

Agent results are stored locally in `%LocalAppData%\MyPcGuard\agent-memory.json`. Agent recommendations can be disabled in Settings, and the memory can be cleared from the app.

## Agent Safety Rules

- Agents cannot make system changes without explicit user approval.
- Action plans must explain the reason, expected benefit and possible side effects.
- Online research is disabled by default and requires user consent.
- File uploads are not part of the current design.
- Unknown web results are not treated as trusted.
- Third-party driver updater sites, download mirrors, crack/keygen sites and unknown EXE download pages are rejected.
- MyPcGuard does not claim to be antivirus software.

## Driver Download Policy

MyPcGuard must never automatically download or install drivers.

Allowed driver behavior:

- Detect missing or problematic drivers locally.
- Identify hardware vendor and device model when local data is available.
- Search possible official sources only when online research is enabled.
- Verify whether a source is official, likely official, unverified or rejected.
- Show verified official sources to the user.
- Let the user open official sources manually.

Forbidden driver behavior:

- No automatic driver installer downloads.
- No automatic driver installer execution.
- No third-party driver updater websites.
- No download mirror websites.
- No unknown EXE recommendations.
- No unofficial driver packages.

Preferred driver actions are Windows Update, Device Manager, official manufacturer pages and local driver report export. The final decision to download or install anything always belongs to the user.

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
