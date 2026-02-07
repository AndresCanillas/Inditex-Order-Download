using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class LabelCounter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrintCountSelectorField",
                table: "Labels",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrintCountSequence",
                table: "Labels",
                maxLength: 40,
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "PrintCountSequenceType",
                table: "Labels",
                nullable: false,
                defaultValue: 0);

			migrationBuilder.Sql("update Labels set PrintCountSequence = newid()");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintCountSelectorField",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "PrintCountSequence",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "PrintCountSequenceType",
                table: "Labels");
        }
    }
}
