using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGC.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialAndAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameComments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    GameId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameComments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameEngagementEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    GameId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEngagementEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameVotes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    GameId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    IsLike = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LauncherOpenEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LauncherOpenEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameComments_GameId",
                table: "GameComments",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameEngagementEvents_GameId_Kind",
                table: "GameEngagementEvents",
                columns: new[] { "GameId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_GameVotes_GameId_UserId",
                table: "GameVotes",
                columns: new[] { "GameId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LauncherOpenEvents_UserId",
                table: "LauncherOpenEvents",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameComments");

            migrationBuilder.DropTable(
                name: "GameEngagementEvents");

            migrationBuilder.DropTable(
                name: "GameVotes");

            migrationBuilder.DropTable(
                name: "LauncherOpenEvents");
        }
    }
}
