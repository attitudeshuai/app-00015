using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;
using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Enums;
using BalconyFarm.Domain.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;
using TaskStatus = BalconyFarm.Domain.Enums.TaskStatus;
using CropStatus = BalconyFarm.Domain.Enums.CropStatus;

namespace BalconyFarm.Application.Services;

public class CropCareTaskService : ICropCareTaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CropCareTaskService> _logger;

    public CropCareTaskService(IUnitOfWork unitOfWork, ILogger<CropCareTaskService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<CropCareTaskDto>>> GetCropCareTasksAsync(CropCareTaskQueryRequestDto query, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var tasksList = (await _unitOfWork.CropCareTasks.GetAllAsync(cancellationToken)).ToList();

        IEnumerable<CropCareTask> tasksQuery = tasksList;

        if (userId.HasValue)
        {
            var userCropIds = (await _unitOfWork.Crops.FindAsync(c => c.UserId == userId.Value, cancellationToken))
                .Select(c => c.Id)
                .ToList();
            tasksQuery = tasksQuery.Where(t => userCropIds.Contains(t.CropId));
        }

        if (query.CropId.HasValue)
        {
            tasksQuery = tasksQuery.Where(t => t.CropId == query.CropId.Value);
        }

        if (query.TaskType.HasValue)
        {
            tasksQuery = tasksQuery.Where(t => t.TaskType == query.TaskType.Value);
        }

        if (query.Status.HasValue)
        {
            tasksQuery = tasksQuery.Where(t => t.Status == query.Status.Value);
        }

        if (query.ScheduledDateFrom.HasValue)
        {
            tasksQuery = tasksQuery.Where(t => t.ScheduledDate >= query.ScheduledDateFrom.Value);
        }

        if (query.ScheduledDateTo.HasValue)
        {
            tasksQuery = tasksQuery.Where(t => t.ScheduledDate <= query.ScheduledDateTo.Value);
        }

        var totalCount = tasksQuery.Count();
        var sortFunc = GetSortProperty(query.SortBy ?? "scheduleddate").Compile();

        if (!string.IsNullOrEmpty(query.SortBy))
        {
            tasksQuery = query.SortOrder?.ToLower() == "desc"
                ? tasksQuery.OrderByDescending(sortFunc)
                : tasksQuery.OrderBy(sortFunc);
        }
        else
        {
            tasksQuery = tasksQuery.OrderByDescending(t => t.ScheduledDate);
        }

        var crops = (await _unitOfWork.Crops.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id, c => c.Name);

        var items = tasksQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Adapt<IEnumerable<CropCareTaskDto>>()
            .Select(t =>
            {
                if (crops.TryGetValue(t.CropId, out var cropName))
                {
                    t.CropName = cropName;
                }
                SetOverdueInfo(t);
                return t;
            })
            .ToList();

        var result = new PagedResult<CropCareTaskDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };

        return ApiResponse<PagedResult<CropCareTaskDto>>.Success(result);
    }

    public async Task<ApiResponse<CropCareTaskDto>> GetCropCareTaskByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.CropCareTasks.GetByIdAsync(id, cancellationToken);
        if (task == null)
        {
            return ApiResponse<CropCareTaskDto>.Error("任务不存在", 404);
        }

        if (userId.HasValue)
        {
            var crop = await _unitOfWork.Crops.GetByIdAsync(task.CropId, cancellationToken);
            if (crop == null || crop.UserId != userId.Value)
            {
                return ApiResponse<CropCareTaskDto>.Error("无权访问此任务", 403);
            }
        }

        var taskDto = task.Adapt<CropCareTaskDto>();
        var taskCrop = await _unitOfWork.Crops.GetByIdAsync(task.CropId, cancellationToken);
        if (taskCrop != null)
        {
            taskDto.CropName = taskCrop.Name;
        }
        SetOverdueInfo(taskDto);

        return ApiResponse<CropCareTaskDto>.Success(taskDto);
    }

    public async Task<ApiResponse<CropCareTaskDto>> CreateCropCareTaskAsync(CreateCropCareTaskRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("创建作物护理任务: CropId={CropId}, TaskType={TaskType}, 用户: {UserId}", dto.CropId, dto.TaskType, userId);

        var crop = await _unitOfWork.Crops.GetByIdAsync(dto.CropId, cancellationToken);
        if (crop == null)
        {
            return ApiResponse<CropCareTaskDto>.Error("作物不存在", 404);
        }

        if (crop.UserId != userId)
        {
            return ApiResponse<CropCareTaskDto>.Error("无权为此作物创建任务", 403);
        }

        var task = dto.Adapt<CropCareTask>();
        task.Id = Guid.NewGuid();
        task.Status = TaskStatus.Pending;

        await _unitOfWork.CropCareTasks.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("作物护理任务创建成功: TaskId={TaskId}", task.Id);

        var taskDto = task.Adapt<CropCareTaskDto>();
        taskDto.CropName = crop.Name;
        SetOverdueInfo(taskDto);
        return ApiResponse<CropCareTaskDto>.Success(taskDto, "创建成功");
    }

    public async Task<ApiResponse<CropCareTaskDto>> UpdateCropCareTaskAsync(Guid id, UpdateCropCareTaskRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("更新作物护理任务: TaskId={TaskId}, 用户: {UserId}", id, userId);

        var task = await _unitOfWork.CropCareTasks.GetByIdAsync(id, cancellationToken);
        if (task == null)
        {
            return ApiResponse<CropCareTaskDto>.Error("任务不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(task.CropId, cancellationToken);
        if (crop == null || crop.UserId != userId)
        {
            return ApiResponse<CropCareTaskDto>.Error("无权修改此任务", 403);
        }

        if (dto.TaskType.HasValue)
            task.TaskType = dto.TaskType.Value;
        if (dto.ScheduledDate.HasValue)
            task.ScheduledDate = dto.ScheduledDate.Value;
        if (dto.Status.HasValue)
        {
            task.Status = dto.Status.Value;
            if (dto.Status.Value == Domain.Enums.TaskStatus.Completed)
            {
                task.CompletedDate = DateTime.UtcNow;
            }
        }
        if (dto.Note != null)
            task.Note = dto.Note;

        await _unitOfWork.CropCareTasks.UpdateAsync(task, cancellationToken);

        if (dto.Status.HasValue)
        {
            await CheckAndUpdateCropFinishedStatusAsync(task.CropId, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("作物护理任务更新成功: TaskId={TaskId}", id);

        var taskDto = task.Adapt<CropCareTaskDto>();
        taskDto.CropName = crop.Name;
        SetOverdueInfo(taskDto);
        return ApiResponse<CropCareTaskDto>.Success(taskDto, "更新成功");
    }

    public async Task<ApiResponse> DeleteCropCareTaskAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("删除作物护理任务: TaskId={TaskId}, 用户: {UserId}", id, userId);

        var task = await _unitOfWork.CropCareTasks.GetByIdAsync(id, cancellationToken);
        if (task == null)
        {
            return ApiResponse.Error("任务不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(task.CropId, cancellationToken);
        if (crop == null || crop.UserId != userId)
        {
            return ApiResponse.Error("无权删除此任务", 403);
        }

        var cropId = task.CropId;
        await _unitOfWork.CropCareTasks.DeleteAsync(task, cancellationToken);

        await CheckAndUpdateCropFinishedStatusAsync(cropId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("作物护理任务删除成功: TaskId={TaskId}", id);

        return ApiResponse.Success(null, "删除成功");
    }

    public async Task<ApiResponse<CropCareTaskDto>> UpdateTaskStatusAsync(Guid id, UpdateTaskStatusRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("更新任务状态: TaskId={TaskId}, 状态: {Status}, 用户: {UserId}", id, dto.Status, userId);

        var task = await _unitOfWork.CropCareTasks.GetByIdAsync(id, cancellationToken);
        if (task == null)
        {
            return ApiResponse<CropCareTaskDto>.Error("任务不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(task.CropId, cancellationToken);
        if (crop == null || crop.UserId != userId)
        {
            return ApiResponse<CropCareTaskDto>.Error("无权修改此任务", 403);
        }

        task.Status = dto.Status;
        if (dto.Status == TaskStatus.Completed)
        {
            task.CompletedDate = DateTime.UtcNow;
        }

        await _unitOfWork.CropCareTasks.UpdateAsync(task, cancellationToken);

        await CheckAndUpdateCropFinishedStatusAsync(task.CropId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("任务状态更新成功: TaskId={TaskId}", id);

        var taskDto = task.Adapt<CropCareTaskDto>();
        taskDto.CropName = crop.Name;
        SetOverdueInfo(taskDto);
        return ApiResponse<CropCareTaskDto>.Success(taskDto, "状态更新成功");
    }

    private async Task CheckAndUpdateCropFinishedStatusAsync(Guid cropId, CancellationToken cancellationToken)
    {
        var allTasks = await _unitOfWork.CropCareTasks.FindAsync(t => t.CropId == cropId, cancellationToken);
        var taskList = allTasks.ToList();

        if (taskList.Count == 0)
        {
            return;
        }

        var allTasksCompleted = taskList.All(t => t.Status == TaskStatus.Completed || t.Status == TaskStatus.Cancelled);

        if (allTasksCompleted)
        {
            var crop = await _unitOfWork.Crops.GetByIdAsync(cropId, cancellationToken);
            if (crop != null && crop.Status != CropStatus.Finished)
            {
                crop.Status = CropStatus.Finished;
                await _unitOfWork.Crops.UpdateAsync(crop, cancellationToken);
                _logger.LogInformation("所有任务已完成，作物状态自动更新为已完成: CropId={CropId}", cropId);
            }
        }
    }

    private static System.Linq.Expressions.Expression<Func<CropCareTask, object>> GetSortProperty(string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "tasktype" => task => task.TaskType,
            "scheduleddate" => task => task.ScheduledDate,
            "completeddate" => task => task.CompletedDate!,
            "status" => task => task.Status,
            _ => task => task.ScheduledDate
        };
    }

    private static void SetOverdueInfo(CropCareTaskDto dto)
    {
        var isPending = dto.Status == TaskStatus.Pending || dto.Status == TaskStatus.InProgress;
        if (isPending && dto.ScheduledDate.Date < DateTime.UtcNow.Date)
        {
            dto.IsOverdue = true;
            dto.OverdueDays = (int)(DateTime.UtcNow.Date - dto.ScheduledDate.Date).TotalDays;
        }
        else
        {
            dto.IsOverdue = false;
            dto.OverdueDays = null;
        }
    }
}
