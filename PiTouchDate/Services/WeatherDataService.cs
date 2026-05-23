namespace PiTouchDate.Services;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

public class WeatherDataService
{
    public class HourlyInfo
    {
        public double Temperature { get; init; }
        public int WeatherCode { get; init; }
    }

    public class WeatherData
    {
        public static string GetWeatherCodeDescription(int weatherCode) => weatherCode switch
        {
            0 or 1          => "Sereno",
            2               => "Parzialmente nuvoloso",
            3               => "Nuvoloso",
            45 or 48        => "Nebbia",
            51 or 53 or 55  => "Pioviggine",
            56 or 57        => "Pioviggine congelata",
            61 or 63 or 65  => "Pioggia",
            66 or 67        => "Pioggia congelata",
            71 or 73 or 75  => "Neve",
            77              => "Grandine",
            80 or 81 or 82  => "Rovesci di pioggia",
            85 or 86        => "Rovesci di neve",
            95              => "Temporale",
            96 or 99        => "Temporale con grandine",
            _               => "Sconosciuto"
        };

        public static Bitmap? GetWeatherCodeIcon(int weatherCode)
        {
            var filename = weatherCode switch
            {
                0 or 1          => "sun.png",
                2               => "cloudy.png",
                3               => "clouds.png",
                45 or 48        => "fog.png",
                51 or 53 or 55  => "light-rain.png",
                56 or 57        => "freezing-rain.png",
                61 or 63        => "rain.png",
                65              => "heavy-rain.png",
                66 or 67        => "freezing-rain.png",
                71 or 73        => "snow.png",
                75              => "heavy-snow.png",
                77              => "snow.png",
                80 or 81        => "rain.png",
                82              => "heavy-rain.png",
                85              => "snow.png",
                86              => "heavy-snow.png",
                95 or 96 or 99  => "thunder.png",
                _               => null
            };

            if (filename is null)
                return null;
            using var stream = typeof(WeatherData).Assembly
                .GetManifestResourceStream($"PiTouchDate.Assets.{filename}");
            return new Bitmap(stream!);
        }

        public double CurrentTemperature { get; init; }
        public double? MaxTemperature { get; init; }
        public double? MinTemperature { get; init; }
        public int WeatherCode { get; init; }
        public bool IsDay { get; init; }
        public Dictionary<DateTime, HourlyInfo> HourlyInfo { get; init; } = new();

        public Bitmap? WeatherIcon => GetWeatherCodeIcon(WeatherCode);
        public bool HasWeatherIcon => WeatherIcon != null;
        public string Description => GetWeatherCodeDescription(WeatherCode);
    }


    private readonly HttpClient _httpClient;
    private const string BaseUrl =
        "https://api.open-meteo.com/v1/forecast" +
        "?current=weather_code,temperature_2m,is_day" +
        "&hourly=temperature_2m,weather_code" +
        "&daily=temperature_2m_max,temperature_2m_min" +
        "&timezone=auto&forecast_days=1";

    public WeatherDataService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<WeatherData?> GetWeatherAsync(double latitude, double longitude)
    {
        try
        {
            var url = FormattableString.Invariant($"{BaseUrl}&latitude={latitude}&longitude={longitude}");
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            double temperature = 0;
            int weatherCode = 0;
            bool isDay = true;
            double? maxTemp = null;
            double? minTemp = null;
            var hourlyInfo = new Dictionary<DateTime, HourlyInfo>();

            if (root.TryGetProperty("current", out var current))
            {
                if (current.TryGetProperty("temperature_2m", out var temp))
                    temperature = temp.GetDouble();
                if (current.TryGetProperty("weather_code", out var code))
                    weatherCode = code.GetInt32();
                if (current.TryGetProperty("is_day", out var day))
                    isDay = day.GetInt32() == 1;
            }

            if (root.TryGetProperty("daily", out var daily))
            {
                if (daily.TryGetProperty("temperature_2m_max", out var maxArr) && maxArr.GetArrayLength() > 0)
                    maxTemp = maxArr[0].GetDouble();
                if (daily.TryGetProperty("temperature_2m_min", out var minArr) && minArr.GetArrayLength() > 0)
                    minTemp = minArr[0].GetDouble();
            }

            if (root.TryGetProperty("hourly", out var hourly))
            {
                if (hourly.TryGetProperty("time", out var times) &&
                    hourly.TryGetProperty("temperature_2m", out var temps) &&
                    hourly.TryGetProperty("weather_code", out var codes))
                {
                    for (int i = 0; i < times.GetArrayLength(); i++)
                    {
                        if (DateTime.TryParse(times[i].GetString(), out var time))
                        {
                            hourlyInfo[time] = new HourlyInfo
                            {
                                Temperature = temps[i].GetDouble(),
                                WeatherCode = codes[i].GetInt32()
                            };
                        }
                    }
                }
                
            }

            return new WeatherData
            {
                CurrentTemperature = temperature,
                WeatherCode = weatherCode,
                IsDay = isDay,
                MaxTemperature = maxTemp,
                MinTemperature = minTemp,
                HourlyInfo = hourlyInfo
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching weather: {ex.Message}");
            return null;
        }
    }
}
