using BalconyFarm.Domain.Enums;
using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.DTOs;

public class CropDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public DateTime PlantingDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ContainerType { get; set; } = string.Empty;
    public CropStatus Status { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? OwnerUsername { get; set; }
}

public class CreateCropRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public DateTime PlantingDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ContainerType { get; set; } = string.Empty;
    public CropStatus Status { get; set; } = CropStatus.Growing;
    public string? PhotoUrl { get; set; }
}

public class UpdateCropRequestDto
{
    public string? Name { get; set; }
    public string? Variety { get; set; }
    public DateTime? PlantingDate { get; set; }
    public string? Location { get; set; }
    public string? ContainerType { get; set; }
    public CropStatus? Status { get; set; }
    public string? PhotoUrl { get; set; }
}

public class UpdateCropStatusRequestDto
{
    public CropStatus Status { get; set; }
}

public class CropQueryRequestDto : PagedRequest
{
    public CropStatus? Status { get; set; }
    public string? Location { get; set; }
    public string? ContainerType { get; set; }
    public DateTime? PlantingDateFrom { get; set; }
    public DateTime? PlantingDateTo { get; set; }
}
