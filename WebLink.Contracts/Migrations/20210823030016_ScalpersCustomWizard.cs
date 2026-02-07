using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ScalpersCustomWizard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"

            DECLARE @projectID INT
            DECLARE @companyID INT
            DECLARE @brandID INT

            SELECT DISTINCT TOP (1) @projectID = p.ID, @brandID = b.ID, @companyID = c.ID from Projects p
            LEFT JOIN Brands b ON p.BrandID = b.ID
            LEFT JOIN Companies c ON b.CompanyID = c.ID
            WHERE c.Name LIKE '%Scalper%'

            UPDATE [dbo].[WizardCustomSteps] SET [Type] = 5 WHERE ProjectID = @projectID AND [Type] = 5

            IF @@ROWCOUNT = 0
            BEGIN
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
			            (null
			            ,null
			            ,@projectID
			            ,5 -- See WizardStepType enum
			            ,'/validation/Scalpers/SetArticlesToOrderWizard.js'
			            ,'rafael.guerrero'
			            ,GETDATE()
			            ,'rafael.guerrero'
			            ,GETDATE()
			            ,5
			            ,'Set Articles'
			            ,'Order Data'
			            )

            END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            DECLARE @projectID INT
            DECLARE @companyID INT
            DECLARE @brandID INT

            SELECT DISTINCT TOP (1) @projectID = p.ID, @brandID = b.ID, @companyID = c.ID from Projects p
            LEFT JOIN Brands b ON p.BrandID = b.ID
            LEFT JOIN Companies c ON b.CompanyID = c.ID
            WHERE c.Name LIKE '%Scalper%'

            DELETE FROM [dbo].[WizardCustomSteps] WHERE ProjectID = @projectID AND [Type] = 5
            ");
        }
    }
}
