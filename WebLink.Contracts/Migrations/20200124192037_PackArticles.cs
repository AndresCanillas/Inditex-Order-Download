using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class PackArticles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "PackArticles",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_ArticleID",
                table: "Artifacts",
                column: "ArticleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Artifacts_Articles_ArticleID",
                table: "Artifacts",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Artifacts_Articles_ArticleID",
                table: "Artifacts");

            migrationBuilder.DropIndex(
                name: "IX_Artifacts_ArticleID",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "PackArticles");
        }
    }
}
