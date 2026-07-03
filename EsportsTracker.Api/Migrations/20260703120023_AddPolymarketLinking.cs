using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EsportsTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPolymarketLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    PolymarketSlug = table.Column<string>(type: "text", nullable: false),
                    ConditionId = table.Column<string>(type: "text", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: true),
                    ClobTokenIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    OutcomeNames = table.Column<List<string>>(type: "text[]", nullable: false),
                    GameStartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LinkedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketLinks_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MarketLinkId = table.Column<int>(type: "integer", nullable: false),
                    ClobTokenId = table.Column<string>(type: "text", nullable: false),
                    OutcomeName = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceSnapshots_MarketLinks_MarketLinkId",
                        column: x => x.MarketLinkId,
                        principalTable: "MarketLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketLinks_MatchId",
                table: "MarketLinks",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_MarketLinkId_ClobTokenId_CapturedAtUtc",
                table: "PriceSnapshots",
                columns: new[] { "MarketLinkId", "ClobTokenId", "CapturedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceSnapshots");

            migrationBuilder.DropTable(
                name: "MarketLinks");
        }
    }
}
