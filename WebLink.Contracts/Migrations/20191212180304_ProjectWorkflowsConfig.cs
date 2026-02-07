using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ProjectWorkflowsConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.AlterColumn<int>(
                name: "AllowQuantityEdition",
                table: "Projects",
                nullable: false,
                oldClrType: typeof(bool),
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "AllowExtrasDuringValidation",
                table: "Projects",
                nullable: false,
                oldClrType: typeof(bool),
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "AllowQuantityEdition",
                table: "Projects",
                nullable: false,
                oldClrType: typeof(int),
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "AllowExtrasDuringValidation",
                table: "Projects",
                nullable: false,
                oldClrType: typeof(int),
                defaultValue: false);
        }
    }
}
