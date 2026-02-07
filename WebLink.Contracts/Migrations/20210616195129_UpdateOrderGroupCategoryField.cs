using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class UpdateOrderGroupCategoryField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ERPReference",
                table: "OrderGroups",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderCategoryClient",
                table: "OrderGroups",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ERPReference",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "OrderCategoryClient",
                table: "OrderGroups");
        }
    }
}
