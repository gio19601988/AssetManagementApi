using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Dtos.Orders
{
public class ChangeStatusDto
{
    [StringLength(1000, ErrorMessage = "კომენტარის სიგრძე არ უნდა აღემატებოდეს 1000 სიმბოლოს")]
    public string? Comments { get; set; }
}
}