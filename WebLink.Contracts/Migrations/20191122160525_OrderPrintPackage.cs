using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class OrderPrintPackage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrintPackages");

            migrationBuilder.AddColumn<bool>(
                name: "PrintPackageGenerated",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintPackageGenerated",
                table: "CompanyOrders");

            migrationBuilder.CreateTable(
                name: "PrintPackages",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    BrandID = table.Column<int>(nullable: false),
                    CompanyID = table.Column<int>(nullable: false),
                    OrderID = table.Column<int>(nullable: false),
                    ProjectID = table.Column<int>(nullable: false),
                    RFIDConfigID = table.Column<int>(nullable: false),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintPackages", x => x.ID);
                });
        }
    }
}
