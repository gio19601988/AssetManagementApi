namespace AssetManagementApi.DTOs;
    public class WarehouseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Level { get; set; }  // მაგ. "ცენტრალური", "ფილიალი"
        public int? LocationId { get; set; }
        public string? LocationName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? ResponsiblePersonId { get; set; }
        public string? ResponsiblePersonName { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }