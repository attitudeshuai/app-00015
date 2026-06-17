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
using TaskStatus = BalconyFarm.Domain.Enums.TaskStatus;
using CropStatus = BalconyFarm.Domain.Enums.CropStatus;

namespace BalconyFarm.Tests;

public class CropCareTaskServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CropCareTaskService>> _loggerMock;
    private readonly CancellationToken _cancellationToken;
    private readonly Guid _testUserId;
    private readonly Guid _testCropId;

    public CropCareTaskServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CropCareTaskService>>();
        _cancellationToken = CancellationToken.None;
        _testUserId = Guid.NewGuid();
        _testCropId = Guid.NewGuid();
    }

    private CropCareTaskService CreateService()
    {
        return new CropCareTaskService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    private Crop CreateTestCrop(Guid? cropId = null, Guid? userId = null, string name = "番茄", CropStatus status = CropStatus.Growing)
    {
        return new Crop
        {
            Id = cropId ?? _testCropId,
            UserId = userId ?? _testUserId,
            Name = name,
            Status = status
        };
    }

    private CropCareTask CreateTestTask(Guid? taskId = null, Guid? cropId = null,
        TaskType taskType = TaskType.Water, TaskStatus status = TaskStatus.Pending,
        DateTime? scheduledDate = null, DateTime? completedDate = null, string? note = null)
    {
        return new CropCareTask
        {
            Id = taskId ?? Guid.NewGuid(),
            CropId = cropId ?? _testCropId,
            TaskType = taskType,
            ScheduledDate = scheduledDate ?? DateTime.UtcNow.AddDays(1),
            Status = status,
            CompletedDate = completedDate,
            Note = note
        };
    }

    private List<CropCareTask> CreateTestTasksList(int count, Guid? cropId = null)
    {
        var tasks = new List<CropCareTask>();
        for (int i = 0; i < count; i++)
        {
            tasks.Add(new CropCareTask
            {
                Id = Guid.NewGuid(),
                CropId = cropId ?? _testCropId,
                TaskType = i % 2 == 0 ? TaskType.Water : TaskType.Fertilize,
                ScheduledDate = DateTime.UtcNow.AddDays(i + 1),
                Status = i % 3 == 0 ? TaskStatus.Pending : i % 3 == 1 ? TaskStatus.InProgress : TaskStatus.Completed,
                CompletedDate = i % 3 == 2 ? DateTime.UtcNow.AddDays(i) : null
            });
        }
        return tasks;
    }

    #region 任务状态变更测试

    [Fact]
    public async Task UpdateTaskStatusAsync_FromPendingToInProgress_ShouldSucceed()
    {
        var taskId = Guid.NewGuid();
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Pending);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var service = CreateService();
        var result = await service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequestDto { Status = TaskStatus.InProgress }, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.InProgress);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_FromPendingToCancelled_ShouldSucceed()
    {
        var taskId = Guid.NewGuid();
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Pending);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var service = CreateService();
        var result = await service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequestDto { Status = TaskStatus.Cancelled }, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Cancelled);
        result.Data.CompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ToCompleted_ShouldSetCompletedDate()
    {
        var taskId = Guid.NewGuid();
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.InProgress);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var service = CreateService();
        var result = await service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequestDto { Status = TaskStatus.Completed }, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TaskStatus.Completed);
        result.Data.CompletedDate.Should().NotBeNull();
        result.Data.CompletedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_AllTasksCompleted_ShouldUpdateCropStatusToFinished()
    {
        var task1Id = Guid.NewGuid();
        var task2Id = Guid.NewGuid();
        var task1 = CreateTestTask(taskId: task1Id, status: TaskStatus.Completed, completedDate: DateTime.UtcNow.AddDays(-1));
        var task2 = CreateTestTask(taskId: task2Id, status: TaskStatus.InProgress);
        var crop = CreateTestCrop(status: CropStatus.Growing);

        var allTasks = new List<CropCareTask> { task1, task2 };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(task2Id, _cancellationToken))
            .ReturnsAsync(task2);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.Crops.UpdateAsync(It.IsAny<Crop>(), _cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken))
            .ReturnsAsync(allTasks);

        var service = CreateService();
        var result = await service.UpdateTaskStatusAsync(task2Id,
            new UpdateTaskStatusRequestDto { Status = TaskStatus.Completed }, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);

        _unitOfWorkMock.Verify(u => u.Crops.UpdateAsync(It.Is<Crop>(c =>
            c.Status == CropStatus.Finished), _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_NotAllTasksCompleted_ShouldNotUpdateCropStatus()
    {
        var task1Id = Guid.NewGuid();
        var task2Id = Guid.NewGuid();
        var task1 = CreateTestTask(taskId: task1Id, status: TaskStatus.Pending);
        var task2 = CreateTestTask(taskId: task2Id, status: TaskStatus.Pending);
        var crop = CreateTestCrop(status: CropStatus.Growing);

        var allTasks = new List<CropCareTask> { task1, task2 };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(task1Id, _cancellationToken))
            .ReturnsAsync(task1);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken))
            .ReturnsAsync(allTasks);

        var service = CreateService();
        var result = await service.UpdateTaskStatusAsync(task1Id,
            new UpdateTaskStatusRequestDto { Status = TaskStatus.InProgress }, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);

        _unitOfWorkMock.Verify(u => u.Crops.UpdateAsync(It.IsAny<Crop>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_TaskNotFound_ShouldReturn404()
    {
        var taskId = Guid.NewGuid();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync((CropCareTask?)null);

        var service = CreateService();
        var result = await service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequestDto { Status = TaskStatus.Completed }, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("任务不存在");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_UserNotOwner_ShouldReturn403()
    {
        var taskId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var task = CreateTestTask(taskId: taskId);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);

        var service = CreateService();
        var result = await service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequestDto { Status = TaskStatus.Completed }, otherUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权修改此任务");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCropCareTaskAsync_StatusChange_ShouldTriggerCropStatusCheck()
    {
        var taskId = Guid.NewGuid();
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Pending);
        var crop = CreateTestCrop(status: CropStatus.Growing);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var service = CreateService();
        var result = await service.UpdateCropCareTaskAsync(taskId,
            new UpdateCropCareTaskRequestDto { Status = TaskStatus.Completed }, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);

        _unitOfWorkMock.Verify(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateCropCareTaskAsync_NoStatusChange_ShouldNotTriggerCropStatusCheck()
    {
        var taskId = Guid.NewGuid();
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Pending);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var service = CreateService();
        var result = await service.UpdateCropCareTaskAsync(taskId,
            new UpdateCropCareTaskRequestDto { Note = "更新备注" }, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);

        _unitOfWorkMock.Verify(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken), Times.Never);
    }

    [Fact]
    public async Task DeleteCropCareTaskAsync_ShouldTriggerCropStatusCheck()
    {
        var taskId = Guid.NewGuid();
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Pending);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.DeleteAsync(task, _cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken))
            .ReturnsAsync(new List<CropCareTask>());

        var service = CreateService();
        var result = await service.DeleteCropCareTaskAsync(taskId, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);

        _unitOfWorkMock.Verify(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken), Times.Once);
    }

    #endregion

    #region 逾期提醒测试

    [Fact]
    public async Task GetCropCareTaskByIdAsync_PendingTaskWithPastDate_ShouldBeOverdue()
    {
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(-5).Date;
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Pending, scheduledDate: scheduledDate);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);

        var service = CreateService();
        var result = await service.GetCropCareTaskByIdAsync(taskId, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeTrue();
        result.Data.OverdueDays.Should().Be(5);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_InProgressTaskWithPastDate_ShouldBeOverdue()
    {
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(-3).Date;
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.InProgress, scheduledDate: scheduledDate);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);

        var service = CreateService();
        var result = await service.GetCropCareTaskByIdAsync(taskId, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeTrue();
        result.Data.OverdueDays.Should().Be(3);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_CompletedTaskWithPastDate_ShouldNotBeOverdue()
    {
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(-5).Date;
        var completedDate = DateTime.UtcNow.AddDays(-2);
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Completed, scheduledDate: scheduledDate, completedDate: completedDate);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);

        var service = CreateService();
        var result = await service.GetCropCareTaskByIdAsync(taskId, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeFalse();
        result.Data.OverdueDays.Should().BeNull();
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_CancelledTaskWithPastDate_ShouldNotBeOverdue()
    {
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(-5).Date;
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Cancelled, scheduledDate: scheduledDate);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);

        var service = CreateService();
        var result = await service.GetCropCareTaskByIdAsync(taskId, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeFalse();
        result.Data.OverdueDays.Should().BeNull();
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_TaskWithTodayDate_ShouldNotBeOverdue()
    {
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.Date;
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Pending, scheduledDate: scheduledDate);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);

        var service = CreateService();
        var result = await service.GetCropCareTaskByIdAsync(taskId, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeFalse();
        result.Data.OverdueDays.Should().BeNull();
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_TaskWithFutureDate_ShouldNotBeOverdue()
    {
        var taskId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(3).Date;
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Pending, scheduledDate: scheduledDate);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);

        var service = CreateService();
        var result = await service.GetCropCareTaskByIdAsync(taskId, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeFalse();
        result.Data.OverdueDays.Should().BeNull();
    }

    [Fact]
    public async Task GetCropCareTasksAsync_ShouldSetOverdueInfoCorrectly()
    {
        var tasks = new List<CropCareTask>
        {
            CreateTestTask(taskId: Guid.NewGuid(), status: TaskStatus.Pending, scheduledDate: DateTime.UtcNow.AddDays(-2).Date),
            CreateTestTask(taskId: Guid.NewGuid(), status: TaskStatus.InProgress, scheduledDate: DateTime.UtcNow.AddDays(-1).Date),
            CreateTestTask(taskId: Guid.NewGuid(), status: TaskStatus.Completed, scheduledDate: DateTime.UtcNow.AddDays(-5).Date),
            CreateTestTask(taskId: Guid.NewGuid(), status: TaskStatus.Pending, scheduledDate: DateTime.UtcNow.AddDays(2).Date),
        };
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(tasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { PageNumber = 1, PageSize = 10, SortBy = "scheduleddate", SortOrder = "asc" };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(4);

        var items = result.Data.Items.ToList();

        var pendingOverdueTask = items.First(t => t.Status == TaskStatus.Pending && t.IsOverdue);
        pendingOverdueTask.OverdueDays.Should().Be(2);

        var inProgressOverdueTask = items.First(t => t.Status == TaskStatus.InProgress && t.IsOverdue);
        inProgressOverdueTask.OverdueDays.Should().Be(1);

        var completedTask = items.First(t => t.Status == TaskStatus.Completed);
        completedTask.IsOverdue.Should().BeFalse();
        completedTask.OverdueDays.Should().BeNull();

        var futureTask = items.First(t => t.ScheduledDate > DateTime.UtcNow);
        futureTask.IsOverdue.Should().BeFalse();
        futureTask.OverdueDays.Should().BeNull();
    }

    [Fact]
    public async Task CreateCropCareTaskAsync_WithPastDate_ShouldSetOverdue()
    {
        var crop = CreateTestCrop();
        var scheduledDate = DateTime.UtcNow.AddDays(-3).Date;

        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.AddAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .ReturnsAsync((CropCareTask t, CancellationToken ct) => t);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);

        var service = CreateService();
        var createDto = new CreateCropCareTaskRequestDto
        {
            CropId = _testCropId,
            TaskType = TaskType.Water,
            ScheduledDate = scheduledDate,
            Note = "浇水"
        };
        var result = await service.CreateCropCareTaskAsync(createDto, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeTrue();
        result.Data.OverdueDays.Should().Be(3);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ToCompleted_ShouldClearOverdueStatus()
    {
        var taskId = Guid.NewGuid();
        var task = CreateTestTask(taskId: taskId, status: TaskStatus.Pending, scheduledDate: DateTime.UtcNow.AddDays(-5).Date);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.UpdateAsync(It.IsAny<CropCareTask>(), _cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cancellationToken))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.CropCareTasks.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CropCareTask, bool>>>(), _cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });

        var service = CreateService();
        var result = await service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequestDto { Status = TaskStatus.Completed }, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.IsOverdue.Should().BeFalse();
        result.Data.OverdueDays.Should().BeNull();
    }

    #endregion

    #region 按条件筛选测试

    [Fact]
    public async Task GetCropCareTasksAsync_WithCropIdFilter_ShouldFilterCorrectly()
    {
        var crop1Id = Guid.NewGuid();
        var crop2Id = Guid.NewGuid();
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), cropId: crop1Id);
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), cropId: crop2Id);
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), cropId: crop1Id);
        var allTasks = new List<CropCareTask> { task1, task2, task3 };
        var crop1 = CreateTestCrop(cropId: crop1Id);
        var crop2 = CreateTestCrop(cropId: crop2Id);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop1, crop2 });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { CropId = crop1Id, PageNumber = 1, PageSize = 10 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.Should().OnlyContain(t => t.CropId == crop1Id);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithTaskTypeFilter_ShouldFilterCorrectly()
    {
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), taskType: TaskType.Water);
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), taskType: TaskType.Fertilize);
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), taskType: TaskType.Water);
        var task4 = CreateTestTask(taskId: Guid.NewGuid(), taskType: TaskType.Prune);
        var allTasks = new List<CropCareTask> { task1, task2, task3, task4 };
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { TaskType = TaskType.Water, PageNumber = 1, PageSize = 10 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.Should().OnlyContain(t => t.TaskType == TaskType.Water);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithStatusFilter_ShouldFilterCorrectly()
    {
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), status: TaskStatus.Pending);
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), status: TaskStatus.InProgress);
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), status: TaskStatus.Completed);
        var task4 = CreateTestTask(taskId: Guid.NewGuid(), status: TaskStatus.Pending);
        var allTasks = new List<CropCareTask> { task1, task2, task3, task4 };
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { Status = TaskStatus.Pending, PageNumber = 1, PageSize = 10 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.Should().OnlyContain(t => t.Status == TaskStatus.Pending);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithScheduledDateFromFilter_ShouldFilterCorrectly()
    {
        var baseDate = new DateTime(2024, 6, 15);
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: baseDate.AddDays(-2));
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: baseDate);
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: baseDate.AddDays(2));
        var allTasks = new List<CropCareTask> { task1, task2, task3 };
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { ScheduledDateFrom = baseDate, PageNumber = 1, PageSize = 10 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.Should().OnlyContain(t => t.ScheduledDate >= baseDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithScheduledDateToFilter_ShouldFilterCorrectly()
    {
        var baseDate = new DateTime(2024, 6, 15);
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: baseDate.AddDays(-2));
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: baseDate);
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: baseDate.AddDays(2));
        var allTasks = new List<CropCareTask> { task1, task2, task3 };
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { ScheduledDateTo = baseDate, PageNumber = 1, PageSize = 10 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.Should().OnlyContain(t => t.ScheduledDate <= baseDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithDateRangeFilter_ShouldFilterCorrectly()
    {
        var fromDate = new DateTime(2024, 6, 10);
        var toDate = new DateTime(2024, 6, 20);
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: new DateTime(2024, 6, 5));
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: new DateTime(2024, 6, 15));
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: new DateTime(2024, 6, 25));
        var allTasks = new List<CropCareTask> { task1, task2, task3 };
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto
        {
            ScheduledDateFrom = fromDate,
            ScheduledDateTo = toDate,
            PageNumber = 1,
            PageSize = 10
        };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(1);
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items.First().ScheduledDate.Should().Be(new DateTime(2024, 6, 15));
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithMultipleFilters_ShouldFilterCorrectly()
    {
        var crop1Id = Guid.NewGuid();
        var crop2Id = Guid.NewGuid();
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), cropId: crop1Id, taskType: TaskType.Water, status: TaskStatus.Pending);
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), cropId: crop1Id, taskType: TaskType.Fertilize, status: TaskStatus.Pending);
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), cropId: crop2Id, taskType: TaskType.Water, status: TaskStatus.Completed);
        var task4 = CreateTestTask(taskId: Guid.NewGuid(), cropId: crop1Id, taskType: TaskType.Water, status: TaskStatus.InProgress);
        var allTasks = new List<CropCareTask> { task1, task2, task3, task4 };
        var crop1 = CreateTestCrop(cropId: crop1Id);
        var crop2 = CreateTestCrop(cropId: crop2Id);

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop1, crop2 });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto
        {
            CropId = crop1Id,
            TaskType = TaskType.Water,
            Status = TaskStatus.Pending,
            PageNumber = 1,
            PageSize = 10
        };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(1);
        result.Data.Items.Should().HaveCount(1);
        var item = result.Data.Items.First();
        item.CropId.Should().Be(crop1Id);
        item.TaskType.Should().Be(TaskType.Water);
        item.Status.Should().Be(TaskStatus.Pending);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithPaging_ShouldReturnCorrectPage()
    {
        var allTasks = CreateTestTasksList(15);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { PageNumber = 2, PageSize = 5, SortOrder = "asc" };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

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
    public async Task GetCropCareTasksAsync_FirstPage_ShouldNotHavePrevious()
    {
        var allTasks = CreateTestTasksList(8);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { PageNumber = 1, PageSize = 5 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Data!.HasPrevious.Should().BeFalse();
        result.Data.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetCropCareTasksAsync_LastPage_ShouldNotHaveNext()
    {
        var allTasks = CreateTestTasksList(8);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { PageNumber = 2, PageSize = 5 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Data!.HasPrevious.Should().BeTrue();
        result.Data.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithSortByScheduledDateAsc_ShouldSortCorrectly()
    {
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: DateTime.UtcNow.AddDays(3));
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: DateTime.UtcNow.AddDays(1));
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: DateTime.UtcNow.AddDays(2));
        var allTasks = new List<CropCareTask> { task1, task2, task3 };
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { SortBy = "scheduleddate", SortOrder = "asc", PageNumber = 1, PageSize = 10 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();

        var items = result.Data!.Items.ToList();
        items.Should().HaveCount(3);
        items[0].ScheduledDate.Should().BeBefore(items[1].ScheduledDate);
        items[1].ScheduledDate.Should().BeBefore(items[2].ScheduledDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithSortByScheduledDateDesc_ShouldSortCorrectly()
    {
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: DateTime.UtcNow.AddDays(1));
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: DateTime.UtcNow.AddDays(3));
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: DateTime.UtcNow.AddDays(2));
        var allTasks = new List<CropCareTask> { task1, task2, task3 };
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { SortBy = "scheduleddate", SortOrder = "desc", PageNumber = 1, PageSize = 10 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();

        var items = result.Data!.Items.ToList();
        items.Should().HaveCount(3);
        items[0].ScheduledDate.Should().BeAfter(items[1].ScheduledDate);
        items[1].ScheduledDate.Should().BeAfter(items[2].ScheduledDate);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithUserIdFilter_ShouldFilterCorrectly()
    {
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var crop1Id = Guid.NewGuid();
        var crop2Id = Guid.NewGuid();
        var crop1 = CreateTestCrop(cropId: crop1Id, userId: user1Id, name: "番茄");
        var crop2 = CreateTestCrop(cropId: crop2Id, userId: user2Id, name: "黄瓜");
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), cropId: crop1Id);
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), cropId: crop2Id);
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), cropId: crop1Id);
        var allTasks = new List<CropCareTask> { task1, task2, task3 };

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop1, crop2 });
        _unitOfWorkMock.Setup(u => u.Crops.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Crop, bool>>>(), _cancellationToken))
            .ReturnsAsync(new List<Crop> { crop1 });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { PageNumber = 1, PageSize = 10 };
        var result = await service.GetCropCareTasksAsync(query, user1Id, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCropCareTasksAsync_WithCropName_ShouldSetCropName()
    {
        var task = CreateTestTask(taskId: Guid.NewGuid());
        var crop = CreateTestCrop(name: "小番茄");

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<CropCareTask> { task });
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { PageNumber = 1, PageSize = 10 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().CropName.Should().Be("小番茄");
    }

    [Fact]
    public async Task GetCropCareTasksAsync_DefaultSort_ShouldBeScheduledDateDesc()
    {
        var task1 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: DateTime.UtcNow.AddDays(1));
        var task2 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: DateTime.UtcNow.AddDays(3));
        var task3 = CreateTestTask(taskId: Guid.NewGuid(), scheduledDate: DateTime.UtcNow.AddDays(2));
        var allTasks = new List<CropCareTask> { task1, task2, task3 };
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetAllAsync(_cancellationToken))
            .ReturnsAsync(allTasks);
        _unitOfWorkMock.Setup(u => u.Crops.GetAllAsync(_cancellationToken))
            .ReturnsAsync(new List<Crop> { crop });

        var service = CreateService();
        var query = new CropCareTaskQueryRequestDto { PageNumber = 1, PageSize = 10 };
        var result = await service.GetCropCareTasksAsync(query, null, _cancellationToken);

        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();

        var items = result.Data!.Items.ToList();
        items[0].ScheduledDate.Should().BeAfter(items[1].ScheduledDate);
        items[1].ScheduledDate.Should().BeAfter(items[2].ScheduledDate);
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_TaskNotFound_ShouldReturn404()
    {
        var taskId = Guid.NewGuid();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync((CropCareTask?)null);

        var service = CreateService();
        var result = await service.GetCropCareTaskByIdAsync(taskId, _testUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("任务不存在");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetCropCareTaskByIdAsync_UserNotOwner_ShouldReturn403()
    {
        var taskId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var task = CreateTestTask(taskId: taskId);
        var crop = CreateTestCrop();

        _unitOfWorkMock.Setup(u => u.CropCareTasks.GetByIdAsync(taskId, _cancellationToken))
            .ReturnsAsync(task);
        _unitOfWorkMock.Setup(u => u.Crops.GetByIdAsync(_testCropId, _cancellationToken))
            .ReturnsAsync(crop);

        var service = CreateService();
        var result = await service.GetCropCareTaskByIdAsync(taskId, otherUserId, _cancellationToken);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权访问此任务");
        result.Data.Should().BeNull();
    }

    #endregion
}
