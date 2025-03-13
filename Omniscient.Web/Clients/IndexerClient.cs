using System.Net.Sockets;
using Omniscient.Shared;
using Omniscient.Shared.Dtos;
using Polly;
using Polly.CircuitBreaker;

namespace Omniscient.Web.Clients;

public class IndexerClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IndexerClient> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
    
    public CircuitState CircuitState => _circuitBreakerPolicy.CircuitState;

    public IndexerClient(HttpClient httpClient, ILogger<IndexerClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        
        _circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<SocketException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(5),
                onBreak: (ex, time) => logger.LogWarning("Indexer circuit opened: {Message}", ex.Message),
                onReset: () => logger.LogInformation("Indexer circuit closed"),
                onHalfOpen: () => logger.LogInformation("Indexer circuit half-open")
            );
    }

    public async Task<PaginatedList<EmailDto>> SearchEmailsAsync(string query, int pageNumber = 1, int pageSize = 10)
    {
        var response = await ExecuteInPolicyAsync(async () =>
        {
            _logger.LogInformation("SearchEmailsAsync: {query}", query);
            var response = await _httpClient.GetAsync($"api/indexer?query={query}&pageNumber={pageNumber}&pageSize={pageSize}");
            _logger.LogInformation("SearchEmailsAsync Response: {response}", response);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaginatedList<EmailDto>>();
        });
        
        return response ?? new PaginatedList<EmailDto>([], 0, 0, 0);
    }
    
    public async Task<string> GetFullContentAsync(Guid id)
    {
        var response =  await ExecuteInPolicyAsync(async () =>
        {
            _logger.LogInformation("GetFullContentAsync: {id}", id);
            var response = await _httpClient.GetAsync($"api/indexer/full-content/{id}");
            _logger.LogInformation("GetFullContentAsync Response: {response}", response);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        });

        return response ?? string.Empty;
    }
    
    private async Task<T?> ExecuteInPolicyAsync<T>(Func<Task<T>> action)
    {
        if (_circuitBreakerPolicy.CircuitState == CircuitState.Open)
        {
            _logger.LogWarning("Circuit is open. Failing fast.");
            return default;
        }
        
        try
        {
            var policyResult = await _circuitBreakerPolicy.ExecuteAndCaptureAsync(action);
            if (policyResult.Outcome == OutcomeType.Failure)
            {
                _logger.LogWarning("Policy failed: {Message}", policyResult.FinalException?.Message);
            }
            
            return policyResult.Result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning("Circuit is open: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ActionName} failed: {Message}", action.Method.Name, ex.Message);
        }
        
        return default;
    }
}