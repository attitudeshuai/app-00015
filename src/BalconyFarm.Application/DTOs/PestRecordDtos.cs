using BalconyFarm.Domain.Enums;
using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.DTOs;

public class PestRecordDto
{
    public Guid Id { get; set; }
    public Guid CropId { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public DateTime DetectedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public PestStatus Status { get; set; }
    public string? CropName { get; set; }
    public List<TreatmentLogDto> TreatmentLogs { get; set; } = new();
}

public class TreatmentLogDto
{
    public Guid Id { get; set; }
    public Guid PestRecordId { get; set; }
    public string Medication { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string SymptomChange { get; set; } = string.Empty;
    public DateTime TreatmentDate { get; set; }
    public string? Note { get; set; }
}

public class CreateTreatmentLogRequestDto
{
    public string Medication { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string SymptomChange { get; set; } = string.Empty;
    public DateTime TreatmentDate { get; set; }
    public string? Note { get; set; }
}

public class CreatePestRecordRequestDto
{
    public Guid CropId { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public DateTime DetectedDate { get; set; }
    public PestStatus Status { get; set; } = PestStatus.Detected;
}

public class UpdatePestRecordRequestDto
{
    public string? IssueType { get; set; }
    public string? Symptoms { get; set; }
    public string? Treatment { get; set; }
    public DateTime? DetectedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public PestStatus? Status { get; set; }
}

public class UpdatePestStatusRequestDto
{
    public PestStatus Status { get; set; }
}

public class PestRecordQueryRequestDto : PagedRequest
{
    public Guid? CropId { get; set; }
    public string? IssueType { get; set; }
    public PestStatus? Status { get; set; }
    public DateTime? DetectedDateFrom { get; set; }
    public DateTime? DetectedDateTo { get; set; }
}
