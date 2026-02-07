using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class QuantityRequestBackucpAndMovePackCodeColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackCode",
                table: "PrinterJobs");

            migrationBuilder.AddColumn<string>(
                name: "PackCode",
                table: "PrinterJobDetails",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityRequested",
                table: "PrinterJobDetails",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackCode",
                table: "PrinterJobDetails");

            migrationBuilder.DropColumn(
                name: "QuantityRequested",
                table: "PrinterJobDetails");

            migrationBuilder.AddColumn<string>(
                name: "PackCode",
                table: "PrinterJobs",
                maxLength: 25,
                nullable: true);
        }
    }
}
