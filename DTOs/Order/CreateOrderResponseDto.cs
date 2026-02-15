// DTO for the response of creating an order
using System.ComponentModel.DataAnnotations;
public class CreateOrderResponseDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
    public string EmailMessage { get; set; } = string.Empty;
}