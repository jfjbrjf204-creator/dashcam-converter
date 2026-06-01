# Dashcam Converter

[![CI](https://github.com/jfjbrjf204-creator/dashcam-converter/actions/workflows/ci.yml/badge.svg)](https://github.com/jfjbrjf204-creator/dashcam-converter/actions/workflows/ci.yml)

Windows-приложение для перепаковки видео с видеорегистраторов в MP4 без перекодирования.

**Один self-contained Windows x64 package. Встроенные FFmpeg DLL. Без установки FFmpeg.**

## Что умеет

- **Remux без перекодирования** — быстро, без потери качества.
- **WinForms GUI** — выбрать несколько видеофайлов и папку сохранения.
- **CLI-режим** — запуск из командной строки.
- **FFmpeg через C API** — `FFmpeg.AutoGen` + P/Invoke, без запуска `ffmpeg.exe` как subprocess.
- **Встроенные FFmpeg shared DLL** — DLL извлекаются из ресурсов приложения во временную папку при запуске.
- **Прогресс конвертации** — callback из C#-логики в GUI/CLI.

## Скачать

Скачай последний release:

https://github.com/jfjbrjf204-creator/dashcam-converter/releases/latest

Файл:

- `DashcamConverter-win-x64.zip`

Как запустить:

1. Скачать zip из Releases.
2. Распаковать архив.
3. Запустить `DashcamConverter.exe`.
4. Если Windows SmartScreen предупреждает: **Подробнее → Выполнить**.

## CLI

```cmd
:: Открыть GUI
DashcamConverter.exe

:: Конвертировать один файл, выход рядом с исходным
DashcamConverter.exe remux input.ts

:: Указать выходной файл
DashcamConverter.exe remux input.ts -o output.mp4

:: Сохранить как MKV
DashcamConverter.exe remux input.ts --mkv

:: Версия
DashcamConverter.exe --version
```

## Поддерживаемые форматы

Входные форматы зависят от FFmpeg. Основные проверяемые сценарии:

- TS / MTS / M2TS
- AVI
- MOV
- MP4
- MKV

Выход:

- MP4 по умолчанию
- MKV через `--mkv`

Важно: приложение делает **remux** (`stream copy` на уровне контейнера), а не перекодирование. Если исходный видеопоток несовместим с MP4, используй MKV.

## Сборка из исходников

Требования:

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- `curl` и `tar` в PATH для `build.bat`

Быстрая сборка:

```cmd
build.bat
```

Вручную:

```cmd
:: 1. Скачать FFmpeg shared build
curl -L -o ffmpeg.zip "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl-shared.zip"
tar -xf ffmpeg.zip

:: 2. Скопировать DLL в ресурсы приложения
mkdir DashcamConverter\ffmpeg
copy ffmpeg-*\bin\*.dll DashcamConverter\ffmpeg\

:: 3. Restore / build / test
dotnet restore
dotnet build -c Release
dotnet test -c Release

:: 4. Publish self-contained Windows x64 build
dotnet publish DashcamConverter\DashcamConverter.csproj -c Release -r win-x64 --self-contained true -o publish
```

Выходной файл:

```text
publish\DashcamConverter.exe
```

## Структура проекта

```text
DashcamConverter.sln
├── DashcamConverter/          # WinForms GUI + CLI, net8.0-windows
│   ├── Program.cs             # Точка входа: GUI / CLI
│   ├── MainForm.cs            # WinForms UI
│   └── ffmpeg/                # FFmpeg DLL, добавляются при сборке/CI
├── DashcamConverter.Core/     # C# core-логика, net8.0
│   ├── Ffmpeg.cs              # P/Invoke FFmpeg C API
│   └── Converter.cs           # Фасад ValidateInput / Remux
├── DashcamConverter.Tests/    # xUnit-тесты
├── .github/workflows/ci.yml   # CI, publish zip, GitHub Release
└── build.bat                  # Локальная Windows-сборка
```

## CI/CD

Workflow `.github/workflows/ci.yml` на `windows-latest`:

1. Скачивает FFmpeg shared build.
2. Создаёт тестовую видеофикстуру.
3. Выполняет `dotnet restore`, `dotnet build`, `dotnet test`.
4. Публикует Windows x64 self-contained build.
5. Упаковывает `DashcamConverter-win-x64.zip`.
6. Загружает zip как Actions artifact.
7. На `main` обновляет GitHub Release `v1.0.0` этим zip-файлом.

## Платформы

- Готовое приложение: **Windows x64**.
- Core-проект (`DashcamConverter.Core`) таргетит `net8.0`, но текущая поставка и загрузка FFmpeg DLL настроены под Windows.
- Linux-сборка потребует отдельного CLI/GUI проекта и загрузки `libav*.so` вместо Windows DLL.

## Лицензия

MIT

---

*Проект разработан в помощь председателю СНТ Горьковское Медведеву.*
