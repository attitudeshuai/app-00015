using BalconyFarm.Domain.Enums;

namespace BalconyFarm.Domain.Entities;

public class CropCareTask
{
    public Guid Id { get; set; }
    public Guid CropId { get; set; }
    public TaskType TaskType { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Domain.Enums.TaskStatus Status { get; set; } = Domain.Enums.TaskStatus.Pending;
    public string? Note { get; set; }

    public Crop? Crop { get; set; }
}
