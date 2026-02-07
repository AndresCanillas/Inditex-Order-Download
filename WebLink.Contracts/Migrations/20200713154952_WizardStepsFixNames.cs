using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class WizardStepsFixNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"  UPDATE [dbo].[WizardSteps] SET [Name] = 'Quantities'  WHERE Position = 0");
            migrationBuilder.Sql(@"  UPDATE [dbo].[WizardSteps] SET [Name] = 'Add Extras'  WHERE Position = 1");
            migrationBuilder.Sql(@"  UPDATE [dbo].[WizardSteps] SET [Name] = 'Delivery Address'  WHERE Position = 2");
            migrationBuilder.Sql(@"  UPDATE [dbo].[WizardSteps] SET [Name] = 'Confirm Order'  WHERE Position = 3");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
