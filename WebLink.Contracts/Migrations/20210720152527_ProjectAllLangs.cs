using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ProjectAllLangs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FiberLanguageSeprator",
                table: "Projects",
                newName: "SectionLanguageSeparator");

            migrationBuilder.AddColumn<bool>(
                name: "EnableAllLangs",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FiberLanguageSeparator",
                table: "Projects",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableAllLangs",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FiberLanguageSeparator",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "SectionLanguageSeparator",
                table: "Projects",
                newName: "FiberLanguageSeprator");
        }
    }
}
