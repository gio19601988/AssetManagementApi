using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;                    // <-- ეს დაამატე! (აუცილებელია CommandType-სთვის)
using AssetManagementApi.Models.DTO;

namespace AssetManagementApi.Repositories;

public class AssetRepository
{
    private readonly string _connectionString;

    public AssetRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    // GET ALL
public async Task<IEnumerable<AssetDto>> GetAllAsync()
{
    const string sql = @"
        SELECT a.*, 
               c.Name AS CategoryName,
               d.Name AS DepartmentName,
               ISNULL(l.RoomNumber, '') + ' (' + ISNULL(b.Name, '') + ')' AS LocationName,
               e.FullName AS ResponsiblePersonName,
               s.StatusName AS AssetStatusName,
               m.Name AS DepreciationMethodName
        FROM Assets a
        LEFT JOIN Categories c ON a.CategoryId = c.Id
        LEFT JOIN Departments d ON a.DepartmentId = d.Id
        LEFT JOIN Locations l ON a.LocationId = l.Id
        LEFT JOIN Buildings b ON l.BuildingId = b.Id
        LEFT JOIN Employees e ON a.ResponsiblePersonId = e.Id
        LEFT JOIN AssetStatus s ON a.AssetStatusId = s.Id
        LEFT JOIN DepreciationMethods m ON a.DepreciationMethodId = m.Id
        ORDER BY a.CreatedAt DESC";

    await using var conn = CreateConnection();
    return await conn.QueryAsync<AssetDto>(sql);
}

// GET BY ID
public async Task<AssetDto?> GetByIdAsync(int id)
{
    const string sql = @"
        SELECT a.*, 
               c.Name AS CategoryName,
               d.Name AS DepartmentName,
               ISNULL(l.RoomNumber, '') + ' (' + ISNULL(b.Name, '') + ')' AS LocationName,
               e.FullName AS ResponsiblePersonName,
               s.StatusName AS AssetStatusName,
               m.Name AS DepreciationMethodName
        FROM Assets a
        LEFT JOIN Categories c ON a.CategoryId = c.Id
        LEFT JOIN Departments d ON a.DepartmentId = d.Id
        LEFT JOIN Locations l ON a.LocationId = l.Id
        LEFT JOIN Buildings b ON l.BuildingId = b.Id
        LEFT JOIN Employees e ON a.ResponsiblePersonId = e.Id
        LEFT JOIN AssetStatus s ON a.AssetStatusId = s.Id
        LEFT JOIN DepreciationMethods m ON a.DepreciationMethodId = m.Id
        WHERE a.Id = @Id";

    await using var conn = CreateConnection();
    return await conn.QueryFirstOrDefaultAsync<AssetDto>(sql, new { Id = id });
}

    // CREATE – სწორი ID-ის დაბრუნებით
    public async Task<int> CreateAsync(AssetCreateDto dto, string createdBy)
    {
        await using var conn = CreateConnection();

        var p = new DynamicParameters();
        p.Add("@AssetName", dto.AssetName);
        p.Add("@Description", dto.Description);
        p.Add("@SearchDescription", dto.SearchDescription);
        p.Add("@Manufacturer", dto.Manufacturer);
        p.Add("@ManufactureDate", dto.ManufactureDate);
        p.Add("@Barcode", dto.Barcode);
        p.Add("@SerialNumber", dto.SerialNumber);
        p.Add("@InventoryNumber", dto.InventoryNumber);
        p.Add("@ScannedBarcodeResponse", dto.ScannedBarcodeResponse);
        p.Add("@CategoryId", dto.CategoryId);
        p.Add("@DepartmentId", dto.DepartmentId);
        p.Add("@LocationId", dto.LocationId);
        p.Add("@AssetStatusId", dto.AssetStatusId);
        p.Add("@ResponsiblePersonId", dto.ResponsiblePersonId);
        p.Add("@PurchaseDate", dto.PurchaseDate);
        p.Add("@PurchaseValue", dto.PurchaseValue);
        p.Add("@SalvageValue", dto.SalvageValue);
        p.Add("@UsefulLifeMonths", dto.UsefulLifeMonths);
        p.Add("@DepreciationMethodId", dto.DepreciationMethodId);
        p.Add("@DepreciationStartDate", dto.DepreciationStartDate);
        p.Add("@DepreciationBook", dto.DepreciationBook);
        p.Add("@AssetAccount", dto.AssetAccount);
        p.Add("@DepreciationAccount", dto.DepreciationAccount);
        p.Add("@DisposalValue", dto.DisposalValue);
        p.Add("@SupplierId", dto.SupplierId);
        p.Add("@Currency", dto.Currency);
        p.Add("@ParentAssetId", dto.ParentAssetId);
        p.Add("@CustomFieldsJson", dto.CustomFieldsJson);
        p.Add("@Notes", dto.Notes);
        p.Add("@CreatedBy", createdBy);

        const string sql = @"
            EXEC usp_Asset_Insert_V3 
                @AssetName, @Description, @SearchDescription, @Manufacturer, @ManufactureDate,
                @Barcode, @SerialNumber, @InventoryNumber, @ScannedBarcodeResponse,
                @CategoryId, @DepartmentId, @LocationId, @AssetStatusId, @ResponsiblePersonId,
                @PurchaseDate, @PurchaseValue, @SalvageValue, @UsefulLifeMonths,
                @DepreciationMethodId, @DepreciationStartDate, @DepreciationBook,
                @AssetAccount, @DepreciationAccount, @DisposalValue, @SupplierId, @Currency,
                @ParentAssetId, @CustomFieldsJson, @Notes, @CreatedBy;

            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await conn.QuerySingleAsync<int>(sql, p);
    }

    // UPDATE
    public async Task UpdateAsync(AssetUpdateDto dto, string updatedBy)
    {
        await using var conn = CreateConnection();

        var p = new DynamicParameters();
        p.Add("@Id", dto.Id);
        p.Add("@AssetName", dto.AssetName);
        p.Add("@Description", dto.Description);
        p.Add("@SearchDescription", dto.SearchDescription);
        p.Add("@Manufacturer", dto.Manufacturer);
        p.Add("@ManufactureDate", dto.ManufactureDate);
        p.Add("@Barcode", dto.Barcode);
        p.Add("@SerialNumber", dto.SerialNumber);
        p.Add("@InventoryNumber", dto.InventoryNumber);
        p.Add("@ScannedBarcodeResponse", dto.ScannedBarcodeResponse);
        p.Add("@CategoryId", dto.CategoryId);
        p.Add("@DepartmentId", dto.DepartmentId);
        p.Add("@LocationId", dto.LocationId);
        p.Add("@AssetStatusId", dto.AssetStatusId);
        p.Add("@ResponsiblePersonId", dto.ResponsiblePersonId);
        p.Add("@PurchaseDate", dto.PurchaseDate);
        p.Add("@PurchaseValue", dto.PurchaseValue);
        p.Add("@SalvageValue", dto.SalvageValue);
        p.Add("@UsefulLifeMonths", dto.UsefulLifeMonths);
        p.Add("@DepreciationMethodId", dto.DepreciationMethodId);
        p.Add("@DepreciationStartDate", dto.DepreciationStartDate);
        p.Add("@DepreciationBook", dto.DepreciationBook);
        p.Add("@AssetAccount", dto.AssetAccount);
        p.Add("@DepreciationAccount", dto.DepreciationAccount);
        p.Add("@DisposalValue", dto.DisposalValue);
        p.Add("@SupplierId", dto.SupplierId);
        p.Add("@Currency", dto.Currency);
        p.Add("@ParentAssetId", dto.ParentAssetId);
        p.Add("@CustomFieldsJson", dto.CustomFieldsJson);
        p.Add("@Notes", dto.Notes);
        p.Add("@UpdatedBy", updatedBy);

        await conn.ExecuteAsync("usp_Asset_Update_V3", p, commandType: CommandType.StoredProcedure);
    }

    // DELETE
    public async Task DeleteAsync(int id, string deletedBy)
    {
        await using var conn = CreateConnection();
        await conn.ExecuteAsync("usp_Asset_Delete_V3",
            new { Id = id, DeletedBy = deletedBy },
            commandType: CommandType.StoredProcedure);
    }

    // RUN DEPRECIATION
    public async Task<string> RunManualDepreciationAsync(string runBy)
    {
        await using var conn = CreateConnection();
        await conn.ExecuteAsync("usp_CalculateAssetDepreciation_Manual_V3",
            new { RunBy = runBy },
            commandType: CommandType.StoredProcedure);

        return "Depreciation calculated successfully!";
    }
}