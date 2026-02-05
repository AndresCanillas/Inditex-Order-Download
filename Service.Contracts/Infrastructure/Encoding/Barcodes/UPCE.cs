using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
/*
 * From: http://www.barcodeisland.com/upce.phtml   (copied here in case site goes missing)
 * 
 * UPC-E uses a rather convoluted, but quite effective, method of compressing out unnecessary zeros. Keep in mind that in UPC-A there are
 * five characters for the manufacturer code and five characters for the product code. The trick is to reduce all 10 characters into just
 * 6 characters. How?
 * 
 * CONVERTING A UPC-A CODE TO UPC-E
 * --------------------------------
 * 1) If the manufacturer code ends in 000, 100, or 200, the UPC-E code consists of the first two characters of the manufacturer code, the
 *    last three characters of the product code, followed by the third character of the manufacturer code. The product code must be 00000 
 *    to 00999.
 * 2) If the manufacturer code ends in 00 but does not qualify for #1 above, the UPC-E code consists of the first three characters of the
 *    manufacturer code, the last two characters of the product code, followed by the digit "3". The product code must be 00000 to 00099.
 * 3) If the manufacturer code ends in 0 but does not quality for #1 or #2 above, the UPC-E code consists of the first four characters of
 *    the manufacturer code, the last character of the product code, followed by the digit "4". The product code must be 00000 to 00009.
 * 4) If the manufacturer code does not end in zero, the UPC-E code consists of the entire manufacturer code and the last digit of the product
 *    code. Note that the last digit of the product code must be in the range of 5 through 9. The product code must be 00005 to 00009.
 *    
 * Examples:
 * 
 * UPC-A		UPC-E EQUIV.
 * 12000-00789	127890
 * 12100-00789	127891
 * 12200-00789	127892
 * 12300-00089	123893
 * 12400-00089	124893
 * 12500-00089	125893
 * 12600-00089	126893
 * 12700-00089	127893
 * 12800-00089	128893
 * 12900-00089	129893
 * 12910-00009	129194
 * 12911-00005	129115
 * 12911-00006	129116
 * 12911-00007	129117
 * 12911-00008	129118
 * 12911-00009	129119
*/
	//Pending implementation
	public class UPCE
	{
	}
}
