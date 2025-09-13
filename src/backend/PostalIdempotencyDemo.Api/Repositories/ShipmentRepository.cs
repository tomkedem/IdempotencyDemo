using System.Data;
using Microsoft.Data.SqlClient;
using PostalIdempotencyDemo.Api.Data;
using PostalIdempotencyDemo.Api.Models;

namespace PostalIdempotencyDemo.Api.Repositories
{

    public class ShipmentRepository : IShipmentRepository
    {
        private readonly ISqlExecutor _sqlExecutor;
        private readonly ILogger<ShipmentRepository> _logger;

        public ShipmentRepository(ISqlExecutor sqlExecutor, ILogger<ShipmentRepository> logger)
        {
            _sqlExecutor = sqlExecutor;
            _logger = logger;
        }

        public async Task<Shipment> CreateAsync(Shipment shipment)
        {
            const string query = @"INSERT INTO shipments (id, barcode, customer_name, address, weight, price, status_id, created_at, notes)
                                 VALUES (@id, @barcode, @customer_name, @address, @weight, @price, @status_id, @created_at, @notes)";

            shipment.Id = Guid.NewGuid();
            shipment.CreatedAt = DateTime.Now;

            await _sqlExecutor.ExecuteAsync(async connection =>
            {
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", shipment.Id);
                command.Parameters.AddWithValue("@barcode", shipment.Barcode);
                command.Parameters.AddWithValue("@customer_name", (object?)shipment.CustomerName ?? DBNull.Value);
                command.Parameters.AddWithValue("@address", (object?)shipment.Address ?? DBNull.Value);
                command.Parameters.AddWithValue("@weight", shipment.Weight);
                command.Parameters.AddWithValue("@price", (object?)shipment.Price ?? DBNull.Value);
                command.Parameters.AddWithValue("@status_id", shipment.StatusId);
                command.Parameters.AddWithValue("@created_at", shipment.CreatedAt);
                command.Parameters.AddWithValue("@notes", (object?)shipment.Notes ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            });
            return shipment;
        }

        public async Task<Shipment?> GetByBarcodeAsync(string barcode)
        {
            const string query = @"SELECT s.id, s.barcode, s.customer_name, s.address, s.weight, s.price, 
                                      s.status_id, ss.status_name, ss.status_name_he, s.created_at, s.updated_at, s.notes
                               FROM shipments s
                               JOIN shipment_statuses ss ON s.status_id = ss.id
                               WHERE s.barcode = @barcode";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@barcode", barcode);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapShipmentFromReader(reader);
                }
                return null;
            });
        }

        public async Task<Shipment?> GetByIdAsync(Guid id)
        {
            const string query = @"SELECT s.id, s.barcode, s.customer_name, s.address, s.weight, s.price, 
                                      s.status_id, ss.status_name, ss.status_name_he, s.created_at, s.updated_at, s.notes
                               FROM shipments s
                               JOIN shipment_statuses ss ON s.status_id = ss.id
                               WHERE s.id = @id";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapShipmentFromReader(reader);
                }
                return null;
            });
        }

        public async Task<bool> UpdateStatusAsync(Guid id, ShipmentStatus status)
        {
            const string query = "UPDATE shipments SET status_id = @status_id, updated_at = @updated_at WHERE id = @id";
            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@status_id", (int)status);
                command.Parameters.AddWithValue("@updated_at", DateTime.Now);
                command.Parameters.AddWithValue("@id", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            });
        }

        public async Task<bool> ExistsAsync(string barcode)
        {
            const string query = "SELECT COUNT(1) FROM shipments WHERE barcode = @barcode";
            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@barcode", barcode);

                var count = await command.ExecuteScalarAsync();
                return Convert.ToInt32(count) > 0;
            });
        }

        public async Task<IEnumerable<Shipment>> GetAllAsync()
        {
            const string query = @"SELECT s.id, s.barcode, s.customer_name, s.address, s.weight, s.price, 
                                      s.status_id, ss.status_name, ss.status_name_he, s.created_at, s.updated_at, s.notes
                               FROM shipments s
                               JOIN shipment_statuses ss ON s.status_id = ss.id
                               ORDER BY s.created_at DESC";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                var shipments = new List<Shipment>();
                var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    shipments.Add(MapShipmentFromReader(reader));
                }
                return shipments;
            });
        }

        public async Task<bool> UpdateAsync(Shipment shipment)
        {
            const string query = @"UPDATE shipments SET barcode = @barcode, customer_name = @customer_name, address = @address, weight = @weight, price = @price, status_id = @status_id, updated_at = @updated_at, notes = @notes WHERE id = @id";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", shipment.Id);
                command.Parameters.AddWithValue("@barcode", shipment.Barcode);
                command.Parameters.AddWithValue("@customer_name", (object?)shipment.CustomerName ?? DBNull.Value);
                command.Parameters.AddWithValue("@address", (object?)shipment.Address ?? DBNull.Value);
                command.Parameters.AddWithValue("@weight", shipment.Weight);
                command.Parameters.AddWithValue("@price", (object?)shipment.Price ?? DBNull.Value);
                command.Parameters.AddWithValue("@status_id", shipment.StatusId);
                command.Parameters.AddWithValue("@updated_at", DateTime.Now);
                command.Parameters.AddWithValue("@notes", (object?)shipment.Notes ?? DBNull.Value);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            });
        }

        private static Shipment MapShipmentFromReader(SqlDataReader reader)
        {
            return new Shipment
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                Barcode = reader.GetString(reader.GetOrdinal("barcode")),
                CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name")) ? null : reader.GetString(reader.GetOrdinal("customer_name")),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                Weight = reader.IsDBNull(reader.GetOrdinal("weight")) ? 0 : reader.GetDecimal(reader.GetOrdinal("weight")),
                Price = reader.IsDBNull(reader.GetOrdinal("price")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("price")),
                StatusId = reader.GetInt32(reader.GetOrdinal("status_id")),
                StatusName = reader.IsDBNull(reader.GetOrdinal("status_name")) ? null : reader.GetString(reader.GetOrdinal("status_name")),
                StatusNameHe = reader.IsDBNull(reader.GetOrdinal("status_name_he")) ? null : reader.GetString(reader.GetOrdinal("status_name_he")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes"))
            };
        }
    }
}
