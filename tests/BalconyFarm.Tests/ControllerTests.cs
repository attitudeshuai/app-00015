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

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;
    private readonly CancellationToken _cancellationToken;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new AuthController(_mockAuthService.Object);
        _cancellationToken = CancellationToken.None;
    }

    [Fact]
    public async Task Register_WhenCalledWithValidData_ReturnsOkResult()
    {
        var request = new RegisterRequestDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        var expectedResponse = ApiResponse<LoginResponseDto>.Success(new LoginResponseDto
        {
            Token = "test-jwt-token",
            User = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow
            }
        }, "注册成功");

        _mockAuthService
            .Setup(x => x.RegisterAsync(request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.Register(request, _cancellationToken);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse<LoginResponseDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(200);
        apiResponse.Message.Should().Be("注册成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().Be("test-jwt-token");
        apiResponse.Data.User.Username.Should().Be("testuser");

        _mockAuthService.Verify(x => x.RegisterAsync(request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Register_WhenServiceReturnsError_ReturnsBadRequest()
    {
        var request = new RegisterRequestDto
        {
            Username = "existinguser",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        var expectedResponse = ApiResponse<LoginResponseDto>.Error("用户名已存在", 400);

        _mockAuthService
            .Setup(x => x.RegisterAsync(request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.Register(request, _cancellationToken);

        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);

        var apiResponse = badRequestResult.Value as ApiResponse<LoginResponseDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(400);
        apiResponse.Message.Should().Be("用户名已存在");

        _mockAuthService.Verify(x => x.RegisterAsync(request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Login_WhenCalledWithValidCredentials_ReturnsOkResult()
    {
        var request = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "Password123!"
        };

        var expectedResponse = ApiResponse<LoginResponseDto>.Success(new LoginResponseDto
        {
            Token = "test-jwt-token",
            User = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow
            }
        }, "登录成功");

        _mockAuthService
            .Setup(x => x.LoginAsync(request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.Login(request, _cancellationToken);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse<LoginResponseDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(200);
        apiResponse.Message.Should().Be("登录成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().Be("test-jwt-token");

        _mockAuthService.Verify(x => x.LoginAsync(request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Login_WhenCalledWithInvalidCredentials_ReturnsBadRequest()
    {
        var request = new LoginRequestDto
        {
            UsernameOrEmail = "wronguser",
            Password = "wrongpassword"
        };

        var expectedResponse = ApiResponse<LoginResponseDto>.Error("用户名或密码错误", 400);

        _mockAuthService
            .Setup(x => x.LoginAsync(request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.Login(request, _cancellationToken);

        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);

        var apiResponse = badRequestResult.Value as ApiResponse<LoginResponseDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(400);
        apiResponse.Message.Should().Be("用户名或密码错误");

        _mockAuthService.Verify(x => x.LoginAsync(request, _cancellationToken), Times.Once);
    }
}

public class CropsControllerTests
{
    private readonly Mock<ICropService> _mockCropService;
    private readonly CropsController _controller;
    private readonly CancellationToken _cancellationToken;
    private readonly Guid _testUserId;

    public CropsControllerTests()
    {
        _mockCropService = new Mock<ICropService>();
        _controller = new CropsController(_mockCropService.Object);
        _cancellationToken = CancellationToken.None;
        _testUserId = Guid.NewGuid();
    }

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
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

    [Fact]
    public async Task CreateCrop_WhenAuthenticated_ReturnsCreatedAtAction()
    {
        SetupAuthenticatedUser(_testUserId);

        var request = new CreateCropRequestDto
        {
            Name = "西红柿",
            Variety = "千禧果",
            PlantingDate = DateTime.UtcNow,
            Location = "阳台",
            ContainerType = "花盆",
            Status = CropStatus.Growing
        };

        var cropId = Guid.NewGuid();
        var expectedResponse = ApiResponse<CropDto>.Success(new CropDto
        {
            Id = cropId,
            UserId = _testUserId,
            Name = "西红柿",
            Variety = "千禧果",
            PlantingDate = request.PlantingDate,
            Location = "阳台",
            ContainerType = "花盆",
            Status = CropStatus.Growing,
            CreatedAt = DateTime.UtcNow
        }, "创建成功");

        _mockCropService
            .Setup(x => x.CreateCropAsync(request, _testUserId, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.CreateCrop(request, _cancellationToken);

        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(CropsController.GetCropById));
        createdResult.RouteValues!["id"].Should().Be(cropId);

        var apiResponse = createdResult.Value as ApiResponse<CropDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(200);
        apiResponse.Message.Should().Be("创建成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(cropId);
        apiResponse.Data.Name.Should().Be("西红柿");

        _mockCropService.Verify(x => x.CreateCropAsync(request, _testUserId, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateCrop_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        SetupAnonymousUser();

        var request = new CreateCropRequestDto
        {
            Name = "西红柿",
            Variety = "千禧果",
            PlantingDate = DateTime.UtcNow,
            Location = "阳台",
            ContainerType = "花盆",
            Status = CropStatus.Growing
        };

        var result = await _controller.CreateCrop(request, _cancellationToken);

        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);

        var apiResponse = unauthorizedResult.Value as ApiResponse;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(401);
        apiResponse.Message.Should().Be("用户未认证");

        _mockCropService.Verify(x => x.CreateCropAsync(It.IsAny<CreateCropRequestDto>(), It.IsAny<Guid>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task CreateCrop_WhenServiceReturnsError_ReturnsBadRequest()
    {
        SetupAuthenticatedUser(_testUserId);

        var request = new CreateCropRequestDto
        {
            Name = "",
            Variety = "千禧果",
            PlantingDate = DateTime.UtcNow,
            Location = "阳台",
            ContainerType = "花盆",
            Status = CropStatus.Growing
        };

        var expectedResponse = ApiResponse<CropDto>.Error("作物名称不能为空", 400);

        _mockCropService
            .Setup(x => x.CreateCropAsync(request, _testUserId, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.CreateCrop(request, _cancellationToken);

        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);

        var apiResponse = badRequestResult.Value as ApiResponse<CropDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(400);
        apiResponse.Message.Should().Be("作物名称不能为空");

        _mockCropService.Verify(x => x.CreateCropAsync(request, _testUserId, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetCrops_WhenCalled_ReturnsOkResultWithPagedData()
    {
        SetupAnonymousUser();

        var query = new CropQueryRequestDto
        {
            PageNumber = 1,
            PageSize = 10,
            Status = CropStatus.Growing
        };

        var expectedResponse = ApiResponse<PagedResult<CropDto>>.Success(new PagedResult<CropDto>
        {
            Items = new List<CropDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    Name = "西红柿",
                    Variety = "千禧果",
                    PlantingDate = DateTime.UtcNow,
                    Location = "阳台",
                    ContainerType = "花盆",
                    Status = CropStatus.Growing,
                    CreatedAt = DateTime.UtcNow,
                    OwnerUsername = "testuser"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    Name = "黄瓜",
                    Variety = "水果黄瓜",
                    PlantingDate = DateTime.UtcNow,
                    Location = "阳台",
                    ContainerType = "种植箱",
                    Status = CropStatus.Growing,
                    CreatedAt = DateTime.UtcNow,
                    OwnerUsername = "testuser"
                }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 1
        }, "查询成功");

        _mockCropService
            .Setup(x => x.GetCropsAsync(query, null, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetCrops(query, _cancellationToken);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse<PagedResult<CropDto>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(200);
        apiResponse.Message.Should().Be("查询成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalCount.Should().Be(2);
        apiResponse.Data.Items.Should().HaveCount(2);
        apiResponse.Data.Items.First().Name.Should().Be("西红柿");

        _mockCropService.Verify(x => x.GetCropsAsync(query, null, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetCrops_WhenServiceReturnsError_ReturnsBadRequest()
    {
        SetupAnonymousUser();

        var query = new CropQueryRequestDto
        {
            PageNumber = 0,
            PageSize = 10
        };

        var expectedResponse = ApiResponse<PagedResult<CropDto>>.Error("页码必须大于0", 400);

        _mockCropService
            .Setup(x => x.GetCropsAsync(query, null, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetCrops(query, _cancellationToken);

        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);

        var apiResponse = badRequestResult.Value as ApiResponse<PagedResult<CropDto>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(400);
        apiResponse.Message.Should().Be("页码必须大于0");

        _mockCropService.Verify(x => x.GetCropsAsync(query, null, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetCropById_WhenCropExists_ReturnsOkResult()
    {
        SetupAnonymousUser();

        var cropId = Guid.NewGuid();
        var expectedResponse = ApiResponse<CropDto>.Success(new CropDto
        {
            Id = cropId,
            UserId = _testUserId,
            Name = "西红柿",
            Variety = "千禧果",
            PlantingDate = DateTime.UtcNow,
            Location = "阳台",
            ContainerType = "花盆",
            Status = CropStatus.Growing,
            CreatedAt = DateTime.UtcNow,
            OwnerUsername = "testuser"
        }, "查询成功");

        _mockCropService
            .Setup(x => x.GetCropByIdAsync(cropId, null, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetCropById(cropId, _cancellationToken);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse<CropDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(200);
        apiResponse.Message.Should().Be("查询成功");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(cropId);
        apiResponse.Data.Name.Should().Be("西红柿");

        _mockCropService.Verify(x => x.GetCropByIdAsync(cropId, null, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetCropById_WhenCropNotFound_ReturnsNotFound()
    {
        SetupAnonymousUser();

        var cropId = Guid.NewGuid();
        var expectedResponse = ApiResponse<CropDto>.Error("作物不存在", 404);

        _mockCropService
            .Setup(x => x.GetCropByIdAsync(cropId, null, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetCropById(cropId, _cancellationToken);

        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);

        var apiResponse = notFoundResult.Value as ApiResponse<CropDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(404);
        apiResponse.Message.Should().Be("作物不存在");

        _mockCropService.Verify(x => x.GetCropByIdAsync(cropId, null, _cancellationToken), Times.Once);
    }
}
