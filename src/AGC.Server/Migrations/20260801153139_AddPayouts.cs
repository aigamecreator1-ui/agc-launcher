using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGC.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AmountUsd = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    StripePayoutId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_StripePayoutId",
                table: "Payouts",
                column: "StripePayoutId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payouts");
        }
    }
}
