using System;
using System.Collections.Generic;
using System.Text;

/*
 * 
 * Specification
 * ========================== 
 * 
 * Store in chip only the serial value using all bits 
 * TODO: change the name of the class, this tag was created for EKOI, and now store the EAN13 (without checkdigit - 12 characters) + serial (6 digits) -> like code128 barcode
 * 
 */
namespace Service.Contracts
{
    public class SimpleSerialTagEncoding : TagEncoding, ITagEncoding
    {
        public override int TypeID
        {
            get { return 16; }
        }

        public override string Name
        {
            get { return "sste-96"; }
        }

        public override string UrnNameSpace
        {
            get { return "urn:epc:sste-x96:CompanyPrefix.Serial"; }// serial-x96  sste = SimpleSerialTagEncoding96, x = custom, 96 = available bits, is a harcode name from imagination
        }

        protected override string UrnPattern
        {
            get { return @"^urn:epc:sste-x96:(\w{2}\d{11})"; }
        }

        public SimpleSerialTagEncoding()
        {
            
            fields = new BitFieldInfo[]{
                new BitFieldInfo("Serial",        0, 96,  BitFieldFormat.Hexadecimal, false, true,  "48", null),
            };
        }
    }

}
