using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class LabelRowsCols : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Cols",
                table: "Labels",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LabelsAcross",
                table: "Labels",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rows",
                table: "Labels",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cols",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "LabelsAcross",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "Rows",
                table: "Labels");
        }
    }
}
