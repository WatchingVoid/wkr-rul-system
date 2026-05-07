using Npgsql;

namespace Backend.Api.Data;

public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration cfg)
    {
        _connectionString = cfg.GetConnectionString("Pg")
            ?? throw new InvalidOperationException("ConnectionStrings:Pg is missing");
    }

    public NpgsqlConnection Create()
    {
        return new NpgsqlConnection(_connectionString);
    }
}