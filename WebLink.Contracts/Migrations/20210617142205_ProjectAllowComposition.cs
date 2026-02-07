using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ProjectAllowComposition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AlwaysRequireCompositionValidation",
                table: "Projects",
                newName: "AllowAddOrChangeComposition");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AllowAddOrChangeComposition",
                table: "Projects",
                newName: "AlwaysRequireCompositionValidation");
        }
    }
}
