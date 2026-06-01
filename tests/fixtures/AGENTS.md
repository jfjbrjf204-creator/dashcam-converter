<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-01 | Updated: 2026-06-01 -->

# fixtures

## Purpose
This directory stores generated test video files for FFmpeg integration tests. The canonical fixture is a short MPEG-TS file created by CI and consumed by the xUnit test project.

## Key Files

| File | Description |
|------|-------------|
| `test.ts` | Generated 3-second 320x240 H.264-in-MPEG-TS fixture used for duration probing and remux tests; gitignored by default. |

## Subdirectories

This directory has no subdirectories.

## For AI Agents

### Working In This Directory
- Do not assume fixture files are committed; `.gitignore` ignores `tests/fixtures/*.ts` and `tests/fixtures/*.mp4`.
- Keep generated outputs small and disposable.
- If adding permanent fixtures, revisit `.gitignore`, CI setup, and repository size implications first.

### Testing Requirements
- Generate `test.ts` before running Windows FFmpeg tests when it is missing.
- Delete temporary `.mp4` outputs produced during local test runs if tests fail before cleanup.

### Common Patterns
- CI generation command uses FFmpeg lavfi `testsrc=duration=3:size=320x240:rate=25`, encodes with `libx264`, uses `-pix_fmt yuv420p`, and writes MPEG-TS format.
- Test outputs use names such as `test-output.mp4`, `test-result.mp4`, `test-progress.mp4`, and `test-remux.mp4`.

## Dependencies

### Internal
- Referenced by `DashcamConverter.Tests` through `Path.Combine(ProjectRoot, "tests", "fixtures", "test.ts")`.

### External
- FFmpeg CLI for fixture generation.

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
