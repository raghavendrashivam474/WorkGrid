using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Infrastructure.Persistence;

namespace WorkGrid.Infrastructure.Repositories;

public sealed class AssetRepository : IAssetRepository
{
    private readonly WorkGridDbContext _context;

    public AssetRepository(WorkGridDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .AsNoTracking()
            .OrderBy(a => a.AssetTag)
            .ToListAsync(cancellationToken);
    }

    public async Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByTagAsync(string assetTag, CancellationToken cancellationToken = default)
    {
        var normalizedTag = assetTag.Trim().ToUpper(CultureInfo.InvariantCulture);
        return await _context.Assets
            .AnyAsync(a => a.AssetTag == normalizedTag, cancellationToken);
    }

    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        await _context.Assets.AddAsync(asset, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _context.Assets.Update(asset);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
