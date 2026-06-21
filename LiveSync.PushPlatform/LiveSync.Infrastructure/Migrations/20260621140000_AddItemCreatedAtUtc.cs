using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSync.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddItemCreatedAtUtc : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedAtUtc",
            table: "Items",
            type: "datetime2",
            nullable: false,
            defaultValueSql: "GETUTCDATE()");

        migrationBuilder.Sql("UPDATE Items SET CreatedAtUtc = UpdatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Items_TenantId_CreatedAtUtc",
            table: "Items",
            columns: new[] { "TenantId", "CreatedAtUtc" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Items_TenantId_CreatedAtUtc",
            table: "Items");

        migrationBuilder.DropColumn(
            name: "CreatedAtUtc",
            table: "Items");
    }
}
