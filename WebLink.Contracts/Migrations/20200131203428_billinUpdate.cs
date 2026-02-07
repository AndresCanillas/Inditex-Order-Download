using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class billinUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderBillingsInfo_BillingsInfo_BillingInfoID",
                table: "ProviderBillingsInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_ProviderBillingsInfo_CompanyProviders_ProviderID",
                table: "ProviderBillingsInfo");

            migrationBuilder.DropIndex(
                name: "IX_ProviderBillingsInfo_BillingInfoID",
                table: "ProviderBillingsInfo");

            migrationBuilder.DropIndex(
                name: "IX_ProviderBillingsInfo_ProviderID",
                table: "ProviderBillingsInfo");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProviderBillingsInfo");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "ProviderBillingsInfo");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ProviderBillingsInfo");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "ProviderBillingsInfo");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProviderBillingsInfo",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "ProviderBillingsInfo",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ProviderBillingsInfo",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "ProviderBillingsInfo",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_ProviderBillingsInfo_BillingInfoID",
                table: "ProviderBillingsInfo",
                column: "BillingInfoID");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderBillingsInfo_ProviderID",
                table: "ProviderBillingsInfo",
                column: "ProviderID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderBillingsInfo_BillingsInfo_BillingInfoID",
                table: "ProviderBillingsInfo",
                column: "BillingInfoID",
                principalTable: "BillingsInfo",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderBillingsInfo_CompanyProviders_ProviderID",
                table: "ProviderBillingsInfo",
                column: "ProviderID",
                principalTable: "CompanyProviders",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
