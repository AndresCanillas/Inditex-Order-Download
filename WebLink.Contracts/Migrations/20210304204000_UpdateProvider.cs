using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class UpdateProvider : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingLocation",
                table: "CompanyProviders");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "CompanyProviders");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillingLocation",
                table: "CompanyProviders",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "CompanyProviders",
                nullable: true);
        }
    }
}
