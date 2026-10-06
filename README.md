# ROBOCoy: Your GUI version

A modern, fast, and lightweight Windows GUI wrapper for Microsoft **Robocopy** (Robust File Copy). Designed with clean program flow, responsive real-time feedback, and zero third-party UI framework bloat.

---

## Features

- **Modern Squircle Card Layout**: Two-column responsive layout with soft-cornered ("squircle") cards rendered natively via GDI+ cubic Bézier curves.
- **Interactive Subfolder Tree**: Visual tree view of the source directory allowing selective inclusion and exclusion (`/XD` parameters).
- **Essential Robocopy Flags**:
  - `/J`: Unbuffered I/O (recommended for large files).
  - `/Z`: Restartable mode (automatically resumes if network connection cuts).
  - `/R:n`: Failure retry count.
  - `/W:n`: Wait time between retries in seconds.
  - File Pattern filters (e.g. `*.*`, `*.zip`, `*.pdf`).
- **Real-Time Visual Feedback**:
  - Current file transfer progress bar.
  - Overall transfer progress bar with summary tracking.
  - Dark-mode live streaming log console with auto-scroll.
  - Play (Start), Stop (Cancel), and Disk (Save) icon controls with mouseover tooltips.
- **Robust Execution**:
  - Non-blocking asynchronous process execution (`robocopy.exe`).
  - Instant start without blocking upfront file enumeration.
  - Clean stream-buffered logging directly to timestamped log files in `logs/`.
  - User configuration persisted to `config.json`.

---

## Deployment & Running

ROBOCoy is published as single-file executables located in `publish/`:

| Executable | Size | Description |
| :--- | :--- | :--- |
| **`ROBOCoy-Standalone.exe`** | ~110 MB | **Zero-dependency, standalone executable.** Bundles the complete .NET runtime. Copy and run directly on **any** Windows 10/11 or Windows Server PC without installing anything. |
| **`ROBOCoy-Portable.exe`** | ~230 KB | **Ultra-lightweight single-file executable.** Requires [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) installed on the target machine. |

> **Tip**: You can copy just the single `.exe` file to your Desktop, USB flash drive, or server. No extra folders or files are required. The program will automatically create its companion `config.json` and `logs/` directory wherever the `.exe` is located.

---

## Building from Source

### Prerequisites
- Windows 10 / 11 / Windows Server
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Quick Build & Test
```powershell
# Run the test suite
dotnet test Tests/RobocopyGui.Tests.csproj

# Run directly in development mode
dotnet run --project RobocopyGui.csproj
```

### Automated Deployment
To produce fresh deployment binaries:
```powershell
.\deploy.ps1
```
The output single-file executables will be published directly to `publish\`.

---

## Architecture & Engineering Principles

ROBOCoy adheres to the **Finch Engineering Principles**:
- **Native BCL & GDI+**: Built strictly using native .NET Base Class Libraries and WinForms GDI+ (zero heavy UI framework dependencies).
- **Direct Program Flow**: Input $\rightarrow$ Validation $\rightarrow$ Argument Construction $\rightarrow$ Async Process Execution $\rightarrow$ Event Streaming.
- **Resource Aware**: Single-pass directory expansion, stream-buffered disk logging, and microsecond-level UI paint performance (< 0.05 ms per repaint).

---

## License

MIT License.
