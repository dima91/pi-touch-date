using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Data;

namespace PiTouchDate.Services;

public class LocationingService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://geocode.maps.co/reverse";

    public LocationingService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<Optional<string>> GetLocationNameAsync(double latitude, double longitude, string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("Unknown Location (No API Key)");
        }

        try
        {
            var url = FormattableString.Invariant($"{BaseUrl}?lat={latitude}&lon={longitude}&api_key={apiKey}");
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            
            if (doc.RootElement.TryGetProperty("address", out var address))
            {
                if (address.TryGetProperty("city", out var city) && city.GetString() is { } c) return c;
                if (address.TryGetProperty("town", out var town) && town.GetString() is { } t) return t;
                if (address.TryGetProperty("village", out var village) && village.GetString() is { } v) return v;
            }
            
            return "Unknown location";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching location name: {ex.Message}");
            return "Unknown location";
        }
    }
    
}