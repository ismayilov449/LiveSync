using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeQueueClaimFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastError",
                table: "ChangeQueue",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimToken",
                table: "ChangeQueue",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClaimedAt",
                table: "ChangeQueue",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangeQueue_Version_ProcessedAt_ClaimedAt_CreatedAt",
                table: "ChangeQueue",
                columns: new[] { "Version", "ProcessedAt", "ClaimedAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChangeQueue_Version_ProcessedAt_ClaimedAt_CreatedAt",
                table: "ChangeQueue");

            migrationBuilder.DropColumn(
                name: "ClaimToken",
                table: "ChangeQueue");

            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "ChangeQueue");

            migrationBuilder.AlterColumn<string>(
                name: "LastError",
                table: "ChangeQueue",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
