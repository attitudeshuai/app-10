namespace DeviceMaintenanceSystem.Models;

public class InspectionPhoto
{
    public int Id { get; set; }
    public int InspectionRecordId { get; set; }
    public InspectionRecord? InspectionRecord { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
