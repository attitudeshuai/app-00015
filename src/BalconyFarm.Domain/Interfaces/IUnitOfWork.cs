using BalconyFarm.Domain.Entities;

namespace BalconyFarm.Domain.Interfaces;

public interface IUnitOfWork
{
    IRepository<User> Users { get; }
    IRepository<Crop> Crops { get; }
    IRepository<CropCareTask> CropCareTasks { get; }
    IRepository<HarvestRecord> HarvestRecords { get; }
    IRepository<PestRecord> PestRecords { get; }
    IRepository<TreatmentLog> TreatmentLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
