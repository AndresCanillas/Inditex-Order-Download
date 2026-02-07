using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class EmailErrorsRev2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LocationID",
                table: "EmailTokenItemErrors",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectID",
                table: "EmailTokenItemErrors",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationID",
                table: "EmailTokenItemErrors");

            migrationBuilder.DropColumn(
                name: "ProjectID",
                table: "EmailTokenItemErrors");
        }
    }
}
