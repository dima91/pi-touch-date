namespace PiTouchDate.Services;

using System;
using System.IO;
using System.Text.Json;
using ReactiveUI;

public class AppConfiguration : ReactiveObject
{
    // Adding a new property requires:
    // 1. A backing field + public property below
    // 2. A matching parameter in this constructor  ← compile error if missing
    // 3. A default in the Defaults class           ← compile error if missing
    // 4. A field in ConfigurationDto               ← compile error on Save()
    // 5. A TryGetProperty block in Load()          ← no compile error, but covered by the rest

    public AppConfiguration(double? latitude, double? longitude)
    {
        _latitude = latitude;
        _longitude = longitude;
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
}

public class ConfigurationService
{
    private record ConfigurationDto(double? Latitude, double? Longitude);

    private const string ConfigFileName = "config.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _configFilePath;

    public AppConfiguration Configuration { get; private set; }

    private AppConfiguration DEFAUTLS => new AppConfiguration(
        latitude:null,
        longitude:null
    );


    public ConfigurationService(string? basePath = null)
    {
        basePath ??= AppContext.BaseDirectory;
        _configFilePath = Path.Combine(basePath, ConfigFileName);
        Configuration = Load();
        Configuration.Changed.Subscribe(_ => Save());
    }


    private AppConfiguration Load()
    {
        if (!File.Exists(_configFilePath))
        {
            Console.WriteLine($"Configuration file not found, creating {_configFilePath}");
            return Save(this.DEFAUTLS);
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            bool needsSave = false;

            double? latitude = Read(root, nameof(AppConfiguration.Latitude), DEFAUTLS.Latitude,
                el => el.GetDouble(), ref needsSave);

            double? longitude = Read(root, nameof(AppConfiguration.Longitude), DEFAUTLS.Longitude,
                el => el.GetDouble(), ref needsSave);

            var config = new AppConfiguration(latitude, longitude);

            if (needsSave)
                Save(config);

            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading configuration: {ex.Message}");
            return Save(this.DEFAUTLS);
        }
    }


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
            var dto = new ConfigurationDto(config.Latitude, config.Longitude);
            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving configuration: {ex.Message}");
        }

        return config;
    }
}
