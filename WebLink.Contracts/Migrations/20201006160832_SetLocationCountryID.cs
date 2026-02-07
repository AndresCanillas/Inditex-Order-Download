using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class SetLocationCountryID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // set Spain Counry ID as default
            migrationBuilder.Sql(@"update l
                                  set CountryID = ISNULL(c.ID, 210)
                                  FROM [dbo].[Locations] l
                                  left join [dbo].[Countries] c on l.Country like c.[Name]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"update l
                                  set CountryID = NULL
                                  FROM [dbo].[Locations] l
                                  ");
        }
    }
}
