@echo off
echo === Dashcam Converter v1.0 — Build (.NET 8, P/Invoke FFmpeg) ===
echo.

dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK not found. Install from https://dotnet.microsoft.com
    pause
    exit /b 1
)

echo [0/4] Downloading FFmpeg shared DLLs...
if not exist "DashcamConverter\ffmpeg\avcodec-*.dll" (
    curl -L -o ffmpeg.zip "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl-shared.zip"
    if errorlevel 1 (
        echo [ERROR] Failed to download FFmpeg
        pause
        exit /b 1
    )
    tar -xf ffmpeg.zip
    if not exist "DashcamConverter\ffmpeg" mkdir "DashcamConverter\ffmpeg"
    for /d %%d in (ffmpeg-*) do (
        copy "%%d\bin\avcodec-*.dll" "DashcamConverter\ffmpeg\" >nul
        copy "%%d\bin\avformat-*.dll" "DashcamConverter\ffmpeg\" >nul
        copy "%%d\bin\avutil-*.dll" "DashcamConverter\ffmpeg\" >nul
        copy "%%d\bin\swresample-*.dll" "DashcamConverter\ffmpeg\" >nul
        copy "%%d\bin\swscale-*.dll" "DashcamConverter\ffmpeg\" >nul
        rmdir /s /q "%%d"
    )
    del ffmpeg.zip
) else (
    echo   FFmpeg DLLs already present.
)

echo [1/4] Restoring packages...
dotnet restore

echo [2/4] Running tests...
dotnet test

if errorlevel 1 (
    echo [WARNING] Some tests failed, continuing build...
)

echo [3/4] Publishing single-file .exe...
dotnet publish DashcamConverter\DashcamConverter.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true

if errorlevel 1 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)

echo [4/4] Build complete!
echo.
echo Output: DashcamConverter\bin\Release\net8.0-windows\win-x64\publish\DashcamConverter.exe
pause
