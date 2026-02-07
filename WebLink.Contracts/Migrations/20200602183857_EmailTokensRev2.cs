using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class EmailTokensRev2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SeenDate",
                table: "EmailTokenItems",
                nullable: true,
                oldClrType: typeof(DateTime));

            migrationBuilder.AlterColumn<DateTime>(
                name: "NotifyDate",
                table: "EmailTokenItems",
                nullable: true,
                oldClrType: typeof(DateTime));

            migrationBuilder.CreateTable(
                name: "EmailServiceSettings",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserID = table.Column<string>(nullable: true),
                    NotifyOrderReceived = table.Column<bool>(nullable: false),
                    NotifyOrderPendingValidation = table.Column<bool>(nullable: false),
                    NotifyOrderValidated = table.Column<bool>(nullable: false),
                    NotifyOrderConflict = table.Column<bool>(nullable: false),
                    NotifyOrderCompleted = table.Column<bool>(nullable: false),
                    NotificationPeriodInDays = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailServiceSettings", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailServiceSettings");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SeenDate",
                table: "EmailTokenItems",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NotifyDate",
                table: "EmailTokenItems",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);
        }
    }
}
