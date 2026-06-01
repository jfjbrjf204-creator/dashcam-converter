# Dashcam Converter v1.0 — Implementation Plan

> **For Hermes:** Использовать OpenCode на «звёздной» для реализации.
> Файлы создаются в `/home/z311/dashcam-converter/`.

**Goal:** Single .exe файл, который конвертирует видео с видеорегистратора в MP4 без потери качества.

**Scope v1.0:** Только remux (перепаковка) в H.264 MP4. Без оверлеев, без склейки, без перекодирования.

**Tech Stack:** Python 3.11+, FFmpeg (subprocess), tkinter, PyInstaller.

---

## Task 1: Инициализация и заглушки

**Objective:** Создать `__init__.py`, начальные заглушки модулей.

**Files:**
- `src/__init__.py` — пустой
- `tests/__init__.py` — пустой  
- `src/main.py` — заглушка с `__version__ = "1.0.0"`
- `src/ffmpeg.py` — заглушка с классом `FFmpegError(Exception)`
- `src/converter.py` — заглушка с `class ConversionError(Exception)`
- `src/gui.py` — заглушка функции `main()`

---

## Task 2: ffmpeg.py — SSOT для вызовов FFmpeg

**Objective:** Единственный модуль, запускающий FFmpeg subprocess. Вся остальная кодовая база использует только его.

**Files:**
- `src/ffmpeg.py`
- `tests/test_ffmpeg.py`

**API:**
```python
class FFmpegError(Exception):
    """Любая ошибка FFmpeg."""
    def __init__(self, message: str, stderr: str = ""): ...

class FFmpegNotFoundError(FFmpegError):
    """FFmpeg не найден."""

def find_ffmpeg() -> str:
    """Найти путь к ffmpeg.exe.
    Приоритет:
    1. Переменная окружения FFMPEG_PATH
    2. sys._MEIPASS/ffmpeg/ffmpeg.exe (PyInstaller bundle)
    3. ffmpeg/ffmpeg.exe рядом с .exe
    4. ffmpeg в PATH
    """

def run(args: list[str], on_progress=None, timeout: int = 300) -> tuple[int, str]:
    """Запустить FFmpeg, вернуть (returncode, stderr).
    
    args: список аргументов (без 'ffmpeg' в начале)
    on_progress(percent: float): опциональный callback
    timeout: секунд до принудительного завершения
    
    Парсит stderr для извлечения прогресса (time=...).
    """
```

**Прогресс-парсинг:**
- Регулярка: `time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})`
- Вычисление процента: `current_time / total_duration * 100`

---

## Task 3: converter.py — remux

**Objective:** Единственная операция v1.0 — перепаковка в MP4 без перекодирования.

**Files:**
- `src/converter.py`
- `tests/test_converter.py`

**API:**
```python
class ConversionError(Exception): ...

def remux(input_path: str, output_path: str = None,
          on_progress=None) -> str:
    """Перепаковать видео в MP4 без перекодирования.
    
    Args:
        input_path: путь к исходному файлу
        output_path: путь к выходному .mp4 (если None — input_path с .mp4)
        on_progress(percent: float): опциональный callback
    
    Returns:
        Путь к выходному файлу
    
    Raises:
        ConversionError: если FFmpeg вернул ошибку
    """
```

**Команда FFmpeg:**
```
ffmpeg -i input.ts -c copy -movflags +faststart -y output.mp4
```
- `-c copy` — копировать потоки без перекодирования
- `-movflags +faststart` — переместить moov atom в начало (для веб-плееров)
- `-y` — перезаписать без вопроса

---

## Task 4: main.py — CLI

**Objective:** Точка входа. Без аргументов — GUI, с `remux` — CLI-конвертация.

**Files:**
- `src/main.py`

**CLI:**
```cmd
DashcamConverter.exe                           # Запуск GUI
DashcamConverter.exe remux <input> [-o output] # CLI-конвертация
DashcamConverter.exe --version                 # Версия
DashcamConverter.exe --help                    # Справка
```

---

## Task 5: gui.py — GUI

**Objective:** Окно tkinter: выбор файлов, прогресс-бар, кнопка «Конвертировать».

**Files:**
- `src/gui.py`

**Дизайн:**
```
┌────────────────────────────────────┐
│  Dashcam Converter v1.0            │
├────────────────────────────────────┤
│  Исходные файлы:                   │
│  ┌──────────────────────────────┐  │
│  │ C:\DCIM\FILE0001.TS          │  │
│  │ C:\DCIM\FILE0002.TS          │  │
│  └──────────────────────────────┘  │
│  [➕ Добавить файлы]  [✕ Очистить] │
│                                    │
│  Папка сохранения:                 │
│  [________________________] [📁]   │
│                                    │
│  ┌──────────────────────────────┐  │
│  │ ████████░░░░░░░░ 45%        │  │
│  └──────────────────────────────┘  │
│  Статус: Конвертация FILE0001...  │
│                                    │
│         [▶ Конвертировать]         │
└────────────────────────────────────┘
```

**Ключевые моменты:**
- `filedialog.askopenfilenames()` для выбора файлов
- `filedialog.askdirectory()` для выбора папки сохранения
- Конвертация в отдельном потоке (`threading.Thread`)
- `progressbar` из `ttk` для прогресса
- `root.after(100, check_progress)` для обновления GUI из потока

---

## Task 6: build.bat — сборка .exe

**Objective:** PyInstaller-сборка в один .exe с встроенным FFmpeg.

**Аргументы PyInstaller:**
```
--onefile          → один .exe
--noconsole        → без консольного окна (для GUI)
--add-binary       → встроить ffmpeg.exe
--name             → DashcamConverter
```

**Примечание:** ffmpeg.exe (~80MB) должен быть скачан отдельно (FFmpeg Windows Build от gyan.dev) и положен в `ffmpeg/ffmpeg.exe` перед сборкой.

---

## Task 7: Тесты

**Objective:** Покрыть тестами ffmpeg.py и converter.py.

**Тест-фикстура:** Создать тестовое видео через FFmpeg:
```python
# test_fixture: 1-секундное видео с тестовым паттерном
subprocess.run(["ffmpeg", "-f", "lavfi", "-i", "testsrc=duration=1:size=320x240",
                "-f", "mpegts", "tests/fixtures/test.ts"])
```

---

## Task 8: .gitignore и финализация

**Objective:** Финальный .gitignore, проверка структуры.

---

## Принципы (DRY, SSOT, SOLID)

| Принцип | Как соблюдается |
|---|---|
| **DRY** | `ffmpeg.run()` — единственная функция, вызывающая subprocess |
| **SSOT** | Путь к ffmpeg → `ffmpeg.find_ffmpeg()`, версия → `main.__version__` |
| **SRP** | ffmpeg.py — subprocess; converter.py — логика; gui.py — UI; main.py — роутинг |
| **OCP** | Новые операции — новые функции в converter.py, не трогая ffmpeg.py |
| **ISP** | main.py импортирует gui только при запуске GUI; CLI-запуск не тянет tkinter |
| **DIP** | converter зависит от интерфейса `ffmpeg.run()`, не от конкретного пути |
