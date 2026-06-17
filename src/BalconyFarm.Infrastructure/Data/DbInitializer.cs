using BalconyFarm.Application.Interfaces;
using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Enums;
using BalconyFarm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskStatus = BalconyFarm.Domain.Enums.TaskStatus;

namespace BalconyFarm.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHashService = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();

        await context.Database.EnsureCreatedAsync(cancellationToken);

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            await SeedUsersAsync(context, passwordHashService, cancellationToken);
        }

        if (!await context.Crops.AnyAsync(cancellationToken))
        {
            await SeedCropsAsync(context, cancellationToken);
        }

        if (!await context.CropCareTasks.AnyAsync(cancellationToken))
        {
            await SeedCropCareTasksAsync(context, cancellationToken);
        }

        if (!await context.HarvestRecords.AnyAsync(cancellationToken))
        {
            await SeedHarvestRecordsAsync(context, cancellationToken);
        }

        if (!await context.PestRecords.AnyAsync(cancellationToken))
        {
            await SeedPestRecordsAsync(context, cancellationToken);
        }
    }

    private static async Task SeedUsersAsync(AppDbContext context, IPasswordHashService passwordHashService, CancellationToken cancellationToken)
    {
        var users = new List<User>
        {
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "gardener1",
                Email = "gardener1@example.com",
                PasswordHash = passwordHashService.HashPassword("password123"),
                Avatar = "https://example.com/avatars/gardener1.png",
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UpdatedAt = DateTime.UtcNow.AddMonths(-3)
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Username = "plantlover",
                Email = "plantlover@example.com",
                PasswordHash = passwordHashService.HashPassword("password123"),
                Avatar = "https://example.com/avatars/plantlover.png",
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UpdatedAt = DateTime.UtcNow.AddMonths(-2)
            },
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Username = "urbanfarmer",
                Email = "urbanfarmer@example.com",
                PasswordHash = passwordHashService.HashPassword("password123"),
                Avatar = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UpdatedAt = DateTime.UtcNow.AddMonths(-1)
            }
        };

        await context.Users.AddRangeAsync(users, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedCropsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var crops = new List<Crop>
        {
            new Crop
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "小番茄",
                Variety = "千禧樱桃番茄",
                PlantingDate = DateTime.UtcNow.AddDays(-60),
                Location = "南阳台",
                ContainerType = "塑料花盆",
                Status = CropStatus.Growing,
                PhotoUrl = "https://example.com/crops/tomato1.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new Crop
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "罗勒",
                Variety = "甜罗勒",
                PlantingDate = DateTime.UtcNow.AddDays(-45),
                Location = "东阳台",
                ContainerType = "陶瓷花盆",
                Status = CropStatus.Harvesting,
                PhotoUrl = "https://example.com/crops/basil1.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-45)
            },
            new Crop
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "薄荷",
                Variety = "留兰香薄荷",
                PlantingDate = DateTime.UtcNow.AddDays(-90),
                Location = "北阳台",
                ContainerType = "悬挂花盆",
                Status = CropStatus.Finished,
                PhotoUrl = "https://example.com/crops/mint1.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-90)
            },
            new Crop
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "生菜",
                Variety = "奶油生菜",
                PlantingDate = DateTime.UtcNow.AddDays(-30),
                Location = "南阳台",
                ContainerType = "种植箱",
                Status = CropStatus.Growing,
                PhotoUrl = "https://example.com/crops/lettuce1.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Crop
            {
                Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "辣椒",
                Variety = "小米辣",
                PlantingDate = DateTime.UtcNow.AddDays(-75),
                Location = "西阳台",
                ContainerType = "塑料花盆",
                Status = CropStatus.Harvesting,
                PhotoUrl = "https://example.com/crops/chili1.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-75)
            }
        };

        await context.Crops.AddRangeAsync(crops, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedCropCareTasksAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var tasks = new List<CropCareTask>
        {
            new CropCareTask
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                CropId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                TaskType = TaskType.Water,
                ScheduledDate = DateTime.UtcNow.AddDays(-2),
                CompletedDate = DateTime.UtcNow.AddDays(-2),
                Status = Domain.Enums.TaskStatus.Completed,
                Note = "浇水500ml"
            },
            new CropCareTask
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                CropId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                TaskType = TaskType.Fertilize,
                ScheduledDate = DateTime.UtcNow.AddDays(-7),
                CompletedDate = DateTime.UtcNow.AddDays(-7),
                Status = Domain.Enums.TaskStatus.Completed,
                Note = "施用有机液肥"
            },
            new CropCareTask
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                CropId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                TaskType = TaskType.Water,
                ScheduledDate = DateTime.UtcNow.AddDays(1),
                Status = Domain.Enums.TaskStatus.Pending,
                Note = "浇水500ml"
            },
            new CropCareTask
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                CropId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                TaskType = TaskType.Prune,
                ScheduledDate = DateTime.UtcNow.AddDays(3),
                Status = Domain.Enums.TaskStatus.Pending,
                Note = "修剪侧枝，促进主枝生长"
            },
            new CropCareTask
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                CropId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                TaskType = TaskType.Water,
                ScheduledDate = DateTime.UtcNow,
                Status = Domain.Enums.TaskStatus.InProgress,
                Note = "浇水300ml"
            }
        };

        await context.CropCareTasks.AddRangeAsync(tasks, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedHarvestRecordsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var records = new List<HarvestRecord>
        {
            new HarvestRecord
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                CropId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                HarvestDate = DateTime.UtcNow.AddDays(-5),
                Quantity = 0.15m,
                Unit = "kg",
                QualityNote = "叶片新鲜，香气浓郁",
                PhotoUrl = "https://example.com/harvest/basil_harvest1.jpg"
            },
            new HarvestRecord
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                CropId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                HarvestDate = DateTime.UtcNow.AddDays(-12),
                Quantity = 0.1m,
                Unit = "kg",
                QualityNote = "第一次采收，品质很好",
                PhotoUrl = "https://example.com/harvest/basil_harvest2.jpg"
            },
            new HarvestRecord
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                CropId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                HarvestDate = DateTime.UtcNow.AddDays(-3),
                Quantity = 0.08m,
                Unit = "kg",
                QualityNote = "辣味十足，约50个辣椒",
                PhotoUrl = "https://example.com/harvest/chili_harvest1.jpg"
            },
            new HarvestRecord
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000004"),
                CropId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                HarvestDate = DateTime.UtcNow.AddDays(-20),
                Quantity = 0.25m,
                Unit = "kg",
                QualityNote = "薄荷香气浓郁，共采收3次",
                PhotoUrl = "https://example.com/harvest/mint_harvest1.jpg"
            }
        };

        await context.HarvestRecords.AddRangeAsync(records, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPestRecordsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var records = new List<PestRecord>
        {
            new PestRecord
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                CropId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                IssueType = "蚜虫",
                Symptoms = "叶片背面有绿色小虫子，叶片卷曲",
                Treatment = "喷洒肥皂水稀释液，每天一次，连续3天",
                DetectedDate = DateTime.UtcNow.AddDays(-10),
                ResolvedDate = DateTime.UtcNow.AddDays(-5),
                Status = PestStatus.Resolved
            },
            new PestRecord
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                CropId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                IssueType = "蜗牛",
                Symptoms = "叶片边缘有啃食痕迹，夜晚可见蜗牛",
                Treatment = "在花盆周围撒生石灰，人工捕捉",
                DetectedDate = DateTime.UtcNow.AddDays(-3),
                Status = PestStatus.Treating
            },
            new PestRecord
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000003"),
                CropId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                IssueType = "白粉病",
                Symptoms = "叶片上有白色粉末状物质",
                Treatment = "增加通风，喷洒小苏打溶液",
                DetectedDate = DateTime.UtcNow.AddDays(-1),
                Status = PestStatus.Detected
            }
        };

        await context.PestRecords.AddRangeAsync(records, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
