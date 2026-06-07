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
using PiTouchDate.Utils;
using Avalonia.Platform;
using System.IO.Compression;
using System.IO;

namespace PiTouchDate.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private const int _MINUTES_DELAY_TIMER = 1;
    private const string BACKLIGHT_FILE_PATH = "/sys/devices/platform/soc/3f205000.i2c/i2c-11/i2c-10/10-0045/backlight/10-0045/brightness";

    private DateTime _previousDT;
    private IDisposable? AutoNightModeResetTimer = null;
    private IDisposable? BrightnessResetTimer = null;

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

    private ScreenMode _selectedScreenMode = ScreenMode.Day;
    public ScreenMode SelectedScreenMode
    {
        get => _selectedScreenMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedScreenMode, value);
            this.RaisePropertyChanged(nameof(IsDayMode));
        }
    }

    public bool IsDayMode => _selectedScreenMode == ScreenMode.Day;

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
        var configuration = GetService<ConfigurationService>().Configuration;

        _ReloadShownInfo(true);

        // Aligns the timer to the next exact minute boundary (e.g. 10:03:42 → wait 18s, then tick every 60s)
        var now = DateTimeOffset.Now;
        var nextMinute = now.AddMinutes(_MINUTES_DELAY_TIMER).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        var delay = nextMinute - now;

        Observable.Timer(delay, TimeSpan.FromMinutes(1))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => _ReloadShownInfo());

        // Recomputes brightness whenever the screen mode or configured brightness values change
        Observable.CombineLatest(
            this.WhenAnyValue(x => x.SelectedScreenMode),
            configuration.WhenAnyValue(x => x.DayBrightness),
            configuration.WhenAnyValue(x => x.NightBrightness),
            (mode, day, night) => mode == ScreenMode.Day ? day : night)
            .Subscribe(b => ScreenBrightness = b);

        configuration.WhenAnyValue(x => x.AutoNightMode)
            .Subscribe(currentValue =>
            {
                Console.WriteLine($"AutoNightMode changed to: {currentValue}");
                if (AutoNightModeResetTimer != null)
                {
                    Console.WriteLine("Auto night mode is enabled, disposing existing timer");
                    AutoNightModeResetTimer.Dispose();
                    AutoNightModeResetTimer = null;
                }
                if (BrightnessResetTimer != null)
                {
                    BrightnessResetTimer.Dispose();
                    BrightnessResetTimer = null;
                }
                if (currentValue == true)
                {
                    Console.WriteLine("Auto night mode enabled, updating screen mode immediately");
                    UpdateScreenMode(DateTime.Now);
                }
            });


        // Writes the brightness value to the touchscreen backlight sysfs file
        this.WhenAnyValue(x => x.ScreenBrightness)
        .Subscribe(
            brightness =>
            {
                try
                {
                    if (File.Exists(BACKLIGHT_FILE_PATH))
                        File.WriteAllText(BACKLIGHT_FILE_PATH, brightness.ToString());
                    else
                        Console.Error.WriteLine("Cannot update brightness");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting screen brightness: {ex.Message}");
                }
            }
        );
    }


    private void _ReloadShownInfo(bool forceUpdate = false)
    {
        var now = DateTime.Now;

        // Updates weekday, date and calendar only when the date changes
        try
        {
            if (forceUpdate || _previousDT.Date != now.Date)
            {
                UpdateShownDateInfo(now);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating date info: {ex.Message}");
        }


        // Refreshes weather every 15 minutes to avoid overloading the APIs
        try
        {
            if (forceUpdate || now.Minute % 15 == 0)
            {
                UpdateWeatherInfo();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating weather info: {ex.Message}");
        }


        // Automatically switches between day/night mode based on the current hour
        try
        {
            if (forceUpdate || now.Minute % 5 == 0)
            {
                UpdateScreenMode(now);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during auto night-mode update: {ex.Message}");
        }


        CurrentTime = now.ToString("HH:mm");
        _previousDT = now;
    }

    private async Task UpdateWeatherAndLocationAsync(double lat, double lon, string geocodeApiKey)
    {
        var weatherService = GetService<WeatherDataService>();
        var locationingService = GetService<LocationingService>();

        // Runs both calls in parallel to reduce total wait time
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

    // Resolves a path icon from app resources by key (e.g. "SemiIconWifi")
    private IconElement? GetSemiIcon(string key)
    {
        if (Application.Current is not null && Application.Current.TryFindResource(key, out var res) && res is Geometry geo)
        {
            return new PathIcon { Data = geo };
        }
        return null;
    }

    private void UpdateShownDateInfo(DateTime now)
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

    private void UpdateWeatherInfo()
    {
        Console.WriteLine("Updating weather and placement");
        var config = GetService<ConfigurationService>().Configuration;
        if (config.Latitude is { } lat && config.Longitude is { } lon)
            _ = UpdateWeatherAndLocationAsync(lat, lon, config.GeocodeApiKey ?? "");
        else
            Console.Error.WriteLine("Cannot update weather: coordinates not configured");
    }

    private void UpdateScreenMode(DateTime now)
    {
        Console.WriteLine("Updating screen mode");
        var configuration = GetService<ConfigurationService>().Configuration;
        if (configuration.AutoNightMode && AutoNightModeResetTimer == null)
        {
            bool isNightHour;
            if (CurrentWeatherData?.Sunrise != null && CurrentWeatherData?.Sunset != null)
            {
                // Use actual sunrise/sunset if available
                isNightHour = now < CurrentWeatherData.Sunrise.Value || now >= CurrentWeatherData.Sunset.Value;
            }
            else
            {
                // Fallback to static hours (7 PM to 7 AM)
                isNightHour = now.Hour >= 19 || now.Hour < 7;
            }

            SelectedScreenMode = isNightHour ? ScreenMode.Night : ScreenMode.Day;
        }
    }

    public void OnScreenModeCardClicked()
    {
        try
        {
            var configuration = GetService<ConfigurationService>().Configuration;
            if (configuration != null && configuration.AutoNightMode)
            {
                Console.WriteLine("Activating AutoNightModeResetTimer timer..");

                // Disposing if already existing
                if (AutoNightModeResetTimer != null)
                    AutoNightModeResetTimer?.Dispose();

                AutoNightModeResetTimer = Observable.Timer(TimeSpan.FromMinutes(20))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ =>
                    {
                        Console.WriteLine("Auto night mode reset timer elapsed, resetting screen mode to automatic");
                        AutoNightModeResetTimer?.Dispose();
                        AutoNightModeResetTimer = null;
                        UpdateScreenMode(DateTime.Now);
                    });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing configuration during screen mode toggle: {ex.Message}");
            return;
        }
        SelectedScreenMode = SelectedScreenMode == ScreenMode.Day ? ScreenMode.Night : ScreenMode.Day;

    }


    public void OnWifiCardClicked()
    {
        CurrentOverlayTitle = "Impostazioni wi-fi";
        CurrentOverlayIcon = GetSemiIcon("SemiIconWifi");
        CurrentOverlay = new WifiSettingsViewModel();
        IsOverlayVisible = true;
    }


    public void OnAppSettingsCardClicked()
    {
        CurrentOverlayTitle = "Impostazioni";
        CurrentOverlayIcon = GetSemiIcon("SemiIconSetting");
        // Getting actual sunset and sunrise times
        var sunrise = CurrentWeatherData?.Sunrise ?? DateTime.Today.AddHours(7);
        var sunset = CurrentWeatherData?.Sunset ?? DateTime.Today.AddHours(19);

        CurrentOverlay = new AppSettingsOverlayViewModel(
            sunset,
            sunrise,
            screenBrightness: ScreenBrightness,
            onOpenWifi: () =>
            {
                CloseOverlay();
                OnWifiCardClicked();
            },
            onBrightnessChanged: value =>
            {
                var config = GetService<ConfigurationService>().Configuration;
                if (config.AutoNightMode)
                {
                    BrightnessResetTimer?.Dispose();
                    BrightnessResetTimer = Observable.Timer(TimeSpan.FromMinutes(20))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ =>
                        {
                            BrightnessResetTimer?.Dispose();
                            BrightnessResetTimer = null;
                            var c = GetService<ConfigurationService>().Configuration;
                            ScreenBrightness = SelectedScreenMode == ScreenMode.Day ? c.DayBrightness : c.NightBrightness;
                        });
                }
                ScreenBrightness = value;
            }
            );

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
