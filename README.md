# Oh My Whisper

Windows용 Push-to-talk 음성 전사 오버레이 앱. 단축키를 누르고 있는 동안 마이크를 녹음하고, 커서 근처 오버레이 창에 실시간으로 전사 결과를 표시한다. 키를 떼면 최종 전사 후 텍스트를 편집하고 클립보드에 복사할 수 있다.

> [English](#english) version below.

## 주요 기능

- **Push-to-talk**: `Ctrl+Shift+Space` 누르고 있는 동안만 녹음
- **실시간 부분 전사**: 녹음 중 ~1.5초 간격으로 오버레이 텍스트 갱신
- **커서 근처 오버레이**: 항상 위(Topmost), DPI 인식, 화면 밖 clamp
- **편집 모드**: 키를 떼면 최종 전사 → 텍스트 수정 → Copy 버튼으로 클립보드 복사
- **트레이 아이콘**: 한국어/영어 전환, 단축키 활성화/비활성화, 종료
- **CPU-only**: whisper.cpp 기반, GPU 불필요

## 기술 스택

| 구성 요소 | 기술 |
|-----------|------|
| 앱 프레임워크 | .NET 8 WPF |
| 전역 키 훅 | `WH_KEYBOARD_LL` (P/Invoke) |
| 오디오 캡처 | WASAPI (NAudio) |
| 전사 엔진 | whisper.cpp (git submodule) |
| 엔진 바인딩 | C 브릿지 DLL → C# P/Invoke |
| 트레이 | Hardcodet.NotifyIcon.Wpf |

## 사전 요구사항

- Windows 10/11 (x64)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Whisper GGML 모델 파일 (아래 스크립트로 다운로드)

> 네이티브 DLL은 리포에 포함되어 있어 별도 C++ 빌드가 필요 없습니다. 직접 빌드하려면 [Visual Studio 2022](https://visualstudio.microsoft.com/) C++ 워크로드가 필요합니다.

## 빌드 및 실행

### 1. 리포 클론

```bash
git clone --recursive https://github.com/junjunjunbong/oh-my-whisper.git
cd oh-my-whisper
```

### 2. 모델 다운로드

```bash
scripts\download-model.bat
```

[Hugging Face](https://huggingface.co/ggerganov/whisper.cpp)에서 `ggml-base.bin` (~148MB, 다국어)을 `models/` 디렉토리에 다운로드한다.

> 정확도가 부족하면 `ggml-small.bin` (~466MB)으로 교체할 수 있다.

### 3. 실행

```bash
run.bat
```

끝. 재부팅 후에도 `run.bat` 더블클릭이면 바로 실행된다.

## 사용법

1. 앱 실행 → 트레이 아이콘으로 백그라운드 대기
2. **`Ctrl+Shift+Space`** 누르기 → 커서 근처에 오버레이 표시 + 녹음 시작
3. 말하는 동안 ~1.5초마다 부분 전사 결과 갱신
4. 키 떼기 → 최종 전사 → 편집 모드 (텍스트 수정 가능)
5. **Copy** 버튼 → 클립보드 복사 / **Esc** → 오버레이 닫기

### 트레이 메뉴 (우클릭)

- **Enable/Disable Hotkey** — 단축키 토글
- **한국어 / English** — 전사 언어 전환
- **Quit** — 앱 종료

## 프로젝트 구조

```
oh-my-whisper/
├── run.bat                        # 앱 실행 (더블클릭)
├── app/                           # .NET 8 WPF 앱
│   ├── native/                    # 빌드 완료된 네이티브 DLL
│   ├── Hotkey/                    # 전역 키보드 훅 (Ctrl+Shift+Space)
│   ├── Audio/                     # WASAPI 캡처, 리샘플링, 링버퍼
│   ├── Transcription/             # 브릿지 DLL P/Invoke, 추론 서비스
│   ├── UI/                        # 오버레이 창, 트레이, NativeInterop
│   └── StateMachine/              # 상태 enum (Idle/Recording/Finalizing/Editing)
├── third_party/
│   ├── whisper.cpp/               # git submodule (whisper.cpp)
│   └── whisper_bridge/            # C 브릿지 DLL 소스
├── scripts/
│   ├── download-model.bat         # 모델 다운로드
│   └── build-whisper.bat          # 네이티브 DLL 재빌드 (선택)
├── models/                        # GGML 모델 파일 (gitignored)
└── plan.md                        # 설계 문서
```

## 상태 머신

```
Idle ──(Ctrl+Shift+Space Down)──▶ Recording ──(Up)──▶ Finalizing ──▶ Editing ──(Esc/Copy)──▶ Idle
```

## 라이선스

MIT

---

<a id="english"></a>

# Oh My Whisper (English)

A Windows push-to-talk speech-to-text overlay app. Hold the hotkey to record from your microphone, and a small overlay window near the cursor shows live transcription. Release the key to finalize, edit the text, and copy it to the clipboard.

## Features

- **Push-to-talk**: Records only while `Ctrl+Shift+Space` is held
- **Live partial transcription**: Overlay text updates every ~1.5s during recording
- **Cursor-relative overlay**: Always-on-top, DPI-aware, clamped to screen bounds
- **Edit mode**: Release key → final transcription → edit text → Copy to clipboard
- **System tray**: Toggle Korean/English, enable/disable hotkey, quit
- **CPU-only**: Powered by whisper.cpp, no GPU required

## Tech Stack

| Component | Technology |
|-----------|-----------|
| App framework | .NET 8 WPF |
| Global key hook | `WH_KEYBOARD_LL` (P/Invoke) |
| Audio capture | WASAPI (NAudio) |
| Transcription engine | whisper.cpp (git submodule) |
| Engine binding | C bridge DLL → C# P/Invoke |
| System tray | Hardcodet.NotifyIcon.Wpf |

## Prerequisites

- Windows 10/11 (x64)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Whisper GGML model file (downloaded via script below)

> Pre-built native DLLs are included in the repo. To rebuild them yourself, you need [Visual Studio 2022](https://visualstudio.microsoft.com/) with the C++ desktop workload.

## Setup

### 1. Clone

```bash
git clone --recursive https://github.com/junjunjunbong/oh-my-whisper.git
cd oh-my-whisper
```

### 2. Download model

```bash
scripts\download-model.bat
```

Downloads `ggml-base.bin` (~148MB, multilingual) from [Hugging Face](https://huggingface.co/ggerganov/whisper.cpp) into `models/`.

> For better accuracy, replace with `ggml-small.bin` (~466MB) at the cost of slower inference.

### 3. Run

```bash
run.bat
```

That's it. After reboot, just double-click `run.bat` again.

## Usage

1. Launch app → sits in system tray
2. Hold **`Ctrl+Shift+Space`** → overlay appears near cursor + recording starts
3. Partial transcription updates every ~1.5s while speaking
4. Release key → final transcription → edit mode (text is editable)
5. **Copy** button → clipboard / **Esc** → dismiss overlay

### Tray Menu (right-click)

- **Enable/Disable Hotkey** — toggle hotkey
- **한국어 / English** — switch transcription language
- **Quit** — exit app

## State Machine

```
Idle ──(Ctrl+Shift+Space Down)──▶ Recording ──(Up)──▶ Finalizing ──▶ Editing ──(Esc/Copy)──▶ Idle
```

## License

MIT
