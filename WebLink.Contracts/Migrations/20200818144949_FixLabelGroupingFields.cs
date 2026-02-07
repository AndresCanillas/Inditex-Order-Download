using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class FixLabelGroupingFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [dbo].[Artifacts] ALTER COLUMN SageRef VARCHAR(16)");
            migrationBuilder.Sql("UPDATE [dbo].[Labels] SET [GroupingFields] = '{\"GroupingFields\":\"\",\"DisplayFields\":\"\"}' WHERE [GroupingFields] IS NULL OR LEN([GroupingFields]) < 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [dbo].[Artifacts] ALTER COLUMN SageRef VARCHAR(MAX)");
            migrationBuilder.Sql("UPDATE [dbo].[Labels] SET [GroupingFields] = NULL WHERE [GroupingFields] = '{\"GroupingFields\":\"\",\"DisplayFields\":\"\"}'");
        }
    }
}
