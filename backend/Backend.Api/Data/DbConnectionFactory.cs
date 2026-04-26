using Npgsql;

namespace Backend.Api.Data;

public sealed class DbConnectionFactory
{
    private readonly string _connStr;

    public DbConnectionFactory(IConfiguration cfg)
    {
        _connStr = cfg.GetConnectionString("Pg")
                  ?? throw new InvalidOperationException("ConnectionStrings:Pg is missing");
    }

    public NpgsqlConnection Create() => new NpgsqlConnection(_connStr);
}