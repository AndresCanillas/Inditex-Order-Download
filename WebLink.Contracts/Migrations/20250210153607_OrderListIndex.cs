using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class OrderListIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OrderUpdateProperties_IsActive",
                table: "OrderUpdateProperties",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OrderUpdateProperties_IsRejected",
                table: "OrderUpdateProperties",
                column: "IsRejected");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_CreatedDate",
                table: "CompanyOrders",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_IsBilled",
                table: "CompanyOrders",
                column: "IsBilled");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_IsInConflict",
                table: "CompanyOrders",
                column: "IsInConflict");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_IsStopped",
                table: "CompanyOrders",
                column: "IsStopped");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_OrderStatus",
                table: "CompanyOrders",
                column: "OrderStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_ProductionType",
                table: "CompanyOrders",
                column: "ProductionType");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_ProviderRecordID",
                table: "CompanyOrders",
                column: "ProviderRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_SendToCompanyID",
                table: "CompanyOrders",
                column: "SendToCompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_UpdatedDate",
                table: "CompanyOrders",
                column: "UpdatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ArticleCode",
                table: "Articles",
                column: "ArticleCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderUpdateProperties_IsActive",
                table: "OrderUpdateProperties");

            migrationBuilder.DropIndex(
                name: "IX_OrderUpdateProperties_IsRejected",
                table: "OrderUpdateProperties");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOrders_CreatedDate",
                table: "CompanyOrders");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOrders_IsBilled",
                table: "CompanyOrders");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOrders_IsInConflict",
                table: "CompanyOrders");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOrders_IsStopped",
                table: "CompanyOrders");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOrders_OrderStatus",
                table: "CompanyOrders");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOrders_ProductionType",
                table: "CompanyOrders");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOrders_ProviderRecordID",
                table: "CompanyOrders");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOrders_SendToCompanyID",
                table: "CompanyOrders");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOrders_UpdatedDate",
                table: "CompanyOrders");

            migrationBuilder.DropIndex(
                name: "IX_Articles_ArticleCode",
                table: "Articles");
        }
    }
}
