using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class UpdateProviderDefaultBillingLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE CompanyProviders
SET BillingLocation = DefaultProductionLocation
WHERE BillingLocation IS NULL OR BillingLocation < 1 AND DefaultProductionLocation IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
