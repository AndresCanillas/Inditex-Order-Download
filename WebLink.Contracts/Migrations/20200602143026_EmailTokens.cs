using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class EmailTokens : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailTokens",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTokens", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "EmailTokenItems",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    EmailTokenID = table.Column<int>(nullable: false),
                    TokenType = table.Column<int>(nullable: false),
                    Notified = table.Column<bool>(nullable: false),
                    NotifyDate = table.Column<DateTime>(nullable: false),
                    Seen = table.Column<bool>(nullable: false),
                    SeenDate = table.Column<DateTime>(nullable: false),
                    TokenData = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTokenItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmailTokenItems_EmailTokens_EmailTokenID",
                        column: x => x.EmailTokenID,
                        principalTable: "EmailTokens",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTokenItems_EmailTokenID",
                table: "EmailTokenItems",
                column: "EmailTokenID");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTokens_Code_UserId",
                table: "EmailTokens",
                columns: new[] { "Code", "UserId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailTokenItems");

            migrationBuilder.DropTable(
                name: "EmailTokens");
        }
    }
}
