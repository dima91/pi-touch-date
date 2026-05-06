using System;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Interactivity;

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
        }


        CurrentTime = now.ToString("HH:mm");


        _previousDT = now;
    }


    public void OnBrightnessCardClicked()
    {
        // TODO: Show brightness overlay
    }


    public void OnWifiCardClicked()
    {
        // TODO: Shpw wi-fi selection overlay
    }


    public void OnPowerCardClicked()
    {
        // TODO: Show power options overlay
    }


    public void OnWeatherCardClicked()
    {
        // TODO: Show weather overlay
    }
}
