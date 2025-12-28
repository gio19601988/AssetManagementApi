namespace AssetManagementApi.DTOs;
public class CreateUpdateWarehouseDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Level { get; set; }
        public string? Code { get; set; }
        public int? LocationId { get; set; }
        public int? DepartmentId { get; set; }
        public int? ResponsiblePersonId { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }