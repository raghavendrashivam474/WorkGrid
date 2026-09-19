using WorkGrid.Domain.Entities;

namespace WorkGrid.Domain.Contracts;

public interface IAssetRepository
{
    Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTagAsync(string assetTag, CancellationToken cancellationToken = default);
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);
    Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default);
    Task DeleteAsync(Asset asset, CancellationToken cancellationToken = default);
}
