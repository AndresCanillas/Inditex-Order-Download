using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class UpdateProviderTreeView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER VIEW [dbo].[ProviderTrewView]
	-- Add the parameters for the stored procedure here
AS

WITH Hierarchy(CompanyID, [Name], ClientReference, ParentCompanyID, TopParentID, Parents, DefaultBillingLocationID, DefaultProductionLocationID, Currency, SLADays, ProviderRecordID)
AS
(

SELECT 
		c.ID as CompanyID
	, CAST(c.[Name] AS NVARCHAR(128)) as [Name]
	, CAST(COALESCE(c.ClientReference, c.CompanyCode, '') as NVARCHAR(12)) as ClientReference
	, NULL as ParentCompanyID
	, NULL as TopParentID
	, CAST('' AS VARCHAR(MAX)) as Parents
	, NULL as DefaultBillingLocationID
	, NULL as DefaultProductionLocationID
	, CAST('' as NVARCHAR(16)) as Currency
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
CAST(CASE WHEN Parent.Parents =  ''
		THEN(p.CompanyID)
		ELSE(Parent.TopParentID)
	END AS INT),
CAST(CASE WHEN Parent.Parents = '.'
		THEN(CAST(p.CompanyID AS VARCHAR(MAX)))
		ELSE(Parent.Parents + '.' + CAST(p.CompanyID AS VARCHAR(MAX)) + '.')
	END AS VARCHAR(MAX))
	, NULL as DefaultBillingLocationID
	, p.DefaultProductionLocation as DefaultProductionLocationID
	, CAST('EUR' as NVARCHAR(16)) as Currency
	, p.SLADays
	, p.ID

FROM CompanyProviders p
INNER JOIN  Companies c on p.ProviderCompanyID = c.ID
INNER JOIN Hierarchy as Parent ON p.CompanyID = Parent.CompanyID

)
SELECT * FROM Hierarchy

");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
