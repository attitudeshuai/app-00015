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

        _unitOfWorkMock.Verify(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_FromPendingToInProgress_ShouldUpdateStatus()
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

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.InProgress);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_FromInProgressToCompleted_ShouldSetCompletedDate()
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
            Status = TaskStatus.InProgress,
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
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Completed);
        result.Data.CompletedDate.Should().NotBeNull();
        result.Data.CompletedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_FromPendingToCancelled_ShouldNotSetCompletedDate()
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

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Cancelled);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenAllTasksCompleted_ShouldUpdateCropStatusToFinished()
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
        var allTasks = new List<CropCareTask> { task1, task2 };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.Completed
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId2, _cancellationToken))
            .ReturnsAsync(task2);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.Is<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(e =>
                e.Compile().Invoke(new CropCareTask { CropId = cropId })),
            _cancellationToken))
            .ReturnsAsync(allTasks);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Crops.UpdateAsync(It.IsAny<Crop>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId2, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);

        _unitOfWorkMock.Verify(u => u.Crops.UpdateAsync(It.Is<Crop>(c =>
            c.Id == cropId && c.Status == CropStatus.Finished), _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenNotAllTasksCompleted_ShouldNotUpdateCropStatus()
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
            Status = TaskStatus.InProgress
        };
        var task2 = new CropCareTask
        {
            Id = taskId2,
            CropId = cropId,
            TaskType = TaskType.Fertilize,
            Status = TaskStatus.Pending
        };
        var allTasks = new List<CropCareTask> { task1, task2 };

        var updateDto = new UpdateTaskStatusRequestDto
        {
            Status = TaskStatus.InProgress
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId2, _cancellationToken))
            .ReturnsAsync(task2);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(
            It.Is<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(e =>
                e.Compile().Invoke(new CropCareTask { CropId = cropId })),
            _cancellationToken))
            .ReturnsAsync(allTasks);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

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
        var updateDto = new UpdateCropCareTaskRequestDto { Note = "测试备注" };

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
    public async Task UpdateCropCareTaskAsync_ShouldReturnError_WhenUserIsNotOwner()
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

        var updateDto = new UpdateCropCareTaskRequestDto { Note = "测试备注" };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(cropId, _cancellationToken))
            .ReturnsAsync(crop);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateCropCareTaskAsync(taskId, updateDto, otherUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权修改此任务");
        result.Data.Should().BeNull();

        _unitOfWorkMock.Verify(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateCropCareTaskAsync_PartialUpdateNote_ShouldOnlyUpdateNote()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
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
            ScheduledDate = scheduledDate,
            Note = "原始备注"
        };

        var updateDto = new UpdateCropCareTaskRequestDto
        {
            Note = "更新后的备注"
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
        result.Data!.Note.Should().Be("更新后的备注");
        result.Data.TaskType.Should().Be(TaskType.Water);
        result.Data.Status.Should().Be(TaskStatus.Pending);
        result.Data.ScheduledDate.Should().Be(scheduledDate);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_PendingTaskWithPastDate_ShouldMarkAsOverdue()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(-5).Date;
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
        result.Data.OverdueDays.Should().Be(5);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_InProgressTaskWithPastDate_ShouldMarkAsOverdue()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(-3).Date;
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
        result.Data.OverdueDays.Should().Be(3);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_CompletedTaskWithPastDate_ShouldNotMarkAsOverdue()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(-10).Date;
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
            CompletedDate = DateTime.UtcNow.AddDays(-8)
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
    public async Task GetCropCareTaskByIdAsync_CancelledTaskWithPastDate_ShouldNotMarkAsOverdue()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(-7).Date;
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
    public async Task GetCropCareTaskByIdAsync_PendingTaskWithTodayDate_ShouldNotMarkAsOverdue()
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
    public async Task GetCropCareTaskByIdAsync_PendingTaskWithFutureDate_ShouldNotMarkAsOverdue()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(3).Date;
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
    public async Task GetCropCareTaskByIdAsync_OverdueOneDay_ShouldCalculateOverdueDaysCorrectly()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(-1).Date;
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
        result.Data.OverdueDays.Should().Be(1);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_FilterByCropId_ShouldReturnMatchingTasks()
    {
        var userId = Guid.NewGuid();
        var cropId1 = Guid.NewGuid();
        var cropId2 = Guid.NewGuid();
        var crop1 = new Crop { Id = cropId1, UserId = userId, Name = "番茄" };
        var crop2 = new Crop { Id = cropId2, UserId = userId, Name = "黄瓜" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId1,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId2,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(2)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId1,
                TaskType = TaskType.Prune,
                Status = TaskStatus.Completed,
                ScheduledDate = DateTime.UtcNow.AddDays(-1)
            }
        };
        var allCrops = new List<Crop> { crop1, crop2 };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allCrops);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            CropId = cropId1,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t => t.CropId == cropId1);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_FilterByTaskType_ShouldReturnMatchingTasks()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(2)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Completed,
                ScheduledDate = DateTime.UtcNow.AddDays(-1)
            }
        };
        var allCrops = new List<Crop> { crop };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allCrops);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            TaskType = TaskType.Water,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t => t.TaskType == TaskType.Water);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_FilterByStatus_ShouldReturnMatchingTasks()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.InProgress,
                ScheduledDate = DateTime.UtcNow.AddDays(2)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(3)
            }
        };
        var allCrops = new List<Crop> { crop };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allCrops);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            Status = TaskStatus.Pending,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t => t.Status == TaskStatus.Pending);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_FilterByDateRange_ShouldReturnMatchingTasks()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var baseDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = baseDate.AddDays(-5)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Pending,
                ScheduledDate = baseDate
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Prune,
                Status = TaskStatus.Pending,
                ScheduledDate = baseDate.AddDays(5)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = baseDate.AddDays(10)
            }
        };
        var allCrops = new List<Crop> { crop };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allCrops);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            ScheduledDateFrom = baseDate.AddDays(-1),
            ScheduledDateTo = baseDate.AddDays(6),
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(t =>
            t.ScheduledDate >= query.ScheduledDateFrom &&
            t.ScheduledDate <= query.ScheduledDateTo);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_MultipleFilters_ShouldReturnMatchingTasks()
    {
        var cropId1 = Guid.NewGuid();
        var cropId2 = Guid.NewGuid();
        var crop1 = new Crop { Id = cropId1, UserId = Guid.NewGuid(), Name = "番茄" };
        var crop2 = new Crop { Id = cropId2, UserId = Guid.NewGuid(), Name = "黄瓜" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId1,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId1,
                TaskType = TaskType.Water,
                Status = TaskStatus.Completed,
                ScheduledDate = DateTime.UtcNow.AddDays(-1)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId2,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(2)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId1,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(3)
            }
        };
        var allCrops = new List<Crop> { crop1, crop2 };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allCrops);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            CropId = cropId1,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        var firstItem = result.Data.Items.First();
        firstItem.CropId.Should().Be(cropId1);
        firstItem.TaskType.Should().Be(TaskType.Water);
        firstItem.Status.Should().Be(TaskStatus.Pending);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_FilterByUserId_ShouldReturnUserTasks()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var cropId1 = Guid.NewGuid();
        var cropId2 = Guid.NewGuid();
        var crop1 = new Crop { Id = cropId1, UserId = userId, Name = "番茄" };
        var crop2 = new Crop { Id = cropId2, UserId = otherUserId, Name = "黄瓜" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId1,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId2,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(2)
            }
        };
        var allCrops = new List<Crop> { crop1, crop2 };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allCrops);

        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(
            It.Is<System.Linq.Expressions.Expression<Func<Crop, bool>>>(e =>
                e.Compile().Invoke(new Crop { UserId = userId })),
            _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop1 });

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.First().CropId.Should().Be(cropId1);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_Pagination_ShouldReturnCorrectPage()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>();
        for (int i = 0; i < 15; i++)
        {
            tasks.Add(new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(-i)
            });
        }
        var allCrops = new List<Crop> { crop };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allCrops);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 2,
            PageSize = 5
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(5);
        result.Data.TotalCount.Should().Be(15);
        result.Data.PageNumber.Should().Be(2);
        result.Data.PageSize.Should().Be(5);
        result.Data.TotalPages.Should().Be(3);
        result.Data.HasPrevious.Should().BeTrue();
        result.Data.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetCropCareTasksAsync_SortByScheduledDateAsc_ShouldSortCorrectly()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(3)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Prune,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(2)
            }
        };
        var allCrops = new List<Crop> { crop };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allCrops);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            SortBy = "scheduleddate",
            SortOrder = "asc",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(3);
        var itemsList = result.Data.Items.ToList();
        itemsList[0].ScheduledDate.Should().BeBefore(itemsList[1].ScheduledDate);
        itemsList[1].ScheduledDate.Should().BeBefore(itemsList[2].ScheduledDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_DefaultSort_ShouldSortByScheduledDateDesc()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(3)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Prune,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(2)
            }
        };
        var allCrops = new List<Crop> { crop };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allCrops);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(3);
        var itemsList = result.Data.Items.ToList();
        itemsList[0].ScheduledDate.Should().BeAfter(itemsList[1].ScheduledDate);
        itemsList[1].ScheduledDate.Should().BeAfter(itemsList[2].ScheduledDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithOverdueTasks_ShouldSetOverdueInfo()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(-5).Date
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Completed,
                ScheduledDate = DateTime.UtcNow.AddDays(-3).Date
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Prune,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(2).Date
            }
        };
        var allCrops = new List<Crop> { crop };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allCrops);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        var itemsList = result.Data!.Items.ToList();

        var overdueTask = itemsList.First(t => t.Status == TaskStatus.Pending && t.ScheduledDate.Date < DateTime.UtcNow.Date);
        overdueTask.IsOverdue.Should().BeTrue();
        overdueTask.OverdueDays.Should().Be(5);

        var completedTask = itemsList.First(t => t.Status == TaskStatus.Completed);
        completedTask.IsOverdue.Should().BeFalse();
        completedTask.OverdueDays.Should().BeNull();

        var futureTask = itemsList.First(t => t.ScheduledDate.Date > DateTime.UtcNow.Date);
        futureTask.IsOverdue.Should().BeFalse();
        futureTask.OverdueDays.Should().BeNull();
    }

    [Fact]
    public async Task GetCropCareTasksAsync_EmptyResult_ShouldReturnEmptyList()
    {
        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync([]);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync([]);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
        result.Data.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_PageSizeZero_ShouldFallbackToDefaultPageSize()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            }
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync([crop]);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 1,
            PageSize = 0
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
        result.Data.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_PageNumberBeyondTotal_ShouldReturnEmptyItems()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            }
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync([crop]);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 999,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_SingleItem_ShouldReturnCorrectPagination()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            }
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync([crop]);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.TotalPages.Should().Be(1);
        result.Data.HasPrevious.Should().BeFalse();
        result.Data.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task GetCropCareTasksAsync_FilterNoMatch_ShouldReturnEmptyList()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            }
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync([crop]);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            TaskType = TaskType.Prune,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_SortByInvalidField_ShouldDefaultToScheduledDate()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(3)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            }
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync([crop]);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            SortBy = "nonexistentfield",
            SortOrder = "asc",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        var itemsList = result.Data.Items.ToList();
        itemsList[0].ScheduledDate.Should().BeBefore(itemsList[1].ScheduledDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_SortByWithDefaultSortOrder_ShouldSortDescending()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(3)
            }
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync([crop]);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            SortBy = "scheduleddate",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        var itemsList = result.Data!.Items.ToList();
        itemsList[0].ScheduledDate.Should().BeAfter(itemsList[1].ScheduledDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_SortByWithExplicitNullSortOrder_ShouldSortAscending()
    {
        var cropId = Guid.NewGuid();
        var crop = new Crop { Id = cropId, UserId = Guid.NewGuid(), Name = "番茄" };
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Water,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(3)
            },
            new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId,
                TaskType = TaskType.Fertilize,
                Status = TaskStatus.Pending,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            }
        };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);

        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync([crop]);

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var query = new CropCareTaskQueryRequestDto
        {
            SortBy = "scheduleddate",
            SortOrder = null,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await taskService.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        var itemsList = result.Data!.Items.ToList();
        itemsList[0].ScheduledDate.Should().BeBefore(itemsList[1].ScheduledDate);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_ScheduledDateExactlyYesterday_ShouldBeOverdueOneDay()
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
            ScheduledDate = DateTime.UtcNow.Date.AddDays(-1)
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
        result.Data.OverdueDays.Should().Be(1);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_LargeOverdueDays_ShouldCalculateCorrectly()
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
            ScheduledDate = DateTime.UtcNow.Date.AddDays(-365)
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
        result.Data.OverdueDays.Should().Be(365);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_ScheduledDateExactlyToday_ShouldNotBeOverdue()
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
            ScheduledDate = DateTime.UtcNow.Date
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
    public async Task UpdateTaskStatusAsync_FromCompletedToPending_ShouldClearCompletedDate()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var originalCompletedDate = DateTime.UtcNow.AddDays(-2);
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
            CompletedDate = originalCompletedDate,
            ScheduledDate = DateTime.UtcNow.AddDays(5)
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

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Pending);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_FromCancelledToInProgress_ShouldAllowWithoutValidation()
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
            Status = TaskStatus.Cancelled,
            CompletedDate = null,
            ScheduledDate = DateTime.UtcNow.AddDays(5)
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
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_FromCompletedToCancelled_ShouldClearCompletedDate()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var originalCompletedDate = DateTime.UtcNow.AddDays(-1);
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
            CompletedDate = originalCompletedDate,
            ScheduledDate = DateTime.UtcNow.AddDays(5)
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

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateTaskStatusAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Cancelled);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCropCareTaskAsync_FromCompletedToPendingViaUpdate_ShouldClearCompletedDate()
    {
        var userId = Guid.NewGuid();
        var cropId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var originalCompletedDate = DateTime.UtcNow.AddDays(-1);
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
            CompletedDate = originalCompletedDate,
            ScheduledDate = DateTime.UtcNow.AddDays(5)
        };

        var updateDto = new UpdateCropCareTaskRequestDto
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

        var taskService = new CropCareTaskService(_unitOfWorkMock.Object, _taskLoggerMock.Object);

        var result = await taskService.UpdateCropCareTaskAsync(taskId, updateDto, userId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Pending);
        result.Data.CompletedDate.Should().BeNull();
    }

    #endregion
}
