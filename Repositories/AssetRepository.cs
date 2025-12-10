using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using AssetManagementApi.Models.DTO;

namespace AssetManagementApi.Repositories;

public class AssetRepository
{
    private readonly string _connectionString;

    public AssetRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString); // SqlConnection-ად შეცვლილი

    // GET: api/assets
    public async Task<IEnumerable<AssetDto>> GetAllAsync()
    {
        const string sql = @"
            SELECT a.*, 
                   c.Name AS CategoryName,
                   d.Name AS DepartmentName,
                   l.RoomNumber + ' (' + b.Name + ')' AS LocationName,
                   e.FullName AS ResponsiblePersonName,
                   s.StatusName,
                   m.MethodName AS DepreciationMethodName
            FROM Assets a
            LEFT JOIN Categories c ON a.CategoryId = c.Id
            LEFT JOIN Departments d ON a.DepartmentId = d.Id
            LEFT JOIN Locations l ON a.LocationId = l.Id
            LEFT JOIN Buildings b ON l.BuildingId = b.Id
            LEFT JOIN Employees e ON a.ResponsiblePersonId = e.Id
            LEFT JOIN AssetStatus s ON a.AssetStatusId = s.Id
            LEFT JOIN DepreciationMethods m ON a.DepreciationMethodId = m.Id
            ORDER BY a.CreatedAt DESC";

        using var conn = CreateConnection();
        await conn.OpenAsync(); // ახლა მუშაობს SqlConnection-ზე
        return await conn.QueryAsync<AssetDto>(sql);
    }

    // GET: api/assets/{id}
    public async Task<AssetDto?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT a.*, 
                   c.Name AS CategoryName,
                   d.Name AS DepartmentName,
                   l.RoomNumber + ' (' + b.Name + ')' AS LocationName,
                   e.FullName AS ResponsiblePersonName,
                   s.StatusName,
                   m.MethodName AS DepreciationMethodName
            FROM Assets a
            LEFT JOIN Categories c ON a.CategoryId = c.Id
            LEFT JOIN Departments d ON a.DepartmentId = d.Id
            LEFT JOIN Locations l ON a.LocationId = l.Id
            LEFT JOIN Buildings b ON l.BuildingId = b.Id
            LEFT JOIN Employees e ON a.ResponsiblePersonId = e.Id
            LEFT JOIN AssetStatus s ON a.AssetStatusId = s.Id
            LEFT JOIN DepreciationMethods m ON a.DepreciationMethodId = m.Id
            WHERE a.Id = @Id";

        using var conn = CreateConnection();
        await conn.OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<AssetDto>(sql, new { Id = id });
    }

    // POST: api/assets (usp_Asset_Insert_V3)
    public async Task<int> CreateAsync(AssetCreateDto dto, string createdBy)
    {
        const string sql = "usp_Asset_Insert_V3";
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var parameters = new DynamicParameters(dto);
        parameters.Add("@CreatedBy", createdBy);

        await conn.ExecuteAsync(sql, parameters, commandType: CommandType.StoredProcedure);

        // ბოლო ID-ის მიღება
        return await conn.QuerySingleAsync<int>("SELECT SCOPE_IDENTITY()");
    }

    // PUT: api/assets/{id} (usp_Asset_Update_V3)
    public async Task UpdateAsync(AssetUpdateDto dto, string updatedBy)
    {
        const string sql = "usp_Asset_Update_V3";
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var parameters = new DynamicParameters(dto);
        parameters.Add("@UpdatedBy", updatedBy);

        await conn.ExecuteAsync(sql, parameters, commandType: CommandType.StoredProcedure);
    }

    // DELETE: api/assets/{id} (usp_Asset_Delete_V3)
    public async Task DeleteAsync(int id, string deletedBy)
    {
        const string sql = "usp_Asset_Delete_V3";
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var parameters = new { Id = id, DeletedBy = deletedBy };
        await conn.ExecuteAsync(sql, parameters, commandType: CommandType.StoredProcedure);
    }

    // POST: api/assets/run-depreciation (usp_CalculateAssetDepreciation_Manual_V3)
    public async Task<string> RunManualDepreciationAsync(string runBy)
    {
        const string sql = "usp_CalculateAssetDepreciation_Manual_V3";
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var parameters = new { RunBy = runBy };
        await conn.ExecuteAsync(sql, parameters, commandType: CommandType.StoredProcedure);

        return "Depreciation calculated successfully!";
    }
}