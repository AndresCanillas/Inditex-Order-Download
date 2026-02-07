using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class FtpWatcherLogsAddCreateDateColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // https://csharp.hotexamples.com/examples/-/MigrationBuilder/AddColumn/php-migrationbuilder-addcolumn-method-examples.html
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "FtpWatcherLogs",
                nullable: false,
                defaultValueSql: "GETDATE()");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "FtpWatcherLogs");
        }
    }
}
