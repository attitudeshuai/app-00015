using BalconyFarm.Application.Data;
using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;
using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Enums;
using BalconyFarm.Domain.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;

namespace BalconyFarm.Application.Services;

public class PlantingCalendarService : IPlantingCalendarService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PlantingCalendarService> _logger;
    private readonly IPlantingCalendarDataProvider _dataProvider;

    public PlantingCalendarService(
        IUnitOfWork unitOfWork,
        ILogger<PlantingCalendarService> logger,
        IPlantingCalendarDataProvider dataProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _dataProvider = dataProvider;
    }

    public async Task<ApiResponse<List<CityDto>>> GetAvailableCitiesAsync(CancellationToken cancellationToken = default)
    {
        var citiesDict = await _dataProvider.GetCitiesAsync(cancellationToken);
        var cities = citiesDict.Values
            .Select(c => new CityDto
            {
                Name = c.Name,
                Province = c.Province,
                ClimateZone = c.ClimateZone
            })
            .OrderBy(c => c.Province)
            .ThenBy(c => c.Name)
            .ToList();

        return ApiResponse<List<CityDto>>.Success(cities);
    }

    public async Task<ApiResponse<PlantingCalendarResponseDto>> GetRecommendationsAsync(GetRecommendationsRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.City))
        {
            return ApiResponse<PlantingCalendarResponseDto>.Error("请选择城市", 400);
        }

        if (request.Month.HasValue && (request.Month.Value < 1 || request.Month.Value > 12))
        {
            return ApiResponse<PlantingCalendarResponseDto>.Error("月份必须在1-12之间", 400);
        }

        var citiesDict = await _dataProvider.GetCitiesAsync(cancellationToken);
        if (!citiesDict.TryGetValue(request.City, out var cityClimate))
        {
            return ApiResponse<PlantingCalendarResponseDto>.Error($"暂不支持城市：{request.City}", 404);
        }

        var currentMonth = request.Month ?? DateTime.Now.Month;
        var climateZone = GetClimateZone(cityClimate.ClimateZone);

        var allCrops = await _dataProvider.GetCropsAsync(cancellationToken);
        var solarTerms = await _dataProvider.GetSolarTermsAsync(cancellationToken);

        var recommendations = allCrops
            .Where(crop =>
            {
                if (crop.SuitableMonthsByZone.TryGetValue(climateZone, out var months))
                {
                    return months.Contains(currentMonth);
                }
                return false;
            })
            .Select(crop => new PlantingRecommendationDto
            {
                CropName = crop.Name,
                Variety = crop.Variety,
                PlantingSeason = GetPlantingSeason(currentMonth, climateZone),
                Difficulty = crop.Difficulty,
                GrowthDays = crop.GrowthDays,
                Tips = crop.Tips,
                SuitableLocation = crop.SuitableLocation
            })
            .ToList();

        var solarTerm = solarTerms.TryGetValue(currentMonth, out var termInfo)
            ? termInfo.Term
            : string.Empty;

        var avgTemp = cityClimate.AverageTempByMonth.Length >= currentMonth
            ? cityClimate.AverageTempByMonth[currentMonth - 1]
            : 0;
        var avgPrecipitation = cityClimate.PrecipitationByMonth.Length >= currentMonth
            ? cityClimate.PrecipitationByMonth[currentMonth - 1]
            : 0;

        var climateDescription = $"本月平均气温约 {avgTemp}°C，平均降水量约 {avgPrecipitation}mm。{termInfo?.Description ?? string.Empty}";

        var response = new PlantingCalendarResponseDto
        {
            City = cityClimate.Name,
            Province = cityClimate.Province,
            ClimateZone = cityClimate.ClimateZone,
            Month = currentMonth,
            SolarTerm = solarTerm,
            ClimateDescription = climateDescription,
            Recommendations = recommendations
        };

        return ApiResponse<PlantingCalendarResponseDto>.Success(response);
    }

    public async Task<ApiResponse<CropDto>> CreateCropFromRecommendationAsync(CreateCropFromRecommendationRequestDto request, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("从种植日历推荐创建作物: {CropName}, 用户: {UserId}, 城市: {City}, 月份: {Month}", request.CropName, userId, request.City, request.Month);

        if (request.Month < 1 || request.Month > 12)
        {
            return ApiResponse<CropDto>.Error("月份必须在1-12之间", 400);
        }

        var citiesDict = await _dataProvider.GetCitiesAsync(cancellationToken);
        if (!citiesDict.TryGetValue(request.City, out var cityClimate))
        {
            return ApiResponse<CropDto>.Error($"暂不支持城市：{request.City}", 404);
        }

        var climateZone = GetClimateZone(cityClimate.ClimateZone);
        var allCrops = await _dataProvider.GetCropsAsync(cancellationToken);

        var matchingCrop = allCrops.FirstOrDefault(c =>
            c.Name.Equals(request.CropName, StringComparison.Ordinal) &&
            c.Variety.Equals(request.Variety, StringComparison.Ordinal));

        if (matchingCrop == null)
        {
            return ApiResponse<CropDto>.Error($"未找到作物：{request.CropName}（{request.Variety}）", 404);
        }

        if (!matchingCrop.SuitableMonthsByZone.TryGetValue(climateZone, out var suitableMonths) || !suitableMonths.Contains(request.Month))
        {
            return ApiResponse<CropDto>.Error($"作物 {request.CropName} 不适合在 {request.City} {request.Month} 月种植", 400);
        }

        var crop = new Crop
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.CropName,
            Variety = request.Variety,
            PlantingDate = request.PlantingDate ?? DateTime.UtcNow,
            Location = matchingCrop.DefaultLocation,
            ContainerType = matchingCrop.DefaultContainerType,
            Status = CropStatus.Growing,
            PhotoUrl = request.PhotoUrl,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Crops.AddAsync(crop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("从种植日历创建作物成功: {CropId}", crop.Id);

        var cropDto = crop.Adapt<CropDto>();
        return ApiResponse<CropDto>.Success(cropDto, "创建成功");
    }

    private static string GetClimateZone(string climateZoneFull)
    {
        if (climateZoneFull.Contains("南亚热带")) return "南亚热带";
        if (climateZoneFull.Contains("亚热带")) return "亚热带";
        if (climateZoneFull.Contains("热带")) return "热带";
        if (climateZoneFull.Contains("中温带")) return "温带";
        if (climateZoneFull.Contains("暖温带")) return "温带";
        if (climateZoneFull.Contains("温带")) return "温带";
        if (climateZoneFull.Contains("高原")) return "亚热带";
        return "亚热带";
    }

    private static string GetPlantingSeason(int month, string climateZone)
    {
        switch (climateZone)
        {
            case "温带":
                if (month is 3 or 4 or 5) return "春播";
                if (month is 8 or 9 or 10) return "秋播";
                if (month is 6 or 7) return "夏播（耐热蔬菜）";
                return "越冬种植";
            case "亚热带":
                if (month is 2 or 3 or 4) return "春播";
                if (month is 9 or 10 or 11) return "秋播";
                if (month is 5 or 6 or 7 or 8) return "夏播（耐热蔬菜）";
                return "冬播（耐寒蔬菜）";
            case "南亚热带":
                if (month is 1 or 2 or 3) return "春播";
                if (month is 10 or 11 or 12) return "秋冬播";
                return "夏播（耐热蔬菜）";
            case "热带":
                if (month is 10 or 11 or 12 or 1) return "旱季种植";
                return "雨季种植（耐涝蔬菜）";
            default:
                return "当季种植";
        }
    }
}
