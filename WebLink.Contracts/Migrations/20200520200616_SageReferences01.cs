using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class SageReferences01 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SageRef",
                table: "OrderGroups",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SyncWithSage",
                table: "OrderGroups",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SageRef",
                table: "Companies",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SyncWithSage",
                table: "Companies",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SageRef",
                table: "Articles",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SyncWithSage",
                table: "Articles",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SageRef",
                table: "Addresses",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SyncWithSage",
                table: "Addresses",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SageRef",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "SyncWithSage",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "SageRef",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SyncWithSage",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SageRef",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SyncWithSage",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SageRef",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "SyncWithSage",
                table: "Addresses");
        }
    }
}
