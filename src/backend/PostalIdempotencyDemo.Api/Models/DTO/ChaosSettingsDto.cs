namespace PostalIdempotencyDemo.Api.Models.DTO;

public class ChaosSettingsDto
{
    public bool UseIdempotencyKey { get; set; }
   public int IdempotencyExpirationHours { get; set; } = 24;
  
}
