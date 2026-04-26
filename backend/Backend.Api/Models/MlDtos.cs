namespace Backend.Api.Models;

public sealed class MlPredictRequest
{
    public List<float> Power { get; set; } = new();
    public List<float> Current { get; set; } = new();
    public List<float> Rpm { get; set; } = new();
}

public sealed class MlPredictResponse
{
    public float Rul_Minutes { get; set; }
    public int Alarm_Level { get; set; }
    public string? Model_Version { get; set; }
    public Dictionary<string, float>? Features { get; set; }
}
