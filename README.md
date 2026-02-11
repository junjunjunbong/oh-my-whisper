# Oh My Whisper

[![Korean](https://img.shields.io/badge/lang-한국어-blue)](README.ko.md)

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
- Whisper GGML model file (manually downloaded, see below)

> Pre-built native DLLs are included in the repo. To rebuild them yourself, you need [Visual Studio 2022](https://visualstudio.microsoft.com/) with the C++ desktop workload.

## Setup

### 1. Clone

```bash
git clone --recursive https://github.com/junjunjunbong/oh-my-whisper.git
cd oh-my-whisper
```

### 2. Download model

Download a GGML model from [Hugging Face](https://huggingface.co/ggerganov/whisper.cpp) and place it in the `models/` folder.

| Model | Size | Link |
|-------|------|------|
| ggml-tiny.bin | ~75MB | [Download](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin) |
| ggml-base.bin (default) | ~148MB | [Download](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin) |
| ggml-small.bin | ~466MB | [Download](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin) |
| ggml-medium.bin | ~1.5GB | [Download](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin) |
| ggml-large-v3.bin | ~3.1GB | [Download](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin) |

> Default is `ggml-base.bin`. For better accuracy, use `ggml-small.bin` or larger at the cost of slower inference.

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
