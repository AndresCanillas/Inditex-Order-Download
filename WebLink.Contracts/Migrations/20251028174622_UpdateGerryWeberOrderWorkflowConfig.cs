using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class UpdateGerryWeberOrderWorkflowConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"update OrderWorkflowConfigs set SerializedConfig = '{""OrderReceivedPlugin"":{""_impl"":""GerryWeberOrderReceivedPlugin"",""_data"":{""Compression"":""10"",""CharactersByLine"":""45"",""Page1Lines"":""32"",""Page2Lines"":""55"",""Page3Lines"":""45"",""Page4Lines"":""45"",""Page5Lines"":""45"",""Page6Lines"":""45"",""Page7Lines"":""45"",""Page8Lines"":""45"",""LangsCodeMappingColumns"":""{\""DE\"": \""GERMAN\"", \""EN\"": \""English\"", \""ES\"": \""SPANISH\"", \""FR\"": \""FRENCH\"", \""NL\"": \""DUTCH\"", \""RU\"": \""RUSSIAN\"", \""PL\"": \""POLISH\"", \""AR\"": \""ARABIC\"", \""ET\"": \""ESTONIAN\"", \""SV\"": \""SWEDISH\"", \""KA\"": \""GEORGIAN\"", \""HU\"": \""HUNGARIAN\"", \""CZ\"": \""CZECH\"", \""RO\"": \""ROMANIAN\"", \""BA\"": \""BOSNIAN\"", \""MK\"": \""MACEDONIAN\"", \""SR\"": \""SERBIAN\"", \""UK\"": \""UKRAINIAN\"", \""NO\"": \""NORWEGIAN\"", \""BG\"": \""BULGARIO\"",\""LV\"":\""LATVIAN\"",\""LT\"":\""LITHUANIAN\"", \""HR\"": \""CROATIAN\"",\""SK\"":\""SLOVAK\""}""}},""PreValidationPlugin"":{},""OrderValidatedPlugin"":{},""OrderReadyToPrintPlugin"":{},""ReverseflowStrategy"":{}}' where ID = 47 ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"update OrderWorkflowConfigs set SerializedConfig = '{""OrderReceivedPlugin"":{""_impl"":""GerryWeberOrderReceivedPlugin"",""_data"":{}},""PreValidationPlugin"":{""_impl"":""GerryWeberPreValidationPlugin"",""_data"":{""Compression"":""10"",""CharactersByLine"":""45"",""Page1Lines"":""32"",""Page2Lines"":55,""Page3Lines"":""45"",""Page4Lines"":""45"",""Page5Lines"":""45"",""Page6Lines"":""45"",""Page7Lines"":""45"",""Page8Lines"":""45"",""LangsCodeMappingColumns"":""{\""DE\"": \""GERMAN\"", \""EN\"": \""English\"", \""ES\"": \""SPANISH\"", \""FR\"": \""FRENCH\"", \""NL\"": \""DUTCH\"", \""RU\"": \""RUSSIAN\"", \""PL\"": \""POLISH\"", \""AR\"": \""ARABIC\"", \""ET\"": \""ESTONIAN\"", \""SV\"": \""SWEDISH\"", \""KA\"": \""GEORGIAN\"", \""HU\"": \""HUNGARIAN\"", \""CZ\"": \""CZECH\"", \""RO\"": \""ROMANIAN\"", \""BA\"": \""BOSNIAN\"", \""MK\"": \""MACEDONIAN\"", \""SR\"": \""SERBIAN\"", \""UK\"": \""UKRAINIAN\"", \""NO\"": \""NORWEGIAN\"", \""BG\"": \""BULGARIO\"",\""LV\"":\""LATVIAN\"",\""LT\"":\""LITHUANIAN\"", \""HR\"": \""CROATIAN\"",\""SK\"":\""SLOVAK\""}""}},""OrderValidatedPlugin"":{},""OrderReadyToPrintPlugin"":{},""ReverseflowStrategy"":{}}' where ID = 47");
        }
    }
}
