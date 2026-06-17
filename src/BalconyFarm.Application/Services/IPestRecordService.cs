using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.Services;

public interface IPestRecordService
{
    Task<ApiResponse<PagedResult<PestRecordDto>>> GetPestRecordsAsync(PestRecordQueryRequestDto query, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<PestRecordDto>> GetPestRecordByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<PestRecordDto>> CreatePestRecordAsync(CreatePestRecordRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PestRecordDto>> UpdatePestRecordAsync(Guid id, UpdatePestRecordRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeletePestRecordAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PestRecordDto>> UpdatePestStatusAsync(Guid id, UpdatePestStatusRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PestRecordDto>> AddTreatmentLogAsync(Guid pestRecordId, CreateTreatmentLogRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<TreatmentLogDto>>> GetTreatmentLogsAsync(Guid pestRecordId, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteTreatmentLogAsync(Guid pestRecordId, Guid treatmentLogId, Guid userId, CancellationToken cancellationToken = default);
}
