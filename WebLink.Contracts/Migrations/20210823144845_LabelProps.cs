using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class LabelProps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeightInMM",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "WidthInMM",
                table: "Labels");

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "Labels",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "Labels",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Labels");

            migrationBuilder.AddColumn<double>(
                name: "HeightInMM",
                table: "Labels",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WidthInMM",
                table: "Labels",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
