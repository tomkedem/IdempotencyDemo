using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;

namespace PostalIdempotencyDemo.Api.Data
{
    public interface ISqlExecutor
    {
        Task<TResult> ExecuteAsync<TResult>(Func<SqlConnection, Task<TResult>> action);
        Task ExecuteAsync(Func<SqlConnection, Task> action);
    }

    public class SqlExecutor(IConfiguration configuration, ILogger<SqlExecutor> logger) : ISqlExecutor
    {
        public async Task<TResult> ExecuteAsync<TResult>(Func<SqlConnection, Task<TResult>> action)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            await using var connection = new SqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();
                return await action(connection);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SQL execution error");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task ExecuteAsync(Func<SqlConnection, Task> action)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            await using var connection = new SqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();
                await action(connection);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SQL execution error");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }
}
