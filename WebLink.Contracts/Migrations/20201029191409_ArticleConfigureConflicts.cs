using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class ArticleConfigureConflicts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableConflicts",
                table: "Articles",
                nullable: false,
                defaultValue: false);


            migrationBuilder.Sql(@"
            UPDATE a SET a.EnableConflicts = 0
            FROM Articles a
            INNER JOIN Projects p on a.ProjectID = p.ID
            Where p.BrandID = (SELECT TOP 1 b.ID FROM Brands b where b.[Name] Like 'Mango')
            AND a.LabelID IS NOT NULL");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableConflicts",
                table: "Articles");
        }
    }
}
