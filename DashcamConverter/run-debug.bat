@echo off
REM === Dashcam Converter — запуск в режиме отладки ===
REM Открыть GUI с выводом отладочного лога в файл
REM Лог сохраняется рядом с exe: dashcam_YYYYmmdd_HHmmss.log
REM
REM Для CLI-режима с отладкой:
REM   DashcamConverter.exe remux входной_файл /debug

echo Запуск Dashcam Converter в режиме отладки...
start "" "%~dp0DashcamConverter.exe" /debug
