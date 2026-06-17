using BalconyFarm.Application.Data;
using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;
using BalconyFarm.Application.Services;
using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Enums;
using BalconyFarm.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BalconyFarm.Tests;

public class PlantingCalendarServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<PlantingCalendarService>> _loggerMock;
    private readonly Mock<IPlantingCalendarDataProvider> _dataProviderMock;
    private readonly CancellationToken _cancellationToken;
    private readonly Guid _testUserId;

    public PlantingCalendarServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<PlantingCalendarService>>();
        _dataProviderMock = new Mock<IPlantingCalendarDataProvider>();
        _cancellationToken = CancellationToken.None;
        _testUserId = Guid.NewGuid();

        SetupTestData();
    }

    private void SetupTestData()
    {
        var cities = new Dictionary<string, CityClimateDataModel>(StringComparer.Ordinal)
        {
            {
                "上海",
                new CityClimateDataModel
                {
                    Name = "上海",
                    Province = "上海",
                    ClimateZone = "亚热带季风气候",
                    AverageTempByMonth = new[] { 4, 6, 10, 16, 21, 25, 29, 28, 24, 18, 12, 6 },
                    PrecipitationByMonth = new[] { 50, 60, 95, 105, 115, 170, 145, 140, 155, 65, 55, 45 }
                }
            },
            {
                "北京",
                new CityClimateDataModel
                {
                    Name = "北京",
                    Province = "北京",
                    ClimateZone = "暖温带半湿润大陆性季风气候",
                    AverageTempByMonth = new[] { -4, -2, 6, 14, 21, 25, 27, 26, 21, 13, 4, -3 },
                    PrecipitationByMonth = new[] { 3, 5, 10, 25, 35, 75, 180, 175, 55, 25, 10, 3 }
                }
            }
        };

        var crops = new List<CropPlantingDataModel>
        {
            new()
            {
                Name = "生菜",
                Variety = "奶油生菜",
                Difficulty = "简单",
                GrowthDays = "40-60天",
                Tips = "喜凉爽，避免高温暴晒，保持土壤湿润",
                SuitableLocation = "东阳台、西阳台、北阳台",
                DefaultLocation = "东阳台",
                DefaultContainerType = "种植箱",
                SuitableMonthsByZone = new Dictionary<string, List<int>>
                {
                    { "温带", new List<int> { 3, 4, 5, 8, 9, 10 } },
                    { "亚热带", new List<int> { 2, 3, 4, 10, 11 } },
                    { "南亚热带", new List<int> { 10, 11, 12, 1, 2 } },
                    { "热带", new List<int> { 11, 12, 1, 2 } }
                }
            },
            new()
            {
                Name = "小番茄",
                Variety = "千禧樱桃番茄",
                Difficulty = "简单",
                GrowthDays = "90-120天",
                Tips = "喜阳光，需要充足日照",
                SuitableLocation = "南阳台、东阳台",
                DefaultLocation = "南阳台",
                DefaultContainerType = "塑料花盆",
                SuitableMonthsByZone = new Dictionary<string, List<int>>
                {
                    { "温带", new List<int> { 3, 4, 5, 8, 9 } },
                    { "亚热带", new List<int> { 2, 3, 4, 9, 10 } }
                }
            }
        };

        var solarTerms = new Dictionary<int, SolarTermDataModel>
        {
            { 3, new SolarTermDataModel { Term = "惊蛰/春分", Description = "春雷乍动，万物复苏" } },
            { 10, new SolarTermDataModel { Term = "寒露/霜降", Description = "气温继续下降" } }
        };

        _dataProviderMock.Setup(d => d.GetCitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cities);
        _dataProviderMock.Setup(d => d.GetCropsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(crops);
        _dataProviderMock.Setup(d => d.GetSolarTermsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(solarTerms);
    }

    private PlantingCalendarService CreateService()
    {
        return new PlantingCalendarService(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _dataProviderMock.Object);
    }

    #region GetAvailableCitiesAsync Tests

    [Fact]
    public async Task GetAvailableCitiesAsync_ShouldReturnSuccess_WithCityList()
    {
        var service = CreateService();

        var result = await service.GetAvailableCitiesAsync(_cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Should().HaveCount(2);
        result.Data.Should().Contain(c => c.Name == "北京");
        result.Data.Should().Contain(c => c.Name == "上海");
    }

    #endregion

    #region GetRecommendationsAsync Tests

    [Fact]
    public async Task GetRecommendationsAsync_ShouldReturnError_WhenCityIsEmpty()
    {
        var service = CreateService();
        var request = new GetRecommendationsRequestDto { City = "", Month = 3 };

        var result = await service.GetRecommendationsAsync(request, _cancellationToken);

        result.Code.Should().Be(400);
        result.Message.Should().Be("请选择城市");
    }

    [Fact]
    public async Task GetRecommendationsAsync_ShouldReturnError_WhenMonthIsOutOfRange()
    {
        var service = CreateService();
        var request = new GetRecommendationsRequestDto { City = "上海", Month = 13 };

        var result = await service.GetRecommendationsAsync(request, _cancellationToken);

        result.Code.Should().Be(400);
        result.Message.Should().Be("月份必须在1-12之间");
    }

    [Fact]
    public async Task GetRecommendationsAsync_ShouldReturnError_WhenCityNotSupported()
    {
        var service = CreateService();
        var request = new GetRecommendationsRequestDto { City = "拉萨", Month = 3 };

        var result = await service.GetRecommendationsAsync(request, _cancellationToken);

        result.Code.Should().Be(404);
        result.Message.Should().Be("暂不支持城市：拉萨");
    }

    [Fact]
    public async Task GetRecommendationsAsync_ShouldReturnSuccess_WithRecommendations()
    {
        var service = CreateService();
        var request = new GetRecommendationsRequestDto { City = "上海", Month = 3 };

        var result = await service.GetRecommendationsAsync(request, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.City.Should().Be("上海");
        result.Data.Month.Should().Be(3);
        result.Data.SolarTerm.Should().Be("惊蛰/春分");
        result.Data.Recommendations.Should().NotBeEmpty();
        result.Data.Recommendations.Should().Contain(r => r.CropName == "生菜");
        result.Data.Recommendations.Should().Contain(r => r.CropName == "小番茄");
    }

    [Fact]
    public async Task GetRecommendationsAsync_ShouldFilterCrops_ByMonth()
    {
        var service = CreateService();
        var request = new GetRecommendationsRequestDto { City = "上海", Month = 10 };

        var result = await service.GetRecommendationsAsync(request, _cancellationToken);

        result.Data!.Recommendations.Should().Contain(r => r.CropName == "生菜");
        result.Data.Recommendations.Should().Contain(r => r.CropName == "小番茄");
    }

    #endregion

    #region CreateCropFromRecommendationAsync Tests

    [Fact]
    public async Task CreateCropFromRecommendationAsync_ShouldReturnError_WhenMonthOutOfRange()
    {
        var service = CreateService();
        var request = new CreateCropFromRecommendationRequestDto
        {
            City = "上海",
            Month = 0,
            CropName = "生菜",
            Variety = "奶油生菜"
        };

        var result = await service.CreateCropFromRecommendationAsync(request, _testUserId, _cancellationToken);

        result.Code.Should().Be(400);
        result.Message.Should().Be("月份必须在1-12之间");
    }

    [Fact]
    public async Task CreateCropFromRecommendationAsync_ShouldReturnError_WhenCityNotSupported()
    {
        var service = CreateService();
        var request = new CreateCropFromRecommendationRequestDto
        {
            City = "拉萨",
            Month = 3,
            CropName = "生菜",
            Variety = "奶油生菜"
        };

        var result = await service.CreateCropFromRecommendationAsync(request, _testUserId, _cancellationToken);

        result.Code.Should().Be(404);
        result.Message.Should().Be("暂不支持城市：拉萨");
    }

    [Fact]
    public async Task CreateCropFromRecommendationAsync_ShouldReturnError_WhenCropNotFound()
    {
        var service = CreateService();
        var request = new CreateCropFromRecommendationRequestDto
        {
            City = "上海",
            Month = 3,
            CropName = "不存在的作物",
            Variety = "未知品种"
        };

        var result = await service.CreateCropFromRecommendationAsync(request, _testUserId, _cancellationToken);

        result.Code.Should().Be(404);
        result.Message.Should().Be("未找到作物：不存在的作物（未知品种）");
    }

    [Fact]
    public async Task CreateCropFromRecommendationAsync_ShouldReturnError_WhenCropNotSuitableForMonth()
    {
        var service = CreateService();
        var request = new CreateCropFromRecommendationRequestDto
        {
            City = "上海",
            Month = 6,
            CropName = "生菜",
            Variety = "奶油生菜"
        };

        var result = await service.CreateCropFromRecommendationAsync(request, _testUserId, _cancellationToken);

        result.Code.Should().Be(400);
        result.Message.Should().Be("作物 生菜 不适合在 上海 6 月种植");
    }

    [Fact]
    public async Task CreateCropFromRecommendationAsync_ShouldReturnSuccess_WithDefaultValues()
    {
        _unitOfWorkMock.Setup(u => u.Crops.AddAsync(It.IsAny<Crop>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Crop crop, CancellationToken ct) => crop);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = CreateService();
        var request = new CreateCropFromRecommendationRequestDto
        {
            City = "上海",
            Month = 3,
            CropName = "生菜",
            Variety = "奶油生菜"
        };

        var result = await service.CreateCropFromRecommendationAsync(request, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be("创建成功");
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("生菜");
        result.Data.Variety.Should().Be("奶油生菜");
        result.Data.UserId.Should().Be(_testUserId);
        result.Data.Location.Should().Be("东阳台");
        result.Data.ContainerType.Should().Be("种植箱");
        result.Data.Status.Should().Be(CropStatus.Growing);

        _unitOfWorkMock.Verify(u => u.Crops.AddAsync(It.Is<Crop>(c =>
            c.Name == "生菜" &&
            c.Location == "东阳台" &&
            c.ContainerType == "种植箱" &&
            c.UserId == _testUserId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCropFromRecommendationAsync_ShouldUseProvidedPlantingDate()
    {
        var customPlantingDate = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        _unitOfWorkMock.Setup(u => u.Crops.AddAsync(It.IsAny<Crop>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Crop crop, CancellationToken ct) => crop);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = CreateService();
        var request = new CreateCropFromRecommendationRequestDto
        {
            City = "上海",
            Month = 3,
            CropName = "生菜",
            Variety = "奶油生菜",
            PlantingDate = customPlantingDate
        };

        var result = await service.CreateCropFromRecommendationAsync(request, _testUserId, _cancellationToken);

        result.Data!.PlantingDate.Should().Be(customPlantingDate);
    }

    #endregion
}
