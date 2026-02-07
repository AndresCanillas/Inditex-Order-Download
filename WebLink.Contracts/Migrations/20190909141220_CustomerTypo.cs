using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class CustomerTypo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CostumerSupport2",
                table: "Projects",
                newName: "CustomerSupport2");

            migrationBuilder.RenameColumn(
                name: "CostumerSupport1",
                table: "Projects",
                newName: "CustomerSupport1");

            migrationBuilder.RenameColumn(
                name: "CostumerSupport2",
                table: "Companies",
                newName: "CustomerSupport2");

            migrationBuilder.RenameColumn(
                name: "CostumerSupport1",
                table: "Companies",
                newName: "CustomerSupport1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomerSupport2",
                table: "Projects",
                newName: "CostumerSupport2");

            migrationBuilder.RenameColumn(
                name: "CustomerSupport1",
                table: "Projects",
                newName: "CostumerSupport1");

            migrationBuilder.RenameColumn(
                name: "CustomerSupport2",
                table: "Companies",
                newName: "CostumerSupport2");

            migrationBuilder.RenameColumn(
                name: "CustomerSupport1",
                table: "Companies",
                newName: "CostumerSupport1");
        }
    }
}
