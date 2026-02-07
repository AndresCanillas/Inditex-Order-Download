using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class MangoRev2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabelArtifacts_Labels_LabelID",
                table: "LabelArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_LabelArtifacts_LabelID",
                table: "LabelArtifacts");

            migrationBuilder.DropColumn(
                name: "LabelID",
                table: "LabelArtifacts");

            migrationBuilder.AddColumn<int>(
                name: "ArticleID",
                table: "LabelArtifacts",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogType",
                table: "Catalogs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LabelArtifacts_ArticleID",
                table: "LabelArtifacts",
                column: "ArticleID");

            migrationBuilder.AddForeignKey(
                name: "FK_LabelArtifacts_Articles_ArticleID",
                table: "LabelArtifacts",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabelArtifacts_Articles_ArticleID",
                table: "LabelArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_LabelArtifacts_ArticleID",
                table: "LabelArtifacts");

            migrationBuilder.DropColumn(
                name: "ArticleID",
                table: "LabelArtifacts");

            migrationBuilder.DropColumn(
                name: "CatalogType",
                table: "Catalogs");

            migrationBuilder.AddColumn<int>(
                name: "LabelID",
                table: "LabelArtifacts",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LabelArtifacts_LabelID",
                table: "LabelArtifacts",
                column: "LabelID");

            migrationBuilder.AddForeignKey(
                name: "FK_LabelArtifacts_Labels_LabelID",
                table: "LabelArtifacts",
                column: "LabelID",
                principalTable: "Labels",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
