using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class SelectorType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrintCountSelectorType",
                table: "Articles",
                nullable: false,
                defaultValue: 0);

			migrationBuilder.Sql("update Articles set PrintCountSelectorType = 1 where PrintCountSelectorField = 'Barcode'");
			migrationBuilder.Sql("update SerialSequences set Filter = SUBSTRING(Filter, 1, 12) where len(Filter) = 13");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintCountSelectorType",
                table: "Articles");
        }
    }
}
