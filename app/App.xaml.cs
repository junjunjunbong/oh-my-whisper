using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using OhMyWhisper.Audio;
using OhMyWhisper.Hotkey;
using OhMyWhisper.StateMachine;
using OhMyWhisper.Transcription;
using OhMyWhisper.UI;

namespace OhMyWhisper;

public partial class App : Application
{
    private OverlayWindow? _overlay;
    private HotkeyService? _hotkey;
    private AudioCaptureService? _audio;
    private TranscriptionService? _transcription;
    private TaskbarIcon? _trayIcon;
    private TrayViewModel? _trayVM;

    private AppState _state = AppState.Idle;
    private PeriodicTimer? _partialTimer;
    private CancellationTokenSource? _partialCts;

    // 부분 전사 파라미터
    private const double PartialIntervalSeconds = 1.5;
    private const int PartialWindowSamples = 16000 * 6; // 6초

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 전역 예외 처리
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _overlay = new OverlayWindow();
        _overlay.OverlayClosed += OnOverlayClosed;

        _audio = new AudioCaptureService();
        _transcription = new TranscriptionService();

        // 트레이 아이콘
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayVM = (TrayViewModel)FindResource("TrayVM");
        _trayVM.HotkeyEnabledChanged += enabled =>
        {
            if (_hotkey != null) _hotkey.IsEnabled = enabled;
        };
        _trayVM.LanguageChanged += lang =>
        {
            _transcription?.ReconfigureLanguage(lang);
        };
        _trayVM.QuitRequested += () =>
        {
            Shutdown();
        };

        // 키보드 훅
        _hotkey = new HotkeyService();
        _hotkey.PushToTalkDown += OnPushToTalkDown;
        _hotkey.PushToTalkUp += OnPushToTalkUp;
        _hotkey.Install();

        // 모델 초기화 시도
        try
        {
            await _transcription.InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Whisper init failed (model may be missing): {ex.Message}");
        }
    }

    private void OnPushToTalkDown()
    {
        Dispatcher.Invoke(() =>
        {
            if (_state != AppState.Idle) return;

            _state = AppState.Recording;

            _overlay!.SetText("");
            _overlay.SetStatus("Recording...");
            _overlay.SetEditable(false);
            _overlay.ShowAtCursor();

            try
            {
                _audio!.StartCapture();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio capture failed: {ex.Message}");
                _overlay.SetStatus("Mic error");
                _state = AppState.Idle;
                return;
            }

            StartPartialTranscription();
        });
    }

    private void OnPushToTalkUp()
    {
        Dispatcher.Invoke(async () =>
        {
            if (_state != AppState.Recording) return;

            _state = AppState.Finalizing;
            _overlay!.SetStatus("Finalizing...");

            _audio!.StopCapture();
            StopPartialTranscription();

            // 최종 전사
            if (_transcription?.IsInitialized == true)
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    var samples = _audio.Buffer.ReadAll();
                    if (samples.Length > 0)
                    {
                        var result = await _transcription.TranscribeAsync(samples, skipIfBusy: false);
                        sw.Stop();
                        Debug.WriteLine($"Final transcription: {sw.ElapsedMilliseconds}ms, {samples.Length} samples");
                        if (!string.IsNullOrEmpty(result))
                        {
                            _overlay.SetText(result.Trim());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Transcription error: {ex.Message}");
                    _overlay.SetStatus("Transcription error");
                }
            }

            // 편집 모드 전환
            _state = AppState.Editing;
            _overlay.SetStatus("Edit");
            _overlay.SetEditable(true);
        });
    }

    private void StartPartialTranscription()
    {
        _partialCts = new CancellationTokenSource();
        _partialTimer = new PeriodicTimer(TimeSpan.FromSeconds(PartialIntervalSeconds));

        _ = Task.Run(async () =>
        {
            try
            {
                while (await _partialTimer.WaitForNextTickAsync(_partialCts.Token))
                {
                    if (_state != AppState.Recording) break;
                    if (_transcription?.IsInitialized != true) continue;

                    var samples = _audio!.Buffer.ReadLast(PartialWindowSamples);
                    if (samples.Length < 1600) continue; // 0.1초 미만이면 스킵

                    var sw = Stopwatch.StartNew();
                    var result = await _transcription.TranscribeAsync(samples, skipIfBusy: true);
                    sw.Stop();

                    if (result != null)
                    {
                        Debug.WriteLine($"Partial transcription: {sw.ElapsedMilliseconds}ms");
                        Dispatcher.Invoke(() =>
                        {
                            if (_state == AppState.Recording)
                            {
                                _overlay!.SetText(result.Trim());
                            }
                        });
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"Partial transcription error: {ex.Message}");
            }
        });
    }

    private void StopPartialTranscription()
    {
        _partialCts?.Cancel();
        _partialTimer?.Dispose();
        _partialCts?.Dispose();
        _partialCts = null;
        _partialTimer = null;
    }

    private void OnOverlayClosed()
    {
        _state = AppState.Idle;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Debug.WriteLine($"Unhandled exception: {e.Exception}");
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        StopPartialTranscription();
        _hotkey?.Dispose();
        _audio?.Dispose();
        _transcription?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
