namespace BalconyFarm.Application.Data;

public class CityClimateDataModel
{
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string ClimateZone { get; set; } = string.Empty;
    public int[] AverageTempByMonth { get; set; } = Array.Empty<int>();
    public int[] PrecipitationByMonth { get; set; } = Array.Empty<int>();
}

public class CropPlantingDataModel
{
    public string Name { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string GrowthDays { get; set; } = string.Empty;
    public string Tips { get; set; } = string.Empty;
    public string SuitableLocation { get; set; } = string.Empty;
    public string DefaultLocation { get; set; } = string.Empty;
    public string DefaultContainerType { get; set; } = string.Empty;
    public Dictionary<string, List<int>> SuitableMonthsByZone { get; set; } = new();
}

public class SolarTermDataModel
{
    public string Term { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
