using BalconyFarm.Application.Models;
using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Enums;
using BalconyFarm.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using TaskStatus = BalconyFarm.Domain.Enums.TaskStatus;

namespace BalconyFarm.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(IUnitOfWork unitOfWork, ILogger<StatisticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<OverviewStats>> GetOverviewStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("获取用户总览统计: {UserId}", userId);

        var crops = (await _unitOfWork.Crops.FindAsync(c => c.UserId == userId, cancellationToken)).ToList();
        var cropIds = crops.Select(c => c.Id).ToList();

        var tasks = (await _unitOfWork.CropCareTasks.GetAllAsync(cancellationToken))
            .Where(t => cropIds.Contains(t.CropId))
            .ToList();

        var harvestRecords = (await _unitOfWork.HarvestRecords.GetAllAsync(cancellationToken))
            .Where(h => cropIds.Contains(h.CropId))
            .ToList();

        var pestRecords = (await _unitOfWork.PestRecords.GetAllAsync(cancellationToken))
            .Where(p => cropIds.Contains(p.CropId))
            .ToList();

        var now = DateTime.UtcNow;
        var currentPeriod = GetMonthToDateRange(now);
        var lastMonthPeriod = GetSamePeriodLastMonth(now);
        var lastYearPeriod = GetSamePeriodLastYear(now);

        var currentStats = CalculatePeriodStats(crops, tasks, harvestRecords, currentPeriod.start, currentPeriod.end);
        var lastMonthStats = CalculatePeriodStats(crops, tasks, harvestRecords, lastMonthPeriod.start, lastMonthPeriod.end);
        var lastYearStats = CalculatePeriodStats(crops, tasks, harvestRecords, lastYearPeriod.start, lastYearPeriod.end);

        var stats = new OverviewStats
        {
            TotalCrops = crops.Count,
            GrowingCrops = crops.Count(c => c.Status == CropStatus.Growing),
            HarvestingCrops = crops.Count(c => c.Status == CropStatus.Harvesting),
            FinishedCrops = crops.Count(c => c.Status == CropStatus.Finished),
            TotalTasks = tasks.Count,
            PendingTasks = tasks.Count(t => t.Status == TaskStatus.Pending || t.Status == TaskStatus.InProgress),
            CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed),
            OverdueTaskCount = tasks.Count(t => (t.Status == TaskStatus.Pending || t.Status == TaskStatus.InProgress) && t.ScheduledDate.Date < DateTime.UtcNow.Date),
            TotalHarvestRecords = harvestRecords.Count,
            TotalHarvestQuantity = harvestRecords.Sum(h => h.Quantity),
            ActivePestIssues = pestRecords.Count(p => p.Status != PestStatus.Resolved),
            ComparedToLastMonth = BuildComparison(currentStats, lastMonthStats),
            ComparedToLastYear = BuildComparison(currentStats, lastYearStats)
        };

        return ApiResponse<OverviewStats>.Success(stats);
    }

    private static (DateTime start, DateTime end) GetMonthToDateRange(DateTime now)
    {
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = now.Date.AddDays(1).AddTicks(-1);
        return (start, end);
    }

    private static (DateTime start, DateTime end) GetSamePeriodLastMonth(DateTime now)
    {
        var lastMonth = now.AddMonths(-1);
        var start = new DateTime(lastMonth.Year, lastMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var day = Math.Min(now.Day, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
        var endDate = new DateTime(lastMonth.Year, lastMonth.Month, day, 0, 0, 0, DateTimeKind.Utc);
        var end = endDate.AddDays(1).AddTicks(-1);
        return (start, end);
    }

    private static (DateTime start, DateTime end) GetSamePeriodLastYear(DateTime now)
    {
        var lastYear = now.AddYears(-1);
        var start = new DateTime(lastYear.Year, lastYear.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var day = Math.Min(now.Day, DateTime.DaysInMonth(lastYear.Year, lastYear.Month));
        var endDate = new DateTime(lastYear.Year, lastYear.Month, day, 0, 0, 0, DateTimeKind.Utc);
        var end = endDate.AddDays(1).AddTicks(-1);
        return (start, end);
    }

    private class PeriodStats
    {
        public int NewCrops { get; set; }
        public decimal HarvestQuantity { get; set; }
        public decimal TaskCompletionRate { get; set; }
    }

    private static PeriodStats CalculatePeriodStats(List<Crop> crops, List<CropCareTask> tasks, List<HarvestRecord> harvestRecords, DateTime start, DateTime end)
    {
        var newCrops = crops.Count(c => c.CreatedAt >= start && c.CreatedAt <= end);
        var harvestQuantity = harvestRecords.Where(h => h.HarvestDate >= start && h.HarvestDate <= end).Sum(h => h.Quantity);

        var periodTasks = tasks.Where(t => t.ScheduledDate >= start && t.ScheduledDate <= end).ToList();
        var effectiveTasks = periodTasks.Where(t => t.Status != TaskStatus.Cancelled).ToList();
        var totalTasks = effectiveTasks.Count;
        var completedTasks = effectiveTasks.Count(t => t.Status == TaskStatus.Completed);
        var completionRate = totalTasks > 0 ? Math.Round((decimal)completedTasks / totalTasks * 100, 1) : 0m;

        return new PeriodStats
        {
            NewCrops = newCrops,
            HarvestQuantity = harvestQuantity,
            TaskCompletionRate = completionRate
        };
    }

    private static OverviewComparison BuildComparison(PeriodStats current, PeriodStats previous)
    {
        return new OverviewComparison
        {
            NewCrops = BuildIntComparison(current.NewCrops, previous.NewCrops),
            HarvestQuantity = BuildDecimalComparison(current.HarvestQuantity, previous.HarvestQuantity),
            TaskCompletionRate = BuildDecimalComparison(current.TaskCompletionRate, previous.TaskCompletionRate)
        };
    }

    private static PeriodComparison<int> BuildIntComparison(int current, int previous)
    {
        var change = current - previous;
        decimal? percentage = previous > 0 ? Math.Round((decimal)change / previous * 100, 1) : null;
        return new PeriodComparison<int>
        {
            CurrentValue = current,
            PreviousValue = previous,
            Change = change,
            ChangePercentage = percentage
        };
    }

    private static PeriodComparison<decimal> BuildDecimalComparison(decimal current, decimal previous)
    {
        var change = Math.Round(current - previous, 1);
        decimal? percentage = previous > 0 ? Math.Round(change / previous * 100, 1) : null;
        return new PeriodComparison<decimal>
        {
            CurrentValue = Math.Round(current, 1),
            PreviousValue = Math.Round(previous, 1),
            Change = change,
            ChangePercentage = percentage
        };
    }

    public async Task<ApiResponse<IEnumerable<TrendData>>> GetTrendStatsAsync(DateTime startDate, DateTime endDate, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("获取用户趋势统计: {UserId}, 开始日期: {StartDate}, 结束日期: {EndDate}", userId, startDate, endDate);

        if (startDate > endDate)
        {
            return ApiResponse<IEnumerable<TrendData>>.Error("开始日期不能大于结束日期", 400);
        }

        var cropIds = (await _unitOfWork.Crops.FindAsync(c => c.UserId == userId, cancellationToken))
            .Select(c => c.Id)
            .ToList();

        var crops = (await _unitOfWork.Crops.FindAsync(c => cropIds.Contains(c.Id), cancellationToken)).ToList();
        var tasks = (await _unitOfWork.CropCareTasks.GetAllAsync(cancellationToken))
            .Where(t => cropIds.Contains(t.CropId) && t.CompletedDate.HasValue)
            .ToList();
        var harvestRecords = (await _unitOfWork.HarvestRecords.GetAllAsync(cancellationToken))
            .Where(h => cropIds.Contains(h.CropId))
            .ToList();

        var dateSpan = endDate - startDate;
        var isWeekly = dateSpan.TotalDays > 31;

        var trendData = new List<TrendData>();

        if (isWeekly)
        {
            var startOfWeek = StartOfWeek(startDate, DayOfWeek.Monday);
            while (startOfWeek <= endDate)
            {
                var weekEnd = startOfWeek.AddDays(7).AddTicks(-1);

                var newCrops = crops.Count(c =>
                    c.CreatedAt.Date >= startOfWeek.Date && c.CreatedAt.Date <= weekEnd.Date);

                var completedTasks = tasks.Count(t =>
                    t.CompletedDate!.Value.Date >= startOfWeek.Date && t.CompletedDate!.Value.Date <= weekEnd.Date);

                var weekHarvests = harvestRecords.Where(h =>
                    h.HarvestDate.Date >= startOfWeek.Date && h.HarvestDate.Date <= weekEnd.Date).ToList();

                trendData.Add(new TrendData
                {
                    Period = $"{startOfWeek:yyyy-MM-dd} ~ {weekEnd:yyyy-MM-dd}",
                    NewCrops = newCrops,
                    CompletedTasks = completedTasks,
                    HarvestRecords = weekHarvests.Count,
                    HarvestQuantity = weekHarvests.Sum(h => h.Quantity)
                });

                startOfWeek = startOfWeek.AddDays(7);
            }
        }
        else
        {
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var newCrops = crops.Count(c => c.CreatedAt.Date == date);
                var completedTasks = tasks.Count(t => t.CompletedDate!.Value.Date == date);
                var dayHarvests = harvestRecords.Where(h => h.HarvestDate.Date == date).ToList();

                trendData.Add(new TrendData
                {
                    Period = date.ToString("yyyy-MM-dd"),
                    NewCrops = newCrops,
                    CompletedTasks = completedTasks,
                    HarvestRecords = dayHarvests.Count,
                    HarvestQuantity = dayHarvests.Sum(h => h.Quantity)
                });
            }
        }

        return ApiResponse<IEnumerable<TrendData>>.Success(trendData);
    }

    public async Task<ApiResponse<IEnumerable<CropTaskCompletionItem>>> GetCropTaskCompletionStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("获取作物任务完成率统计: {UserId}", userId);

        var crops = (await _unitOfWork.Crops.FindAsync(c => c.UserId == userId, cancellationToken)).ToList();
        if (!crops.Any())
        {
            return ApiResponse<IEnumerable<CropTaskCompletionItem>>.Success(Enumerable.Empty<CropTaskCompletionItem>());
        }

        var cropIds = crops.Select(c => c.Id).ToList();
        var allTasks = (await _unitOfWork.CropCareTasks.GetAllAsync(cancellationToken))
            .Where(t => cropIds.Contains(t.CropId))
            .ToList();

        var result = crops.Select(crop =>
        {
            var cropTasks = allTasks.Where(t => t.CropId == crop.Id).ToList();
            var effectiveTasks = cropTasks.Where(t => t.Status != TaskStatus.Cancelled).ToList();
            var completed = effectiveTasks.Count(t => t.Status == TaskStatus.Completed);
            var onTimeCompleted = effectiveTasks.Count(t =>
                t.Status == TaskStatus.Completed &&
                t.CompletedDate.HasValue &&
                t.CompletedDate.Value.Date <= t.ScheduledDate.Date);
            var overdue = effectiveTasks.Count(t =>
                (t.Status == TaskStatus.Pending || t.Status == TaskStatus.InProgress) &&
                t.ScheduledDate.Date < DateTime.UtcNow.Date);

            var total = effectiveTasks.Count;

            return new CropTaskCompletionItem
            {
                CropId = crop.Id,
                CropName = crop.Name,
                TotalTasks = total,
                CompletedTasks = completed,
                OnTimeCompletedTasks = onTimeCompleted,
                OverdueTasks = overdue,
                CompletionRate = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0,
                OnTimeRate = total > 0 ? Math.Round((decimal)onTimeCompleted / total * 100, 1) : 0
            };
        }).ToList();

        return ApiResponse<IEnumerable<CropTaskCompletionItem>>.Success(result);
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
