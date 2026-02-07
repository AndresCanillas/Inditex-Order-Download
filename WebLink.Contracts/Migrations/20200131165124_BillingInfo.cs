using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class BillingInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PackCode",
                table: "PrinterJobs",
                maxLength: 25,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingInfoID",
                table: "CompanyProviders",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStopped",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Instructions",
                table: "Articles",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BillingsInfo",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingsInfo", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ProviderBillingsInfo",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProviderID = table.Column<int>(nullable: false),
                    BillingInfoID = table.Column<int>(nullable: false),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderBillingsInfo", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ProviderBillingsInfo_BillingsInfo_BillingInfoID",
                        column: x => x.BillingInfoID,
                        principalTable: "BillingsInfo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderBillingsInfo_CompanyProviders_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "CompanyProviders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderBillingsInfo_BillingInfoID",
                table: "ProviderBillingsInfo",
                column: "BillingInfoID");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderBillingsInfo_ProviderID",
                table: "ProviderBillingsInfo",
                column: "ProviderID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderBillingsInfo");

            migrationBuilder.DropTable(
                name: "BillingsInfo");

            migrationBuilder.DropColumn(
                name: "BillingInfoID",
                table: "CompanyProviders");

            migrationBuilder.DropColumn(
                name: "IsStopped",
                table: "CompanyOrders");

            migrationBuilder.AlterColumn<string>(
                name: "PackCode",
                table: "PrinterJobs",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 25,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Instructions",
                table: "Articles",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 4000,
                oldNullable: true);
        }
    }
}
