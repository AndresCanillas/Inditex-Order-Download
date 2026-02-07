using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class WizardStepsNameAndDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WizardSteps",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WizardSteps",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WizardCustomSteps",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WizardCustomSteps",
                nullable: true);


            migrationBuilder.Sql(@"  UPDATE [dbo].[WizardSteps] SET [Name] = 'Validate Quantities'  WHERE Position = 0");
            migrationBuilder.Sql(@"  UPDATE [dbo].[WizardSteps] SET [Name] = 'Add Extras'  WHERE Position = 1");
            migrationBuilder.Sql(@"  UPDATE [dbo].[WizardSteps] SET [Name] = 'Validate Delivery Address'  WHERE Position = 2");
            migrationBuilder.Sql(@"  UPDATE [dbo].[WizardSteps] SET [Name] = 'Confirm Order'  WHERE Position = 3");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "WizardSteps");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "WizardSteps");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "WizardCustomSteps");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "WizardCustomSteps");
        }
    }
}
