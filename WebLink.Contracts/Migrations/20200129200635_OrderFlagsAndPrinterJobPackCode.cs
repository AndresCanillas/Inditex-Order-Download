using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class OrderFlagsAndPrinterJobPackCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PackCode",
                table: "PrinterJobs",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBillable",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBilled",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInConflict",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackCode",
                table: "PrinterJobs");

            migrationBuilder.DropColumn(
                name: "IsBillable",
                table: "CompanyOrders");

            migrationBuilder.DropColumn(
                name: "IsBilled",
                table: "CompanyOrders");

            migrationBuilder.DropColumn(
                name: "IsInConflict",
                table: "CompanyOrders");
        }
    }
}
