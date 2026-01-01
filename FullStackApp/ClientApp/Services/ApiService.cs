using System.Text.Json;

namespace ClientApp.Services;

/// <summary>
/// Reusable service for making API calls with caching, error handling, and timeout support
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService>? _logger;
    private const int DefaultTimeoutSeconds = 30;
    
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public ApiService(HttpClient httpClient, ILogger<ApiService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Fetches data from an API endpoint with error handling
    /// </summary>
    public async Task<ApiResult<T>> GetAsync<T>(
        string url, 
        int timeoutSeconds = DefaultTimeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            
            var response = await _httpClient.GetAsync(url, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = $"Server returned {response.StatusCode}";
                _logger?.LogWarning(error);
                return ApiResult<T>.Failure(error);
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync(cts.Token);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return ApiResult<T>.Failure("Server returned empty response");
            }
            
            var data = JsonSerializer.Deserialize<T>(jsonContent, DefaultJsonOptions);
            
            if (data == null)
            {
                return ApiResult<T>.Failure("Failed to deserialize response");
            }
            
            return ApiResult<T>.Success(data);
        }
        catch (TaskCanceledException)
        {
            var error = $"Request timed out after {timeoutSeconds} seconds";
            _logger?.LogError(error);
            return ApiResult<T>.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            var error = $"Network error: {ex.Message}";
            _logger?.LogError(ex, error);
            return ApiResult<T>.Failure(error);
        }
        catch (JsonException ex)
        {
            var error = $"JSON error: {ex.Message}";
            _logger?.LogError(ex, error);
            return ApiResult<T>.Failure(error);
        }
        catch (Exception ex)
        {
            var error = $"Unexpected error: {ex.Message}";
            _logger?.LogError(ex, error);
            return ApiResult<T>.Failure(error);
        }
    }
}

/// <summary>
/// Result wrapper for API calls
/// </summary>
public class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApiResult<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    public static ApiResult<T> Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
