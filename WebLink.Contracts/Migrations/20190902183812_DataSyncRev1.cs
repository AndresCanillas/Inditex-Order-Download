using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class DataSyncRev1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintHeaders",
                table: "PrinterSettings");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PrinterSettings",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "PrinterSettings",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PrinterSettings",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "PrinterSettings",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "PrinterJobs",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "PrinterJobDetails",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PackArticles",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "PackArticles",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PackArticles",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "PackArticles",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CompanyProviders",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "CompanyProviders",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "CompanyProviders",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "CompanyProviders",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PrinterSettings");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "PrinterSettings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PrinterSettings");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "PrinterSettings");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "PrinterJobs");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "PrinterJobDetails");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PackArticles");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "PackArticles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PackArticles");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "PackArticles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CompanyProviders");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "CompanyProviders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CompanyProviders");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "CompanyProviders");

            migrationBuilder.AddColumn<bool>(
                name: "PrintHeaders",
                table: "PrinterSettings",
                nullable: false,
                defaultValue: false);
        }
    }
}
