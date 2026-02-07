using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ChangeNameColumnInProjectTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsReadOnly",
                table: "Projects",
                newName: "AllowEditQuantity");
            migrationBuilder.Sql("Update Projects set AllowEditQuantity=1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AllowEditQuantity",
                table: "Projects",
                newName: "IsReadOnly");
        }
    }
}
