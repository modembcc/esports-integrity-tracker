using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EsportsTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSettledAtToMarketLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SettledAtUtc",
                table: "MarketLinks",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SettledAtUtc",
                table: "MarketLinks");
        }
    }
}
