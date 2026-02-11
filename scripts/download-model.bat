@echo off
setlocal

set MODELS_DIR=%~dp0..\models
if not exist "%MODELS_DIR%" mkdir "%MODELS_DIR%"

if exist "%MODELS_DIR%\ggml-base.bin" (
    echo Model already exists: %MODELS_DIR%\ggml-base.bin
    exit /b 0
)

echo Downloading ggml-base.bin (multilingual, ~148MB)...
curl -L -o "%MODELS_DIR%\ggml-base.bin" https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin

if errorlevel 1 (
    echo Download failed.
    exit /b 1
)

echo Done: %MODELS_DIR%\ggml-base.bin
