using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class Hercules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintPackageGenerated",
                table: "CompanyOrders");

            migrationBuilder.RenameColumn(
                name: "Encode",
                table: "PrinterJobs",
                newName: "SendToHercules");

            migrationBuilder.AddColumn<bool>(
                name: "PrintPackageGenerated",
                table: "PrinterJobs",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintPackageGenerated",
                table: "PrinterJobs");

            migrationBuilder.RenameColumn(
                name: "SendToHercules",
                table: "PrinterJobs",
                newName: "Encode");

            migrationBuilder.AddColumn<bool>(
                name: "PrintPackageGenerated",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: false);
        }
    }
}
