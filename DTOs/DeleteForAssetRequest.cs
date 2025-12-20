namespace AssetManagementApi.DTOs;
public class DeleteForAssetRequest
{
    public int AssetId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}