using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class UpdateOrderProviderRecordID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Esta actualizacion no corrige el caso cuando un proveedor le sirve a varias compañias, lo resolvi manual en el server
            // caso LAYSO y MALHAPERTADA para mango
            migrationBuilder.Sql(@"
UPDATE o 
SET ProviderRecordID = v.ProviderRecordID
--select o.ID, o.OrderNumber, o.SendToCompanyID, o.ProviderRecordID, v.CompanyID, v.ProviderRecordID, v.CompanyID, v.Name
FROM CompanyOrders o
LEFT JOIN ProviderTrewView v ON o.SendToCompanyID = v.CompanyID
WHERE v.TopParentID = o.CompanyID AND o.ProviderRecordID IS NULL
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
