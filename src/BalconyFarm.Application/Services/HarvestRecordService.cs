using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;
using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Enums;
using BalconyFarm.Domain.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;

namespace BalconyFarm.Application.Services;

public class HarvestRecordService : IHarvestRecordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HarvestRecordService> _logger;

    public HarvestRecordService(IUnitOfWork unitOfWork, ILogger<HarvestRecordService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<HarvestRecordDto>>> GetHarvestRecordsAsync(HarvestRecordQueryRequestDto query, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var harvestRecords = (await _unitOfWork.HarvestRecords.GetAllAsync(cancellationToken)).ToList();
        var crops = (await _unitOfWork.Crops.GetAllAsync(cancellationToken)).ToList();

        IEnumerable<HarvestRecord> harvestRecordsQuery = harvestRecords;

        if (userId.HasValue)
        {
            var userCropIds = crops.Where(c => c.UserId == userId.Value).Select(c => c.Id).ToList();
            harvestRecordsQuery = harvestRecordsQuery.Where(h => userCropIds.Contains(h.CropId));
        }

        if (query.CropId.HasValue)
        {
            harvestRecordsQuery = harvestRecordsQuery.Where(h => h.CropId == query.CropId.Value);
        }

        if (!string.IsNullOrEmpty(query.Unit))
        {
            harvestRecordsQuery = harvestRecordsQuery.Where(h => h.Unit.Contains(query.Unit));
        }

        if (query.HarvestDateFrom.HasValue)
        {
            harvestRecordsQuery = harvestRecordsQuery.Where(h => h.HarvestDate >= query.HarvestDateFrom.Value);
        }

        if (query.HarvestDateTo.HasValue)
        {
            harvestRecordsQuery = harvestRecordsQuery.Where(h => h.HarvestDate <= query.HarvestDateTo.Value);
        }

        var totalCount = harvestRecordsQuery.Count();
        var sortFunc = GetSortProperty(query.SortBy ?? "harvestdate").Compile();

        if (!string.IsNullOrEmpty(query.SortBy))
        {
            harvestRecordsQuery = query.SortOrder?.ToLower() == "desc"
                ? harvestRecordsQuery.OrderByDescending(sortFunc)
                : harvestRecordsQuery.OrderBy(sortFunc);
        }
        else
        {
            harvestRecordsQuery = harvestRecordsQuery.OrderByDescending(h => h.HarvestDate);
        }

        var items = harvestRecordsQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(h =>
            {
                var dto = h.Adapt<HarvestRecordDto>();
                var crop = crops.FirstOrDefault(c => c.Id == h.CropId);
                dto.CropName = crop?.Name;
                return dto;
            })
            .ToList();

        var result = new PagedResult<HarvestRecordDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };

        return ApiResponse<PagedResult<HarvestRecordDto>>.Success(result);
    }

    public async Task<ApiResponse<HarvestRecordDto>> GetHarvestRecordByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var harvestRecord = (await _unitOfWork.HarvestRecords.GetAllAsync(cancellationToken))
            .FirstOrDefault(h => h.Id == id);

        if (harvestRecord == null)
        {
            return ApiResponse<HarvestRecordDto>.Error("收获记录不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(harvestRecord.CropId, cancellationToken);

        if (userId.HasValue && crop != null && crop.UserId != userId.Value)
        {
            return ApiResponse<HarvestRecordDto>.Error("无权访问此收获记录", 403);
        }

        var harvestRecordDto = harvestRecord.Adapt<HarvestRecordDto>();
        harvestRecordDto.CropName = crop?.Name;
        return ApiResponse<HarvestRecordDto>.Success(harvestRecordDto);
    }

    public async Task<ApiResponse<HarvestRecordDto>> CreateHarvestRecordAsync(CreateHarvestRecordRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("创建收获记录: 作物ID: {CropId}, 用户: {UserId}", dto.CropId, userId);

        var crop = await _unitOfWork.Crops.GetByIdAsync(dto.CropId, cancellationToken);
        if (crop == null)
        {
            return ApiResponse<HarvestRecordDto>.Error("作物不存在", 404);
        }

        if (crop.UserId != userId)
        {
            return ApiResponse<HarvestRecordDto>.Error("无权为此作物创建收获记录", 403);
        }

        if (crop.Status == CropStatus.Finished)
        {
            return ApiResponse<HarvestRecordDto>.Error("已结束的作物无法创建收获记录", 400);
        }

        var harvestRecord = dto.Adapt<HarvestRecord>();
        harvestRecord.Id = Guid.NewGuid();

        await _unitOfWork.HarvestRecords.AddAsync(harvestRecord, cancellationToken);

        if (dto.IsFinalHarvest)
        {
            crop.Status = CropStatus.Finished;
            await _unitOfWork.Crops.UpdateAsync(crop, cancellationToken);
            _logger.LogInformation("作物状态自动更新为已结束: CropId={CropId}", crop.Id);
        }
        else if (crop.Status == CropStatus.Growing)
        {
            crop.Status = CropStatus.Harvesting;
            await _unitOfWork.Crops.UpdateAsync(crop, cancellationToken);
            _logger.LogInformation("作物状态自动更新为可收获: CropId={CropId}", crop.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("收获记录创建成功: {HarvestRecordId}", harvestRecord.Id);

        var harvestRecordDto = harvestRecord.Adapt<HarvestRecordDto>();
        harvestRecordDto.CropName = crop.Name;
        return ApiResponse<HarvestRecordDto>.Success(harvestRecordDto, "创建成功");
    }

    public async Task<ApiResponse<HarvestRecordDto>> UpdateHarvestRecordAsync(Guid id, UpdateHarvestRecordRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("更新收获记录: {HarvestRecordId}, 用户: {UserId}", id, userId);

        var harvestRecord = (await _unitOfWork.HarvestRecords.GetAllAsync(cancellationToken))
            .FirstOrDefault(h => h.Id == id);

        if (harvestRecord == null)
        {
            return ApiResponse<HarvestRecordDto>.Error("收获记录不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(harvestRecord.CropId, cancellationToken);
        if (crop != null && crop.UserId != userId)
        {
            return ApiResponse<HarvestRecordDto>.Error("无权修改此收获记录", 403);
        }

        if (dto.HarvestDate.HasValue)
            harvestRecord.HarvestDate = dto.HarvestDate.Value;
        if (dto.Quantity.HasValue)
            harvestRecord.Quantity = dto.Quantity.Value;
        if (!string.IsNullOrEmpty(dto.Unit))
            harvestRecord.Unit = dto.Unit;
        if (dto.QualityNote != null)
            harvestRecord.QualityNote = dto.QualityNote;
        if (dto.PhotoUrl != null)
            harvestRecord.PhotoUrl = dto.PhotoUrl;

        await _unitOfWork.HarvestRecords.UpdateAsync(harvestRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("收获记录更新成功: {HarvestRecordId}", id);

        var harvestRecordDto = harvestRecord.Adapt<HarvestRecordDto>();
        harvestRecordDto.CropName = crop?.Name;
        return ApiResponse<HarvestRecordDto>.Success(harvestRecordDto, "更新成功");
    }

    public async Task<ApiResponse> DeleteHarvestRecordAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("删除收获记录: {HarvestRecordId}, 用户: {UserId}", id, userId);

        var harvestRecord = (await _unitOfWork.HarvestRecords.GetAllAsync(cancellationToken))
            .FirstOrDefault(h => h.Id == id);

        if (harvestRecord == null)
        {
            return ApiResponse.Error("收获记录不存在", 404);
        }

        var crop = await _unitOfWork.Crops.GetByIdAsync(harvestRecord.CropId, cancellationToken);
        if (crop != null && crop.UserId != userId)
        {
            return ApiResponse.Error("无权删除此收获记录", 403);
        }

        await _unitOfWork.HarvestRecords.DeleteAsync(harvestRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("收获记录删除成功: {HarvestRecordId}", id);

        return ApiResponse.Success(null, "删除成功");
    }

    private static System.Linq.Expressions.Expression<Func<HarvestRecord, object>> GetSortProperty(string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "harvestdate" => record => record.HarvestDate,
            "quantity" => record => record.Quantity,
            "unit" => record => record.Unit,
            "cropid" => record => record.CropId,
            _ => record => record.HarvestDate
        };
    }
}
