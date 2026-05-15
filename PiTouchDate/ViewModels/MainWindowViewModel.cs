using System;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Interactivity;
using PiTouchDate.Controls;
using PiTouchDate.Overlays;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;

namespace PiTouchDate.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private DateTime _previousDT;
    private const int _MINUTES_DELAY_TIMER = 1;

    private string _weekDay = "";
    public string WeekDay
    {
        get => _weekDay;
        set => this.RaiseAndSetIfChanged(ref _weekDay, value);
    }

    private string _currentDate = "";
    public string CurrentDate
    {
        get => _currentDate;
        set => this.RaiseAndSetIfChanged(ref _currentDate, value);
    }

    private string _currentTime = "";
    public string CurrentTime
    {
        get => _currentTime;
        set => this.RaiseAndSetIfChanged(ref _currentTime, value);
    }

    private int _screenBrightness = 255;
    public int ScreenBrightness
    {
        get => _screenBrightness;
        set => this.RaiseAndSetIfChanged(ref _screenBrightness, value);
    }

    private bool _isOverlayVisible = false;
    public bool IsOverlayVisible
    {
        get => _isOverlayVisible;
        set => this.RaiseAndSetIfChanged(ref _isOverlayVisible, value);
    }

    private string _currentOverlayTitle = "";
    public string CurrentOverlayTitle
    {
        get => _currentOverlayTitle;
        set => this.RaiseAndSetIfChanged(ref _currentOverlayTitle, value);
    }

    private IconElement? _currentOverlayIcon = null;
    public IconElement? CurrentOverlayIcon
    {
        get => _currentOverlayIcon;
        set => this.RaiseAndSetIfChanged(ref _currentOverlayIcon, value);
    }

    private ViewModelBase? _currentOverlay = null;
    public ViewModelBase? CurrentOverlay
    {
        get => _currentOverlay;
        set => this.RaiseAndSetIfChanged(ref _currentOverlay, value);
    }


    public MainWindowViewModel()
    {
        _ReloadShownInfo(true);

        var now = DateTimeOffset.Now;
        var nextMinute = now.AddMinutes(_MINUTES_DELAY_TIMER).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        var delay = nextMinute - now;

        Observable.Timer(delay, TimeSpan.FromMinutes(1))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => _ReloadShownInfo());
    }


    private void _ReloadShownInfo(bool forceUpdate = false)
    {
        var now = DateTime.Now;
        Console.WriteLine($"Fired at: {DateTime.Now:HH:mm:ss}");

        if (forceUpdate || _previousDT.Date != now.Date)
        {
            Console.WriteLine("Updating WeekDay, CurrentDate and Calendar day");
            var day = now.ToString("dddd");
            if (!string.IsNullOrEmpty(day))
            {
                WeekDay = char.ToUpperInvariant(day[0]) + day.Substring(1);
            }

            CurrentDate = now.ToString("d MMMM yyyy").ToUpper();

            // TODO: Update calendar day
        }


        if (forceUpdate || (now.Hour % 2 == 0 && now.Minute == 0))
        {
            // TODO: Update Weather
            Console.WriteLine("Updating weather");
        }


        CurrentTime = now.ToString("HH:mm");


        _previousDT = now;
    }

    private IconElement? GetSemiIcon(string key)
    {
        if (Application.Current is not null && Application.Current.TryFindResource(key, out var res) && res is Geometry geo)
        {
            return new PathIcon { Data = geo };
        }
        return null;
    }

    public void OnBrightnessCardClicked()
    {
        // Not yet implemented overlay
        CurrentOverlayTitle = "Luminosità";
        CurrentOverlayIcon = GetSemiIcon("SemiIconSun");
        CurrentOverlay = null;
        IsOverlayVisible = true;
    }


    public void OnWifiCardClicked()
    {
        CurrentOverlayTitle = "Impostazioni wi-fi";
        CurrentOverlayIcon = GetSemiIcon("SemiIconWifi");
        CurrentOverlay = new WifiSettingsViewModel();
        IsOverlayVisible = true;
    }


    public void OnPowerCardClicked()
    {
        // Not yet implemented overlay
        CurrentOverlayTitle = "Azioni di sistema";
        CurrentOverlayIcon = GetSemiIcon("SemiIconPoweroff");
        CurrentOverlay = null;
        IsOverlayVisible = true;
    }


    public void OnWeatherCardClicked()
    {
        // Not yet implemented overlay
        CurrentOverlayTitle = "Meteo";
        CurrentOverlayIcon = GetSemiIcon("SemiIconCloud");
        CurrentOverlay = null;
        IsOverlayVisible = true;
    }


    public void CloseOverlay()
    {
        IsOverlayVisible = false;
        CurrentOverlay = null;
    }
}
