using System.Data;
using Microsoft.Data.SqlClient;
using PostalIdempotencyDemo.Api.Data;
using PostalIdempotencyDemo.Api.Models;

namespace PostalIdempotencyDemo.Api.Repositories
{
    public class IdempotencyRepository : IIdempotencyRepository
    {
        private readonly ISqlExecutor _sqlExecutor;

        public IdempotencyRepository(ISqlExecutor sqlExecutor)
        {
            _sqlExecutor = sqlExecutor;
        }

        // מחזיר את הרשומה האחרונה לפי request_path
        public async Task<IdempotencyEntry?> GetLatestByRequestPathAsync(string requestPath)
        {
            const string sql = @"
                SELECT TOP 1 idempotency_key, request_hash, response_body, status_code, 
                       created_at, expires_at, request_path, operation, related_entity_id 
                FROM idempotency_entries 
                WHERE request_path = @requestPath
                ORDER BY created_at DESC";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@requestPath", requestPath);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new IdempotencyEntry
                    {
                        IdempotencyKey = reader.GetString(reader.GetOrdinal("idempotency_key")),
                        RequestHash = reader.GetString(reader.GetOrdinal("request_hash")),
                        ResponseData = reader.IsDBNull(reader.GetOrdinal("response_body")) ? null : reader.GetString(reader.GetOrdinal("response_body")),
                        StatusCode = reader.GetInt32(reader.GetOrdinal("status_code")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                        ExpiresAt = reader.GetDateTime(reader.GetOrdinal("expires_at")),
                        Endpoint = reader.IsDBNull(reader.GetOrdinal("request_path")) ? string.Empty : reader.GetString(reader.GetOrdinal("request_path")),
                        HttpMethod = reader.GetString(reader.GetOrdinal("operation"))
                    };
                }
                return null;
            });
        }

        public async Task<IdempotencyEntry?> GetByKeyAsync(string key)
        {
            const string sql = @"
                SELECT idempotency_key, request_hash, response_body, status_code, 
                       created_at, expires_at, request_path, operation, related_entity_id 
                FROM idempotency_entries 
                WHERE idempotency_key = @key AND expires_at > @now";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@key", key);
                command.Parameters.AddWithValue("@now", DateTime.Now);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new IdempotencyEntry
                    {
                        IdempotencyKey = reader.GetString(reader.GetOrdinal("idempotency_key")),
                        RequestHash = reader.GetString(reader.GetOrdinal("request_hash")),
                        ResponseData = reader.IsDBNull(reader.GetOrdinal("response_body")) ? null : reader.GetString(reader.GetOrdinal("response_body")),
                        StatusCode = reader.GetInt32(reader.GetOrdinal("status_code")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                        ExpiresAt = reader.GetDateTime(reader.GetOrdinal("expires_at")),
                        Endpoint = reader.IsDBNull(reader.GetOrdinal("request_path")) ? string.Empty : reader.GetString(reader.GetOrdinal("request_path")),
                        HttpMethod = reader.GetString(reader.GetOrdinal("operation"))
                    };
                }
                return null;
            });
        }

        public async Task<bool> CreateAsync(IdempotencyEntry entry)
        {
            const string sql = @"
                INSERT INTO idempotency_entries 
                (idempotency_key, request_hash, response_body, status_code, 
                 created_at, expires_at, request_path, operation)
                VALUES (@key, @hash, @response, @statusCode, @createdAt, @expiresAt, @requestPath, @operation)";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@key", entry.IdempotencyKey);
                command.Parameters.AddWithValue("@hash", entry.RequestHash);
                command.Parameters.AddWithValue("@response", (object?)entry.ResponseData ?? DBNull.Value);
                command.Parameters.AddWithValue("@statusCode", entry.StatusCode);
                command.Parameters.AddWithValue("@createdAt", entry.CreatedAt);
                command.Parameters.AddWithValue("@expiresAt", entry.ExpiresAt);
                command.Parameters.AddWithValue("@requestPath", (object?)entry.Endpoint ?? DBNull.Value);
                command.Parameters.AddWithValue("@operation", entry.HttpMethod);

                var result = await command.ExecuteNonQueryAsync();

                return result > 0;
            });
        }

        public async Task<bool> UpdateResponseAsync(string key, string responseData, int statusCode)
        {
            const string sql = @"
                UPDATE idempotency_entries 
                SET response_body = @response, status_code = @statusCode 
                WHERE idempotency_key = @key";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@response", responseData);
                command.Parameters.AddWithValue("@statusCode", statusCode);
                command.Parameters.AddWithValue("@key", key);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            });
        }

        public async Task<bool> DeleteExpiredAsync()
        {
            const string sql = "DELETE FROM idempotency_entries WHERE expires_at <= @now";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@now", DateTime.Now);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            });
        }
    }
}
