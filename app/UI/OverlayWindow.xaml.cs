using System.Windows;
using System.Windows.Input;
using OhMyWhisper.UI.Helpers;

namespace OhMyWhisper.UI;

public partial class OverlayWindow : Window
{
    public event Action? OverlayClosed;

    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void SetText(string text)
    {
        TranscriptionBox.Text = text;
    }

    public void SetStatus(string status)
    {
        StatusText.Text = status;
    }

    public void SetEditable(bool editable)
    {
        TranscriptionBox.IsReadOnly = !editable;
        if (editable)
        {
            TranscriptionBox.Focus();
            TranscriptionBox.SelectAll();
        }
    }

    public void ShowAtCursor()
    {
        NativeInterop.GetCursorPos(out var pt);

        // 현재 모니터 정보 가져오기
        var hMonitor = NativeInterop.MonitorFromPoint(pt, NativeInterop.MONITOR_DEFAULTTONEAREST);
        var mi = new NativeInterop.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeInterop.MONITORINFO>() };
        NativeInterop.GetMonitorInfo(hMonitor, ref mi);

        // 물리 픽셀 → WPF DIP 변환
        var source = PresentationSource.FromVisual(this)
                     ?? PresentationSource.FromVisual(Application.Current.MainWindow ?? this);

        double dpiScaleX = 1.0, dpiScaleY = 1.0;
        if (source?.CompositionTarget != null)
        {
            dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
        }

        double cursorX = pt.X / dpiScaleX;
        double cursorY = pt.Y / dpiScaleY;

        double workLeft = mi.rcWork.Left / dpiScaleX;
        double workTop = mi.rcWork.Top / dpiScaleY;
        double workRight = mi.rcWork.Right / dpiScaleX;
        double workBottom = mi.rcWork.Bottom / dpiScaleY;

        // 커서 근처에 배치 (약간 오프셋)
        double targetX = cursorX + 16;
        double targetY = cursorY + 16;

        // 화면 밖 clamp
        if (targetX + Width > workRight)
            targetX = workRight - Width;
        if (targetY + Height > workBottom)
            targetY = workBottom - Height;
        if (targetX < workLeft)
            targetX = workLeft;
        if (targetY < workTop)
            targetY = workTop;

        Left = targetX;
        Top = targetY;

        Show();
    }

    public void HideOverlay()
    {
        Hide();
        OverlayClosed?.Invoke();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TranscriptionBox.Text))
        {
            Clipboard.SetText(TranscriptionBox.Text);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        HideOverlay();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HideOverlay();
            e.Handled = true;
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 창을 닫지 않고 숨기기만 (앱 종료 시에만 실제 닫기)
        if (Application.Current.ShutdownMode == ShutdownMode.OnExplicitShutdown)
        {
            e.Cancel = true;
            HideOverlay();
        }
    }
}
