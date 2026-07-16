using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EsportsTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeague : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PandaScoreSerieId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_PandaScoreSerieId",
                table: "Leagues",
                column: "PandaScoreSerieId",
                unique: true);

            // Pre-existing matches were all synced under the single legacy
            // SerieId (MSI 2026) before multi-league support existed —
            // seed that league so we have somewhere to point them.
            migrationBuilder.Sql(@"
                INSERT INTO ""Leagues"" (""Name"", ""PandaScoreSerieId"")
                VALUES ('Mid-Season Invitational 2026', 10676)
                ON CONFLICT (""PandaScoreSerieId"") DO NOTHING;
            ");

            migrationBuilder.AddColumn<int>(
                name: "LeagueId",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Matches""
                SET ""LeagueId"" = (SELECT ""Id"" FROM ""Leagues"" WHERE ""PandaScoreSerieId"" = 10676)
                WHERE ""LeagueId"" IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "LeagueId",
                table: "Matches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_LeagueId",
                table: "Matches",
                column: "LeagueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Leagues_LeagueId",
                table: "Matches",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Leagues_LeagueId",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "Leagues");

            migrationBuilder.DropIndex(
                name: "IX_Matches_LeagueId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "LeagueId",
                table: "Matches");
        }
    }
}
