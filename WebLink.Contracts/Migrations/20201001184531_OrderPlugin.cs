using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class OrderPlugin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderPlugin",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncState",
                table: "EncodedLabels",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderPlugin",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SyncState",
                table: "EncodedLabels");
        }
    }
}
