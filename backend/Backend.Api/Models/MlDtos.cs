namespace Backend.Api.Models;

public sealed class MlPredictRequest
{
    public Dictionary<string, float> Features { get; set; } = new();
}

public sealed class MlPredictResponse
{
    public float RulMinutes { get; set; }
    public int AlarmLevel { get; set; }
    public string? ModelVersion { get; set; }
}