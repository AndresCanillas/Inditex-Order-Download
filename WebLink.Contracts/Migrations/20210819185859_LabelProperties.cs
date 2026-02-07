using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class LabelProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "HeightInMM",
                table: "Labels",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDataBound",
                table: "Labels",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "WidthInMM",
                table: "Labels",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeightInMM",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "IsDataBound",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "WidthInMM",
                table: "Labels");
        }
    }
}
