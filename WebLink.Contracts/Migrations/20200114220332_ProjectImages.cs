using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ProjectImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupFileColumn_Projects_ProjectId",
                table: "GroupFileColumn");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupFileColumn",
                table: "GroupFileColumn");

            migrationBuilder.RenameTable(
                name: "GroupFileColumn",
                newName: "GroupFileColumns");

            migrationBuilder.RenameIndex(
                name: "IX_GroupFileColumn_ProjectId",
                table: "GroupFileColumns",
                newName: "IX_GroupFileColumns_ProjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupFileColumns",
                table: "GroupFileColumns",
                column: "ID");

            migrationBuilder.CreateTable(
                name: "ProjectImages",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    ProjectID = table.Column<int>(nullable: true),
                    Extension = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectImages", x => x.ID);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_GroupFileColumns_Projects_ProjectId",
                table: "GroupFileColumns",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupFileColumns_Projects_ProjectId",
                table: "GroupFileColumns");

            migrationBuilder.DropTable(
                name: "ProjectImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupFileColumns",
                table: "GroupFileColumns");

            migrationBuilder.RenameTable(
                name: "GroupFileColumns",
                newName: "GroupFileColumn");

            migrationBuilder.RenameIndex(
                name: "IX_GroupFileColumns_ProjectId",
                table: "GroupFileColumn",
                newName: "IX_GroupFileColumn_ProjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupFileColumn",
                table: "GroupFileColumn",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupFileColumn_Projects_ProjectId",
                table: "GroupFileColumn",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
