using BalconyFarm.Domain.Enums;

namespace BalconyFarm.Domain.Entities;

public class PestRecord
{
    public Guid Id { get; set; }
    public Guid CropId { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public DateTime DetectedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public PestStatus Status { get; set; } = PestStatus.Detected;

    public Crop? Crop { get; set; }
    public ICollection<TreatmentLog> TreatmentLogs { get; set; } = new List<TreatmentLog>();
}
