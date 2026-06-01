<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-01 | Updated: 2026-06-01 -->

# DashcamConverter.Core

## Purpose
This directory contains the core .NET 8 library that validates video inputs and remuxes them through FFmpeg's C API. It is intentionally independent of WinForms so both GUI and CLI entry points can use the same conversion facade.

## Key Files

| File | Description |
|------|-------------|
| `DashcamConverter.Core.csproj` | `net8.0` class library with nullable/implicit usings, unsafe blocks, `FFmpeg.AutoGen` dependency, and embedded FFmpeg DLL resource includes. |
| `Converter.cs` | Public facade exposing `ValidateInput` and `Remux`, plus `ConversionException` for conversion failures. |
| `Ffmpeg.cs` | Low-level FFmpeg wrapper for initialization, probing duration, direct remuxing, packet timestamp rescaling, resource cleanup, and HEVC codec ID correction. |

## Subdirectories

This directory has no source subdirectories. Ignore generated `bin/` and `obj/` folders.

## For AI Agents

### Working In This Directory
- Be careful with unsafe code and FFmpeg pointer lifetimes; every allocated/opened FFmpeg resource must be freed in `finally` or an equivalent cleanup path.
- Preserve the environment override in `Ffmpeg.Initialize`: `DASHCAM_FFMPEG_PATH` is required for CI tests with downloaded DLLs.
- Preserve embedded resource extraction fallback for packaged app execution.
- Preserve the `FixCodecIds` HEVC workaround unless replacing it with an explicitly verified equivalent; dashcam HEVC streams can present `codec_tag == 0x35363278` with missing codec IDs.
- Keep this library UI-free. It should not reference WinForms or CLI concerns.

### Testing Requirements
- Run `dotnet test -c Release` on Windows with FFmpeg DLLs available.
- When changing FFmpeg calls, verify both duration probing and remux output creation.
- When changing error paths, verify `Converter.ValidateInput`, nonexistent file behavior, and `ConversionException` handling.
- If testing in CI-like conditions, set `DASHCAM_FFMPEG_PATH` to the extracted FFmpeg `bin` directory.

### Common Patterns
- `Converter.ValidateInput` returns false for missing/unprobeable inputs.
- `Converter.Remux` defaults the output path to the input path with `.mp4` extension.
- `Ffmpeg.RemuxDirect` returns `(ExitCode, Error)` instead of throwing for most FFmpeg failures.
- Packet timestamps are rescaled from input stream time base to output stream time base before writing.
- Progress is reported as percentages through `Action<double>?` and throttled by percent changes.

## Error Codes

См. корневой `AGENTS.md#error-codification` для формата и правил. Реестр только для `DashcamConverter.Core`.

### FFMPEG — `Ffmpeg.cs`

| Code | Source | Description |
|------|--------|-------------|
| `FFMPEG-001` | `FfmpegNotFoundException` | FFmpeg DLL не найдены / не загружены |
| `FFMPEG-002` | `FfmpegException` (общий) | Ошибка операции FFmpeg (без детализации) |
| `FFMPEG-003` | `Initialize()` — catch | Ошибка извлечения DLL из ресурсов |
| `FFMPEG-010` | `ProbeDuration()` | `avformat_open_input` не удалось открыть файл |
| `FFMPEG-011` | `ProbeDuration()` | Не удалось определить длительность (`duration <= 0`) |
| `FFMPEG-012` | `ProbeDuration()` | Исключение при анализе файла |
| `FFMPEG-020` | `RemuxDirect()` | `avformat_open_input` не удалось открыть входной файл |
| `FFMPEG-021` | `RemuxDirect()` | `avformat_find_stream_info` не удалось получить информацию о потоках |
| `FFMPEG-022` | `RemuxDirect()` | `avformat_alloc_output_context2` не удалось создать выходной контекст |
| `FFMPEG-023` | `RemuxDirect()` | `avformat_new_stream` не удалось создать выходной поток |
| `FFMPEG-024` | `RemuxDirect()` | `avio_open` не удалось открыть выходной файл на запись |
| `FFMPEG-025` | `RemuxDirect()` | `avformat_write_header` не удалось записать заголовок |
| `FFMPEG-026` | `RemuxDirect()` | `av_write_trailer` не удалось завершить запись |
| `FFMPEG-027` | `RemuxDirect()` — catch `FfmpegException` | FFmpeg-специфичное исключение при remux |
| `FFMPEG-028` | `RemuxDirect()` — catch `Exception` | Общее исключение при remux |
| `FFMPEG-030` | `RemuxDirect()` — transcoding setup | Декодер не найден для кодека входного аудиопотока |
| `FFMPEG-031` | `RemuxDirect()` — transcoding setup | Не удалось выделить контекст декодера |
| `FFMPEG-032` | `RemuxDirect()` — transcoding setup | `avcodec_open2` декодера не удался |
| `FFMPEG-033` | `RemuxDirect()` — transcoding setup | Не найден MP3/MP2 энкодер |
| `FFMPEG-034` | `RemuxDirect()` — transcoding setup | Не удалось выделить контекст энкодера |
| `FFMPEG-035` | `RemuxDirect()` — transcoding setup | `avcodec_open2` энкодера не удался |
| `FFMPEG-036` | `RemuxDirect()` — transcoding setup | `swr_alloc_set_opts2` не удался |
| `FFMPEG-037` | `RemuxDirect()` — transcoding setup | `swr_init` не удался |
| `FFMPEG-038` | `RemuxDirect()` — transcoding setup | `avcodec_parameters_from_context` не удался |
| `FFMPEG-039` | `RemuxDirect()` — transcoding setup | `av_frame_alloc` для decFrame не удался |
| `FFMPEG-040` | `RemuxDirect()` — transcoding setup | `av_frame_alloc` для encFrame не удался |
| `FFMPEG-041` | `RemuxDirect()` — transcoding setup | `av_frame_get_buffer` для encFrame не удался |
| `FFMPEG-042` | `RemuxDirect()` — transcoding setup | `av_packet_alloc` для encPkt не удался |
| `FFMPEG-043` | `RemuxDirect()` — transcoding setup | `avformat_new_stream` для транскодируемого потока не удался |

### CORE — `Converter.cs`

| Code | Source | Description |
|------|--------|-------------|
| `CORE-001` | `Remux()` — `throw` | Входной файл не найден (FileNotFoundException) |
| `CORE-002` | `Remux()` — `throw` | Ошибка конвертации: FFmpeg вернул ненулевой код (ConversionException) |

## Dependencies

### Internal
- Reads FFmpeg DLL resources from `../DashcamConverter/ffmpeg/*.dll` through project resource includes.
- Used by `../DashcamConverter/` and `../DashcamConverter.Tests/`.

### External
- `FFmpeg.AutoGen` 8.1.0.
- Native FFmpeg shared DLLs: `avcodec`, `avformat`, `avutil`, `swresample`, and `swscale`.
- .NET `System.Reflection` for embedded resource extraction.

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
