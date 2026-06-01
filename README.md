# Dashcam Converter

[![CI](https://github.com/jfjbrjf204-creator/dashcam-converter/actions/workflows/ci.yml/badge.svg)](https://github.com/jfjbrjf204-creator/dashcam-converter/actions/workflows/ci.yml)

Windows-приложение для перепаковки видео с видеорегистраторов в MP4 без перекодирования.

**Один self-contained Windows x64 package. Встроенные FFmpeg DLL. Без установки FFmpeg.**

## Что умеет

- **Remux видео без перекодирования** — быстро, без потери качества.
- **Автоперекодировка несовместимого аудио** — ADPCM и другие неподходящие аудиокодеки перекодируются в AAC для MP4 или MP3 для остальных контейнеров.
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

:: CLI с отладочным логом (stderr + файл рядом с exe)
DashcamConverter.exe remux input.ts /debug
```

### Аргументы CLI

| Команда | Описание |
|---------|----------|
| `remux <input>` | Обязательная подкоманда. `<input>` — путь к исходному видеофайлу. |
| `-o <output>` | Путь к выходному файлу. По умолчанию: `<input>.mp4` рядом с исходным. |
| `--mkv` | Сохранять в MKV вместо MP4. Форсирует расширение `.mkv`. |
| `--version` | Показать версию приложения и выйти. |
| `/debug` | Включить подробный отладочный лог. Можно указать в любом месте. |
| *(без аргументов)* | Запустить графический интерфейс (GUI). |

### Вывод и коды возврата

- **stdout** — прогресс (`Конвертация: 45%`) и результат (`Готово: output.mp4`).
- **stderr** — сообщения об ошибках и debug-лог (если включён).
- Код возврата `0` — успех, `1` — ошибка конвертации.

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

Важно: приложение делает **remux видео** (`stream copy` на уровне контейнера). Видео не перекодируется. Если исходный видеопоток несовместим с MP4, используй MKV. Несовместимое аудио может автоматически перекодироваться.

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

## FFmpeg

Этот проект использует **FFmpeg** — мощный кроссплатформенный набор инструментов для работы с мультимедиа.

- **Официальный сайт FFmpeg**: https://ffmpeg.org/
- **Исходный код FFmpeg**: https://github.com/FFmpeg/FFmpeg
- **Сборки для Windows (BtbN GPL-shared)**: https://github.com/BtbN/FFmpeg-Builds/releases

Приложение встраивает `FFmpeg.AutoGen` для P/Invoke вызовов FFmpeg C API и поставляет `avcodec`, `avformat`, `avutil`, `swresample` shared DLL внутри exe-файла.

## Лицензия

GNU General Public License v3.0 (GPL-3.0) — проект использует GPL-сборки FFmpeg shared DLL.

Полный текст лицензии — в файле [LICENSE](LICENSE).

## Сообщение об ошибках

Если конвертация завершилась с ошибкой, или приложение ведёт себя неожиданно — пожалуйста, отправьте отчёт. Это поможет быстро найти и исправить проблему.

### 1. Включите режим отладки

Запустите приложение с флагом `/debug`:

**GUI:**
```cmd
DashcamConverter.exe /debug
```
В статусной строке появится путь к лог-файлу: `Режим отладки: C:\...\dashcam_20260601_143022.log`

**CLI:**
```cmd
DashcamConverter.exe remux problem.ts /debug
```
Отладочный лог будет выведен в stderr, а также сохранён в файл рядом с exe.

### 2. Воспроизведите ошибку

Повторите те же действия, которые привели к проблеме — с включённым режимом отладки. Лог запишет каждый шаг: открытие файла, анализ потоков, процесс remux и ошибки FFmpeg.

### 3. Соберите лог

Лог-файл находится в папке с `DashcamConverter.exe` и называется так:

```
dashcam_ГГГГММДД_ЧЧММСС.log
```

Пример: `dashcam_20260601_143022.log`

### 4. Создайте issue

Перейдите по ссылке и нажмите **New Issue**:

https://github.com/jfjbrjf204-creator/dashcam-converter/issues

Обязательно укажите:

- **Версию приложения** — показывается в шапке GUI (`v0.0.11`) или через `--version`.
- **Действия** — что именно вы делали перед ошибкой.
- **Код ошибки** — если в сообщении был код (например, `[FFMPEG-020]` или `[CORE-CONV-001]`).
- **Файл** — какой видеофайл конвертировали (формат, размер, модель регистратора).
- **Лог-файл** — прикрепите `dashcam_*.log` к issue.

### Пример хорошего баг-репорта

```
Версия: 0.0.11

Файл: видео с регистратора Xiaomi 70mai, формат TS, 200 МБ.
Действия: выбрал файл в GUI, папка сохранения — D:\Video.
Нажал «Начать конвертацию» — ошибка сразу после старта.

Ошибка: [FFMPEG-020] avformat_open_input: ...

Лог: прикрепил dashcam_20260601_143022.log
```

### Коды ошибок

Если в сообщении есть код — это ускоряет диагностику. Наиболее частые:

| Код | Значение |
|-----|----------|
| `FFMPEG-001` | FFmpeg DLL не найдены при запуске |
| `FFMPEG-020` | Не удалось открыть входной файл (битый или неподдерживаемый формат) |
| `FFMPEG-021` | Не удалось прочитать информацию о потоках (файл повреждён) |
| `FFMPEG-024` | Не удалось создать выходной файл (нет прав на запись в папку) |
| `CORE-VAL-001` | Входной файл не найден на диске |
| `CORE-CONV-001` | Ошибка конвертации (FFmpeg вернул ошибку) |
| `GUI-001` | FFmpeg не инициализировался при запуске GUI |

С полным списком можно ознакомиться в [`ARCHITECTURE.md`](ARCHITECTURE.md) на уровне кода.

---

*Проект разработан в помощь председателю СНТ Горьковское Медведеву.*
