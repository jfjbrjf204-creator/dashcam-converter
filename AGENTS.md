<!-- Generated: 2026-06-01 | Updated: 2026-06-01 -->

# dashcam-converter

## Purpose
This repository contains a .NET 8/C# Windows application for remuxing dashcam video files into MP4 or MKV containers without transcoding. The app has both a WinForms GUI and a CLI entry point, with conversion implemented through FFmpeg's C API via `FFmpeg.AutoGen` and native shared DLLs. Existing human-facing documentation is mostly Russian; preserve that language for user-visible strings and docs unless explicitly asked otherwise.

## Key Files

| File | Description |
|------|-------------|
| `DashcamConverter.sln` | Visual Studio solution containing the WinForms app, core library, and xUnit test project. |
| `README.md` | Russian README with usage, supported formats, build instructions, and release notes. |
| `ARCHITECTURE.md` | Russian architecture reference covering stack, components, FFmpeg loading, data flow, and CI/CD. |
| `build.bat` | Windows local build script that downloads FFmpeg DLLs, restores packages, runs tests, and publishes. |
| `.gitignore` | Ignores .NET build outputs, downloaded FFmpeg DLLs, generated fixtures, IDE files, and local OMC caches. |
| `LICENSE` | GPL-3.0 license; important because the project ships GPL FFmpeg shared DLLs. |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `DashcamConverter/` | WinForms GUI and CLI executable project (see `DashcamConverter/AGENTS.md`). |
| `DashcamConverter.Core/` | Core remuxing library and FFmpeg P/Invoke wrapper (see `DashcamConverter.Core/AGENTS.md`). |
| `DashcamConverter.Tests/` | xUnit tests for the converter facade and FFmpeg wrapper (see `DashcamConverter.Tests/AGENTS.md`). |
| `.github/` | GitHub Actions CI/CD configuration (see `.github/AGENTS.md`). |
| `tests/` | Generated test fixture area used by CI and xUnit tests (see `tests/AGENTS.md`). |
| `ffmpeg/` | Empty-at-rest extraction/download workspace; do not treat as source. |

Generated local directories such as `.codemap/`, `.lsmcp/`, `.omo/`, `bin/`, and `obj/` are tool/build artifacts and should not be documented as source areas or edited for product changes.

## For AI Agents

### Working In This Directory
- Treat the codebase as a small, disciplined .NET solution: keep changes focused and aligned with the existing three-project split.
- User-facing text, comments, and existing documentation are Russian; keep new user-visible strings Russian unless the task requests a language change.
- Do not commit generated artifacts (`bin/`, `obj/`, `publish/`, downloaded FFmpeg zips, generated fixtures) or local agent/tool state.
- Prefer updating `ARCHITECTURE.md` or the relevant nested `AGENTS.md` when changing architecture, build, CI, or test behavior.
- The main runtime dependency chain is `DashcamConverter` → `DashcamConverter.Core`; tests also reference `DashcamConverter.Core`.

### Testing Requirements
- Preferred full verification on Windows: `dotnet restore`, `dotnet build -c Release`, then `dotnet test -c Release`.
- `build.bat` performs the full Windows packaging flow, including FFmpeg DLL download and publish.
- Tests are Windows-guarded and require FFmpeg DLL access through `DASHCAM_FFMPEG_PATH` or embedded DLL resources.
- On non-Windows environments, expect GUI/publish behavior and FFmpeg tests to be limited; note platform limitations explicitly.

### Common Patterns
- File-scoped namespace: `namespace DashcamConverter;`.
- Nullable and implicit usings are enabled in all projects.
- The application performs remux/container copy only, not video transcoding.
- CLI mode is implemented in `Program.cs`; GUI behavior is implemented in `MainForm.cs`; conversion logic belongs in `DashcamConverter.Core`.

### Error Codification
Все ошибки должны иметь **уникальный код** в формате `{ПРЕФИКС}-{NNN}`. Код позволяет однозначно определить участок кода, в котором возникла ошибка, без чтения текста сообщения.

#### Формат: `{ПРЕФИКС}-{NNN}`

| Префикс | Слой | Файл |
|---------|------|------|
| `FFMPEG` | Core | `DashcamConverter.Core/Ffmpeg.cs` |
| `CORE` | Core | `DashcamConverter.Core/Converter.cs` |
| `GUI` | App | `DashcamConverter/MainForm.cs` |
| `CLI` | App | `DashcamConverter/Program.cs` |

#### Правила
1. Каждый `throw`, `MessageBox.Show` с ошибкой, `Console.Error.WriteLine` и возврат ошибки через `out string? error` / `(int, string)` **обязан** содержать код ошибки.
2. Код ошибки указывается первым элементом в сообщении: `"[{КОД}] описание ошибки"`.
3. Коды в пределах одного префикса нумеруются последовательно (001, 002, …).
4. Один и тот же код не используется дважды. При добавлении новой ошибки берётся следующий свободный номер.
5. Коды ошибок документируются в `AGENTS.md` соответствующего проекта (см. ссылки ниже).
6. Для FFmpeg AVERROR код ошибки указывается в скобках после системного: `"[FFMPEG-020] avformat_open_input: {FfmpegErrorString(ret)} (код FFmpeg: {ret})"`.

#### Структура `ConversionException` и `FfmpegException`
Оба класса исключений должны быть расширены полем `ErrorCode` (string). Пример:

```csharp
public class ConversionException : Exception
{
    public string ErrorCode { get; }
    public string Stderr { get; }

    public ConversionException(string errorCode, string message, string stderr = "")
        : base($"[{errorCode}] {message}")
    {
        ErrorCode = errorCode;
        Stderr = stderr;
    }
}
```

Полный реестр кодов ошибок по проектам:
- Core-слой: см. `DashcamConverter.Core/AGENTS.md#error-codes`
- GUI/CLI-слой: см. `DashcamConverter/AGENTS.md#error-codes`

## Dependencies

### Internal
- `DashcamConverter/` depends on `DashcamConverter.Core/`.
- `DashcamConverter.Tests/` depends on `DashcamConverter.Core/` and `tests/fixtures/`.
- `.github/workflows/ci.yml` coordinates FFmpeg downloads, fixture generation, tests, publish, artifact upload, and release publishing.

### External
- .NET 8 SDK/runtime.
- Windows Forms via `net8.0-windows`.
- `FFmpeg.AutoGen` 8.1.0 plus native FFmpeg shared DLLs.
- xUnit 2.9.0, `xunit.runner.visualstudio`, and `Microsoft.NET.Test.Sdk`.
- GitHub Actions `windows-latest`, `actions/checkout`, `actions/setup-dotnet`, and `actions/upload-artifact`.

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
