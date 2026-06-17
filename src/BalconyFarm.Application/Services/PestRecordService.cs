using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;
using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Enums;
using BalconyFarm.Domain.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;

namespace BalconyFarm.Application.Services;

public class PestRecordService : IPestRecordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PestRecordService> _logger;

    public PestRecordService(IUnitOfWork unitOfWork, ILogger<PestRecordService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<PestRecordDto>>> GetPestRecordsAsync(PestRecordQueryRequestDto query, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var pestRecords = (await _unitOfWork.PestRecords.GetAllAsync(cancellationToken)).ToList();
        var crops = (await _unitOfWork.Crops.GetAllAsync(cancellationToken)).ToList();

        IEnumerable<PestRecord> pestRecordsQuery = pestRecords;

        if (userId.HasValue)
        {
            var userCropIds = crops.Where(c => c.UserId == userId.Value).Select(c => c.Id).ToList();
            pestRecordsQuery = pestRecordsQuery.Where(p => userCropIds.Contains(p.CropId));
        }

        if (query.CropId.HasValue)
        {
            pestRecordsQuery = pestRecordsQuery.Where(p => p.CropId == query.CropId.Value);
        }

        if (!string.IsNullOrEmpty(query.IssueType))
        {
            pestRecordsQuery = pestRecordsQuery.Where(p => p.IssueType.Contains(query.IssueType));
        }

        if (query.Status.HasValue)
        {
            pestRecordsQuery = pestRecordsQuery.Where(p => p.Status == query.Status.Value);
        }

        if (query.DetectedDateFrom.HasValue)
        {
            pestRecordsQuery = pestRecordsQuery.Where(p => p.DetectedDate >= query.DetectedDateFrom.Value);
        }

        if (query.DetectedDateTo.HasValue)
        {
            pestRecordsQuery = pestRecordsQuery.Where(p => p.DetectedDate <= query.DetectedDateTo.Value);
        }

        if (!string.IsNullOrEmpty(query.SearchKeyword))
        {
            pestRecordsQuery = pestRecordsQuery.Where(p =>
                p.IssueType.Contains(query.SearchKeyword) ||
                p.Symptoms.Contains(query.SearchKeyword) ||
                p.Treatment.Contains(query.SearchKeyword));
        }

        var totalCount = pestRecordsQuery.Count();
        var sortFunc = GetSortProperty(query.SortBy ?? "detecteddate").Compile();

        if (!string.IsNullOrEmpty(query.SortBy))
        {
            pestRecordsQuery = query.SortOrder?.ToLower() == "desc"
                ? pestRecordsQuery.OrderByDescending(sortFunc)
                : pestRecordsQuery.OrderBy(sortFunc);
        }
        else
        {
            pestRecordsQuery = pestRecordsQuery.OrderByDescending(p => p.DetectedDate);
        }

        var allTreatmentLogs = (await _unitOfWork.TreatmentLogs.GetAllAsync(cancellationToken)).ToList();

        var items = pestRecordsQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p =>
            {
                var dto = p.Adapt<PestRecordDto>();
                var crop = crops.FirstOrDefault(c => c.Id == p.CropId);
                dto.CropName = crop?.Name;
                dto.TreatmentLogs = allTreatmentLogs
                    .Where(t => t.PestRecordId == p.Id)
                    .OrderByDescending(t => t.TreatmentDate)
                    .Select(t => t.Adapt<TreatmentLogDto>())
                    .ToList();
                return dto;
            })
            .ToList();

        var result = new PagedResult<PestRecordDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };

        return ApiResponse<PagedResult<PestRecordDto>>.Success(result);
    }

    public async Task<ApiResponse<PestRecordDto>> GetPestRecordByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var pestRecord = (await _unitOfWork.PestRecords.GetAllAsync(cancellationToken))
            .FirstOrDefault(p => p.Id == id);

        if (pestRecord == null)
        {
            return ApiResponse<PestRecordDto>.Error("病虫害记录不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(pestRecord.CropId, cancellationToken);

        if (userId.HasValue && crop != null && crop.UserId != userId.Value)
        {
            return ApiResponse<PestRecordDto>.Error("无权访问此病虫害记录", 403);
        }

        var treatmentLogs = (await _unitOfWork.TreatmentLogs.GetAllAsync(cancellationToken))
            .Where(t => t.PestRecordId == id)
            .OrderByDescending(t => t.TreatmentDate)
            .ToList();

        var pestRecordDto = pestRecord.Adapt<PestRecordDto>();
        pestRecordDto.CropName = crop?.Name;
        pestRecordDto.TreatmentLogs = treatmentLogs.Select(t => t.Adapt<TreatmentLogDto>()).ToList();
        return ApiResponse<PestRecordDto>.Success(pestRecordDto);
    }

    public async Task<ApiResponse<PestRecordDto>> CreatePestRecordAsync(CreatePestRecordRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("创建病虫害记录: {CropId}, 用户: {UserId}", dto.CropId, userId);

        var crop = await _unitOfWork.Crops.GetByIdAsync(dto.CropId, cancellationToken);
        if (crop == null)
        {
            return ApiResponse<PestRecordDto>.Error("作物不存在", 404);
        }

        if (crop.UserId != userId)
        {
            return ApiResponse<PestRecordDto>.Error("无权为此作物创建病虫害记录", 403);
        }

        var pestRecord = dto.Adapt<PestRecord>();
        pestRecord.Id = Guid.NewGuid();

        await _unitOfWork.PestRecords.AddAsync(pestRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("病虫害记录创建成功: {PestRecordId}", pestRecord.Id);

        var pestRecordDto = pestRecord.Adapt<PestRecordDto>();
        pestRecordDto.CropName = crop.Name;
        return ApiResponse<PestRecordDto>.Success(pestRecordDto, "创建成功");
    }

    public async Task<ApiResponse<PestRecordDto>> UpdatePestRecordAsync(Guid id, UpdatePestRecordRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("更新病虫害记录: {PestRecordId}, 用户: {UserId}", id, userId);

        var pestRecord = (await _unitOfWork.PestRecords.GetAllAsync(cancellationToken))
            .FirstOrDefault(p => p.Id == id);

        if (pestRecord == null)
        {
            return ApiResponse<PestRecordDto>.Error("病虫害记录不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(pestRecord.CropId, cancellationToken);
        if (crop != null && crop.UserId != userId)
        {
            return ApiResponse<PestRecordDto>.Error("无权修改此病虫害记录", 403);
        }

        if (!string.IsNullOrEmpty(dto.IssueType))
            pestRecord.IssueType = dto.IssueType;
        if (!string.IsNullOrEmpty(dto.Symptoms))
            pestRecord.Symptoms = dto.Symptoms;
        if (!string.IsNullOrEmpty(dto.Treatment))
            pestRecord.Treatment = dto.Treatment;
        if (dto.DetectedDate.HasValue)
            pestRecord.DetectedDate = dto.DetectedDate.Value;
        if (dto.ResolvedDate.HasValue)
            pestRecord.ResolvedDate = dto.ResolvedDate.Value;
        if (dto.Status.HasValue)
            pestRecord.Status = dto.Status.Value;

        if (pestRecord.Status == PestStatus.Resolved && !pestRecord.ResolvedDate.HasValue)
        {
            pestRecord.ResolvedDate = DateTime.UtcNow;
        }

        await _unitOfWork.PestRecords.UpdateAsync(pestRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("病虫害记录更新成功: {PestRecordId}", id);

        var pestRecordDto = pestRecord.Adapt<PestRecordDto>();
        pestRecordDto.CropName = crop?.Name;
        return ApiResponse<PestRecordDto>.Success(pestRecordDto, "更新成功");
    }

    public async Task<ApiResponse> DeletePestRecordAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("删除病虫害记录: {PestRecordId}, 用户: {UserId}", id, userId);

        var pestRecord = (await _unitOfWork.PestRecords.GetAllAsync(cancellationToken))
            .FirstOrDefault(p => p.Id == id);

        if (pestRecord == null)
        {
            return ApiResponse.Error("病虫害记录不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(pestRecord.CropId, cancellationToken);
        if (crop != null && crop.UserId != userId)
        {
            return ApiResponse.Error("无权删除此病虫害记录", 403);
        }

        await _unitOfWork.PestRecords.DeleteAsync(pestRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("病虫害记录删除成功: {PestRecordId}", id);

        return ApiResponse.Success(null, "删除成功");
    }

    public async Task<ApiResponse<PestRecordDto>> UpdatePestStatusAsync(Guid id, UpdatePestStatusRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("更新病虫害状态: {PestRecordId}, 状态: {Status}, 用户: {UserId}", id, dto.Status, userId);

        var pestRecord = (await _unitOfWork.PestRecords.GetAllAsync(cancellationToken))
            .FirstOrDefault(p => p.Id == id);

        if (pestRecord == null)
        {
            return ApiResponse<PestRecordDto>.Error("病虫害记录不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(pestRecord.CropId, cancellationToken);
        if (crop != null && crop.UserId != userId)
        {
            return ApiResponse<PestRecordDto>.Error("无权修改此病虫害记录", 403);
        }

        pestRecord.Status = dto.Status;

        if (dto.Status == PestStatus.Resolved)
        {
            pestRecord.ResolvedDate = DateTime.UtcNow;
        }

        await _unitOfWork.PestRecords.UpdateAsync(pestRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("病虫害状态更新成功: {PestRecordId}", id);

        var pestRecordDto = pestRecord.Adapt<PestRecordDto>();
        pestRecordDto.CropName = crop?.Name;
        return ApiResponse<PestRecordDto>.Success(pestRecordDto, "状态更新成功");
    }

    public async Task<ApiResponse<PestRecordDto>> AddTreatmentLogAsync(Guid pestRecordId, CreateTreatmentLogRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("追加治疗记录: {PestRecordId}, 用户: {UserId}", pestRecordId, userId);

        var pestRecord = (await _unitOfWork.PestRecords.GetAllAsync(cancellationToken))
            .FirstOrDefault(p => p.Id == pestRecordId);

        if (pestRecord == null)
        {
            return ApiResponse<PestRecordDto>.Error("病虫害记录不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(pestRecord.CropId, cancellationToken);
        if (crop != null && crop.UserId != userId)
        {
            return ApiResponse<PestRecordDto>.Error("无权修改此病虫害记录", 403);
        }

        if (pestRecord.Status == PestStatus.Resolved)
        {
            return ApiResponse<PestRecordDto>.Error("病虫害已解决，无法追加治疗记录", 400);
        }

        var treatmentLog = dto.Adapt<TreatmentLog>();
        treatmentLog.Id = Guid.NewGuid();
        treatmentLog.PestRecordId = pestRecordId;

        await _unitOfWork.TreatmentLogs.AddAsync(treatmentLog, cancellationToken);

        if (pestRecord.Status == PestStatus.Detected)
        {
            pestRecord.Status = PestStatus.Treating;
            await _unitOfWork.PestRecords.UpdateAsync(pestRecord, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("治疗记录追加成功: {TreatmentLogId}, 病虫害: {PestRecordId}", treatmentLog.Id, pestRecordId);

        return await GetPestRecordByIdAsync(pestRecordId, userId, cancellationToken);
    }

    public async Task<ApiResponse<IEnumerable<TreatmentLogDto>>> GetTreatmentLogsAsync(Guid pestRecordId, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var pestRecord = (await _unitOfWork.PestRecords.GetAllAsync(cancellationToken))
            .FirstOrDefault(p => p.Id == pestRecordId);

        if (pestRecord == null)
        {
            return ApiResponse<IEnumerable<TreatmentLogDto>>.Error("病虫害记录不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(pestRecord.CropId, cancellationToken);
        if (userId.HasValue && crop != null && crop.UserId != userId.Value)
        {
            return ApiResponse<IEnumerable<TreatmentLogDto>>.Error("无权访问此病虫害记录", 403);
        }

        var treatmentLogs = (await _unitOfWork.TreatmentLogs.GetAllAsync(cancellationToken))
            .Where(t => t.PestRecordId == pestRecordId)
            .OrderByDescending(t => t.TreatmentDate)
            .Select(t => t.Adapt<TreatmentLogDto>())
            .ToList();

        return ApiResponse<IEnumerable<TreatmentLogDto>>.Success(treatmentLogs);
    }

    public async Task<ApiResponse> DeleteTreatmentLogAsync(Guid pestRecordId, Guid treatmentLogId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("删除治疗记录: {TreatmentLogId}, 病虫害: {PestRecordId}, 用户: {UserId}", treatmentLogId, pestRecordId, userId);

        var pestRecord = (await _unitOfWork.PestRecords.GetAllAsync(cancellationToken))
            .FirstOrDefault(p => p.Id == pestRecordId);

        if (pestRecord == null)
        {
            return ApiResponse.Error("病虫害记录不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(pestRecord.CropId, cancellationToken);
        if (crop != null && crop.UserId != userId)
        {
            return ApiResponse.Error("无权修改此病虫害记录", 403);
        }

        var treatmentLog = (await _unitOfWork.TreatmentLogs.GetAllAsync(cancellationToken))
            .FirstOrDefault(t => t.Id == treatmentLogId && t.PestRecordId == pestRecordId);

        if (treatmentLog == null)
        {
            return ApiResponse.Error("治疗记录不存在", 404);
        }

        await _unitOfWork.TreatmentLogs.DeleteAsync(treatmentLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("治疗记录删除成功: {TreatmentLogId}", treatmentLogId);

        return ApiResponse.Success(null, "删除成功");
    }

    private static System.Linq.Expressions.Expression<Func<PestRecord, object>> GetSortProperty(string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "issuetype" => p => p.IssueType,
            "detecteddate" => p => p.DetectedDate,
            "resolveddate" => p => p.ResolvedDate ?? DateTime.MinValue,
            "status" => p => p.Status,
            _ => p => p.DetectedDate
        };
    }
}
