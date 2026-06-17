using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.Services;

public interface IPlantingCalendarService
{
    Task<ApiResponse<List<CityDto>>> GetAvailableCitiesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<PlantingCalendarResponseDto>> GetRecommendationsAsync(GetRecommendationsRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CropDto>> CreateCropFromRecommendationAsync(CreateCropFromRecommendationRequestDto request, Guid userId, CancellationToken cancellationToken = default);
}
