using BalconyFarm.Api.Controllers;
using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;
using BalconyFarm.Application.Services;
using BalconyFarm.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace BalconyFarm.Tests;

public class PlantingCalendarControllerTests
{
    private readonly Mock<IPlantingCalendarService> _mockService;
    private readonly PlantingCalendarController _controller;
    private readonly CancellationToken _cancellationToken;
    private readonly Guid _testUserId;

    public PlantingCalendarControllerTests()
    {
        _mockService = new Mock<IPlantingCalendarService>();
        _controller = new PlantingCalendarController(_mockService.Object);
        _cancellationToken = CancellationToken.None;
        _testUserId = Guid.NewGuid();
    }

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetupAnonymousUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    #region GetAvailableCities Tests

    [Fact]
    public async Task GetAvailableCities_WhenCalled_ReturnsOkResult()
    {
        var expectedResponse = ApiResponse<List<CityDto>>.Success(new List<CityDto>
        {
            new() { Name = "上海", Province = "上海", ClimateZone = "亚热带季风气候" },
            new() { Name = "北京", Province = "北京", ClimateZone = "暖温带半湿润大陆性季风气候" }
        });

        _mockService
            .Setup(s => s.GetAvailableCitiesAsync(_cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetAvailableCities(_cancellationToken);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse<List<CityDto>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(200);
        apiResponse.Data.Should().HaveCount(2);

        _mockService.Verify(s => s.GetAvailableCitiesAsync(_cancellationToken), Times.Once);
    }

    #endregion

    #region GetRecommendations Tests

    [Fact]
    public async Task GetRecommendations_WhenValidRequest_ReturnsOkResult()
    {
        var request = new GetRecommendationsRequestDto { City = "上海", Month = 3 };
        var expectedResponse = ApiResponse<PlantingCalendarResponseDto>.Success(new PlantingCalendarResponseDto
        {
            City = "上海",
            Province = "上海",
            ClimateZone = "亚热带季风气候",
            Month = 3,
            SolarTerm = "惊蛰/春分",
            ClimateDescription = "测试描述",
            Recommendations = new List<PlantingRecommendationDto>
            {
                new() { CropName = "生菜", Variety = "奶油生菜", Difficulty = "简单" }
            }
        });

        _mockService
            .Setup(s => s.GetRecommendationsAsync(request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetRecommendations(request, _cancellationToken);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse<PlantingCalendarResponseDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(200);
        apiResponse.Data!.City.Should().Be("上海");
        apiResponse.Data.Recommendations.Should().HaveCount(1);

        _mockService.Verify(s => s.GetRecommendationsAsync(request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetRecommendations_WhenServiceReturnsError_ReturnsBadRequest()
    {
        var request = new GetRecommendationsRequestDto { City = "", Month = 3 };
        var expectedResponse = ApiResponse<PlantingCalendarResponseDto>.Error("请选择城市", 400);

        _mockService
            .Setup(s => s.GetRecommendationsAsync(request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetRecommendations(request, _cancellationToken);

        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);

        var apiResponse = badRequestResult.Value as ApiResponse<PlantingCalendarResponseDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(400);
        apiResponse.Message.Should().Be("请选择城市");
    }

    #endregion

    #region CreateCropFromRecommendation Tests

    [Fact]
    public async Task CreateCropFromRecommendation_WhenAuthenticated_ReturnsCreatedAtAction()
    {
        SetupAuthenticatedUser(_testUserId);

        var request = new CreateCropFromRecommendationRequestDto
        {
            City = "上海",
            Month = 3,
            CropName = "生菜",
            Variety = "奶油生菜"
        };

        var cropId = Guid.NewGuid();
        var expectedResponse = ApiResponse<CropDto>.Success(new CropDto
        {
            Id = cropId,
            UserId = _testUserId,
            Name = "生菜",
            Variety = "奶油生菜",
            Location = "东阳台",
            ContainerType = "种植箱",
            Status = CropStatus.Growing,
            CreatedAt = DateTime.UtcNow
        }, "创建成功");

        _mockService
            .Setup(s => s.CreateCropFromRecommendationAsync(request, _testUserId, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.CreateCropFromRecommendation(request, _cancellationToken);

        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(PlantingCalendarController.GetRecommendations));
        createdResult.RouteValues!["city"].Should().Be("上海");
        createdResult.RouteValues["month"].Should().Be(3);

        var apiResponse = createdResult.Value as ApiResponse<CropDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(200);
        apiResponse.Data!.Id.Should().Be(cropId);
        apiResponse.Data.Name.Should().Be("生菜");

        _mockService.Verify(s => s.CreateCropFromRecommendationAsync(request, _testUserId, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateCropFromRecommendation_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        SetupAnonymousUser();

        var request = new CreateCropFromRecommendationRequestDto
        {
            City = "上海",
            Month = 3,
            CropName = "生菜",
            Variety = "奶油生菜"
        };

        var result = await _controller.CreateCropFromRecommendation(request, _cancellationToken);

        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);

        var apiResponse = unauthorizedResult.Value as ApiResponse;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(401);
        apiResponse.Message.Should().Be("用户未认证");

        _mockService.Verify(s => s.CreateCropFromRecommendationAsync(
            It.IsAny<CreateCropFromRecommendationRequestDto>(),
            It.IsAny<Guid>(),
            _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task CreateCropFromRecommendation_WhenServiceReturnsError_ReturnsBadRequest()
    {
        SetupAuthenticatedUser(_testUserId);

        var request = new CreateCropFromRecommendationRequestDto
        {
            City = "上海",
            Month = 6,
            CropName = "生菜",
            Variety = "奶油生菜"
        };

        var expectedResponse = ApiResponse<CropDto>.Error("作物 生菜 不适合在 上海 6 月种植", 400);

        _mockService
            .Setup(s => s.CreateCropFromRecommendationAsync(request, _testUserId, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.CreateCropFromRecommendation(request, _cancellationToken);

        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);

        var apiResponse = badRequestResult.Value as ApiResponse<CropDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(400);
        apiResponse.Message.Should().Be("作物 生菜 不适合在 上海 6 月种植");

        _mockService.Verify(s => s.CreateCropFromRecommendationAsync(request, _testUserId, _cancellationToken), Times.Once);
    }

    #endregion
}
