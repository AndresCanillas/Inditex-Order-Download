using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class LocationCutoffTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CutoffTime",
                table: "Locations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CutoffTime",
                table: "Locations");
        }
    }
}
