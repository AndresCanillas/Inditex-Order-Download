using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class AddCompanyIDAndFormTypeToManualEntryForms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "ManualEntryForms",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FormType",
                table: "ManualEntryForms",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "ManualEntryForms");

            migrationBuilder.DropColumn(
                name: "FormType",
                table: "ManualEntryForms");
        }
    }
}
