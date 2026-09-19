using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Domain.Tests;

public sealed class AssetTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesAsset()
    {
        var id = Guid.NewGuid();
        var asset = new Asset(id, "AST-001", "MacBook Pro", "Laptop", "SN12345", AssetStatus.Available);

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
    public void Constructor_WithInvalidTag_ThrowsDomainValidationException(string? tag)
    {
        Assert.Throws<DomainValidationException>(() =>
            new Asset(Guid.NewGuid(), tag!, "MacBook Pro"));
    }

    [Fact]
    public void StateTransitions_WorkAsExpected()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-001", "MacBook Pro");
        Assert.Equal(AssetStatus.Available, asset.Status);

        asset.MarkAssigned();
        Assert.Equal(AssetStatus.Assigned, asset.Status);

        asset.MarkAvailable();
        Assert.Equal(AssetStatus.Available, asset.Status);

        asset.MarkMaintenance();
        Assert.Equal(AssetStatus.Maintenance, asset.Status);

        asset.Retire();
        Assert.Equal(AssetStatus.Retired, asset.Status);
    }

    [Fact]
    public void MarkAssigned_OnRetiredAsset_ThrowsDomainValidationException()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-001", "MacBook Pro");
        asset.Retire();

        Assert.Throws<DomainValidationException>(() => asset.MarkAssigned());
        Assert.Throws<DomainValidationException>(() => asset.MarkAvailable());
        Assert.Throws<DomainValidationException>(() => asset.MarkMaintenance());
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesProperties()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-001", "ThinkPad T14", "Laptop", "TP123");
        asset.UpdateDetails("ThinkPad T14s", "Laptop Pro", "TP123-Updated");

        Assert.Equal("ThinkPad T14s", asset.Name);
        Assert.Equal("Laptop Pro", asset.AssetType);
        Assert.Equal("TP123-Updated", asset.SerialNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithInvalidName_ThrowsDomainValidationException(string? name)
    {
        var asset = new Asset(Guid.NewGuid(), "AST-001", "ThinkPad T14");
        Assert.Throws<DomainValidationException>(() => asset.UpdateDetails(name!));
    }
}
