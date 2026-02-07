using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ERPConfigurationRev01 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AddressID",
                table: "ERPCompanyLocations",
                newName: "ExpeditionAddressCode");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryAddressID",
                table: "ERPCompanyLocations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAddressID",
                table: "ERPCompanyLocations");

            migrationBuilder.RenameColumn(
                name: "ExpeditionAddressCode",
                table: "ERPCompanyLocations",
                newName: "AddressID");
        }
    }
}
