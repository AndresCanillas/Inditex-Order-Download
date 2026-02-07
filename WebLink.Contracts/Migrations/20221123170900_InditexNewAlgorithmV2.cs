using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class InditexNewAlgorithmV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // change name to the current configuration to InditexPerfumeryEncodingAlgorithm
            migrationBuilder.Sql(@"
            UPDATE rp SET SerializedConfig = JSON_MODIFY(rp.SerializedConfig, '$.Algorithm._impl', 'InditexPerfumeryEncodingAlgorithm')
            FROM[dbo].[RFIDParameters] rp
            WHERE JSON_VALUE(rp.SerializedConfig, '$.Algorithm._impl') = 'InditexV2EncodingAlgorithm';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            UPDATE rp SET SerializedConfig = JSON_MODIFY(rp.SerializedConfig, '$.Algorithm._impl', 'InditexV2EncodingAlgorithm')
            FROM[dbo].[RFIDParameters] rp
            WHERE JSON_VALUE(rp.SerializedConfig, '$.Algorithm._impl') = 'InditexPerfumeryEncodingAlgorithm';
            ");
        }
    }
}
