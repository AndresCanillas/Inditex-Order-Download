using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class AddProviderTreeView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProviderRecordID",
                table: "CompanyOrders",
                nullable: true);

            migrationBuilder.Sql(
@"
exec sp_sqlexec N'
    -- =============================================
    -- Author:		Rafael Guerrero
    -- Create date: 2020-11-27
    -- Description:	Get Providers Tree hierarchy
    -- =============================================
    CREATE VIEW [dbo].[ProviderTrewView]
    	-- Add the parameters for the stored procedure here
    AS

    WITH Hierarchy(CompanyID, [Name], ClientReference, ParentCompanyID, TopParentID, Parents, DefaultBillingLocationID, DefaultProductionLocationID, Currency, SLADays, ProviderRecordID)
    AS
    (

    SELECT 
    		c.ID as CompanyID
    	, CAST(c.[Name] AS NVARCHAR(128)) as [Name]
    	, CAST(COALESCE(c.ClientReference, c.CompanyCode, '''') as NVARCHAR(12)) as ClientReference
    	, NULL as ParentCompanyID
    	, NULL as TopParentID
    	, CAST('''' AS VARCHAR(MAX)) as Parents
    	, NULL as DefaultBillingLocationID
    	, NULL as DefaultProductionLocationID
    	, CAST('''' as NVARCHAR(16)) as Currency
    	, NULL as SLADays
    	, NULL as ProviderRecordID
    FROM Companies c
    left join CompanyProviders p on c.Id = p.ProviderCompanyID
    where p.ID is null and c.IsBroker = 1

    UNION ALL
    SELECT 
    		p.ProviderCompanyID as CompanyID
    	, CAST(c.[Name] AS NVARCHAR(128)) as [Name]
    	, CAST(p.ClientReference AS NVARCHAR(12)) as ClientReference
    	, p.CompanyID as ParentCompanyID,
    CAST(CASE WHEN Parent.Parents =  ''''
    		THEN(p.CompanyID)
    		ELSE(Parent.TopParentID)
    	END AS INT),
    CAST(CASE WHEN Parent.Parents = ''.''
    		THEN(CAST(p.CompanyID AS VARCHAR(MAX)))
    		ELSE(Parent.Parents + ''.'' + CAST(p.CompanyID AS VARCHAR(MAX)) + ''.'')
    	END AS VARCHAR(MAX))
    	, p.DefaultProductionLocation as DefaultProductionLocationID
    	, p.BillingLocation as DefaultBillingLocationID
    	, CAST(ISNULL(p.Currency, ''EUR'') as NVARCHAR(16)) as Currency
    	, p.SLADays
    	, p.ID

    FROM CompanyProviders p
    INNER JOIN  Companies c on p.ProviderCompanyID = c.ID
    INNER JOIN Hierarchy as Parent ON p.CompanyID = Parent.CompanyID

    )
    SELECT * FROM Hierarchy'
GO");
            
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS [dbo].[ProviderTrewView]");

            migrationBuilder.DropColumn(
                name: "ProviderRecordID",
                table: "CompanyOrders");
        }
    }
}
