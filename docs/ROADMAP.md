# Implementation roadmap

## Foundation (this change)
- [x] WPF main window with dark theme
- [x] C# Core/Infrastructure separation
- [x] PS2EXE module probe and isolated script invocation
- [x] Basic GUI/console, architecture, STA/MTA, DPI, UAC, icon and metadata controls
- [x] SHA-256 output hash
- [ ] Windows CI verified (pending GitHub Actions)

## Remaining approved v1 scope
- [ ] Full MVVM refactor and shared styling
- [ ] Visual Identity Studio: PNG to multiresolution ICO
- [ ] Embedded files editor
- [ ] Batch compile with cancellation
- [ ] JSON profiles
- [ ] SQLite history, structured logs
- [ ] PE inspection, icon/version verification, optional execution tests
- [ ] Security review, PowerShell compatibility and edge cases
- [ ] Windows release validation

The compiled executables remain PS2EXE / Windows PowerShell 5.1 based, not .NET 10 applications.
