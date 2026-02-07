using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class AddDeliveryCentralIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArticleCentralID",
                table: "PackageDetails",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AddressCentralID",
                table: "DeliveryNotes",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArticleCentralID",
                table: "PackageDetails");

            migrationBuilder.DropColumn(
                name: "AddressCentralID",
                table: "DeliveryNotes");
        }
    }
}
