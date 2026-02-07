using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class EmailTokensRev1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenData",
                table: "EmailTokenItems");

            migrationBuilder.RenameColumn(
                name: "TokenType",
                table: "EmailTokenItems",
                newName: "OrderID");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "EmailTokens",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "EmailTokens");

            migrationBuilder.RenameColumn(
                name: "OrderID",
                table: "EmailTokenItems",
                newName: "TokenType");

            migrationBuilder.AddColumn<string>(
                name: "TokenData",
                table: "EmailTokenItems",
                nullable: true);
        }
    }
}
