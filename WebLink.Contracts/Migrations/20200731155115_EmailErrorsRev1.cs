using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class EmailErrorsRev1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyOrderProcesingErrors",
                table: "EmailServiceSettings",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "EmailTokenItemErrors",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    EmailTokenID = table.Column<int>(nullable: false),
                    TokenKey = table.Column<string>(nullable: true),
                    TokenType = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true),
                    Notified = table.Column<bool>(nullable: false),
                    NotifyDate = table.Column<DateTime>(nullable: true),
                    Seen = table.Column<bool>(nullable: false),
                    SeenDate = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTokenItemErrors", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmailTokenItemErrors_EmailTokens_EmailTokenID",
                        column: x => x.EmailTokenID,
                        principalTable: "EmailTokens",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTokenItemErrors_EmailTokenID",
                table: "EmailTokenItemErrors",
                column: "EmailTokenID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailTokenItemErrors");

            migrationBuilder.DropColumn(
                name: "NotifyOrderProcesingErrors",
                table: "EmailServiceSettings");
        }
    }
}
