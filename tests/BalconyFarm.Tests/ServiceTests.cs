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

    #endregion
}
