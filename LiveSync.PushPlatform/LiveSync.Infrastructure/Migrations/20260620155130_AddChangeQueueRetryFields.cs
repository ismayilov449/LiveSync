using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeQueueRetryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "ChangeQueue",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedAt",
                table: "ChangeQueue",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "ChangeQueue",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastError",
                table: "ChangeQueue");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "ChangeQueue");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "ChangeQueue");
        }
    }
}
