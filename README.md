# Dashcam Converter

[![CI](https://github.com/z311/dashcam-converter/actions/workflows/ci.yml/badge.svg)](https://github.com/z311/dashcam-converter/actions/workflows/ci.yml)

Конвертер видео с видеорегистраторов в читаемый формат MP4.  
**Один .exe-файл. Встроенный FFmpeg. Никаких зависимостей.**

## Особенности

- **0 subprocess** — FFmpeg через P/Invoke (FFmpeg.AutoGen), без `Process.Start`
- **Single-file .exe** — всё внутри (~70 MB), FFmpeg DLL распаковываются в `%TEMP%` при первом запуске
- **GUI + CLI** — drag-and-drop окно и командная строка
- **Нестандартные кодеки** — поддержка x265 в AVI (автоопределение fourcc)
- **Прогресс-бар** — процент конвертации в реальном времени

## Быстрый старт

1. Скачать `DashcamConverter.exe` из [релизов](https://github.com/z311/dashcam-converter/releases)
2. Запустить (Windows SmartScreen → «Подробнее → Выполнить»)
3. Добавить видеофайлы → **Конвертировать**

## CLI

```cmd
# Конвертировать один файл (выход рядом с исходным)
DashcamConverter.exe remux input.avi

# Указать выходной файл
DashcamConverter.exe remux input.avi -o output.mp4

# Версия
DashcamConverter.exe --version
```

Без аргументов — запускается GUI.

## Поддерживаемые форматы

**Вход:** TS, MOV, AVI (включая H.265/x265), MP4, MKV, WMV, FLV, WebM, MTS, M2TS, VOB, 3GP  
**Выход:** MP4 (remux без перекодирования)

## Сборка из исходников

Требования:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- FFmpeg shared DLL (скачиваются автоматически)

```cmd
build.bat
```

Или вручную:

```cmd
# 1. Скачать FFmpeg DLL
curl -L -o ffmpeg.zip "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl-shared.zip"
tar -xf ffmpeg.zip
copy ffmpeg-*\bin\av*.dll DashcamConverter\ffmpeg\
copy ffmpeg-*\bin\sw*.dll DashcamConverter\ffmpeg\

# 2. Тесты
dotnet test

# 3. Сборка .exe
dotnet publish DashcamConverter/DashcamConverter.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Выход: `DashcamConverter/bin/Release/net8.0-windows/win-x64/publish/DashcamConverter.exe`

## Архитектура

```
DashcamConverter.sln
├── DashcamConverter/          # WinForms GUI + CLI (net8.0-windows)
│   ├── Program.cs             # Точка входа: CLI / GUI
│   └── MainForm.cs            # WinForms-окно
├── DashcamConverter.Core/     # Бизнес-логика (net8.0, кроссплатформа)
│   ├── Ffmpeg.cs              # P/Invoke FFmpeg: RemuxDirect, ProbeDuration, FixCodecIds
│   └── Converter.cs           # Фасад: ValidateInput, Remux
└── DashcamConverter.Tests/    # xUnit-тесты
    ├── FfmpegTests.cs
    └── ConverterTests.cs
```

**Принципы:** SSOT (Single Source of Truth), SOLID, DRY.

## CI/CD

- **CI** — билд + тесты на каждом push и PR (windows-latest)
- **Release** — при пуше тега `v*`: тесты → сборка .exe → GitHub Release

## Лицензия

MIT
