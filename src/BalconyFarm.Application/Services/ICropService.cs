using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.Services;

public interface ICropService
{
    Task<ApiResponse<PagedResult<CropDto>>> GetCropsAsync(CropQueryRequestDto query, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<CropDto>> GetCropByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<CropDto>> CreateCropAsync(CreateCropRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CropDto>> UpdateCropAsync(Guid id, UpdateCropRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteCropAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CropDto>> UpdateCropStatusAsync(Guid id, UpdateCropStatusRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<CropDto>>> GetMyCropsAsync(CropQueryRequestDto query, Guid userId, CancellationToken cancellationToken = default);
}
