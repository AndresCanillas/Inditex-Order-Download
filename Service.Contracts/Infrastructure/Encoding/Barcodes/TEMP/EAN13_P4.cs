using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
    /*
	 * Also known as GTIN-13, is a 13 digit code composed of:
	 *	> 2 digit country code
	 *	> 5 digit company code
	 *	> 5 digit product code
	 *	> 1 check digit
	 *	
	 *	NOTE: The country code is interpreted as part of the company prefix when encoding a tag. Therefore, all tags 
	 *	using EAN-13 must specify a partition of 5 (so the company prefix can store 7 digits).
	 *	
	 *	Check digit is calculated using the standard MOD algorithm from GS1.
	 */
    public class EAN13_P4 : Barcode1D, IBarcode1D, IConfigurable<EmptyConfig>
    {
        private string companyCode;
        private string productCode;

        public override string Code
        {
            get
            {
                return base.Code;
            }
            set
            {
                if (value == null)
                    throw new InvalidOperationException("Cannot set EAN13.Code to null");
                if (!value.IsNumeric() || value.Length < 12 || value.Length > 13)
                    throw new InvalidOperationException($"Value {value} is not a valid EAN13.");
                if (value.Length == 12)
                {
                    value += GetCheckDigit(value);
                }
                else
                {
                    var checkDigit = GetCheckDigit(value.Substring(0, 12));
                    if (checkDigit != value[12])
                        throw new InvalidOperationException($"Invalid EAN13 ({value}): Check Digit is invalid.");
                }
                base.Code = value;
            }
        }

        public override void Encode(ITagEncoding tag, string code)
        {
            if (!tag.ContainsField("Partition") || !tag.ContainsField("CompanyPrefix") || !tag.ContainsField("ItemReference"))
                throw new InvalidOperationException($"Invalid TagEncoding Scheme {tag.Name}. EAN-13 barcode requires a TagEncoding Scheme that supports fields 'Partition', 'CompanyPrefix' and 'ItemReference'.");
            if (code.Length < 12 || code.Length > 13)
                throw new InvalidOperationException($"Invalid EAN-13 code: '{code}'. Expected either a 12 or 13 digit code.");
            Code = code;
            companyCode = Code.Substring(0, 8);
            productCode = Code.Substring(8, 4);
            tag["Partition"].Value = "4";
            tag["CompanyPrefix"].Value = companyCode;
            tag["ItemReference"].Value = productCode;
        }

        public override void CodeUpdated(string code)
        {
            if (String.IsNullOrWhiteSpace(code))
            {
                companyCode = "";
                productCode = "";
            }
            else
            {
                if (code.Length < 12 || code.Length > 13)
                    throw new InvalidOperationException("The specified code is not a valid EAN13");
                companyCode = code.Substring(0, 8);
                productCode = code.Substring(8, 4);
            }
        }

        public string CompanyCode { get { return companyCode; } }
        public string ProductCode { get { return productCode; } }

        public EmptyConfig GetConfiguration()
        {
            return EmptyConfig.Value;
        }

        public void SetConfiguration(EmptyConfig config)
        {
        }
    }
}
