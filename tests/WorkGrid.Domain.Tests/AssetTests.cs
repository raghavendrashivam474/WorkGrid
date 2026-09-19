using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Domain.Tests;

public class AssetTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesAsset()
    {
        var id = Guid.NewGuid();
        var asset = new Asset(id, "AST-100", "MacBook Pro", "Laptop", "SN-998877");

        Assert.Equal(id, asset.Id);
        Assert.Equal("AST-100", asset.AssetTag);
        Assert.Equal("MacBook Pro", asset.Name);
        Assert.Equal("Laptop", asset.AssetType);
        Assert.Equal("SN-998877", asset.SerialNumber);
        Assert.Equal(AssetStatus.Available, asset.Status);
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Asset(Guid.Empty, "AST-100", "Laptop"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidTag_ThrowsDomainValidationException(string tag)
    {
        Assert.Throws<DomainValidationException>(() =>
            new Asset(Guid.NewGuid(), tag, "Laptop"));
    }

    [Fact]
    public void StateTransitions_WorkAsExpected()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-100", "Monitor");

        asset.MarkAssigned();
        Assert.Equal(AssetStatus.Assigned, asset.Status);

        asset.MarkMaintenance();
        Assert.Equal(AssetStatus.Maintenance, asset.Status);

        asset.MarkAvailable();
        Assert.Equal(AssetStatus.Available, asset.Status);

        asset.Retire();
        Assert.Equal(AssetStatus.Retired, asset.Status);
    }

    [Fact]
    public void MarkAssigned_OnRetiredAsset_ThrowsDomainValidationException()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-100", "Old Device");
        asset.Retire();

        Assert.Throws<DomainValidationException>(() => asset.MarkAssigned());
    }
}
