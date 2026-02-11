# Oh My Whisper

Windows용 Push-to-talk 음성 전사 오버레이 앱. 단축키를 누르고 있는 동안 마이크를 녹음하고, 커서 근처 오버레이 창에 실시간으로 전사 결과를 표시한다. 키를 떼면 최종 전사 후 텍스트를 편집하고 클립보드에 복사할 수 있다.

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
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (C++ 데스크톱 개발 워크로드)
- Whisper GGML 모델 파일

## 빌드 및 실행

### 1. 리포 클론

```bash
git clone --recursive https://github.com/junjunjunbong/oh-my-whisper.git
cd oh-my-whisper
```

> `--recursive`로 whisper.cpp 서브모듈도 함께 클론한다.

### 2. 모델 다운로드

`models/` 디렉토리에 GGML 모델을 다운로드한다.

```bash
# base 모델 (다국어, 권장)
curl -L -o models/ggml-base.bin https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
```

> 정확도가 부족하면 `ggml-small.bin`을 사용할 수 있다 (추론 시간 증가).

### 3. 네이티브 DLL 빌드

```bash
scripts\build-whisper.bat
```

Visual Studio 2022에 포함된 CMake를 자동 감지하여 `whisper.dll` + `whisper_bridge.dll`을 빌드하고 앱 출력 디렉토리에 복사한다.

### 4. 앱 빌드 및 실행

```bash
dotnet run --project app/OhMyWhisper.csproj
```

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
├── app/                           # .NET 8 WPF 앱
│   ├── Hotkey/                    # 전역 키보드 훅 (Ctrl+Shift+Space)
│   ├── Audio/                     # WASAPI 캡처, 리샘플링, 링버퍼
│   ├── Transcription/             # 브릿지 DLL P/Invoke, 추론 서비스
│   ├── UI/                        # 오버레이 창, 트레이, NativeInterop
│   └── StateMachine/              # 상태 enum (Idle/Recording/Finalizing/Editing)
├── third_party/
│   ├── whisper.cpp/               # git submodule (whisper.cpp)
│   └── whisper_bridge/            # C 브릿지 DLL (간단한 API만 노출)
├── scripts/
│   └── build-whisper.bat          # 네이티브 DLL 빌드 스크립트
├── models/                        # GGML 모델 파일 (gitignored)
└── plan.md                        # 설계 문서
```

## 상태 머신

```
Idle ──(Ctrl+Shift+Space Down)──▶ Recording ──(Up)──▶ Finalizing ──▶ Editing ──(Esc/Copy)──▶ Idle
```

## 라이선스

MIT
