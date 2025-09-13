using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace PostalIdempotencyDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestController> _logger;

    public TestController(IConfiguration configuration, ILogger<TestController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("connection")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            _logger.LogInformation("Using connection string: {ConnectionString}", connectionString);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            _logger.LogInformation("Database connection successful");
            return Ok(new { message = "Database connection successful", timestamp = DateTime.Now, connectionString = connectionString?.Substring(0, Math.Min(50, connectionString.Length)) + "..." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection failed");
            return StatusCode(500, new { error = "Database connection failed", details = ex.Message, innerException = ex.InnerException?.Message });
        }
    }

    [HttpGet("shipments")]
    public async Task<IActionResult> GetShipments()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string sql = "SELECT TOP 10 * FROM shipments ORDER BY created_at DESC";
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var shipments = new List<object>();
            while (await reader.ReadAsync())
            {
                shipments.Add(new
                {
                    id = reader["id"],
                    barcode = reader["barcode"],
                    customerName = reader["customer_name"],
                    address = reader["address"],
                    weight = reader["weight"],
                    price = reader["price"],
                    status = reader["status"],
                    createdAt = reader["created_at"],
                    notes = reader["notes"]
                });
            }

            return Ok(new { count = shipments.Count, data = shipments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipments");
            return StatusCode(500, new { error = "Failed to retrieve shipments", details = ex.Message });
        }
    }
}
