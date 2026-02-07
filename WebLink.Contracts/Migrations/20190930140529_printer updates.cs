using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class printerupdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IP",
                table: "Printers",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemote",
                table: "Printers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Port",
                table: "Printers",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrinterType",
                table: "Printers",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsCutter",
                table: "Printers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsRFID",
                table: "Printers",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IP",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "IsRemote",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "PrinterType",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "SupportsCutter",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "SupportsRFID",
                table: "Printers");
        }
    }
}
