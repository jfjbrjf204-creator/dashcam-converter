<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-01 | Updated: 2026-06-01 -->

# .github

## Purpose
This directory contains repository automation configuration. The current automation is a GitHub Actions workflow that builds, tests, packages, uploads artifacts, and publishes a Windows release.

## Key Files

There are no files directly in this directory besides subdirectories.

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `workflows/` | GitHub Actions workflow definitions (see `workflows/AGENTS.md`). |

## For AI Agents

### Working In This Directory
- Keep automation changes scoped to workflows and GitHub metadata.
- Be careful with release permissions; the workflow uses `contents: write` to create/update releases.
- Preserve the Windows runner unless the native FFmpeg DLL packaging/test approach becomes cross-platform.

### Testing Requirements
- Validate workflow YAML syntax after edits.
- For CI changes, reason through the full order: setup .NET, download FFmpeg, create fixture, restore, build, test, publish, upload artifact, release.
- If changing release behavior, verify branch/tag conditions and artifact paths.

### Common Patterns
- CI runs on `push` and `pull_request` targeting `main`.
- Release publishing only happens on `refs/heads/main`.
- FFmpeg is downloaded from BtbN shared GPL builds during the workflow.

## Dependencies

### Internal
- Uses solution/project files at the repository root and under `DashcamConverter/`.
- Copies FFmpeg DLLs into `DashcamConverter/ffmpeg/` before build/publish.
- Generates `tests/fixtures/test.ts` before running tests.

### External
- GitHub Actions hosted `windows-latest` runner.
- `actions/checkout@v4`, `actions/setup-dotnet@v4`, and `actions/upload-artifact@v4`.
- GitHub CLI `gh` available in the runner for release operations.
- BtbN FFmpeg shared builds.

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
