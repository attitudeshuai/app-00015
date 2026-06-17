namespace BalconyFarm.Application.Data;

public interface IPlantingCalendarDataProvider
{
    Task<Dictionary<string, CityClimateDataModel>> GetCitiesAsync(CancellationToken cancellationToken = default);
    Task<List<CropPlantingDataModel>> GetCropsAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<int, SolarTermDataModel>> GetSolarTermsAsync(CancellationToken cancellationToken = default);
}
