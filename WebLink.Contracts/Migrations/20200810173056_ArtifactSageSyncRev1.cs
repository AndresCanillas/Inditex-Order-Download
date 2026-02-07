using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ArtifactSageSyncRev1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SageRef",
                table: "Artifacts",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SyncWithSage",
                table: "Artifacts",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SageRef",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "SyncWithSage",
                table: "Artifacts");
        }
    }
}
