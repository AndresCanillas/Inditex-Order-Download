using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations.IdentityDBMigrations
{
    public partial class SelectedCompanyID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedCompanyID",
                table: "AspNetUsers",
                nullable: true);

			migrationBuilder.Sql("update AspNetUsers set SelectedCompanyID = CompanyID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedCompanyID",
                table: "AspNetUsers");
        }
    }
}
