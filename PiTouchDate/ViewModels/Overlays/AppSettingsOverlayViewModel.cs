using System;
using System.Diagnostics;
using System.Reactive;
using System.Reflection;
using ReactiveUI;
using PiTouchDate.Services;
using PiTouchDate.ViewModels;

namespace PiTouchDate.Overlays;

public class AppSettingsOverlayViewModel : ViewModelBase, IDisposable
{
    private readonly Action<int>? _onBrightnessChanged;
    private IDisposable? _brightnessSubscription;

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

    public DateTime SunsetTime { get; private set; }
    public DateTime SunriseTime { get; private set; }

    public string NightModeScheduleText =>
        $"Passa alla modalità notte tra le {SunsetTime:HH:mm} (tramonto) e le {SunriseTime:HH:mm} (alba)";

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

    public ReactiveCommand<Unit, Unit> OpenWifiCommand { get; }
    public ReactiveCommand<Unit, Unit> RestartCommand { get; }
    public ReactiveCommand<Unit, Unit> ShutdownCommand { get; }

    public AppSettingsOverlayViewModel(DateTime sunriseTime, DateTime sunsetTime, int screenBrightness = 255,
                                        Action<int>? onBrightnessChanged = null,
                                        Action? onOpenWifi = null,
                                        IObservable<int>? currentBrightnessSource = null)
    {
        SunsetTime = sunsetTime;
        SunriseTime = sunriseTime;
        _screenBrightness = screenBrightness;
        _onBrightnessChanged = onBrightnessChanged;

        _brightnessSubscription = currentBrightnessSource?.Subscribe(b =>
            this.RaiseAndSetIfChanged(ref _screenBrightness, b, nameof(ScreenBrightness)));

        OpenWifiCommand = ReactiveCommand.Create(() => onOpenWifi?.Invoke());
        RestartCommand = ReactiveCommand.Create(() =>
            { Process.Start(new ProcessStartInfo("sudo", "reboot") { CreateNoWindow = true }); });
        ShutdownCommand = ReactiveCommand.Create(() =>
            { Process.Start(new ProcessStartInfo("sudo", "shutdown -h now") { CreateNoWindow = true }); });
    }

    public void Dispose()
    {
        _brightnessSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }
}
