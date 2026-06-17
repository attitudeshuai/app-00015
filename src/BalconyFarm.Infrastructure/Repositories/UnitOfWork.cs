using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Interfaces;
using BalconyFarm.Infrastructure.Data;

namespace BalconyFarm.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IRepository<User>? _users;
    private IRepository<Crop>? _crops;
    private IRepository<CropCareTask>? _cropCareTasks;
    private IRepository<HarvestRecord>? _harvestRecords;
    private IRepository<PestRecord>? _pestRecords;
    private IRepository<TreatmentLog>? _treatmentLogs;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Crop> Crops => _crops ??= new Repository<Crop>(_context);
    public IRepository<CropCareTask> CropCareTasks => _cropCareTasks ??= new Repository<CropCareTask>(_context);
    public IRepository<HarvestRecord> HarvestRecords => _harvestRecords ??= new Repository<HarvestRecord>(_context);
    public IRepository<PestRecord> PestRecords => _pestRecords ??= new Repository<PestRecord>(_context);
    public IRepository<TreatmentLog> TreatmentLogs => _treatmentLogs ??= new Repository<TreatmentLog>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
