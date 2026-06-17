using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.DTOs;

public class HarvestRecordDto
{
    public Guid Id { get; set; }
    public Guid CropId { get; set; }
    public DateTime HarvestDate { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? QualityNote { get; set; }
    public string? PhotoUrl { get; set; }
    public string? CropName { get; set; }
}

public class CreateHarvestRecordRequestDto
{
    public Guid CropId { get; set; }
    public DateTime HarvestDate { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? QualityNote { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsFinalHarvest { get; set; }
}

public class UpdateHarvestRecordRequestDto
{
    public DateTime? HarvestDate { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? QualityNote { get; set; }
    public string? PhotoUrl { get; set; }
}

public class HarvestRecordQueryRequestDto : PagedRequest
{
    public Guid? CropId { get; set; }
    public string? Unit { get; set; }
    public DateTime? HarvestDateFrom { get; set; }
    public DateTime? HarvestDateTo { get; set; }
}
