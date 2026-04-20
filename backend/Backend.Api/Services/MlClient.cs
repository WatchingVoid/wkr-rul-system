using System.Net.Http.Json;
using Backend.Api.Models;

namespace Backend.Api.Services;

public sealed class MlClient
{
    private readonly HttpClient _http;

    public MlClient(HttpClient http) => _http = http;

    public async Task<MlPredictResponse?> PredictAsync(MlPredictRequest req, CancellationToken ct)
    {
        var resp = await _http.PostAsJsonAsync("/predict", req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<MlPredictResponse>(cancellationToken: ct);
    }
}