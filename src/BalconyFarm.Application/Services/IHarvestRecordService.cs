using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.Services;

public interface IHarvestRecordService
{
    Task<ApiResponse<PagedResult<HarvestRecordDto>>> GetHarvestRecordsAsync(HarvestRecordQueryRequestDto query, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<HarvestRecordDto>> GetHarvestRecordByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<HarvestRecordDto>> CreateHarvestRecordAsync(CreateHarvestRecordRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<HarvestRecordDto>> UpdateHarvestRecordAsync(Guid id, UpdateHarvestRecordRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteHarvestRecordAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
