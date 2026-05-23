using System;
using System.Globalization;
using System.Reactive;
using ReactiveUI;
using PiTouchDate.ViewModels;
using PiTouchDate.Services;
using static PiTouchDate.Services.WeatherDataService;
using System.Collections.Generic;
using System.Linq;

namespace PiTouchDate.Overlays;

public class WeatherSettingsViewModel : ViewModelBase
{
    public class DayPeriodWeatherInfo
    {
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public required string TargetDayPeriod { get; init; }
        public double AverageTemperature { get; init; }
        public int AverageWeatherCode { get; init; }
    }


    private bool _weatherSettingsSelected = false;
    public bool WeatherSettingsSelected
    {
        get => _weatherSettingsSelected;
        set => this.RaiseAndSetIfChanged(ref _weatherSettingsSelected, value);
    }

    public double? CurrentTemperature { get; }
    public string? WeatherDescription { get; }
    public string? Placement { get; }
    public WeatherData? WeatherData { get; }
    public List<DayPeriodWeatherInfo?> WeatherInfoDayPeriods { get; init; }

    private bool _isLatitudeActive = true;
    public bool IsLatitudeActive
    {
        get => _isLatitudeActive;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isLatitudeActive, value);
            this.RaisePropertyChanged(nameof(IsLongitudeActive));
        }
    }

    public bool IsLongitudeActive => !_isLatitudeActive;

    private string _latitudeInput = "";
    public string LatitudeInput
    {
        get => _latitudeInput;
        set => this.RaiseAndSetIfChanged(ref _latitudeInput, value);
    }

    private string _longitudeInput = "";
    public string LongitudeInput
    {
        get => _longitudeInput;
        set => this.RaiseAndSetIfChanged(ref _longitudeInput, value);
    }

    public ReactiveCommand<string, Unit> KeyPressCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectLatitudeCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectLongitudeCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectWeatherInfoCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectWeatherSettingsCommand { get; }

    private readonly Action? _onSaved;

    public WeatherSettingsViewModel(
        WeatherData? weatherData = null,
        string? placement = null,
        Action? onSaved = null)
    {
        CurrentTemperature = weatherData?.CurrentTemperature;
        WeatherDescription = weatherData?.Description;
        Placement = placement;
        WeatherData = weatherData;
        WeatherInfoDayPeriods = BuildDayPeriods(weatherData);

        _onSaved = onSaved;

        var config = GetService<ConfigurationService>().Configuration;
        _latitudeInput = config.Latitude?.ToString(CultureInfo.InvariantCulture) ?? "";
        _longitudeInput = config.Longitude?.ToString(CultureInfo.InvariantCulture) ?? "";

        KeyPressCommand = ReactiveCommand.Create<string>(OnKeyPress);

        var canSave = this.WhenAnyValue(
            x => x.LatitudeInput,
            x => x.LongitudeInput,
            (lat, lon) =>
                double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
                double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
        );

        SaveCommand = ReactiveCommand.Create(Save, canSave);
        SelectLatitudeCommand = ReactiveCommand.Create(() => { IsLatitudeActive = true; });
        SelectLongitudeCommand = ReactiveCommand.Create(() => { IsLatitudeActive = false; });
        SelectWeatherInfoCommand = ReactiveCommand.Create(() => { WeatherSettingsSelected = false; });
        SelectWeatherSettingsCommand = ReactiveCommand.Create(() => { WeatherSettingsSelected = true; });
    }

    private static List<DayPeriodWeatherInfo?> BuildDayPeriods(WeatherData? weatherData)
    {
        var periods = new (string Name, int StartHour, int EndHour)[]
        {
            ("Notte",      0,  6),
            ("Mattina",    6,  12),
            ("Pomeriggio", 12, 18),
            ("Sera",       18, 24)
        };

        if (weatherData == null)
            return new List<DayPeriodWeatherInfo?> { null, null, null, null };

        var today = DateTime.Today;
        var result = new List<DayPeriodWeatherInfo?>();

        foreach (var (name, startHour, endHour) in periods)
        {
            var entries = weatherData.HourlyInfo
                .Where(kv => kv.Key.Hour >= startHour && kv.Key.Hour < endHour)
                .Select(kv => kv.Value)
                .ToList();

            if (entries.Count == 0)
            {
                result.Add(null);
                continue;
            }

            var avgTemp = entries.Average(e => e.Temperature);
            var modeCode = entries[entries.Count / 2].WeatherCode;

            result.Add(new DayPeriodWeatherInfo
            {
                StartTime = today.AddHours(startHour),
                EndTime = today.AddHours(endHour),
                TargetDayPeriod = name,
                AverageTemperature = avgTemp,
                AverageWeatherCode = modeCode
            });
        }

        return result;
    }

    private void OnKeyPress(string key)
    {
        var current = IsLatitudeActive ? LatitudeInput : LongitudeInput;

        string updated = key == "Backspace"
            ? (current.Length > 0 ? current[..^1] : current)
            : current + key;

        if (IsLatitudeActive)
            LatitudeInput = updated;
        else
            LongitudeInput = updated;
    }

    private void Save()
    {
        var config = GetService<ConfigurationService>().Configuration;

        if (double.TryParse(LatitudeInput, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
            config.Latitude = lat;
        if (double.TryParse(LongitudeInput, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
            config.Longitude = lon;

        _onSaved?.Invoke();
    }
}
