namespace BalconyFarm.Domain.Entities;

public class HarvestRecord
{
    public Guid Id { get; set; }
    public Guid CropId { get; set; }
    public DateTime HarvestDate { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? QualityNote { get; set; }
    public string? PhotoUrl { get; set; }

    public Crop? Crop { get; set; }
}
