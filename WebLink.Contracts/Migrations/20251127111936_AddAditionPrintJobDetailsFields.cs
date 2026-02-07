using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class AddAditionPrintJobDetailsFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExportProgress",
                table: "PrinterJobDetails",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEncodeDate",
                table: "PrinterJobDetails",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPrintDate",
                table: "PrinterJobDetails",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifyDate",
                table: "PrinterJobDetails",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransferProgress",
                table: "PrinterJobDetails",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VerifyProgress",
                table: "PrinterJobDetails",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExportProgress",
                table: "PrinterJobDetails");

            migrationBuilder.DropColumn(
                name: "LastEncodeDate",
                table: "PrinterJobDetails");

            migrationBuilder.DropColumn(
                name: "LastPrintDate",
                table: "PrinterJobDetails");

            migrationBuilder.DropColumn(
                name: "LastVerifyDate",
                table: "PrinterJobDetails");

            migrationBuilder.DropColumn(
                name: "TransferProgress",
                table: "PrinterJobDetails");

            migrationBuilder.DropColumn(
                name: "VerifyProgress",
                table: "PrinterJobDetails");
        }
    }
}
