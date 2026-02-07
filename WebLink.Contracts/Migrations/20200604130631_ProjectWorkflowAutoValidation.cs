using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ProjectWorkflowAutoValidation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnableAuthorizationWorkflow",
                table: "Projects",
                newName: "TakeOrdersAsValid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TakeOrdersAsValid",
                table: "Projects",
                newName: "EnableAuthorizationWorkflow");
        }
    }
}
