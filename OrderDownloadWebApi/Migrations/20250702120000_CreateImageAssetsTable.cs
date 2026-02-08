using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OrderDownloadWebApi.Migrations
{
    public partial class CreateImageAssetsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageAssets",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    Hash = table.Column<string>(nullable: true),
                    ContentType = table.Column<string>(nullable: true),
                    Content = table.Column<byte[]>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    IsLatest = table.Column<bool>(nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageAssets", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageAssets_Url_Hash",
                table: "ImageAssets",
                columns: new[] { "Url", "Hash" },
                unique: true,
                filter: "[Url] IS NOT NULL AND [Hash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImageAssets_Url_IsLatest",
                table: "ImageAssets",
                columns: new[] { "Url", "IsLatest" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageAssets");
        }
    }
}
