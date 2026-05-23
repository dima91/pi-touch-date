using System;
using System.Diagnostics;
using System.Reactive;
using System.Reflection;
using ReactiveUI;
using PiTouchDate.Services;
using PiTouchDate.ViewModels;

namespace PiTouchDate.Overlays;

public class AppSettingsOverlayViewModel : ViewModelBase
{
    private readonly Action<int>? _onBrightnessChanged;

    private int _screenBrightness;
    public int ScreenBrightness
    {
        get => _screenBrightness;
        set
        {
            this.RaiseAndSetIfChanged(ref _screenBrightness, value);
            _onBrightnessChanged?.Invoke(value);
        }
    }

    public string SoftwareVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "—";

    public int DayBrightness
    {
        get => GetService<ConfigurationService>().Configuration.DayBrightness;
        set => GetService<ConfigurationService>().Configuration.DayBrightness = value;
    }

    public int NightBrightness
    {
        get => GetService<ConfigurationService>().Configuration.NightBrightness;
        set => GetService<ConfigurationService>().Configuration.NightBrightness = value;
    }

    public bool IsAutoNightModeEnabled
    {
        get => GetService<ConfigurationService>().Configuration.AutoNightMode;
        set => GetService<ConfigurationService>().Configuration.AutoNightMode = value;
    }

    public ReactiveCommand<Unit, Unit> RestartCommand { get; }
    public ReactiveCommand<Unit, Unit> ShutdownCommand { get; }

    public AppSettingsOverlayViewModel(int screenBrightness = 255, Action<int>? onBrightnessChanged = null)
    {
        _screenBrightness = screenBrightness;
        _onBrightnessChanged = onBrightnessChanged;

        RestartCommand = ReactiveCommand.Create(() =>
            { Process.Start(new ProcessStartInfo("sudo", "reboot") { CreateNoWindow = true }); });
        ShutdownCommand = ReactiveCommand.Create(() =>
            { Process.Start(new ProcessStartInfo("sudo", "shutdown -h now") { CreateNoWindow = true }); });
    }
}
