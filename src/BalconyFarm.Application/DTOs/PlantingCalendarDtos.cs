namespace BalconyFarm.Application.DTOs;

public class CityDto
{
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string ClimateZone { get; set; } = string.Empty;
}

public class PlantingRecommendationDto
{
    public string CropName { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public string PlantingSeason { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string GrowthDays { get; set; } = string.Empty;
    public string Tips { get; set; } = string.Empty;
    public string SuitableLocation { get; set; } = string.Empty;
}

public class GetRecommendationsRequestDto
{
    public string City { get; set; } = string.Empty;
    public int? Month { get; set; }
}

public class PlantingCalendarResponseDto
{
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string ClimateZone { get; set; } = string.Empty;
    public int Month { get; set; }
    public string SolarTerm { get; set; } = string.Empty;
    public string ClimateDescription { get; set; } = string.Empty;
    public List<PlantingRecommendationDto> Recommendations { get; set; } = new();
}

public class CreateCropFromRecommendationRequestDto
{
    public string City { get; set; } = string.Empty;
    public int Month { get; set; }
    public string CropName { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public DateTime? PlantingDate { get; set; }
    public string? PhotoUrl { get; set; }
}
