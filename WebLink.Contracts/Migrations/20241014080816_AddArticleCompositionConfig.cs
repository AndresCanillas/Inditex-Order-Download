using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class AddArticleCompositionConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleCompositionConfigs",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompanyID = table.Column<int>(nullable: false),
                    ProjectID = table.Column<int>(nullable: false),
                    ArticleID = table.Column<int>(nullable: false),
                    ArticleCode = table.Column<string>(nullable: true),
                    HeightInInches = table.Column<float>(nullable: false),
                    WidthInches = table.Column<float>(nullable: false),
                    LineNumber = table.Column<int>(nullable: false),
                    WithSeparatedPercentage = table.Column<bool>(nullable: false),
                    DefaultCompresion = table.Column<int>(nullable: false),
                    PPI = table.Column<int>(nullable: false),
                    IsSimpleAdditional = table.Column<bool>(nullable: false),
                    WidthAdditionalInInches = table.Column<float>(nullable: false),
                    SelectedLanguage = table.Column<string>(nullable: true),
                    ArticleCompositionCalculationType = table.Column<int>(nullable: false),
                    MaxPages = table.Column<int>(nullable: false),
                    MaxLinesToIncludeAdditional = table.Column<int>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleCompositionConfigs", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleCompositionConfigs");
        }
    }
}
