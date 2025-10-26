using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace WeaponPaints.Services
{
  public class ApiResponse<T>
  {
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
      return new ApiResponse<T>
      {
        Success = true,
        Data = data,
        Message = message,
        StatusCode = 200
      };
    }

    public static ApiResponse<T> ErrorResponse(string message, int statusCode = 500)
    {
      return new ApiResponse<T>
      {
        Success = false,
        Message = message,
        StatusCode = statusCode
      };
    }
  }

  public class ApiClientOptions
  {
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public bool EnableLogging { get; set; } = true;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
  }

  public interface IWeaponPaintsApiClient
  {
    Task<ApiResponse<T>> GetAsync<T>(string endpoint);
    Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data);
    Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data);
    Task<ApiResponse<bool>> DeleteAsync(string endpoint);

    // Métodos específicos para WeaponPaints
    Task<ApiResponse<List<WeaponInfo>>> GetWeaponPaintsAsync();
    Task<ApiResponse<WeaponInfo>> GetWeaponPaintByIdAsync(int id);
    Task<ApiResponse<WeaponInfo>> CreateWeaponPaintAsync(WeaponInfo weaponPaint);
    Task<ApiResponse<WeaponInfo>> UpdateWeaponPaintAsync(int id, WeaponInfo weaponPaint);
    Task<ApiResponse<bool>> DeleteWeaponPaintAsync(int id);
  }

  public class WeaponPaintsApiClient : IWeaponPaintsApiClient, IDisposable
  {
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ApiClientOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public WeaponPaintsApiClient(ApiClientOptions options, ILogger logger)
    {
      _options = options;
      _logger = logger;

      _httpClient = new HttpClient()
      {
        BaseAddress = new Uri(_options.BaseUrl),
        Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
      };

      // Configurar headers padrão
      if (!string.IsNullOrEmpty(_options.ApiKey))
      {
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
      }

      foreach (var header in _options.DefaultHeaders)
      {
        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
      }

      _jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
      return await ExecuteWithRetryAsync(async () =>
      {
        var response = await _httpClient.GetAsync(endpoint);
        return await ProcessResponseAsync<T>(response);
      });
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
    {
      return await ExecuteWithRetryAsync(async () =>
      {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, content);
        return await ProcessResponseAsync<T>(response);
      });
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
    {
      return await ExecuteWithRetryAsync(async () =>
      {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(endpoint, content);
        return await ProcessResponseAsync<T>(response);
      });
    }

    public async Task<ApiResponse<bool>> DeleteAsync(string endpoint)
    {
      return await ExecuteWithRetryAsync(async () =>
      {
        var response = await _httpClient.DeleteAsync(endpoint);
        return new ApiResponse<bool>
        {
          Success = response.IsSuccessStatusCode,
          Data = response.IsSuccessStatusCode,
          StatusCode = (int)response.StatusCode,
          Message = response.IsSuccessStatusCode ? "Deleted successfully" : "Delete failed"
        };
      });
    }

    // Métodos específicos para WeaponPaints
    public async Task<ApiResponse<List<WeaponInfo>>> GetWeaponPaintsAsync()
    {
      return await GetAsync<List<WeaponInfo>>("api/weaponpaints");
    }

    public async Task<ApiResponse<WeaponInfo>> GetWeaponPaintByIdAsync(int id)
    {
      return await GetAsync<WeaponInfo>($"api/weaponpaints/{id}");
    }

    public async Task<ApiResponse<WeaponInfo>> CreateWeaponPaintAsync(WeaponInfo weaponPaint)
    {
      return await PostAsync<WeaponInfo>("api/weaponpaints", weaponPaint);
    }

    public async Task<ApiResponse<WeaponInfo>> UpdateWeaponPaintAsync(int id, WeaponInfo weaponPaint)
    {
      return await PutAsync<WeaponInfo>($"api/weaponpaints/{id}", weaponPaint);
    }

    public async Task<ApiResponse<bool>> DeleteWeaponPaintAsync(int id)
    {
      return await DeleteAsync($"api/weaponpaints/{id}");
    }

    private async Task<ApiResponse<T>> ProcessResponseAsync<T>(HttpResponseMessage response)
    {
      try
      {
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
          var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
          return ApiResponse<T>.SuccessResponse(data!, "Request successful");
        }
        else
        {
          if (_options.EnableLogging)
          {
            _logger.LogError($"API request failed: {response.StatusCode} - {content}");
          }

          return ApiResponse<T>.ErrorResponse(
              $"Request failed: {response.ReasonPhrase}",
              (int)response.StatusCode);
        }
      }
      catch (JsonException ex)
      {
        if (_options.EnableLogging)
        {
          _logger.LogError(ex, "Failed to deserialize API response");
        }

        return ApiResponse<T>.ErrorResponse("Failed to process response", 500);
      }
    }

    private async Task<ApiResponse<T>> ExecuteWithRetryAsync<T>(Func<Task<ApiResponse<T>>> operation)
    {
      var lastException = new Exception();

      for (int attempt = 0; attempt <= _options.RetryCount; attempt++)
      {
        try
        {
          return await operation();
        }
        catch (HttpRequestException ex)
        {
          lastException = ex;
          if (_options.EnableLogging)
          {
            _logger.LogWarning($"API request attempt {attempt + 1} failed: {ex.Message}");
          }

          if (attempt == _options.RetryCount)
            break;

          await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
        }
        catch (TaskCanceledException ex)
        {
          lastException = ex;
          if (_options.EnableLogging)
          {
            _logger.LogError(ex, "API request timed out");
          }
          break;
        }
      }

      return ApiResponse<T>.ErrorResponse($"Request failed after {_options.RetryCount + 1} attempts: {lastException.Message}");
    }

    public void Dispose()
    {
      _httpClient?.Dispose();
    }
  }
}
