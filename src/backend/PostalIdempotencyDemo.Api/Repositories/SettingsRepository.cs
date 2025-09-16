using Dapper;
using PostalIdempotencyDemo.Api.Data;
using PostalIdempotencyDemo.Api.Models;

namespace PostalIdempotencyDemo.Api.Repositories;

public class SettingsRepository(IDbConnectionFactory connectionFactory) : ISettingsRepository
{
    public async Task<IEnumerable<SystemSetting>> GetSettingsAsync()
    {
        const string query = "SELECT * FROM SystemSettings";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<SystemSetting>(query);
    }

    public async Task<bool> UpdateSettingsAsync(IEnumerable<SystemSetting> settings)
    {
        const string sql = @"
            UPDATE SystemSettings WITH (UPDLOCK, HOLDLOCK)
            SET SettingValue = @SettingValue, UpdatedAt = GETUTCDATE()
            WHERE SettingKey = @SettingKey;

            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO SystemSettings (SettingKey, SettingValue, UpdatedAt)
                VALUES (@SettingKey, @SettingValue, GETUTCDATE());
            END";

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            foreach (var setting in settings)
            {
                await connection.ExecuteAsync(sql, setting, transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }
}
