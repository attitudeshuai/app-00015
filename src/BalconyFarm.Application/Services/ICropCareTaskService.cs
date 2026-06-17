using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.Services;

public interface ICropCareTaskService
{
    Task<ApiResponse<PagedResult<CropCareTaskDto>>> GetCropCareTasksAsync(CropCareTaskQueryRequestDto query, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<CropCareTaskDto>> GetCropCareTaskByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<CropCareTaskDto>> CreateCropCareTaskAsync(CreateCropCareTaskRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CropCareTaskDto>> UpdateCropCareTaskAsync(Guid id, UpdateCropCareTaskRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteCropCareTaskAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CropCareTaskDto>> UpdateTaskStatusAsync(Guid id, UpdateTaskStatusRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
}
