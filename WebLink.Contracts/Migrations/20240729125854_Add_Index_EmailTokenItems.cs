using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class Add_Index_EmailTokenItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailTokenItems_EmailTokenID",
                table: "EmailTokenItems");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTokenItems_EmailTokenID_OrderID",
                table: "EmailTokenItems",
                columns: new[] { "EmailTokenID", "OrderID" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailTokenItems_EmailTokenID_OrderID",
                table: "EmailTokenItems");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTokenItems_EmailTokenID",
                table: "EmailTokenItems",
                column: "EmailTokenID");
        }
    }
}
