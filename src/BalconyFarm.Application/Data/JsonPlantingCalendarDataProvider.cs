using System.Reflection;
using System.Text.Json;

namespace BalconyFarm.Application.Data;

public class JsonPlantingCalendarDataProvider : IPlantingCalendarDataProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _dataDirectory;

    public JsonPlantingCalendarDataProvider()
    {
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        _dataDirectory = Path.Combine(assemblyLocation, "Data");

        if (!Directory.Exists(_dataDirectory))
        {
            var projectRoot = GetProjectRoot();
            if (projectRoot != null)
            {
                _dataDirectory = Path.Combine(projectRoot, "Data");
            }
        }
    }

    private static string? GetProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null)
        {
            if (Directory.GetDirectories(currentDir).Any(d => d.EndsWith("BalconyFarm.Application")))
            {
                var appDir = Path.Combine(currentDir, "BalconyFarm.Application");
                if (Directory.Exists(Path.Combine(appDir, "Data")))
                {
                    return appDir;
                }
            }
            var parent = Directory.GetParent(currentDir);
            currentDir = parent?.FullName;
        }
        return null;
    }

    public async Task<Dictionary<string, CityClimateDataModel>> GetCitiesAsync(CancellationToken cancellationToken = default)
    {
        var cities = await LoadJsonAsync<List<CityClimateDataModel>>("cities.json", cancellationToken) ?? new();
        return cities.ToDictionary(c => c.Name, c => c, StringComparer.Ordinal);
    }

    public async Task<List<CropPlantingDataModel>> GetCropsAsync(CancellationToken cancellationToken = default)
    {
        return await LoadJsonAsync<List<CropPlantingDataModel>>("crops.json", cancellationToken) ?? new();
    }

    public async Task<Dictionary<int, SolarTermDataModel>> GetSolarTermsAsync(CancellationToken cancellationToken = default)
    {
        var solarTerms = await LoadJsonAsync<Dictionary<string, SolarTermDataModel>>("solar_terms.json", cancellationToken) ?? new();
        return solarTerms.ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value);
    }

    private async Task<T?> LoadJsonAsync<T>(string fileName, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"数据文件不存在: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
