using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace PostalIdempotencyDemo.Api.Data;

public class SqlServerConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
