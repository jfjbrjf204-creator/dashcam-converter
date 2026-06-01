# Dashcam Converter — архитектура

Текущая версия — C#/.NET 8 Windows-приложение с WinForms UI и core-библиотекой для remux через FFmpeg C API.

## Технический стек

- **Язык:** C# / .NET 8
- **GUI:** Windows Forms (`net8.0-windows`)
- **Core:** `.NET 8` class library
- **FFmpeg:** shared DLL + `FFmpeg.AutoGen` P/Invoke
- **Тесты:** xUnit
- **CI:** GitHub Actions `windows-latest`
- **Поставка:** self-contained Windows x64 zip в GitHub Releases

## Структура

```text
DashcamConverter.sln
├── DashcamConverter/
│   ├── DashcamConverter.csproj     # WinForms app, net8.0-windows, win-x64
│   ├── Program.cs                  # CLI/GUI entrypoint
│   ├── MainForm.cs                 # WinForms UI
│   └── ffmpeg/                     # FFmpeg DLL copied before build/publish
├── DashcamConverter.Core/
│   ├── DashcamConverter.Core.csproj# net8.0, unsafe enabled, FFmpeg.AutoGen
│   ├── Ffmpeg.cs                   # FFmpeg C API wrapper
│   └── Converter.cs                # Public conversion facade
├── DashcamConverter.Tests/
│   ├── FfmpegTests.cs              # FFmpeg wrapper tests
│   └── ConverterTests.cs           # Converter facade tests
├── .github/workflows/ci.yml        # Build/test/package/release
└── build.bat                       # Local Windows build script
```

## Компоненты

### `DashcamConverter`

Windows-приложение:

- без аргументов запускает WinForms GUI;
- `remux <input> [-o output] [--mkv]` запускает CLI-конвертацию;
- `--version` печатает версию.

### `DashcamConverter.Core`

Core-библиотека:

- `Converter.ValidateInput(path)` — проверяет существование файла и возможность открыть его через FFmpeg;
- `Converter.Remux(input, output, onProgress)` — фасад для remux;
- `Ffmpeg.Initialize()` — настраивает путь к FFmpeg DLL;
- `Ffmpeg.ProbeDuration(path)` — читает длительность через FFmpeg C API;
- `Ffmpeg.RemuxDirect(...)` — перепаковывает контейнер без перекодирования.

### `DashcamConverter.Tests`

xUnit-тесты создают/используют тестовую TS-фикстуру и проверяют:

- probe длительности;
- remux в выходной файл;
- progress callback;
- обработку отсутствующих файлов.

## FFmpeg loading

Порядок инициализации в `Ffmpeg.Initialize()`:

1. Если задана переменная `DASHCAM_FFMPEG_PATH`, используется этот каталог DLL. Это нужно для CI-тестов.
2. Иначе embedded DLL извлекаются из ресурсов сборки в `%TEMP%\DashcamConverter\ffmpeg`.
3. `ffmpeg.RootPath` указывает на выбранный каталог.

DLL добавляются в ресурсы через `DashcamConverter.Core.csproj`:

```xml
<EmbeddedResource Include="..\DashcamConverter\ffmpeg\avcodec-*.dll" />
<EmbeddedResource Include="..\DashcamConverter\ffmpeg\avformat-*.dll" />
<EmbeddedResource Include="..\DashcamConverter\ffmpeg\avutil-*.dll" />
<EmbeddedResource Include="..\DashcamConverter\ffmpeg\swresample-*.dll" />
<EmbeddedResource Include="..\DashcamConverter\ffmpeg\swscale-*.dll" />
```

## Поток данных

```text
GUI/CLI
  → Converter.Remux()
    → Ffmpeg.RemuxDirect()
      → avformat_open_input
      → avformat_find_stream_info
      → avformat_alloc_output_context2
      → avformat_write_header
      → av_read_frame / av_interleaved_write_frame
      → av_write_trailer
  → output .mp4/.mkv
```

## CI/CD

`.github/workflows/ci.yml`:

1. Checkout.
2. Setup .NET 8.
3. Download FFmpeg shared build from BtbN.
4. Copy FFmpeg DLL into `DashcamConverter/ffmpeg`.
5. Generate test fixture `tests/fixtures/test.ts`.
6. Restore/build/test.
7. Publish Windows x64 self-contained build.
8. Zip publish output into `DashcamConverter-win-x64.zip`.
9. Upload Actions artifact.
10. On `main`, create/update GitHub Release `v1.0.0` with the zip asset.

## Платформенные ограничения

- GUI-проект — Windows-only (`net8.0-windows`, WinForms).
- Published package — `win-x64`.
- Core таргетит `net8.0`, но текущая реализация загрузки нативных библиотек и embedded resources настроена под Windows DLL.
- Для Linux нужен отдельный entrypoint и загрузка `libav*.so`.
