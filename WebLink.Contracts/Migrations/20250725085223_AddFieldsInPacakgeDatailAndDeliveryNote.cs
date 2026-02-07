using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class AddFieldsInPacakgeDatailAndDeliveryNote : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrinterJobID",
                table: "PackageDetails",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "DeliveryNotes",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrinterJobID",
                table: "PackageDetails");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "DeliveryNotes");
        }
    }
}
