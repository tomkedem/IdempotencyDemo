using System.Threading.Tasks;
using PostalIdempotencyDemo.Api.Repositories;

namespace PostalIdempotencyDemo.Api.Repositories
{
    public class DeliveryRepository : IDeliveryRepository
    {
        private readonly Data.ISqlExecutor _sqlExecutor;

        public DeliveryRepository(Data.ISqlExecutor sqlExecutor)
        {
            _sqlExecutor = sqlExecutor;
        }

        public async Task<Models.Delivery?> GetDeliveryByBarcodeAsync(string barcode)
        {
            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                const string query = @"SELECT TOP 1 id, barcode, employee_id, delivery_date, location_lat, location_lng, recipient_name, status_id, notes, created_at, updated_at FROM deliveries WHERE barcode = @barcode ORDER BY delivery_date DESC";
                using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
                command.Parameters.AddWithValue("@barcode", barcode);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var statusId = reader.IsDBNull(reader.GetOrdinal("status_id")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("status_id"));
                    string? statusNameHe = statusId
                    switch
                    {
                        1 => "נוצר",
                        2 => "נמסר",
                        3 => "נכשל",
                        4 => "נמסר חלקית",
                        5 => "בדרך",
                        6 => "בדרך לחלוקה",
                        7 => "חריגה",
                        _ => null
                    };


                    return new Models.Delivery
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("id")),
                        Barcode = reader.GetString(reader.GetOrdinal("barcode")),
                        EmployeeId = reader.IsDBNull(reader.GetOrdinal("employee_id")) ? null : reader.GetString(reader.GetOrdinal("employee_id")),
                        DeliveryDate = reader.GetDateTime(reader.GetOrdinal("delivery_date")),
                        LocationLat = reader.IsDBNull(reader.GetOrdinal("location_lat")) ? null : (double)reader.GetDecimal(reader.GetOrdinal("location_lat")),
                        LocationLng = reader.IsDBNull(reader.GetOrdinal("location_lng")) ? null : (double)reader.GetDecimal(reader.GetOrdinal("location_lng")),
                        RecipientName = reader.IsDBNull(reader.GetOrdinal("recipient_name")) ? null : reader.GetString(reader.GetOrdinal("recipient_name")),
                        StatusId = reader.GetInt32(reader.GetOrdinal("status_id")),
                        StatusNameHe = statusNameHe,
                        Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                    };
                }
                return null;
            });
        }

        public async Task CreateDeliveryAsync(object delivery)
        {
            // TODO: Implement actual data access logic here
            await Task.CompletedTask;
        }

        public async Task<Models.Shipment?> UpdateDeliveryStatusAsync(string barcode, int statusId)
        {
            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                // Update status in both deliveries and shipments tables, and update updated_at in shipments
                const string sqlDeliveries = "UPDATE deliveries SET status_id = @statusId, updated_at = @updatedAt WHERE barcode = @barcode";
                const string sqlShipments = "UPDATE shipments SET status_id = @statusId, updated_at = @updatedAt WHERE barcode = @barcode";

                using var cmdDeliveries = new Microsoft.Data.SqlClient.SqlCommand(sqlDeliveries, connection);
                cmdDeliveries.Parameters.AddWithValue("@statusId", statusId);
                cmdDeliveries.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                cmdDeliveries.Parameters.AddWithValue("@barcode", barcode);
                int rowsDeliveries = await cmdDeliveries.ExecuteNonQueryAsync();

                using var cmdShipments = new Microsoft.Data.SqlClient.SqlCommand(sqlShipments, connection);
                cmdShipments.Parameters.AddWithValue("@statusId", statusId);
                cmdShipments.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                cmdShipments.Parameters.AddWithValue("@barcode", barcode);
                int rowsShipments = await cmdShipments.ExecuteNonQueryAsync();

                if (rowsDeliveries == 0 && rowsShipments == 0)
                {
                    return null;
                }

                return await GetShipmentByBarcodeAsync(barcode);
            });
        }

        public async Task<Models.Shipment?> GetShipmentByBarcodeAsync(string barcode)
        {
            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                const string query = @"
                    SELECT 
                        s.id, s.barcode, s.customer_name, s.address, s.weight, s.price, s.status_id, s.created_at, s.updated_at, s.notes,
                        d.employee_id, d.delivery_date, d.location_lat, d.location_lng, d.recipient_name, d.status_id AS delivery_status_id, d.notes AS delivery_notes, d.created_at AS delivery_created_at
                    FROM shipments s
                    LEFT JOIN deliveries d ON s.barcode = d.barcode
                    WHERE s.barcode = @barcode";

                using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
                command.Parameters.AddWithValue("@barcode", barcode);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var statusId = reader.IsDBNull(reader.GetOrdinal("status_id")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("status_id"));
                    string? statusNameHe = statusId
                    switch
                    {
                        1 => "נוצר",
                        2 => "נמסר",
                        3 => "נכשל",
                        4 => "נמסר חלקית",
                        5 => "בדרך",
                        6 => "בדרך לחלוקה",
                        7 => "חריגה",
                        _ => null
                    };
                    return new Models.Shipment
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("id")),
                        Barcode = reader.GetString(reader.GetOrdinal("barcode")),
                        CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name")) ? null : reader.GetString(reader.GetOrdinal("customer_name")),
                        Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                        Weight = reader.IsDBNull(reader.GetOrdinal("weight")) ? null : reader.GetDecimal(reader.GetOrdinal("weight")),
                        Price = reader.IsDBNull(reader.GetOrdinal("price")) ? null : reader.GetDecimal(reader.GetOrdinal("price")),
                        Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                        StatusId = statusId,
                        StatusNameHe = statusNameHe,
                        // ניתן להוסיף כאן שדות נוספים ממסירת המשלוח (deliveries) אם נדרש
                    };
                }
                return null;
            });
        }
    }
}
