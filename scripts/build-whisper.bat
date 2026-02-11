@echo off
setlocal

REM ====================================================
REM  whisper.cpp + whisper_bridge DLL 빌드 스크립트
REM  사전 요구: Visual Studio 2022 C++ 워크로드
REM ====================================================

set ROOT=%~dp0..
set BRIDGE_DIR=%ROOT%\third_party\whisper_bridge
set BUILD_DIR=%ROOT%\build\whisper_bridge
set OUTPUT_DIR=%ROOT%\app\native

REM Visual Studio 2022 개발자 환경 설정
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat"
) else (
    echo ERROR: Visual Studio 2022 not found
    exit /b 1
)

REM CMake 경로 설정 (VS2022 포함 CMake 우선, 없으면 시스템 PATH)
set CMAKE_EXE=cmake
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe" (
    set "CMAKE_EXE=C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe" (
    set "CMAKE_EXE=C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe" (
    set "CMAKE_EXE=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
)

REM CMake 확인
"%CMAKE_EXE%" --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: CMake not found. Install CMake or ensure VS2022 C++ workload is installed.
    exit /b 1
)

echo Using CMake: %CMAKE_EXE%

REM 빌드 디렉토리 생성
if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"

REM CMake 설정 + 빌드
echo === Configuring CMake ===
"%CMAKE_EXE%" -S "%BRIDGE_DIR%" -B "%BUILD_DIR%" -G "Visual Studio 17 2022" -A x64
if errorlevel 1 (
    echo CMake configure failed
    exit /b 1
)

echo === Building ===
"%CMAKE_EXE%" --build "%BUILD_DIR%" --config Release
if errorlevel 1 (
    echo Build failed
    exit /b 1
)

REM DLL 복사
echo === Copying DLLs ===
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

REM 모든 관련 DLL 찾아서 복사
for /r "%BUILD_DIR%" %%f in (whisper.dll) do (
    echo Copying %%f
    copy /y "%%f" "%OUTPUT_DIR%\" >nul
)
for /r "%BUILD_DIR%" %%f in (whisper_bridge.dll) do (
    echo Copying %%f
    copy /y "%%f" "%OUTPUT_DIR%\" >nul
)
for /r "%BUILD_DIR%" %%f in (ggml.dll) do (
    echo Copying %%f
    copy /y "%%f" "%OUTPUT_DIR%\" >nul
)
for /r "%BUILD_DIR%" %%f in (ggml-base.dll) do (
    echo Copying %%f
    copy /y "%%f" "%OUTPUT_DIR%\" >nul
)
for /r "%BUILD_DIR%" %%f in (ggml-cpu.dll) do (
    echo Copying %%f
    copy /y "%%f" "%OUTPUT_DIR%\" >nul
)

echo === Done ===
echo DLLs copied to %OUTPUT_DIR%
