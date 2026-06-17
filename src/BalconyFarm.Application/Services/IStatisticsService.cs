using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.Services;

public interface IStatisticsService
{
    Task<ApiResponse<OverviewStats>> GetOverviewStatsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<TrendData>>> GetTrendStatsAsync(DateTime startDate, DateTime endDate, Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<CropTaskCompletionItem>>> GetCropTaskCompletionStatsAsync(Guid userId, CancellationToken cancellationToken = default);
}
