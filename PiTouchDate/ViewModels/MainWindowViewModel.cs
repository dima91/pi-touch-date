using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reactive.Linq;
using ReactiveUI;
using PiTouchDate.Controls;
using PiTouchDate.Overlays;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;
using PiTouchDate.Services;
using static PiTouchDate.Services.WeatherDataService;

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

    private DateTime _calendarSelectedDate = DateTime.Today;
    public DateTime CalendarSelectedDate
    {
        get => _calendarSelectedDate;
        set => this.RaiseAndSetIfChanged(ref _calendarSelectedDate, value);
    }

    private int _screenBrightness = 255;
    public int ScreenBrightness
    {
        get => _screenBrightness;
        set => this.RaiseAndSetIfChanged(ref _screenBrightness, value);
    }

    // ===============   Weather properties   ===============
    private WeatherData? _currentWeatherData = null;
    public WeatherData? CurrentWeatherData
    {
        get => _currentWeatherData;
        set => this.RaiseAndSetIfChanged(ref _currentWeatherData, value);
    }

    private double? _currentTemperature = null;
    public double? CurrentTemperature
    {
        get => _currentTemperature;
        set => this.RaiseAndSetIfChanged(ref _currentTemperature, value);
    }

    private string? _weatherDescription = null;
    public string? WeatherDescription
    {
        get => _weatherDescription;
        set => this.RaiseAndSetIfChanged(ref _weatherDescription, value);
    }

    private string? _placement = null;
    public string? Placement
    {
        get => _placement;
        set => this.RaiseAndSetIfChanged(ref _placement, value);
    }

    // ===============   Overlay properties   ===============
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

        try {
            if (forceUpdate || _previousDT.Date != now.Date)
            {
                Console.WriteLine("Updating WeekDay, CurrentDate and Calendar day");
                var day = now.ToString("dddd");
                if (!string.IsNullOrEmpty(day))
                {
                    WeekDay = char.ToUpperInvariant(day[0]) + day.Substring(1);
                }

                CurrentDate = now.ToString("d MMMM yyyy").ToUpper();

                CalendarSelectedDate = now.Date;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating date info: {ex.Message}");
        }


        try {
            if (forceUpdate || now.Minute % 15 == 0)
            {
                Console.WriteLine("Updating weather and placement");
                var config = GetService<ConfigurationService>().Configuration;
                if (config.Latitude is { } lat && config.Longitude is { } lon)
                    _ = UpdateWeatherAndLocationAsync(lat, lon, config.GeocodeApiKey ?? "");
                else
                    Console.Error.WriteLine("Cannot update weather: coordinates not configured");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating weather info: {ex.Message}");
        }


        CurrentTime = now.ToString("HH:mm");


        _previousDT = now;
    }

    private async Task UpdateWeatherAndLocationAsync(double lat, double lon, string geocodeApiKey)
    {
        var weatherService = GetService<WeatherDataService>();
        var locationingService = GetService<LocationingService>();

        var weatherTask = weatherService.GetWeatherAsync(lat, lon);
        var locationTask = locationingService.GetLocationNameAsync(lat, lon, geocodeApiKey);

        await Task.WhenAll(weatherTask, locationTask);

        CurrentWeatherData = weatherTask.Result;
        
        if (CurrentWeatherData != null)
        {
            CurrentTemperature = CurrentWeatherData.CurrentTemperature;
            WeatherDescription = CurrentWeatherData.Description;
        }
        else
        {
            CurrentTemperature = null;
            WeatherDescription = null;
        }

        var location = locationTask.Result;
        Placement = location.HasValue ? location.Value : null;
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
        CurrentOverlayTitle = "Meteo";
        CurrentOverlayIcon = GetSemiIcon("SemiIconCloud");
        CurrentOverlay = new WeatherSettingsViewModel(
            weatherData: CurrentWeatherData,
            placement: Placement,
            onSaved: () =>
            {
                CloseOverlay();
                _ReloadShownInfo(true);
            });
        IsOverlayVisible = true;
    }


    public void CloseOverlay()
    {
        IsOverlayVisible = false;
        (CurrentOverlay as IDisposable)?.Dispose();
        CurrentOverlay = null;
    }
}
