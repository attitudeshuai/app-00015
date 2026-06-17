using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;
using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Enums;
using BalconyFarm.Domain.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;

namespace BalconyFarm.Application.Services;

public class CropService : ICropService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CropService> _logger;

    public CropService(IUnitOfWork unitOfWork, ILogger<CropService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<CropDto>>> GetCropsAsync(CropQueryRequestDto query, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var cropsList = (await _unitOfWork.Crops.GetAllAsync(cancellationToken)).ToList();

        IEnumerable<Crop> crops = cropsList;

        if (userId.HasValue)
        {
            crops = crops.Where(c => c.UserId == userId.Value);
        }

        if (query.Status.HasValue)
        {
            crops = crops.Where(c => c.Status == query.Status.Value);
        }

        if (!string.IsNullOrEmpty(query.Location))
        {
            crops = crops.Where(c => c.Location.Contains(query.Location));
        }

        if (!string.IsNullOrEmpty(query.ContainerType))
        {
            crops = crops.Where(c => c.ContainerType.Contains(query.ContainerType));
        }

        if (query.PlantingDateFrom.HasValue)
        {
            crops = crops.Where(c => c.PlantingDate >= query.PlantingDateFrom.Value);
        }

        if (query.PlantingDateTo.HasValue)
        {
            crops = crops.Where(c => c.PlantingDate <= query.PlantingDateTo.Value);
        }

        if (!string.IsNullOrEmpty(query.SearchKeyword))
        {
            crops = crops.Where(c =>
                c.Name.Contains(query.SearchKeyword) ||
                c.Variety.Contains(query.SearchKeyword));
        }

        var totalCount = crops.Count();
        var sortFunc = GetSortProperty(query.SortBy ?? "createdat").Compile();

        if (!string.IsNullOrEmpty(query.SortBy))
        {
            crops = query.SortOrder?.ToLower() == "desc"
                ? crops.OrderByDescending(sortFunc)
                : crops.OrderBy(sortFunc);
        }
        else
        {
            crops = crops.OrderByDescending(c => c.CreatedAt);
        }

        var items = crops
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Adapt<IEnumerable<CropDto>>()
            .ToList();

        var result = new PagedResult<CropDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };

        return ApiResponse<PagedResult<CropDto>>.Success(result);
    }

    public async Task<ApiResponse<CropDto>> GetCropByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var crop = await _unitOfWork.Crops.GetByIdAsync(id, cancellationToken);
        if (crop == null)
        {
            return ApiResponse<CropDto>.Error("作物不存在", 404);
        }

        if (userId.HasValue && crop.UserId != userId.Value)
        {
            return ApiResponse<CropDto>.Error("无权访问此作物", 403);
        }

        var cropDto = crop.Adapt<CropDto>();
        return ApiResponse<CropDto>.Success(cropDto);
    }

    public async Task<ApiResponse<CropDto>> CreateCropAsync(CreateCropRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("创建作物: {Name}, 用户: {UserId}", dto.Name, userId);

        var crop = dto.Adapt<Crop>();
        crop.Id = Guid.NewGuid();
        crop.UserId = userId;
        crop.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.Crops.AddAsync(crop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("作物创建成功: {CropId}", crop.Id);

        var cropDto = crop.Adapt<CropDto>();
        return ApiResponse<CropDto>.Success(cropDto, "创建成功");
    }

    public async Task<ApiResponse<CropDto>> UpdateCropAsync(Guid id, UpdateCropRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("更新作物: {CropId}, 用户: {UserId}", id, userId);

        var crop = await _unitOfWork.Crops.GetByIdAsync(id, cancellationToken);
        if (crop == null)
        {
            return ApiResponse<CropDto>.Error("作物不存在", 404);
        }

        if (crop.UserId != userId)
        {
            return ApiResponse<CropDto>.Error("无权修改此作物", 403);
        }

        if (!string.IsNullOrEmpty(dto.Name))
            crop.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Variety))
            crop.Variety = dto.Variety;
        if (dto.PlantingDate.HasValue)
            crop.PlantingDate = dto.PlantingDate.Value;
        if (!string.IsNullOrEmpty(dto.Location))
            crop.Location = dto.Location;
        if (!string.IsNullOrEmpty(dto.ContainerType))
            crop.ContainerType = dto.ContainerType;
        if (dto.Status.HasValue)
            crop.Status = dto.Status.Value;
        if (dto.PhotoUrl != null)
            crop.PhotoUrl = dto.PhotoUrl;

        await _unitOfWork.Crops.UpdateAsync(crop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("作物更新成功: {CropId}", id);

        var cropDto = crop.Adapt<CropDto>();
        return ApiResponse<CropDto>.Success(cropDto, "更新成功");
    }

    public async Task<ApiResponse> DeleteCropAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("删除作物: {CropId}, 用户: {UserId}", id, userId);

        var crop = await _unitOfWork.Crops.GetByIdAsync(id, cancellationToken);
        if (crop == null)
        {
            return ApiResponse.Error("作物不存在", 404);
        }

        if (crop.UserId != userId)
        {
            return ApiResponse.Error("无权删除此作物", 403);
        }

        await _unitOfWork.Crops.DeleteAsync(crop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("作物删除成功: {CropId}", id);

        return ApiResponse.Success(null, "删除成功");
    }

    public async Task<ApiResponse<CropDto>> UpdateCropStatusAsync(Guid id, UpdateCropStatusRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("更新作物状态: {CropId}, 状态: {Status}, 用户: {UserId}", id, dto.Status, userId);

        var crop = await _unitOfWork.Crops.GetByIdAsync(id, cancellationToken);
        if (crop == null)
        {
            return ApiResponse<CropDto>.Error("作物不存在", 404);
        }

        if (crop.UserId != userId)
        {
            return ApiResponse<CropDto>.Error("无权修改此作物", 403);
        }

        crop.Status = dto.Status;

        await _unitOfWork.Crops.UpdateAsync(crop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("作物状态更新成功: {CropId}", id);

        var cropDto = crop.Adapt<CropDto>();
        return ApiResponse<CropDto>.Success(cropDto, "状态更新成功");
    }

    public async Task<ApiResponse<PagedResult<CropDto>>> GetMyCropsAsync(CropQueryRequestDto query, Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetCropsAsync(query, userId, cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<Crop, object>> GetSortProperty(string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "name" => crop => crop.Name,
            "plantingdate" => crop => crop.PlantingDate,
            "status" => crop => crop.Status,
            "createdat" => crop => crop.CreatedAt,
            _ => crop => crop.CreatedAt
        };
    }
}
