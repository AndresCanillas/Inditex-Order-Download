using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations.IdentityDBMigrations
{
    public partial class BrandClaim : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedBrandID",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedProjectID",
                table: "AspNetUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedBrandID",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SelectedProjectID",
                table: "AspNetUsers");
        }
    }
}
