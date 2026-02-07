using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class UpdateClientProjectConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AllowAddingArticlesToOrder",
                table: "Projects",
                newName: "EnableMultipleFiles");

            migrationBuilder.AddColumn<bool>(
                name: "AlwaysRequireCompositionValidation",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UpdateType",
                table: "Projects",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GroupFileColumn",
                columns: table => new
                {
                    ProjectId = table.Column<int>(nullable: false),
                    TableName = table.Column<string>(nullable: true),
                    Key = table.Column<string>(nullable: true),
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupFileColumn", x => x.ID);
                    table.ForeignKey(
                        name: "FK_GroupFileColumn_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupFileColumn_ProjectId",
                table: "GroupFileColumn",
                column: "ProjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupFileColumn");

            migrationBuilder.DropColumn(
                name: "AlwaysRequireCompositionValidation",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdateType",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "EnableMultipleFiles",
                table: "Projects",
                newName: "AllowAddingArticlesToOrder");
        }
    }
}
