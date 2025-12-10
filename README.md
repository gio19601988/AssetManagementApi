# Asset Management API (.NET 8)

## Setup
1. Update appsettings.json with your SQL connection.
2. `dotnet restore && dotnet run`
3. Test in Swagger: https://localhost:7xxx/swagger
   - Auth: admin / 12345

## Endpoints
- GET /api/assets - List assets
- POST /api/assets - Create asset
- PUT /api/assets/{id} - Update
- DELETE /api/assets/{id} - Delete
- POST /api/assets/run-depreciation - Calculate depreciation

## Tech
- .NET 8 Web API
- Dapper + SQL Server Stored Procs
- Basic Auth