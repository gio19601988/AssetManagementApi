namespace AssetManagementApi.DTOs;
public class DeleteByMonthRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string? DepreciationBook { get; set; }
}