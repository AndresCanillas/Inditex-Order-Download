using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class FtpFileReceived : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FtpFilesReceived",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    FileName = table.Column<string>(nullable: true),
                    ProjectID = table.Column<int>(nullable: false),
                    FactoryID = table.Column<int>(nullable: false),
                    IsProcessed = table.Column<bool>(nullable: false),
                    UploadOrderDTO = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FtpFilesReceived", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EncodedLabels_CompanyID",
                table: "EncodedLabels",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_EncodedLabels_EPC",
                table: "EncodedLabels",
                column: "EPC");

            migrationBuilder.CreateIndex(
                name: "IX_EncodedLabels_OrderID",
                table: "EncodedLabels",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_EncodedLabels_ProjectID",
                table: "EncodedLabels",
                column: "ProjectID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FtpFilesReceived");

            migrationBuilder.DropIndex(
                name: "IX_EncodedLabels_CompanyID",
                table: "EncodedLabels");

            migrationBuilder.DropIndex(
                name: "IX_EncodedLabels_EPC",
                table: "EncodedLabels");

            migrationBuilder.DropIndex(
                name: "IX_EncodedLabels_OrderID",
                table: "EncodedLabels");

            migrationBuilder.DropIndex(
                name: "IX_EncodedLabels_ProjectID",
                table: "EncodedLabels");
        }
    }
}
