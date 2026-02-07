using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ChangeProvidersKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyProviders_CompanyID_ProviderCompanyID_ClientReference",
                table: "CompanyProviders");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProviders_CompanyID_ClientReference",
                table: "CompanyProviders",
                columns: new[] { "CompanyID", "ClientReference" },
                unique: true,
                filter: "[ClientReference] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyProviders_CompanyID_ClientReference",
                table: "CompanyProviders");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProviders_CompanyID_ProviderCompanyID_ClientReference",
                table: "CompanyProviders",
                columns: new[] { "CompanyID", "ProviderCompanyID", "ClientReference" },
                unique: true,
                filter: "[ClientReference] IS NOT NULL");
        }
    }
}
