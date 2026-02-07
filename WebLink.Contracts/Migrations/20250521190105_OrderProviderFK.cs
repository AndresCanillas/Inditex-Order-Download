using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class OrderProviderFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // clean orphans
            migrationBuilder.Sql(@"UPDATE o SET ProviderRecordID = NULL
            FROM CompanyOrders o
            LEFT JOIN CompanyProviders pv ON o.ProviderRecordID = pv.ID
            where o.ProviderRecordID IS NOT NULL AND pv.ID IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyOrders_CompanyProviders_ProviderRecordID",
                table: "CompanyOrders",
                column: "ProviderRecordID",
                principalTable: "CompanyProviders",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyOrders_CompanyProviders_ProviderRecordID",
                table: "CompanyOrders");
        }
    }
}
