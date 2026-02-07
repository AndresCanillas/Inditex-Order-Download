using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class MapingsRev1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FixedType",
                table: "DataImportColMapping");

            migrationBuilder.DropColumn(
                name: "IsPK",
                table: "DataImportColMapping");

            migrationBuilder.AddColumn<string>(
                name: "DateFormat",
                table: "DataImportColMapping",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateFormat",
                table: "DataImportColMapping");

            migrationBuilder.AddColumn<int>(
                name: "FixedType",
                table: "DataImportColMapping",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPK",
                table: "DataImportColMapping",
                nullable: true);
        }
    }
}
