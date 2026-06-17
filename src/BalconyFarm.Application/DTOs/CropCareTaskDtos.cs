using BalconyFarm.Domain.Enums;
using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.DTOs;

public class CropCareTaskDto
{
    public Guid Id { get; set; }
    public Guid CropId { get; set; }
    public TaskType TaskType { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Domain.Enums.TaskStatus Status { get; set; }
    public string? Note { get; set; }
    public string? CropName { get; set; }
    public bool IsOverdue { get; set; }
    public int? OverdueDays { get; set; }
}

public class CreateCropCareTaskRequestDto
{
    public Guid CropId { get; set; }
    public TaskType TaskType { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string? Note { get; set; }
}

public class UpdateCropCareTaskRequestDto
{
    public TaskType? TaskType { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public Domain.Enums.TaskStatus? Status { get; set; }
    public string? Note { get; set; }
}

public class UpdateTaskStatusRequestDto
{
    public Domain.Enums.TaskStatus Status { get; set; }
}

public class CropCareTaskQueryRequestDto : PagedRequest
{
    public Guid? CropId { get; set; }
    public TaskType? TaskType { get; set; }
    public Domain.Enums.TaskStatus? Status { get; set; }
    public DateTime? ScheduledDateFrom { get; set; }
    public DateTime? ScheduledDateTo { get; set; }
}
