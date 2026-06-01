<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-01 | Updated: 2026-06-01 -->

# workflows

## Purpose
This directory contains GitHub Actions workflow YAML. The primary workflow performs the full Windows build-and-release pipeline for Dashcam Converter.

## Key Files

| File | Description |
|------|-------------|
| `ci.yml` | CI workflow for pushes and pull requests to `main`: downloads FFmpeg, generates the fixture, restores/builds/tests, publishes a self-contained Windows package, uploads an artifact, and updates release `v1.0.0` on `main`. |

## Subdirectories

This directory has no subdirectories.

## For AI Agents

### Working In This Directory
- Preserve PowerShell syntax in multi-line `run` blocks.
- Keep `DASHCAM_FFMPEG_PATH` and `PATH` setup in the test step unless test native library loading is redesigned.
- If changing release versioning, update both workflow release metadata and app version expectations where applicable.
- Do not remove fixture generation unless tests are changed to provide their own fixture.

### Testing Requirements
- Check YAML indentation and GitHub Actions expression syntax.
- For command changes, verify commands are valid on `windows-latest` PowerShell or default shell as declared.
- Ensure artifact paths match publish output and zip naming.

### Common Patterns
- FFmpeg DLLs are copied from the downloaded archive into `DashcamConverter/ffmpeg/` for embedding/publish.
- The workflow stores the downloaded FFmpeg `bin` path in `ffmpeg_bin_path.txt` for the test step.
- Release upload uses `gh release upload --clobber` when release `v1.0.0` exists; otherwise it creates the release.

## Dependencies

### Internal
- `DashcamConverter/DashcamConverter.csproj` for publish.
- `tests/fixtures/test.ts` generated before tests.
- `DashcamConverter/ffmpeg/` populated before build.

### External
- GitHub Actions runner image, setup-dotnet, artifact upload action, BtbN FFmpeg release zip, and GitHub CLI.

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
