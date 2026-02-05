using System;
using System.Collections.Generic;
using System.Text;


/*
 * Specification
 * ========================== 
 * Custom encoding scheme, used exclusively for brands and companies by Mayoral
 * 
 * 
 * Internal Structure
 * ========================== 
 * | Prefix | Season | Year   | Article | Color   | Size   | Order   | Serial  |
 * |--------|--------|--------|---------|---------|--------|---------|---------|
 * | 8 bits | 4 bits | 4 bits | 20 bits | 12 bits | 8 Bits | 20 bits | 20 bits |
 * 
 * - Prefix: fixed value 77
 * - Season: 1 digit integer value
 * - Year:   1 digit integer value
 * - Article: 5 digits integer value
 * - Color: 3 digits integer value
 * - Size: 2 digits integer value. From source file some times come 1 digit only
 * - Order: 5 digits integer value
 * - Serial: 5 digits integer value
 * 
 * URN format
 * ==========================
 * TODO: to be defined
 */

namespace Service.Contracts
{
    public class Mayoral96 : TagEncoding, ITagEncoding
    {
        public override int TypeID { get => 13; }
        public override string Name { get => "MAYORAL96v1"; }
        public override string UrnNameSpace { get => "XXXXXX"; }
        protected override string UrnPattern { get => "XXXXXX"; }

        public Mayoral96()
        {
            fields = new BitFieldInfo[] {
                new BitFieldInfo("Prefix",          0,  8,  BitFieldFormat.Decimal, true, false, "77", null),
                new BitFieldInfo("Season",          8,  4,  BitFieldFormat.Decimal, true, false, "1", null),
                new BitFieldInfo("Year",            12, 4,  BitFieldFormat.Decimal, true, false, "1", null),
                new BitFieldInfo("ArticleCode",     16, 20, BitFieldFormat.Decimal, true, false, "1", null),
                new BitFieldInfo("ColorCode",       36, 12, BitFieldFormat.Decimal, true, false, "1", null),
                new BitFieldInfo("Size",            48, 8,  BitFieldFormat.Decimal, true, false, "1", null),
                new BitFieldInfo("OrderNumber",     56, 20, BitFieldFormat.Decimal, true, false, "1", null),
                new BitFieldInfo("SerialNumber",    76, 20, BitFieldFormat.Decimal, true, false, "1", null),

            };
        }

        /// <summary>
		/// Retrieves the hexadecimal representation of the code.
		/// </summary>
		public string ToHex()
        {
            string hex;
            int bitlen = 0;
            var sb = new StringBuilder(24);
            for(int i = 0; i<fields.Length; i++)
            {
                bitlen = fields[i].BitLength / 4;
                hex = string.Format("{0:X6}", Convert.ToInt32(fields[i].Value));
                hex = hex.Substring(hex.Length - bitlen);
                sb.Append(hex);
            }

            return sb.ToString();
        }
    }
}
