using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class EncodedLabelsRev1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "EncodedLabels",
                newName: "Barcode");

            migrationBuilder.AlterColumn<string>(
                name: "Speed",
                table: "PrinterSettings",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "Darkness",
                table: "PrinterSettings",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<int>(
                name: "OrderID",
                table: "EncodedLabels",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "RSSI",
                table: "EncodedLabels",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderID",
                table: "EncodedLabels");

            migrationBuilder.DropColumn(
                name: "RSSI",
                table: "EncodedLabels");

            migrationBuilder.RenameColumn(
                name: "Barcode",
                table: "EncodedLabels",
                newName: "ProductCode");

            migrationBuilder.AlterColumn<int>(
                name: "Speed",
                table: "PrinterSettings",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Darkness",
                table: "PrinterSettings",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
