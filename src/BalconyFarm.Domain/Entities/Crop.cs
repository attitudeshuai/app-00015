using BalconyFarm.Domain.Enums;

namespace BalconyFarm.Domain.Entities;

public class Crop
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public DateTime PlantingDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ContainerType { get; set; } = string.Empty;
    public CropStatus Status { get; set; } = CropStatus.Growing;
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public ICollection<CropCareTask> CareTasks { get; set; } = new List<CropCareTask>();
    public ICollection<HarvestRecord> HarvestRecords { get; set; } = new List<HarvestRecord>();
    public ICollection<PestRecord> PestRecords { get; set; } = new List<PestRecord>();
}
