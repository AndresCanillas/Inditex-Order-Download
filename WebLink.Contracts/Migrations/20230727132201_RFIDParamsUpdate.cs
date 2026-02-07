using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class RFIDParamsUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"
				UPDATE RFIDParameters
				SET SerializedConfig = '{""Process"":{""_impl"": ""AllocateSerials"",""_data"":' + SerializedConfig + '}}'
				WHERE SerializedConfig LIKE '{""Algorithm"":{%'
			");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"
				UPDATE RFIDParameters
				SET SerializedConfig = SUBSTRING(SerializedConfig, 48, LEN(SerializedConfig)-49)
				WHERE SerializedConfig LIKE '{""Process"":{%'
			");
		}
    }
}
