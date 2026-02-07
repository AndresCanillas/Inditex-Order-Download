using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class FixRFIDConfigSelectorType : Migration
    {
		// Flip the value of SelectorType from 1 to 0, this was done because we changed the SelectorType enumeration  
		//
		//	from:
		//		{ String=0, EAN13=1 }
		//
		//	to:
		//		{ EAN13=0, String=1 }
		//
		// This works ok because currently all MultiserialSequences that use the selector field "Barcode"
		// should be using the selector type EAN13 (0)

		protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"

				update RFIDParameters 
					set SerializedConfig = JSON_MODIFY(SerializedConfig, '$.Algorithm._data.Sequence._data.SelectorType', '0')
				where
					Json_value(SerializedConfig, '$.Algorithm._data.Sequence._data.SelectorField') = 'Barcode' and
					Json_value(SerializedConfig, '$.Algorithm._data.Sequence._data.SelectorType') = '1'

			");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
