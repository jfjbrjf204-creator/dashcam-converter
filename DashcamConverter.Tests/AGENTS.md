<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-01 | Updated: 2026-06-01 -->

# DashcamConverter.Tests

## Purpose
This directory contains xUnit tests for the core conversion facade and low-level FFmpeg wrapper. Tests focus on validating input probing, remux output creation, progress callbacks, and missing-file behavior.

## Key Files

| File | Description |
|------|-------------|
| `DashcamConverter.Tests.csproj` | `net8.0` xUnit test project referencing `DashcamConverter.Core` with `xunit`, `xunit.runner.visualstudio`, and `Microsoft.NET.Test.Sdk`. |
| `ConverterTests.cs` | Tests `Converter.ValidateInput`, `Converter.Remux`, progress callback behavior, return paths, and nonexistent-file exceptions. |
| `FfmpegTests.cs` | Tests `Ffmpeg.ProbeDuration` and `Ffmpeg.RemuxDirect` against the generated TS fixture. |

## Subdirectories

This directory has no source subdirectories. Ignore generated `bin/` and `obj/` folders.

## For AI Agents

### Working In This Directory
- Follow the existing xUnit `[Fact]` style and descriptive `Method_Scenario_ExpectedResult` test names.
- Keep Windows guards (`if (!IsWindows) return;`) for FFmpeg-dependent tests unless the project gains cross-platform native library support.
- Resolve the repository root by walking upward to `DashcamConverter.sln`, matching the existing helper pattern.
- Clean up output files in `finally` blocks so generated `.mp4` files are not left behind.

### Testing Requirements
- Fixture-dependent tests require `tests/fixtures/test.ts`; CI creates it with FFmpeg before running tests.
- On Windows, run `dotnet test -c Release` from the repository root.
- If running outside CI, ensure FFmpeg DLLs are either embedded in the app resources or available through `DASHCAM_FFMPEG_PATH`.

### Common Patterns
- Test fixture path: `Path.Combine(ProjectRoot, "tests", "fixtures", "test.ts")`.
- Output files are created under `tests/fixtures/` and deleted after assertions.
- Tests skip rather than fail on non-Windows platforms due to current native DLL packaging constraints.

## Dependencies

### Internal
- References `../DashcamConverter.Core/DashcamConverter.Core.csproj`.
- Depends on `../tests/fixtures/test.ts` for successful Windows FFmpeg tests.

### External
- xUnit 2.9.0.
- `xunit.runner.visualstudio` 2.8.2.
- `Microsoft.NET.Test.Sdk` 17.10.0.
- FFmpeg native DLLs for actual probe/remux execution.

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
