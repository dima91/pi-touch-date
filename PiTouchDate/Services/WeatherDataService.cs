namespace PiTouchDate.Services;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class WeatherDataService
{
    public class WeatherData
    {
        public double Temperature { get; init; }
        public double? MaxTemperature { get; init; }
        public double? MinTemperature { get; init; }
        public int WeatherCode { get; init; }
        public bool IsDay { get; init; }

        public string Description => WeatherCode switch
        {
            0 or 1        => "Sereno",
            2             => "Parzialmente nuvoloso",
            3             => "Nuvoloso",
            45 or 48      => "Nebbia",
            51 or 53 or 55 => "Pioviggine",
            56 or 57      => "Pioviggine congelata",
            61 or 63 or 65 => "Pioggia",
            66 or 67      => "Pioggia congelata",
            71 or 73 or 75 => "Neve",
            77            => "Grandine",
            80 or 81 or 82 => "Rovesci di pioggia",
            85 or 86      => "Rovesci di neve",
            95            => "Temporale",
            96 or 99      => "Temporale con grandine",
            _             => "Sconosciuto"
        };
    }


    private readonly HttpClient _httpClient;
    private const string BaseUrl =
        "https://api.open-meteo.com/v1/forecast" +
        "?current=weather_code,temperature_2m,is_day" +
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

            return new WeatherData
            {
                Temperature = temperature,
                WeatherCode = weatherCode,
                IsDay = isDay,
                MaxTemperature = maxTemp,
                MinTemperature = minTemp
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching weather: {ex.Message}");
            return null;
        }
    }
}
