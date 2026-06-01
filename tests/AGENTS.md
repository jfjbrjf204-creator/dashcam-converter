<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-01 | Updated: 2026-06-01 -->

# tests

## Purpose
This directory holds non-project test assets used by the xUnit suite and CI pipeline. It is not a C# test project; the actual test code lives in `DashcamConverter.Tests/`.

## Key Files

There are no files directly in this directory.

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `fixtures/` | Generated media fixtures consumed by FFmpeg and converter tests (see `fixtures/AGENTS.md`). |

## For AI Agents

### Working In This Directory
- Treat this area as test data only. Do not place production source code here.
- Fixture media files are generated and gitignored; avoid committing large binary/media outputs unless explicitly required.
- Coordinate fixture path changes with `DashcamConverter.Tests` and `.github/workflows/ci.yml`.

### Testing Requirements
- If fixture naming or layout changes, update both test project path helpers and CI fixture generation.
- Regenerate fixtures with FFmpeg when test media content requirements change.

### Common Patterns
- CI creates `tests/fixtures/test.ts` from FFmpeg `testsrc` using H.264 video in an MPEG-TS container.
- Tests create temporary output MP4 files in the same fixture directory and delete them in `finally` blocks.

## Dependencies

### Internal
- Used by `DashcamConverter.Tests/ConverterTests.cs` and `DashcamConverter.Tests/FfmpegTests.cs`.
- Populated by `.github/workflows/ci.yml` before `dotnet test`.

### External
- FFmpeg executable is used in CI to generate the fixture.

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
