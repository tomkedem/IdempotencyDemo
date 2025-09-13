using System.Data;

namespace PostalIdempotencyDemo.Api.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
