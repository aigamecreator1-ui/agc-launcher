using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGC.Server.Migrations
{
    /// <inheritdoc />
    public partial class RenamePayoutAmountAddCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountUsd",
                table: "Payouts");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Payouts",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Payouts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Payouts");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountUsd",
                table: "Payouts",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
