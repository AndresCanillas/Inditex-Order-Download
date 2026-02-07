using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class CompositionSeparators : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CILanguageSeparator",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CISeparator",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiberLanguageSeprator",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FibersSeparator",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionsSeparator",
                table: "Projects",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CILanguageSeparator",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CISeparator",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FiberLanguageSeprator",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FibersSeparator",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SectionsSeparator",
                table: "Projects");
        }
    }
}
