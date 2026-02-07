using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class PrintPackageRev2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_CompanyID",
                table: "CompanyOrders",
                column: "CompanyID");

            migrationBuilder.Sql("update o set o.CompanyID = b.CompanyID from CompanyOrders o join Projects p on o.ProjectID = p.ID join Brands b on p.BrandID = b.ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyOrders_CompanyID",
                table: "CompanyOrders");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "CompanyOrders");
        }
    }
}
