using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class MappingOrderWithSage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditStatus",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryStatus",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceStatus",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProjectPrefix",
                table: "CompanyOrders",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredOn",
                table: "CompanyOrders",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageReference",
                table: "CompanyOrders",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SageStatus",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SyncWithSage",
                table: "CompanyOrders",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditStatus",
                table: "CompanyOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "CompanyOrders");

            migrationBuilder.DropColumn(
                name: "InvoiceStatus",
                table: "CompanyOrders");

            migrationBuilder.DropColumn(
                name: "ProjectPrefix",
                table: "CompanyOrders");

            migrationBuilder.DropColumn(
                name: "RegisteredOn",
                table: "CompanyOrders");

            migrationBuilder.DropColumn(
                name: "SageReference",
                table: "CompanyOrders");

            migrationBuilder.DropColumn(
                name: "SageStatus",
                table: "CompanyOrders");

            migrationBuilder.DropColumn(
                name: "SyncWithSage",
                table: "CompanyOrders");
        }
    }
}
