using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class LabelsFileTrancing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UpdatedFileBy",
                table: "Labels",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedFileDate",
                table: "Labels",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedFileBy",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "UpdatedFileDate",
                table: "Labels");
        }
    }
}
