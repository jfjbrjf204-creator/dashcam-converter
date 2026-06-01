# Dashcam Converter — Архитектура

> Принципы: **DRY, SSOT, SOLID**. Разработка через OpenCode на «звёздной».

## Технический стек

- **Язык:** Python 3.11+ (встроен в PyInstaller .exe)
- **GUI:** tkinter (встроен в Python, не требует внешних библиотек)
- **Видео:** FFmpeg (subprocess, бинарник встроен в .exe)
- **Сборка:** PyInstaller → один .exe файл
- **Тесты:** pytest

## Структура проекта

```
dashcam-converter/
├── src/
│   ├── __init__.py
│   ├── main.py          # Точка входа: парсинг аргументов → выбор режима
│   ├── converter.py     # Бизнес-логика: remux-операции
│   ├── ffmpeg.py        # SSOT для вызовов FFmpeg (единственное место!)
│   └── gui.py           # tkinter GUI
├── tests/
│   ├── __init__.py
│   ├── test_converter.py
│   └── test_ffmpeg.py
├── ffmpeg/              # Бинарник ffmpeg.exe (встраивается в сборку)
├── README.md
├── ARCHITECTURE.md      # Этот файл
├── requirements.txt
├── build.bat            # Сборка .exe
└── .gitignore
```

## SOLID

### S — Single Responsibility
- `ffmpeg.py` — только запуск и управление процессом FFmpeg
- `converter.py` — только логика конвертации (какие аргументы передать)
- `gui.py` — только GUI
- `main.py` — только точка входа и роутинг

### O — Open/Closed
- Новые операции конвертации добавляются через новые функции в `converter.py`, не трогая `ffmpeg.py`
- Новые форматы вывода — через параметризацию, не через if/else

### L — Liskov Substitution
- Не применимо явно (нет наследования в v1)

### I — Interface Segregation
- CLI-клиент не зависит от tkinter (main.py импортирует gui только при `--gui`)
- GUI не знает о CLI-аргументах

### D — Dependency Inversion
- `converter.py` зависит от абстракции (интерфейс `ffmpeg.run()`), не от конкретного пути к бинарнику

## DRY

Единственная функция, запускающая FFmpeg — `ffmpeg.run(args, progress_callback)`.
Все операции (remux, будущие overlay, concat) используют только её.

```python
# ffmpeg.py — SSOT для всех вызовов FFmpeg
def run(args: list[str], on_progress=None) -> tuple[int, str]:
    """Запустить FFmpeg, вернуть (returncode, stderr).
    on_progress(percent: float) — опциональный callback."""
    ...
```

## SSOT (Single Source of Truth)

| Данные | Источник |
|---|---|
| Путь к ffmpeg.exe | `ffmpeg.py` → `_find_ffmpeg()` |
| Формат выходного файла | `converter.py` → константа `OUTPUT_EXT = ".mp4"` |
| Версия приложения | `main.py` → `__version__` |
| Настройки GUI (размеры, шрифты) | `gui.py` → `UIConfig` |

## Поток данных

```
Пользователь
    │
    ├── CLI: main.py → converter.remux() → ffmpeg.run() → subprocess.Popen
    │
    └── GUI: gui.py → converter.remux() → ffmpeg.run() → subprocess.Popen
                                          ↑
                                     ffmpeg.exe
                                          │
                                     Выходной MP4
```

## Сборка .exe

```cmd
pyinstaller --onefile --windowed ^
    --add-binary "ffmpeg/ffmpeg.exe;ffmpeg" ^
    --name "DashcamConverter" ^
    src/main.py
```

Результат: один файл `DashcamConverter.exe` (~90-100 MB, из которых ~80 MB — ffmpeg).

При запуске PyInstaller распаковывает ffmpeg.exe во временную директорию.
`ffmpeg.py` находит его через `sys._MEIPASS`.

## Этапы реализации (через OpenCode)

1. Инициализация проекта: структура, requirements.txt, .gitignore
2. `ffmpeg.py` — SSOT-обёртка над FFmpeg subprocess
3. `converter.py` — функция `remux(input, output)`
4. `main.py` — CLI с argparse
5. `gui.py` — окно с drag-and-drop и прогресс-баром
6. `build.bat` — сборка PyInstaller
7. Тесты + фикстуры
8. CI/интеграция на «звёздной»
