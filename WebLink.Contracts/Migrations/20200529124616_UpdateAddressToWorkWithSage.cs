using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class UpdateAddressToWorkWithSage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SageCountryCode",
                table: "Addresses",
                newName: "Telephone2");

            migrationBuilder.AddColumn<string>(
                name: "BusinessName1",
                table: "Addresses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessName2",
                table: "Addresses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email1",
                table: "Addresses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email2",
                table: "Addresses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telephone1",
                table: "Addresses",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessName1",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "BusinessName2",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Email1",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Email2",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Telephone1",
                table: "Addresses");

            migrationBuilder.RenameColumn(
                name: "Telephone2",
                table: "Addresses",
                newName: "SageCountryCode");
        }
    }
}
