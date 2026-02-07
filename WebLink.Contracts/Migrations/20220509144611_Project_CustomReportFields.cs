using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class Project_CustomReportFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomOrderDataReport",
                table: "Projects",
                nullable: true);

            // update all projects with empty configuration
            migrationBuilder.Sql(@"UPDATE Projects SET CustomOrderDataReport = '{}'");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomOrderDataReport",
                table: "Projects");
        }
    }
}
