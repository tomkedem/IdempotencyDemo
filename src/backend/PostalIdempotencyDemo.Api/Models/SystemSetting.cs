namespace PostalIdempotencyDemo.Api.Models;

public class SystemSetting
{
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = "string";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
