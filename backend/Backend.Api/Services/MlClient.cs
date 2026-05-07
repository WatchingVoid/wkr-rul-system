using System.Net.Http.Json;
using Backend.Api.Models;

namespace Backend.Api.Services;

public sealed class MlClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MlClient> _log;

    public MlClient(HttpClient http, ILogger<MlClient> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<MlPredictResponse?> PredictAsync(
        MlPredictRequest req,
        CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync("/predict", req, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            _log.LogWarning(
                "ML predict failed: status={StatusCode}, body={Body}",
                (int)response.StatusCode,
                body
            );

            return null;
        }

        return await response.Content.ReadFromJsonAsync<MlPredictResponse>(
            cancellationToken: ct
        );
    }
}