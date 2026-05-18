namespace PiTouchDate.Services;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

public class WeatherDataService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast?daily=temperature_2m_max,temperature_2m_min&" +
                                   "hourly=temperature_2m,weather_code&current=weather_code,temperature_2m,is_day&timezone=auto&forecast_days=1";

    public WeatherDataService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<HttpResponseMessage?> GetWeatherAsync(double latitude, double longitude)
    {
        try
        {
            var url = FormattableString.Invariant($"{BaseUrl}&latitude={latitude}&longitude={longitude}");
            var response = await _httpClient.GetAsync(url);
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching weather: {ex.Message}");
            return null;
        }
    }
}
