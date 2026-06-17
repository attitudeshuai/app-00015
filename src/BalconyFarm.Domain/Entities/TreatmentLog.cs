namespace BalconyFarm.Domain.Entities;

public class TreatmentLog
{
    public Guid Id { get; set; }
    public Guid PestRecordId { get; set; }
    public string Medication { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string SymptomChange { get; set; } = string.Empty;
    public DateTime TreatmentDate { get; set; }
    public string? Note { get; set; }

    public PestRecord? PestRecord { get; set; }
}
