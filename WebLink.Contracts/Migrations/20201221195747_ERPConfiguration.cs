using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ERPConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ERPCompanyLocations",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompanyID = table.Column<int>(nullable: false),
                    ProductionLocationID = table.Column<int>(nullable: false),
                    ERPInstanceID = table.Column<int>(nullable: false),
                    Currency = table.Column<string>(nullable: true),
                    BillingFactoryCode = table.Column<string>(nullable: true),
                    ProductionFactoryCode = table.Column<string>(nullable: true),
                    AddressID = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ERPCompanyLocations", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ERPConfigs",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ERPConfigs", x => x.ID);
                });

            // add ERP Instace for SAGE ID 1
            migrationBuilder.Sql(@"
            INSERT INTO [dbo].[ERPConfigs]
           ([Name]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[UpdatedBy]
           ,[UpdatedDate])
            VALUES
           ('SAGE'
           ,'rafael.guerrero'
           ,'2020-12-21 00:00:00'
           ,'rafael.guerrero'
           ,'2020-12-21 00:00:00')
            ");

            migrationBuilder.Sql(@"
            INSERT INTO [dbo].[ERPCompanyLocations]
                       ([CompanyID]
                       ,[ProductionLocationID]
                       ,[ERPInstanceID]
                       ,[Currency]
                       ,[BillingFactoryCode]
                       ,[ProductionFactoryCode]
                       ,[AddressID]
                       ,[CreatedBy]
                       ,[CreatedDate]
                       ,[UpdatedBy]
                       ,[UpdatedDate])
            SELECT 
             c.ID as CompanyID
            , CASE WHEN l.FactoryCode = 'SUM01' THEN 29 ELSE l.ID END AS ProductionLocationID
            , 1 AS ERPInstanceID, 'EUR' AS Currency
            , l.FactoryCode AS BillingFactoryCode
            , l.FactoryCode AS ProductionFactoryCode
            , NULL AS AddressID
            , 'rafael.guerrero' AS CreatedBy
            , '2020-12-21 00:00:00' AS CreatedDate
            , 'rafael.guerrero' AS UpdatedBy
            , '2020-12-21 00:00:00' AS UpdatedDate

            FROM CompanyProviders prv
            LEFT JOIN Companies c ON prv.ProviderCompanyID = c.ID
            LEFT JOIN Locations l ON l.ID = prv.DefaultProductionLocation
            WHERE LEN(SageRef)>0 
            AND SyncWithSage = 1
            ");

            migrationBuilder.Sql(@"

            INSERT INTO [dbo].[ERPCompanyLocations]
                       ([CompanyID]
                       ,[ProductionLocationID]
                       ,[ERPInstanceID]
                       ,[Currency]
                       ,[BillingFactoryCode]
                       ,[ProductionFactoryCode]
                       ,[AddressID]
                       ,[CreatedBy]
                       ,[CreatedDate]
                       ,[UpdatedBy]
                       ,[UpdatedDate])

            SELECT 

            c.ID AS CompanyID
            , CASE WHEN l.FactoryCode = 'SUM01' THEN 29 ELSE ISNULL(l.ID, 1) END AS ProductionLocationID
            , 1 AS ERPInstanceID
            , 'EUR' AS Currency
            , ISNULL(l.FactoryCode, 'SDS01') AS BillingFactoryCode
            , ISNULL(l.FactoryCode, 'SDS01') AS ProductionFactoryCode
            , NULL AS AddressID
            , 'rafael.guerrero' AS CreatedBy
            , '2020-12-21 00:00:00' AS CreatedDate
            , 'rafael.guerrero' AS UpdatedBy
            , '2020-12-21 00:00:00' AS UpdatedDate
            FROM Companies c
            LEFT JOIN Locations l ON l.ID = c.DefaultProductionLocation
            LEFT JOIN CompanyProviders prv ON c.ID = prv.ProviderCompanyID
            WHERE LEN(SageRef)>0 
            AND SyncWithSage = 1 
            AND prv.ID IS NULL
            ");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ERPCompanyLocations");

            migrationBuilder.DropTable(
                name: "ERPConfigs");
        }
    }
}
