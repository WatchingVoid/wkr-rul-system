using Backend.Api.Data;
using Backend.Api.Services;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<DbConnectionFactory>();

builder.Services.AddSingleton<TelemetryRepository>();
builder.Services.AddSingleton<RulRepository>();
builder.Services.AddSingleton<AlarmRepository>();
builder.Services.AddSingleton<FeatureExtractor>();
builder.Services.AddSingleton<MachineStateResolver>();

builder.Services.AddHttpClient<MlClient>(client =>
{
    var baseUrl = builder.Configuration["Ml:BaseUrl"] ?? "http://ml:8001";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHostedService<RulWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new
{
    ok = true,
    service = "backend",
    utc = DateTimeOffset.UtcNow
}));

app.MapControllers();

app.Run();