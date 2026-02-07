using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class eseOeseCustomCompoWizard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            INSERT INTO [dbo].[WizardCustomSteps]
               ([CompanyID]
               ,[BrandID]
               ,[ProjectID]
               ,[Type]
               ,[Url]
               ,[CreatedBy]
               ,[CreatedDate]
               ,[UpdatedBy]
               ,[UpdatedDate]
               ,[Position]
               ,[Description]
               ,[Name])
            VALUES
               (4679
               ,null
               ,null
               ,15
               ,'/validation/EseOEse/LabellingCompoSimpleWizard.js'
               ,'rafael.guerrero'
               ,'2026-01-19 12:45:00'
               ,'rafael.guerrero'
               ,'2026-01-19 12:45:00'
               ,15
               ,'Define Composition'
               ,'Define Composition')
            ");

            migrationBuilder.Sql(@"
            UPDATE ws SET [Url]='/validation/EseOEse/LabellingCompoSimpleWizard.js'

            FROM CompanyOrders o
            INNER JOIN Wizards w ON o.ID = w.OrderID
            INNER JOIN WizardSteps ws ON w.ID = ws.WizardID
            WHERE ws.[Type] = 15 
            AND o.[OrderStatus] NOT IN (7,6)
            AND o.[CompanyID] = 4679
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [WizardCustomSteps] WHERE [CompanyID] = 4679 AND [Type] = 15");

            migrationBuilder.Sql(@"
            UPDATE ws SET [Url]='/validation/Common/LabellingCompoSimpleWizard.js'

            FROM CompanyOrders o
            INNER JOIN Wizards w ON o.ID = w.OrderID
            INNER JOIN WizardSteps ws ON w.ID = ws.WizardID
            WHERE ws.[Type] = 15 
            AND o.[CompanyID] = 4679
");
        }
    }
}
