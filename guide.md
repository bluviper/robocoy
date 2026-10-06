# Robocopy GUI Wrapper - Feature Roadmap

## Phase 1: Core Essentials (MVP)
- [ ] Basic path selection (source/target) with folder browser dialogs
- [ ] File/folder selection interface (checkbox list or tree view)
- [ ] Essential flags toggle: /J, /Z, /R:3, /W:2
- [ ] Start/Stop/Cancel buttons with real-time progress
- [ ] Log viewer panel showing Robocopy output
- [ ] Error handling with user-friendly messages

## Phase 2: Productivity Boosters
- [ ] Preset profiles (Backup, Sync, Migration)
- [ ] Schedule integration (Windows Task Scheduler)
- [ ] Duplicate file detection before copy
- [ ] Email notification on completion
- [ ] Copy queue management (multiple transfers)
- [ ] Bandwidth throttling option

## Phase 3: Advanced Features
- [ ] Real-time speed graph and ETA
- [ ] File filter rules (extensions, size, date)
- [ ] Mirror mode with dry-run preview
- [ ] Cloud storage integration (OneDrive, Google Drive)
- [ ] Multi-thread control slider
- [ ] System resource monitor (CPU, disk usage)

## Technical Implementation Notes
- Use async process handling to avoid UI freezing
- Direct Robocopy.exe calls with generated parameters
- Parse Robocopy exit codes for status reporting
- Store user preferences in JSON config file
- Include portable mode (no installation required)
- Add logging to file for troubleshooting

## Performance Guarantees
- Zero file data relay through wrapper
- Direct I/O operations via Robocopy core
- Minimal memory footprint (<50MB RAM)
- No additional CPU overhead during transfers
- Native Windows API calls only