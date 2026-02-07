using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class OrderIsBillableByDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsBillable",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: true,
                oldNullable:false
                );

            migrationBuilder.Sql("UPDATE CompanyOrders SET IsBillable = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
               name: "IsBillable",
               table: "CompanyOrders",
               nullable: false,
               defaultValue: false,
               oldNullable: false
               );

            migrationBuilder.Sql("UPDATE CompanyOrders SET IsBillable = 0");
        }
    }
}
