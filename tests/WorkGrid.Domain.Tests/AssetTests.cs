using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using Xunit;

namespace WorkGrid.Domain.Tests;

public class AssetTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesAsset()
    {
        var id = Guid.NewGuid();
        var asset = new Asset(id, "AST-001", "MacBook Pro", "Laptop", "SN12345");

        Assert.Equal(id, asset.Id);
        Assert.Equal("AST-001", asset.AssetTag);
        Assert.Equal("MacBook Pro", asset.Name);
        Assert.Equal("Laptop", asset.AssetType);
        Assert.Equal("SN12345", asset.SerialNumber);
        Assert.Equal(AssetStatus.Available, asset.Status);
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Asset(Guid.Empty, "AST-001", "MacBook Pro"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTag_ThrowsDomainValidationException(string? invalidTag)
    {
        Assert.Throws<DomainValidationException>(() =>
            new Asset(Guid.NewGuid(), invalidTag!, "MacBook Pro"));
    }

    [Fact]
    public void StateTransitions_WorkAsExpected()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-001", "MacBook Pro");

        // Available -> Assigned
        asset.MarkAssigned();
        Assert.Equal(AssetStatus.Assigned, asset.Status);

        // Assigned -> Available
        asset.MarkAvailable();
        Assert.Equal(AssetStatus.Available, asset.Status);

        // Available -> Maintenance
        asset.MarkMaintenance();
        Assert.Equal(AssetStatus.Maintenance, asset.Status);

        // Maintenance -> Available
        asset.MarkAvailable();
        Assert.Equal(AssetStatus.Available, asset.Status);

        // Available -> Retired
        asset.Retire();
        Assert.Equal(AssetStatus.Retired, asset.Status);
    }

    [Fact]
    public void MarkAssigned_OnNonAvailableAsset_ThrowsDomainValidationException()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-001", "MacBook Pro", status: AssetStatus.Maintenance);
        Assert.Throws<DomainValidationException>(() => asset.MarkAssigned());

        var retiredAsset = new Asset(Guid.NewGuid(), "AST-002", "Dell XPS", status: AssetStatus.Retired);
        Assert.Throws<DomainValidationException>(() => retiredAsset.MarkAssigned());
    }

    [Fact]
    public void MarkAvailable_OnAvailableOrRetiredAsset_ThrowsDomainValidationException()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-001", "MacBook Pro", status: AssetStatus.Available);
        Assert.Throws<DomainValidationException>(() => asset.MarkAvailable());

        var retiredAsset = new Asset(Guid.NewGuid(), "AST-002", "Dell XPS", status: AssetStatus.Retired);
        Assert.Throws<DomainValidationException>(() => retiredAsset.MarkAvailable());
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesProperties()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-001", "MacBook Pro");

        asset.UpdateDetails("MacBook Pro M3", "Hardware", "SN9999");

        Assert.Equal("MacBook Pro M3", asset.Name);
        Assert.Equal("Hardware", asset.AssetType);
        Assert.Equal("SN9999", asset.SerialNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithInvalidName_ThrowsDomainValidationException(string? invalidName)
    {
        var asset = new Asset(Guid.NewGuid(), "AST-001", "MacBook Pro");

        Assert.Throws<DomainValidationException>(() =>
            asset.UpdateDetails(invalidName!));
    }
}
