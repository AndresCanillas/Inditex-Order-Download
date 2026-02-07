using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class DynamicPacks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackArticles_Articles_ArticleID",
                table: "PackArticles");

            migrationBuilder.AlterColumn<int>(
                name: "ArticleID",
                table: "PackArticles",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<int>(
                name: "CatalogID",
                table: "PackArticles",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Condition",
                table: "PackArticles",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FieldName",
                table: "PackArticles",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mapping",
                table: "PackArticles",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PluginName",
                table: "PackArticles",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "PackArticles",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PackArticles_CatalogID",
                table: "PackArticles",
                column: "CatalogID");

            migrationBuilder.AddForeignKey(
                name: "FK_PackArticles_Articles_ArticleID",
                table: "PackArticles",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PackArticles_Catalogs_CatalogID",
                table: "PackArticles",
                column: "CatalogID",
                principalTable: "Catalogs",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            // default type for current packs is ByArticle
            migrationBuilder.Sql(@"UPDATE PackArticles SET [Type] = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackArticles_Articles_ArticleID",
                table: "PackArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_PackArticles_Catalogs_CatalogID",
                table: "PackArticles");

            migrationBuilder.DropIndex(
                name: "IX_PackArticles_CatalogID",
                table: "PackArticles");

            migrationBuilder.DropColumn(
                name: "CatalogID",
                table: "PackArticles");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "PackArticles");

            migrationBuilder.DropColumn(
                name: "FieldName",
                table: "PackArticles");

            migrationBuilder.DropColumn(
                name: "Mapping",
                table: "PackArticles");

            migrationBuilder.DropColumn(
                name: "PluginName",
                table: "PackArticles");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "PackArticles");

            migrationBuilder.AlterColumn<int>(
                name: "ArticleID",
                table: "PackArticles",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PackArticles_Articles_ArticleID",
                table: "PackArticles",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
