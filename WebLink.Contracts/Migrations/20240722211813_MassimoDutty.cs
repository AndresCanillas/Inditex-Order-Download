using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class MassimoDutty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableOrderPool",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnablePoolFile",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OrderPoolFileProcessor",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoolFileHandler",
                table: "Projects",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ManualEntryForms",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProjectID = table.Column<int>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualEntryForms", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OrderPools",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProjectID = table.Column<int>(nullable: false),
                    OrderNumber = table.Column<string>(maxLength: 20, nullable: true),
                    Seasson = table.Column<string>(maxLength: 20, nullable: true),
                    Year = table.Column<int>(nullable: false),
                    ProviderCode1 = table.Column<string>(maxLength: 20, nullable: true),
                    ProviderName1 = table.Column<string>(maxLength: 50, nullable: true),
                    ProviderCode2 = table.Column<string>(maxLength: 20, nullable: true),
                    ProviderName2 = table.Column<string>(maxLength: 50, nullable: true),
                    Size = table.Column<string>(maxLength: 10, nullable: true),
                    ColorCode = table.Column<string>(maxLength: 10, nullable: true),
                    ColorName = table.Column<string>(maxLength: 30, nullable: true),
                    Price1 = table.Column<string>(nullable: true),
                    Price2 = table.Column<string>(nullable: true),
                    Quantity = table.Column<int>(nullable: false),
                    ArticleCode = table.Column<string>(maxLength: 30, nullable: true),
                    CategoryCode1 = table.Column<string>(maxLength: 10, nullable: true),
                    CategoryText1 = table.Column<string>(maxLength: 100, nullable: true),
                    CategoryCode2 = table.Column<string>(maxLength: 10, nullable: true),
                    CategoryText2 = table.Column<string>(maxLength: 100, nullable: true),
                    CategoryCode3 = table.Column<string>(maxLength: 10, nullable: true),
                    CategoryText3 = table.Column<string>(maxLength: 100, nullable: true),
                    CategoryCode4 = table.Column<string>(maxLength: 10, nullable: true),
                    CategoryText4 = table.Column<string>(maxLength: 100, nullable: true),
                    CategoryCode5 = table.Column<string>(maxLength: 10, nullable: true),
                    CategoryText5 = table.Column<string>(maxLength: 100, nullable: true),
                    CategoryCode6 = table.Column<string>(maxLength: 10, nullable: true),
                    CategoryText6 = table.Column<string>(maxLength: 100, nullable: true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(nullable: true),
                    ExpectedProductionDate = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPools", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OrderPools_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPools_ProjectID",
                table: "OrderPools",
                column: "ProjectID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualEntryForms");

            migrationBuilder.DropTable(
                name: "OrderPools");

            migrationBuilder.DropColumn(
                name: "EnableOrderPool",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EnablePoolFile",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OrderPoolFileProcessor",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PoolFileHandler",
                table: "Projects");
        }
    }
}
