#pragma warning disable CA1861
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkGrid.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncChanges",
                columns: table => new
                {
                    ChangeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjectType = table.Column<int>(type: "INTEGER", nullable: false),
                    ObjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Operation = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginatingDeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SequenceNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: true),
                    PayloadHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncChanges", x => x.ChangeId);
                });

            migrationBuilder.CreateTable(
                name: "SyncCheckpoints",
                columns: table => new
                {
                    RemoteReplicaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastAppliedSequenceNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncCheckpoints", x => x.RemoteReplicaId);
                });

            migrationBuilder.CreateTable(
                name: "SyncDevices",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncDevices", x => x.DeviceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncChanges_ObjectType_ObjectId",
                table: "SyncChanges",
                columns: new[] { "ObjectType", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncChanges_OriginatingDeviceId_ObjectType_ObjectId",
                table: "SyncChanges",
                columns: new[] { "OriginatingDeviceId", "ObjectType", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncChanges_OriginatingDeviceId_SequenceNumber",
                table: "SyncChanges",
                columns: new[] { "OriginatingDeviceId", "SequenceNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncChanges");

            migrationBuilder.DropTable(
                name: "SyncCheckpoints");

            migrationBuilder.DropTable(
                name: "SyncDevices");
        }
    }
}

