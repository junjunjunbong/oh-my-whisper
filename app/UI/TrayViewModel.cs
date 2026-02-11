using System.Windows;
using System.Windows.Input;

namespace OhMyWhisper.UI;

public class TrayViewModel
{
    public event Action<bool>? HotkeyEnabledChanged;
    public event Action<string>? LanguageChanged;
    public event Action? QuitRequested;

    public bool IsHotkeyEnabled { get; private set; } = true;
    public string CurrentLanguage { get; private set; } = "ko";

    public ICommand ToggleHotkeyCommand { get; }
    public ICommand SetKoreanCommand { get; }
    public ICommand SetEnglishCommand { get; }
    public ICommand QuitCommand { get; }

    public TrayViewModel()
    {
        ToggleHotkeyCommand = new RelayCommand(() =>
        {
            IsHotkeyEnabled = !IsHotkeyEnabled;
            HotkeyEnabledChanged?.Invoke(IsHotkeyEnabled);
        });

        SetKoreanCommand = new RelayCommand(() =>
        {
            CurrentLanguage = "ko";
            LanguageChanged?.Invoke("ko");
        });

        SetEnglishCommand = new RelayCommand(() =>
        {
            CurrentLanguage = "en";
            LanguageChanged?.Invoke("en");
        });

        QuitCommand = new RelayCommand(() =>
        {
            QuitRequested?.Invoke();
        });
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}
