using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Api.Data;
using Backend.Api.Services;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JSON (можно оставить как было)
builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
});

builder.Services.AddSingleton<FeatureExtractor>();

builder.Services.AddHttpClient<MlClient>(client =>
{
    var baseUrl = builder.Configuration["Ml:BaseUrl"] ?? "http://localhost:8001";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// DB + репозитории
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddSingleton<TelemetryRepository>();
builder.Services.AddSingleton<RulRepository>();

builder.Services.AddHostedService<RulWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTimeOffset.UtcNow }));

app.Run();