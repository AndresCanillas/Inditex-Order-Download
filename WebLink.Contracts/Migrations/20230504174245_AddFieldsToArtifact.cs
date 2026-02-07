using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class AddFieldsToArtifact : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Artifacts",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnablePreview",
                table: "Artifacts",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHead",
                table: "Artifacts",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "Artifacts",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTail",
                table: "Artifacts",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "EnablePreview",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "IsHead",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "IsTail",
                table: "Artifacts");
        }
    }
}
