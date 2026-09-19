using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Domain.Entities;

public sealed class Asset
{
    public Guid Id { get; private set; }
    public string AssetTag { get; private set; }
    public string Name { get; private set; }
    public string? AssetType { get; private set; }
    public string? SerialNumber { get; private set; }
    public AssetStatus Status { get; private set; }

    public Asset(
        Guid id,
        string assetTag,
        string name,
        string? assetType = null,
        string? serialNumber = null,
        AssetStatus status = AssetStatus.Available)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Asset ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(assetTag))
        {
            throw new DomainValidationException("Asset tag cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Asset name cannot be null or whitespace.");
        }

        Id = id;
        AssetTag = assetTag.Trim().ToUpperInvariant();
        Name = name.Trim();
        AssetType = assetType?.Trim();
        SerialNumber = serialNumber?.Trim();
        Status = status;
    }

    public void UpdateDetails(string name, string? assetType = null, string? serialNumber = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Asset name cannot be null or whitespace.");
        }

        Name = name.Trim();
        AssetType = assetType?.Trim();
        SerialNumber = serialNumber?.Trim();
    }

    public void MarkAssigned()
    {
        if (Status == AssetStatus.Retired)
        {
            throw new DomainValidationException("Cannot assign a retired asset.");
        }

        Status = AssetStatus.Assigned;
    }

    public void MarkAvailable()
    {
        if (Status == AssetStatus.Retired)
        {
            throw new DomainValidationException("Cannot make a retired asset available.");
        }

        Status = AssetStatus.Available;
    }

    public void MarkMaintenance()
    {
        if (Status == AssetStatus.Retired)
        {
            throw new DomainValidationException("Cannot place a retired asset into maintenance.");
        }

        Status = AssetStatus.Maintenance;
    }

    public void Retire()
    {
        Status = AssetStatus.Retired;
    }
}
