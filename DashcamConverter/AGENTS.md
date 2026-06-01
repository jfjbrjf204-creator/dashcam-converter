<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-01 | Updated: 2026-06-01 -->

# DashcamConverter

## Purpose
This directory contains the Windows executable project for Dashcam Converter. It provides the WinForms UI, the command-line dispatch layer, and packaging settings for the self-contained Windows x64 single-file application. Conversion work is delegated to `DashcamConverter.Core`.

## Key Files

| File | Description |
|------|-------------|
| `DashcamConverter.csproj` | WinExe project targeting `net8.0-windows`, enabling WinForms, Windows targeting, win-x64 runtime, self-contained single-file publish, and native library self-extraction. |
| `Program.cs` | Application entry point: no args starts the GUI, `--version` prints the version, and `remux <input> [-o output] [--mkv]` runs CLI conversion. |
| `MainForm.cs` | WinForms UI with file selection, output directory selection, progress bar, status messages, cancellation on close, and calls to `Converter.Remux`. |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `ffmpeg/` | Build-time bundle location for FFmpeg native DLLs used as embedded resources by the core project. DLLs are downloaded by CI/build scripts and ignored by git. |

## For AI Agents

### Working In This Directory
- Keep this project as the thin app/UI layer; put reusable conversion behavior in `DashcamConverter.Core`.
- Keep user-facing strings Russian to match the existing GUI and CLI.
- Preserve `[STAThread]` and `ApplicationConfiguration.Initialize()` for WinForms startup.
- Maintain CLI behavior in `Program.cs` without introducing a large framework unless explicitly requested.
- Do not hand-edit `bin/` or `obj/`; they are generated build outputs.

### Testing Requirements
- For CLI changes, verify `DashcamConverter.exe --version` and `DashcamConverter.exe remux <input> [-o output] [--mkv]` behavior on Windows after build.
- For GUI changes, build on Windows and manually verify file selection, output folder validation, progress updates, and conversion status messages.
- Run `dotnet build -c Release` and relevant `dotnet test -c Release` checks from the repository root when feasible.

### Common Patterns
- `MainForm` builds controls programmatically using `TableLayoutPanel`; there are no designer files.
- Conversion runs inside `Task.Run` to keep the UI responsive; UI updates use `BeginInvoke`.
- `_isConverting` gates UI state through `SetBusy` and `UpdateActionState`.
- CLI output and errors are written through `Console.WriteLine`/`Console.Error.WriteLine` with non-zero exit on conversion exceptions.

## Error Codes

See the root `AGENTS.md#error-codification` for the format and rules. This registry covers `DashcamConverter` (GUI + CLI) only.

### GUI — `MainForm.cs`

| Code | Source | Description |
|------|--------|-------------|
| `GUI-001` | `OnFormLoad()` — `MessageBox` | FFmpeg не удалось инициализировать при запуске приложения |
| `GUI-002` | `OnConvert()` — `MessageBox` | Не выбраны файлы для конвертации |
| `GUI-003` | `OnConvert()` — `MessageBox` | Папка сохранения не существует |
| `GUI-004` | `OnConvert()` — `MessageBox` (per file) | Ошибка конвертации файла (ConversionException) |
| `GUI-005` | `OnConvert()` — `MessageBox` (per file) | Файл не найден при конвертации (FileNotFoundException) |
| `GUI-006` | `OnConvert()` — status label | Конвертация отменена пользователем |
| `GUI-007` | `GetCommitHash()` — catch | Не удалось прочитать хэш коммита (silent, returns "unknown") |

### CLI — `Program.cs`

| Code | Source | Description |
|------|--------|-------------|
| `CLI-001` | `Main()` — else branch | Неверные аргументы командной строки (вывод usage) |
| `CLI-002` | `Main()` — `catch(Exception)` | Ошибка выполнения CLI-команды remux |

## Dependencies

### Internal
- References `../DashcamConverter.Core/DashcamConverter.Core.csproj`.
- Calls `Converter.Remux`, `Ffmpeg.Initialize`, and handles `ConversionException` from the core library.

### External
- Windows Forms (`System.Windows.Forms`) through `net8.0-windows`.
- Native FFmpeg DLLs are present under `ffmpeg/` during build/publish but should remain gitignored.

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
