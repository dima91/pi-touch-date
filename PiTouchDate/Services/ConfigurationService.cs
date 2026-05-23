namespace PiTouchDate.Services;

using System;
using System.IO;
using System.Text.Json;
using ReactiveUI;

public class AppConfiguration : ReactiveObject
{
    // To add a new configuration key:
    // 1. Add a backing field + public property
    // 2. Add a constructor parameter here         ← breaks Load() and DEFAULTS at compile time
    // 3. Add the field to ConfigurationDto        ← breaks Save() at compile time
    // 4. Add a Read() call in Load()              ← only runtime, but enforced by the above
    public AppConfiguration(double? latitude, double? longitude, string? GeocodeApiKey, bool autoNightMode, int dayBrightness, int nightBrightness)
    {
        _latitude = latitude;
        _longitude = longitude;
        _geocodeApiKey = GeocodeApiKey;
        _autoNightMode = autoNightMode;
        _dayBrightness = dayBrightness;
        _nightBrightness = nightBrightness;
    }

    private double? _latitude;
    public double? Latitude
    {
        get => _latitude;
        set => this.RaiseAndSetIfChanged(ref _latitude, value);
    }

    private double? _longitude;
    public double? Longitude
    {
        get => _longitude;
        set => this.RaiseAndSetIfChanged(ref _longitude, value);
    }

    private string? _geocodeApiKey;
    public string? GeocodeApiKey
    {
        get => _geocodeApiKey;
        set => this.RaiseAndSetIfChanged(ref _geocodeApiKey, value);
    }

    private bool _autoNightMode;
    public bool AutoNightMode
    {
        get => _autoNightMode;
        set => this.RaiseAndSetIfChanged(ref _autoNightMode, value);
    }

    private int _dayBrightness;
    public int DayBrightness
    {
        get => _dayBrightness;
        set => this.RaiseAndSetIfChanged(ref _dayBrightness, value);
    }

    private int _nightBrightness;
    public int NightBrightness
    {
        get => _nightBrightness;
        set => this.RaiseAndSetIfChanged(ref _nightBrightness, value);
    }
}

public class ConfigurationService
{
    // Positional record: adding a field breaks the constructor call in Save() at compile time.
    private record ConfigurationDto(double? Latitude, double? Longitude, string? GeocodeApiKey, bool AutoNightMode, int DayBrightness, int NightBrightness);

    private const string ConfigFileName = "config.json";
    private const string SecretsFilePath = "/home/pi/.secrets.env";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _configFilePath;

    public AppConfiguration Configuration { get; private set; }

    // Instance property (not static field) so the compiler checks it against the constructor signature.
    private AppConfiguration DEFAUTLS() => new(
        latitude: null,
        longitude: null,
        GeocodeApiKey: LoadGeocodeApiKey(),
        autoNightMode: false,
        dayBrightness: 255,
        nightBrightness: 40
    );

    public ConfigurationService(string? basePath = null)
    {
        basePath ??= AppContext.BaseDirectory;
        _configFilePath = Path.Combine(basePath, ConfigFileName);
        Configuration = Load();
        // Subscribe after Load() to avoid triggering Save() during initialization.
        Configuration.Changed.Subscribe(_ => Save());
    }

    private AppConfiguration Load()
    {
        var defaults = DEFAUTLS();

        if (!File.Exists(_configFilePath))
        {
            Console.WriteLine($"Configuration file not found, creating {_configFilePath}");
            return Save(defaults);
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            bool needsSave = false;

            double? latitude = Read(root, nameof(AppConfiguration.Latitude), defaults.Latitude,
                el => el.GetDouble(), ref needsSave);

            double? longitude = Read(root, nameof(AppConfiguration.Longitude), defaults.Longitude,
                el => el.GetDouble(), ref needsSave);
            
            string? geocodeApiKey = Read(root, nameof(AppConfiguration.GeocodeApiKey), defaults.GeocodeApiKey,
                el => el.GetString(), ref needsSave);

            bool autoNightMode = Read(root, nameof(AppConfiguration.AutoNightMode), defaults.AutoNightMode,
                el => el.GetBoolean(), ref needsSave);

            int dayBrightness = Read(root, nameof(AppConfiguration.DayBrightness), defaults.DayBrightness,
                el => el.GetInt32(), ref needsSave);

            int nightBrightness = Read(root, nameof(AppConfiguration.NightBrightness), defaults.NightBrightness,
                el => el.GetInt32(), ref needsSave);

            var config = new AppConfiguration(latitude, longitude, geocodeApiKey, autoNightMode, dayBrightness, nightBrightness);

            if (needsSave)
                Save(config);

            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading configuration: {ex.Message}");
            return Save(defaults);
        }
    }

    private string? LoadGeocodeApiKey()
    {
        if (!File.Exists(SecretsFilePath))
        {
            Console.Error.WriteLine($"Error: secrets file not found at {SecretsFilePath}");
            return null;
        }

        foreach (var line in File.ReadAllLines(SecretsFilePath))
        {
            var trimmed = line.Trim();
            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex < 0) continue;

            var key = trimmed[..eqIndex].Trim();
            if (key == "GEOCODE_API_KEY")
                return trimmed[(eqIndex + 1)..].Trim();
        }

        Console.Error.WriteLine("Error: GEOCODE_API_KEY not found in secrets file");
        return null;
    }

    // Returns the parsed value if the key exists (even if null), or defaultValue if the key is absent.
    // Sets needsSave when a key is absent so the caller can persist the updated file.
    private static T? Read<T>(JsonElement root, string key, T? defaultValue,
        Func<JsonElement, T> parse, ref bool needsSave)
    {
        if (!root.TryGetProperty(key, out var el))
        {
            Console.WriteLine($"Missing configuration key '{key}', adding with default");
            needsSave = true;
            return defaultValue;
        }

        return el.ValueKind == JsonValueKind.Null ? default : parse(el);
    }

    private AppConfiguration Save(AppConfiguration? config = null)
    {
        config ??= Configuration;
        try
        {
            var dto = new ConfigurationDto(config.Latitude, config.Longitude, config.GeocodeApiKey, config.AutoNightMode, config.DayBrightness, config.NightBrightness);
            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving configuration: {ex.Message}");
        }

        return config;
    }
}
