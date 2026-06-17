using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Interfaces;
using BalconyFarm.Application.Models;
using BalconyFarm.Application.Services;
using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Enums;
using BalconyFarm.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TaskStatus = BalconyFarm.Domain.Enums.TaskStatus;

namespace BalconyFarm.Tests;

public class ServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHashService> _passwordHashServiceMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _authLoggerMock;
    private readonly Mock<ILogger<CropService>> _cropLoggerMock;
    private readonly Mock<ILogger<CropCareTaskService>> _taskLoggerMock;
    private readonly CancellationToken _cancellationToken;

    public ServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _passwordHashServiceMock = new Mock<IPasswordHashService>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _authLoggerMock = new Mock<ILogger<AuthService>>();
        _cropLoggerMock = new Mock<ILogger<CropService>>();
        _taskLoggerMock = new Mock<ILogger<CropCareTaskService>>();
        _cancellationToken = CancellationToken.None;
    }

    #region AuthService Tests

    [Fact]
    public async Task RegisterAsync_ShouldReturnSuccess_WhenUserDoesNotExist()
    {
        var registerDto = new RegisterRequestDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        _unitOfWorkMock.Setup(u => u.Users.ExistsAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(false);

        _passwordHashServiceMock.Setup(p => p.HashPassword(registerDto.Password))
            .Returns("hashed_password");

        _jwtTokenServiceMock.Setup(j => j.GenerateToken(It.IsAny<Guid>(), registerDto.Username))
            .Returns("test_token");

        _unitOfWorkMock.Setup(u => u.Users.AddAsync(It.IsAny<User>(), _cancellationToken))
            .ReturnsAsync((User user, CancellationToken ct) => user);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var authService = new AuthService(
            _unitOfWorkMock.Object,
            _passwordHashServiceMock.Object,
            _jwtTokenServiceMock.Object,
            _authLoggerMock.Object);

        var result = await authService.RegisterAsync(registerDto, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be("注册成功");
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("test_token");
        result.Data.User.Username.Should().Be(registerDto.Username);
        result.Data.User.Email.Should().Be(registerDto.Email);

        _unitOfWorkMock.Verify(u => u.Users.AddAsync(It.Is<User>(u =>
            u.Username == registerDto.Username &&
            u.Email == registerDto.Email &&
            u.PasswordHash == "hashed_password"), _cancellationToken), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cancellationToken), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenUsernameExists()
    {
        var registerDto = new RegisterRequestDto
        {
            Username = "existinguser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        _unitOfWorkMock.Setup(u => u.Users.ExistsAsync(
            It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(e =>
                e.Compile().Invoke(new User { Username = registerDto.Username })),
            _cancellationToken))
            .ReturnsAsync(true);

        var authService = new AuthService(
            _unitOfWorkMock.Object,
            _passwordHashServiceMock.Object,
            _jwtTokenServiceMock.Object,
            _authLoggerMock.Object);

        var result = await authService.RegisterAsync(registerDto, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(400);
        result.Message.Should().Be("用户名已存在");
        result.Data.Should().BeNull();

        _unitOfWorkMock.Verify(u => u.Users.AddAsync(It.IsAny<User>(), _cancellationToken), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cancellationToken), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenEmailExists()
    {
        var registerDto = new RegisterRequestDto
        {
            Username = "testuser",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        _unitOfWorkMock.SetupSequence(u => u.Users.ExistsAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var authService = new AuthService(
            _unitOfWorkMock.Object,
            _passwordHashServiceMock.Object,
            _jwtTokenServiceMock.Object,
            _authLoggerMock.Object);

        var result = await authService.RegisterAsync(registerDto, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(400);
        result.Message.Should().Be("邮箱已被注册");
        result.Data.Should().BeNull();

        _unitOfWorkMock.Verify(u => u.Users.AddAsync(It.IsAny<User>(), _cancellationToken), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cancellationToken), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        var loginDto = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed_password"
        };

        _unitOfWorkMock.Setup(u => u.Users.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<User> { user });

        _passwordHashServiceMock.Setup(p => p.VerifyPassword(loginDto.Password, user.PasswordHash))
            .Returns(true);

        _jwtTokenServiceMock.Setup(j => j.GenerateToken(user.Id, user.Username))
            .Returns("test_token");

        var authService = new AuthService(
            _unitOfWorkMock.Object,
            _passwordHashServiceMock.Object,
            _jwtTokenServiceMock.Object,
            _authLoggerMock.Object);

        var result = await authService.LoginAsync(loginDto, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be("登录成功");
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("test_token");
        result.Data.User.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnError_WhenUserDoesNotExist()
    {
        var loginDto = new LoginRequestDto
        {
            UsernameOrEmail = "nonexistent",
            Password = "Password123!"
        };

        _unitOfWorkMock.Setup(u => u.Users.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<User>());

        var authService = new AuthService(
            _unitOfWorkMock.Object,
            _passwordHashServiceMock.Object,
            _jwtTokenServiceMock.Object,
            _authLoggerMock.Object);

        var result = await authService.LoginAsync(loginDto, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(401);
        result.Message.Should().Be("用户名或密码错误");
        result.Data.Should().BeNull();

        _passwordHashServiceMock.Verify(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _jwtTokenServiceMock.Verify(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnError_WhenPasswordIsInvalid()
    {
        var loginDto = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "WrongPassword!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed_password"
        };

        _unitOfWorkMock.Setup(u => u.Users.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<User> { user });

        _passwordHashServiceMock.Setup(p => p.VerifyPassword(loginDto.Password, user.PasswordHash))
            .Returns(false);

        var authService = new AuthService(
            _unitOfWorkMock.Object,
            _passwordHashServiceMock.Object,
            _jwtTokenServiceMock.Object,
            _authLoggerMock.Object);

        var result = await authService.LoginAsync(loginDto, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(401);
        result.Message.Should().Be("用户名或密码错误");
        result.Data.Should().BeNull();

        _jwtTokenServiceMock.Verify(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region CropService Tests

    [Fact]
    public async Task CreateCropAsync_ShouldReturnSuccess_WhenValidData()
    {
        var userId = Guid.NewGuid();
        var createDto = new CreateCropRequestDto
        {
            Name = "番茄",
            Variety = "圣女果",
            PlantingDate = DateTime.UtcNow,
            Location = "阳台",
            ContainerType = "花盆"
        };

        _unitOfWorkMock.Setup(u => u.Crops.AddAsync(It.IsAny<Crop>(), _cancellationToken))
            .ReturnsAsync((Crop crop, CancellationToken ct) => crop);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var cropService = new CropService(_unitOfWorkMock.Object, _cropLoggerMock.Object);

        var result = await cropService.CreateCropAsync(createDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be("创建成功");
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(createDto.Name);
        result.Data.Variety.Should().Be(createDto.Variety);
        result.Data.UserId.Should().Be(userId);

        _unitOfWorkMock.Verify(u => u.Crops.AddAsync(It.Is<Crop>(c =>
            c.Name == createDto.Name &&
            c.UserId == userId), _cancellationToken), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetCropByIdAsync_ShouldReturnSuccess_WhenCropExistsAndUserIsOwner()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Variety = "圣女果"
        };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var cropService = new CropService(_unitOfWorkMock.Object, _cropLoggerMock.Object);

        var result = await cropService.GetCropByIdAsync(cropId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(cropId);
        result.Data.Name.Should().Be("番茄");
    }

    [Fact]
    public async Task GetCropByIdAsync_ShouldReturnError_WhenCropDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync((Crop?)null);

        var cropService = new CropService(_unitOfWorkMock.Object, _cropLoggerMock.Object);

        var result = await cropService.GetCropByIdAsync(cropId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("作物不存在");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetCropByIdAsync_ShouldReturnError_WhenUserIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = ownerId,
            Name = "番茄"
        };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var cropService = new CropService(_unitOfWorkMock.Object, _cropLoggerMock.Object);

        var result = await cropService.GetCropByIdAsync(cropId, otherUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权访问此作物");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCropAsync_ShouldReturnSuccess_WhenValidDataAndUserIsOwner()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var existingCrop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Variety = "圣女果",
            Status = CropStatus.Growing
        };

        var updateDto = new UpdateCropRequestDto
        {
            Name = "大番茄",
            Status = CropStatus.Harvesting
        };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(existingCrop);

        _unitOfWorkMock.Setup(u => u.Crops.UpdateAsync(It.IsAny<Crop>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var cropService = new CropService(_unitOfWorkMock.Object, _cropLoggerMock.Object);

        var result = await cropService.UpdateCropAsync(cropId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be("更新成功");
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("大番茄");
        result.Data.Status.Should().Be(CropStatus.Harvesting);

        _unitOfWorkMock.Verify(u => u.Crops.UpdateAsync(It.Is<Crop>(c =>
            c.Name == "大番茄" &&
            c.Status == CropStatus.Harvesting), _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateCropAsync_ShouldReturnError_WhenCropDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var updateDto = new UpdateCropRequestDto { Name = "大番茄" };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync((Crop?)null);

        var cropService = new CropService(_unitOfWorkMock.Object, _cropLoggerMock.Object);

        var result = await cropService.UpdateCropAsync(cropId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("作物不存在");
        result.Data.Should().BeNull();

        _unitOfWorkMock.Verify(u => u.Crops.UpdateAsync(It.IsAny<Crop>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateCropAsync_ShouldReturnError_WhenUserIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var existingCrop = new Crop
        {
            Id = cropId,
            UserId = ownerId,
            Name = "番茄"
        };

        var updateDto = new UpdateCropRequestDto { Name = "大番茄" };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(existingCrop);

        var cropService = new CropService(_unitOfWorkMock.Object, _cropLoggerMock.Object);

        var result = await cropService.UpdateCropAsync(cropId, updateDto, otherUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权修改此作物");
        result.Data.Should().BeNull();

        _unitOfWorkMock.Verify(u => u.Crops.UpdateAsync(It.IsAny<Crop>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task DeleteCropAsync_ShouldReturnSuccess_WhenUserIsOwner()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.Crops.DeleteAsync(crop, _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var cropService = new CropService(_unitOfWorkMock.Object, _cropLoggerMock.Object);

        var result = await cropService.DeleteCropAsync(cropId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be("删除成功");

        _unitOfWorkMock.Verify(u => u.Crops.DeleteAsync(crop, _cancellationToken), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cancellationToken), Times.Once);
    }

    [Fact]
    public async Task DeleteCropAsync_ShouldReturnError_WhenCropDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync((Crop?)null);

        var cropService = new CropService(_unitOfWorkMock.Object, _cropLoggerMock.Object);

        var result = await cropService.DeleteCropAsync(cropId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("作物不存在");

        _unitOfWorkMock.Verify(u => u.Crops.DeleteAsync(It.IsAny<Crop>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task DeleteCropAsync_ShouldReturnError_WhenUserIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = ownerId,
            Name = "番茄"
        };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var cropService = new CropService(_unitOfWorkMock.Object, _cropLoggerMock.Object);

        var result = await cropService.DeleteCropAsync(cropId, otherUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权删除此作物");

        _unitOfWorkMock.Verify(u => u.Crops.DeleteAsync(It.IsAny<Crop>(), _cancellationToken), Times.Never);
    }

    #endregion

    #region CropCareTaskService Tests

    [Fact]
    public async Task CreateCropCareTaskAsync_ShouldReturnSuccess_WhenValidData()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };

        var createDto = new CreateCropCareTaskRequestDto
        {
            CropId = cropId,
            TaskType = TaskType.Water,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Note = "记得浇水"
        };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.AddAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .ReturnsAsync((CropCareTask task, CancellationToken ct) => task);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.CreateCropCareTaskAsync(createDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be("创建成功");
        result.Data.Should().NotBeNull();
        result.Data!.TaskType.Should().Be(TaskType.Water);
        result.Data.Status.Should().Be(TaskStatus.Pending);
        result.Data.CropName.Should().Be("番茄");

        _unitOfWorkMock.Verify(u => u.CropCareTasks.AddAsync(It.Is<CropCareTask>(t =>
            t.CropId == cropId &&
            t.TaskType == TaskType.Water &&
            t.Status == TaskStatus.Pending), _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldSetCompletedDate_WhenStatusIsCompleted()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            CompletedDate = null
        };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.Completed
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be("状态更新成功");
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Completed);
        result.Data.CompletedDate.Should().NotBeNull();
        result.Data.CompletedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        _unitOfWorkMock.Verify(u => u.CropCareTasks.UpdateAsync(It.Is<CropCareTask>(t =>
            t.Status == TaskStatus.Completed &&
            t.CompletedDate.HasValue), _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateCropCareTaskAsync_ShouldSetCompletedDate_WhenStatusIsCompleted()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            CompletedDate = null
        };

        var updateDto = new UpdateCropCareTaskRequestDto
        {
            Status = TaskStatus.Completed,
            Note = "已完成浇水"
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateCropCareTaskAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Completed);
        result.Data.CompletedDate.Should().NotBeNull();
        result.Data.CompletedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Data.Note.Should().Be("已完成浇水");
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldNotSetCompletedDate_WhenStatusIsNotCompleted()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            CompletedDate = null
        };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.InProgress
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.InProgress);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldReturnError_WhenUserIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = ownerId,
            Name = "番茄"
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending
        };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.Completed
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, otherUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权修改此任务");
        result.Data.Should().BeNull();

        _unitOfWorkMock.Verify(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldReturnError_WhenTaskDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.InProgress
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync((CropCareTask?)null);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("任务不存在");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldChangeToInProgress_WhenFromPending()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Status = CropStatus.Growing
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            CompletedDate = null
        };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.InProgress
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.InProgress);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldChangeToCancelled_WhenFromPending()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Status = CropStatus.Growing
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            CompletedDate = null
        };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.Cancelled
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Cancelled);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldUpdateCropToFinished_WhenAllTasksCompleted()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Status = CropStatus.Growing
        };
        var task1 = new CropCareTask
        {
            Id = taskId1,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Completed,
            CompletedDate = DateTime.UtcNow.AddDays(-1)
        };
        var task2 = new CropCareTask
        {
            Id = taskId2,
            CropId = cropId,
            TaskType = TaskType.Fertilize,
            Status = TaskStatus.Pending,
            CompletedDate = null
        };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.Completed
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId2, _cancellationToken))
            .ReturnsAsync(task2);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Crops.UpdateAsync(It.IsAny<Crop>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task1, task2 });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId2, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);

        _unitOfWorkMock.Verify(u => u.Crops.UpdateAsync(It.Is<Crop>(c =>
            c.Status == CropStatus.Finished), _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldNotUpdateCrop_WhenNotAllTasksCompleted()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Status = CropStatus.Growing
        };
        var task1 = new CropCareTask
        {
            Id = taskId1,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Completed
        };
        var task2 = new CropCareTask
        {
            Id = taskId2,
            CropId = cropId,
            TaskType = TaskType.Fertilize,
            Status = TaskStatus.Pending
        };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.InProgress
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId2, _cancellationToken))
            .ReturnsAsync(task2);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task1, task2 });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId2, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);

        _unitOfWorkMock.Verify(u => u.Crops.UpdateAsync(It.IsAny<Crop>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateCropCareTaskAsync_ShouldReturnError_WhenTaskDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var updateDto = new UpdateCropCareTaskRequestDto
        {
            Status = TaskStatus.InProgress,
            Note = "开始浇水"
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync((CropCareTask?)null);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateCropCareTaskAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("任务不存在");
        result.Data.Should().BeNull();

        _unitOfWorkMock.Verify(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateCropCareTaskAsync_ShouldChangeStatusToCancelled()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Status = CropStatus.Growing
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            ScheduledDate = DateTime.UtcNow.AddDays(3),
            Note = "待浇水"
        };

        var updateDto = new UpdateCropCareTaskRequestDto
        {
            Status = TaskStatus.Cancelled,
            Note = "不需要浇水了"
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateCropCareTaskAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Cancelled);
        result.Data.Note.Should().Be("不需要浇水了");
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCropCareTaskAsync_ShouldClearCompletedDate_WhenStatusChangedFromCompletedToPending()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Status = CropStatus.Growing
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Completed,
            ScheduledDate = DateTime.UtcNow.AddDays(-2),
            CompletedDate = DateTime.UtcNow.AddDays(-1),
            Note = "已完成"
        };

        var updateDto = new UpdateCropCareTaskRequestDto
        {
            Status = TaskStatus.Pending,
            Note = "重新安排"
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateCropCareTaskAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Pending);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCropCareTaskAsync_ShouldClearCompletedDate_WhenStatusChangedFromCompletedToInProgress()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Status = CropStatus.Growing
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Fertilize,
            Status = TaskStatus.Completed,
            ScheduledDate = DateTime.UtcNow.AddDays(-3),
            CompletedDate = DateTime.UtcNow.AddDays(-2),
            Note = "已完成施肥"
        };

        var updateDto = new UpdateCropCareTaskRequestDto
        {
            Status = TaskStatus.InProgress,
            Note = "重新开始施肥"
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateCropCareTaskAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.InProgress);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldClearCompletedDate_WhenStatusChangedFromCompletedToPending()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Status = CropStatus.Growing
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Completed,
            ScheduledDate = DateTime.UtcNow.AddDays(-2),
            CompletedDate = DateTime.UtcNow.AddDays(-1)
        };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.Pending
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Pending);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldClearCompletedDate_WhenStatusChangedFromCompletedToInProgress()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Status = CropStatus.Growing
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Prune,
            Status = TaskStatus.Completed,
            ScheduledDate = DateTime.UtcNow.AddDays(-5),
            CompletedDate = DateTime.UtcNow.AddDays(-3)
        };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.InProgress
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.InProgress);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCropCareTaskAsync_ShouldUpdateCropToFinished_WhenRemainingTasksAllCompleted()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskIdToDelete = Guid.NewGuid();
        var remainingTaskId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄",
            Status = CropStatus.Growing
        };
        var taskToDelete = new CropCareTask
        {
            Id = taskIdToDelete,
            CropId = cropId,
            TaskType = TaskType.Fertilize,
            Status = TaskStatus.Pending
        };
        var remainingTask = new CropCareTask
        {
            Id = remainingTaskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Completed
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskIdToDelete, _cancellationToken))
            .ReturnsAsync(taskToDelete);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.DeleteAsync(taskToDelete, _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Crops.UpdateAsync(It.IsAny<Crop>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { remainingTask });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.DeleteCropCareTaskAsync(taskIdToDelete, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);

        _unitOfWorkMock.Verify(u => u.Crops.UpdateAsync(It.Is<Crop>(c =>
            c.Status == CropStatus.Finished), _cancellationToken), Times.Once);
    }

    #endregion

    #region CropCareTaskService Overdue Tests

    [Fact]
    public async Task GetCropCareTaskByIdAsync_ShouldMarkOverdue_WhenPendingAndScheduledDateIsPast()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.Date.AddDays(-3);
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            ScheduledDate = scheduledDate
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTaskByIdAsync(taskId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeTrue();
        result.Data.OverdueDays.Should().Be(3);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_ShouldMarkOverdue_WhenInProgressAndScheduledDateIsPast()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.Date.AddDays(-5);
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Fertilize,
            Status = TaskStatus.InProgress,
            ScheduledDate = scheduledDate
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTaskByIdAsync(taskId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeTrue();
        result.Data.OverdueDays.Should().Be(5);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_ShouldNotMarkOverdue_WhenCompletedAndScheduledDateIsPast()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.Date.AddDays(-10);
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Completed,
            ScheduledDate = scheduledDate,
            CompletedDate = DateTime.UtcNow.AddDays(-2)
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTaskByIdAsync(taskId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeFalse();
        result.Data.OverdueDays.Should().BeNull();
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_ShouldNotMarkOverdue_WhenCancelledAndScheduledDateIsPast()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.Date.AddDays(-7);
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Prune,
            Status = TaskStatus.Cancelled,
            ScheduledDate = scheduledDate
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTaskByIdAsync(taskId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeFalse();
        result.Data.OverdueDays.Should().BeNull();
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_ShouldNotMarkOverdue_WhenPendingAndScheduledDateIsToday()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.Date;
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            ScheduledDate = scheduledDate
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTaskByIdAsync(taskId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeFalse();
        result.Data.OverdueDays.Should().BeNull();
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_ShouldNotMarkOverdue_WhenPendingAndScheduledDateIsFuture()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.Date.AddDays(5);
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            ScheduledDate = scheduledDate
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTaskByIdAsync(taskId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeFalse();
        result.Data.OverdueDays.Should().BeNull();
    }

    [Fact]
    public async Task CreateCropCareTaskAsync_ShouldSetOverdueInfoCorrectly()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop
        {
            Id = cropId,
            UserId = userId,
            Name = "番茄"
        };

        var createDto = new CreateCropCareTaskRequestDto
        {
            CropId = cropId,
            TaskType = TaskType.Water,
            ScheduledDate = DateTime.UtcNow.AddDays(-2),
            Note = "补浇水"
        };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.AddAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .ReturnsAsync((CropCareTask task, CancellationToken ct) => task);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.CreateCropCareTaskAsync(createDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeTrue();
        result.Data.OverdueDays.Should().Be(2);
    }

    #endregion

    #region CropCareTaskService Query Tests

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldFilterByCropId()
    {
        var userId = Guid.NewGuid();
        var cropId1 = Guid.NewGuid();
        var cropId2 = Guid.NewGuid();
        var crop1 = new Crop { Id = cropId1, UserId = userId, Name = "番茄" };
        var crop2 = new Crop { Id = cropId2, UserId = userId, Name = "黄瓜" };

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId1, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), CropId = cropId2, TaskType = TaskType.Fertilize, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(2) },
            new() { Id = Guid.NewGuid(), CropId = cropId1, TaskType = TaskType.Prune, Status = TaskStatus.Completed, ScheduledDate = DateTime.UtcNow.AddDays(-1) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            CropId = cropId1,
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop1, crop2 });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop1, crop2 });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t => t.CropId == cropId1);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldFilterByTaskType()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(2) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Completed, ScheduledDate = DateTime.UtcNow.AddDays(-1) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            TaskType = TaskType.Water,
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t => t.TaskType == TaskType.Water);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldFilterByStatus()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.InProgress, ScheduledDate = DateTime.UtcNow.AddDays(2) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Prune, Status = TaskStatus.Completed, ScheduledDate = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Repot, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(5) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            Status = TaskStatus.Pending,
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t => t.Status == TaskStatus.Pending);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldFilterByScheduledDateFrom()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };
        var fromDate = new DateTime(2024, 6, 1);

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 5, 15) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 6, 5) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Prune, Status = TaskStatus.Completed, ScheduledDate = new DateTime(2024, 6, 10) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Repot, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 5, 20) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            ScheduledDateFrom = fromDate,
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t => t.ScheduledDate >= fromDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldFilterByScheduledDateTo()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };
        var toDate = new DateTime(2024, 6, 15);

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 5, 15) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 6, 5) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Prune, Status = TaskStatus.Completed, ScheduledDate = new DateTime(2024, 6, 20) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Repot, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 7, 1) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            ScheduledDateTo = toDate,
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t => t.ScheduledDate <= toDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldFilterByDateRange()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };
        var fromDate = new DateTime(2024, 6, 1);
        var toDate = new DateTime(2024, 6, 30);

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 5, 15) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 6, 5) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Prune, Status = TaskStatus.Completed, ScheduledDate = new DateTime(2024, 6, 20) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Repot, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 7, 1) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            ScheduledDateFrom = fromDate,
            ScheduledDateTo = toDate,
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t => t.ScheduledDate >= fromDate && t.ScheduledDate <= toDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldFilterByUserId()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var cropId1 = Guid.NewGuid();
        var cropId2 = Guid.NewGuid();
        var crop1 = new Crop { Id = cropId1, UserId = userId1, Name = "番茄" };
        var crop2 = new Crop { Id = cropId2, UserId = userId2, Name = "黄瓜" };

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId1, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), CropId = cropId2, TaskType = TaskType.Fertilize, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(2) },
            new() { Id = Guid.NewGuid(), CropId = cropId1, TaskType = TaskType.Prune, Status = TaskStatus.Completed, ScheduledDate = DateTime.UtcNow.AddDays(-1) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop1, crop2 });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.Is<System.Linq.Expressions.Expression<Func<Crop, bool>>>(e =>
                e.Compile().Invoke(crop1)),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop1 });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId1, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t => t.CropId == cropId1);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldApplyCombinedFilters()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 6, 1) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Completed, ScheduledDate = new DateTime(2024, 6, 5) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 6, 10) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = new DateTime(2024, 7, 1) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            ScheduledDateFrom = new DateTime(2024, 5, 1),
            ScheduledDateTo = new DateTime(2024, 6, 30),
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(1);
        var item = result.Data.Items.First();
        item.TaskType.Should().Be(TaskType.Water);
        item.Status.Should().Be(TaskStatus.Pending);
        item.ScheduledDate.Should().BeOnOrAfter(new DateTime(2024, 5, 1));
        item.ScheduledDate.Should().BeOnOrBefore(new DateTime(2024, 6, 30));
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldReturnPagedResults()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };

        var tasks = new List<CropCareTask>();
        for (int i = 0; i < 15; i++)
        {
            tasks.Add(new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(i)
            });
        }

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 2,
            PageSize = 5
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(15);
        result.Data.PageNumber.Should().Be(2);
        result.Data.PageSize.Should().Be(5);
        result.Data.TotalPages.Should().Be(3);
        result.Data.Items.Should().HaveCount(5);
        result.Data.HasPrevious.Should().BeTrue();
        result.Data.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldSortByScheduledDateDescending_ByDefault()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };
        var date1 = DateTime.UtcNow.AddDays(1);
        var date2 = DateTime.UtcNow.AddDays(5);
        var date3 = DateTime.UtcNow.AddDays(3);

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = date1 },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.Pending, ScheduledDate = date2 },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Prune, Status = TaskStatus.Pending, ScheduledDate = date3 }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        var items = result.Data!.Items.ToList();
        items.Should().HaveCount(3);
        items[0].ScheduledDate.Should().Be(date2);
        items[1].ScheduledDate.Should().Be(date3);
        items[2].ScheduledDate.Should().Be(date1);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldSortByTaskTypeAscending()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(2) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Prune, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(3) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            SortBy = "tasktype",
            SortOrder = "asc",
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        var items = result.Data!.Items.ToList();
        items.Should().HaveCount(3);
        items[0].TaskType.Should().Be(TaskType.Water);
        items[1].TaskType.Should().Be(TaskType.Fertilize);
        items[2].TaskType.Should().Be(TaskType.Prune);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldIncludeCropNameAndOverdueInfo()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };
        var overdueDate = DateTime.UtcNow.Date.AddDays(-2);

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = overdueDate }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        var item = result.Data!.Items.First();
        item.CropName.Should().Be("番茄");
        item.IsOverdue.Should().BeTrue();
        item.OverdueDays.Should().Be(2);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldReturnEmpty_WhenNoTasksMatchFilter()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.Completed, ScheduledDate = DateTime.UtcNow.AddDays(-1) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            Status = TaskStatus.Cancelled,
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(0);
        result.Data.Items.Should().BeEmpty();
        result.Data.TotalPages.Should().Be(0);
        result.Data.HasPrevious.Should().BeFalse();
        result.Data.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldSortByStatusDescending()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Pending, ScheduledDate = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.Completed, ScheduledDate = DateTime.UtcNow.AddDays(2) },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Prune, Status = TaskStatus.InProgress, ScheduledDate = DateTime.UtcNow.AddDays(3) }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            SortBy = "status",
            SortOrder = "desc",
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        var items = result.Data!.Items.ToList();
        items.Should().HaveCount(3);
        items[0].Status.Should().Be(TaskStatus.Completed);
        items[1].Status.Should().Be(TaskStatus.InProgress);
        items[2].Status.Should().Be(TaskStatus.Pending);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldSortByCompletedDateDescending()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = userId, Name = "番茄" };
        var date1 = DateTime.UtcNow.AddDays(-1);
        var date2 = DateTime.UtcNow.AddDays(-5);
        var date3 = DateTime.UtcNow.AddDays(-3);

        var tasks = new List<CropCareTask>
        {
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Water, Status = TaskStatus.Completed, ScheduledDate = DateTime.UtcNow.AddDays(-2), CompletedDate = date1 },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Fertilize, Status = TaskStatus.Completed, ScheduledDate = DateTime.UtcNow.AddDays(-6), CompletedDate = date2 },
            new() { Id = Guid.NewGuid(), CropId = cropId, TaskType = TaskType.Prune, Status = TaskStatus.Completed, ScheduledDate = DateTime.UtcNow.AddDays(-4), CompletedDate = date3 }
        };

        var query = new CropCareTaskQueryRequestDto
        {
            SortBy = "completeddate",
            SortOrder = "desc",
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        var items = result.Data!.Items.ToList();
        items.Should().HaveCount(3);
        items[0].CompletedDate.Should().BeCloseTo(date1, TimeSpan.FromSeconds(1));
        items[1].CompletedDate.Should().BeCloseTo(date3, TimeSpan.FromSeconds(1));
        items[2].CompletedDate.Should().BeCloseTo(date2, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_ShouldReturnError_WhenTaskDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync((CropCareTask?)null);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTaskByIdAsync(taskId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("任务不存在");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_ShouldReturnError_WhenUserIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = ownerId, Name = "番茄" };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            ScheduledDate = DateTime.UtcNow.AddDays(1)
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.GetCropCareTaskByIdAsync(taskId, otherUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权访问此任务");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task CreateCropCareTaskAsync_ShouldReturnError_WhenCropDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();

        var createDto = new CreateCropCareTaskRequestDto
        {
            CropId = cropId,
            TaskType = TaskType.Water,
            ScheduledDate = DateTime.UtcNow.AddDays(1)
        };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync((Crop?)null);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.CreateCropCareTaskAsync(createDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("作物不存在");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task CreateCropCareTaskAsync_ShouldReturnError_WhenUserIsNotCropOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = ownerId, Name = "番茄" };

        var createDto = new CreateCropCareTaskRequestDto
        {
            CropId = cropId,
            TaskType = TaskType.Water,
            ScheduledDate = DateTime.UtcNow.AddDays(1)
        };

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.CreateCropCareTaskAsync(createDto, otherUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权为此作物创建任务");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCropCareTaskAsync_ShouldReturnError_WhenTaskDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync((CropCareTask?)null);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.DeleteCropCareTaskAsync(taskId, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("任务不存在");
    }

    [Fact]
    public async Task DeleteCropCareTaskAsync_ShouldReturnError_WhenUserIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = ownerId, Name = "番茄" };
        var task = new CropCareTask
        {
            Id = taskId,
            CropId = cropId,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.DeleteCropCareTaskAsync(taskId, otherUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权删除此任务");
    }

    #endregion
}
