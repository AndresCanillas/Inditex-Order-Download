using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ProvidersIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyProviders_CompanyID",
                table: "CompanyProviders");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProviders_CompanyID_ProviderCompanyID",
                table: "CompanyProviders",
                columns: new[] { "CompanyID", "ProviderCompanyID" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyProviders_CompanyID_ProviderCompanyID",
                table: "CompanyProviders");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProviders_CompanyID",
                table: "CompanyProviders",
                column: "CompanyID");
        }
    }
}
