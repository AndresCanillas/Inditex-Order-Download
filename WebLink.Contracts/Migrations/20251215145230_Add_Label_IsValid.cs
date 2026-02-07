using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class Add_Label_IsValid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "Labels",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IsValidBy",
                table: "Labels",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IsValidDate",
                table: "Labels",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "IsValidBy",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "IsValidDate",
                table: "Labels");
        }
    }
}
