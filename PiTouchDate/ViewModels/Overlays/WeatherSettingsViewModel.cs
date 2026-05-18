using System;
using System.Globalization;
using System.Reactive;
using ReactiveUI;
using PiTouchDate.ViewModels;
using PiTouchDate.Services;

namespace PiTouchDate.Overlays;

public class WeatherSettingsViewModel : ViewModelBase
{
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

    private readonly Action? _onSaved;

    public WeatherSettingsViewModel(Action? onSaved = null)
    {
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
