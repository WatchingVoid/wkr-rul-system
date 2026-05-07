using System.Text.Json.Serialization;

namespace Backend.Api.Models;

public sealed class MlPredictRequest
{
    [JsonPropertyName("features")]
    public Dictionary<string, float> Features { get; set; } = new();
}

public sealed class MlPredictResponse
{
    [JsonPropertyName("rulMinutes")]
    public float RulMinutes { get; set; }

    [JsonPropertyName("alarmLevel")]
    public int AlarmLevel { get; set; }

    [JsonPropertyName("alarmCode")]
    public string AlarmCode { get; set; } = "";

    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("requiredAction")]
    public string RequiredAction { get; set; } = "";

    [JsonPropertyName("modelVersion")]
    public string ModelVersion { get; set; } = "";

    [JsonPropertyName("explanation")]
    public List<string> Explanation { get; set; } = new();

    [JsonPropertyName("usedFeatures")]
    public Dictionary<string, float> UsedFeatures { get; set; } = new();
}