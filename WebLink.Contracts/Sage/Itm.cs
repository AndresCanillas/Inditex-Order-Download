using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;


namespace WebLink.Contracts.Sage
{
    public interface ISageItem
    {
        IXmlTransfer Result { get; set; }
        string Reference { get; }
        string Descripcion1 { get; }
        string SeaKey { get; }
        int Status { get; }
        string StatusText { get; }
        bool HasImage { get; }
        byte[] Image { get; }
        string Family { get; }
        string Brand { get; }
        string SubBrand { get; }
        string FamilyText { get; }
        string BrandText { get; }
        string SubBrandText { get; }
        string AccCode { get; }
        string ZAccCode { get; }
        string MimeType { get; }
    }

    public class Itm : ISageItem
    {
        public IXmlTransfer Result { get; set; }

        public string Reference
        {
            get => Result.GetValueInGroup("ITMREF", "ITM0_1");
        }

        public string Descripcion1
        {
            get => Result.GetValueInGroup("DES1AXX", "ITM0_1");
        }
        /// <summary>
        /// Search Key
        /// </summary>
        public string SeaKey
        {
            get => Result.GetValueInGroup("SEAKEY", "ITM1_1");
        }

        public int Status
        {
            get
            {
                var st = Convert.ToInt32 (Result.GetValueInGroup("ITMSTA", "ITM0_1"));
                return st;
            }
        }

        /// <summary>
        /// Status Description
        /// </summary>
        public string StatusText
        {
            get => Result.Groups.Find(g => g.Id.Equals("ITM0_1"))
                    .Fields.Find(f => f.Name.Equals("ITMSTA")).MenuLab;
        }

        public bool HasImage 
        {
            get  => Result.GetValueInGroup("IMG", "ITM1_7") != null;
        }

        public byte[] Image
        {
            get
            {
                if (!HasImage)
                {
                    return null;
                }

                return Convert.FromBase64String(Result.GetValueInGroup("IMG", "ITM1_7"));
            }
        }

        public string MimeType
        {
            get
            {
                var grp = Result.Groups.Find(g => g.Id.Equals("ITM1_7"));

                var field =  grp.Fields.Find(f => f.Name.Equals("IMG"));

                return field.MimeType;
            }

        }

        public string Family {
            get {

                var grp = Result.Groups.Find(g => g.Id.Equals("ITM1_5"));

                var lst = grp.List.Find(l => l.Name.Equals("TSICOD"));

                var itm = lst.Itm[0];

                return itm;
            }
        }

        public string Brand
        {
            get
            {
                var grp = Result.Groups.Find(g => g.Id.Equals("ITM1_5"));

                var lst = grp.List.Find(l => l.Name.Equals("TSICOD"));

                var itm = lst.Itm[1];

                return itm;
            }
        }

        public string SubBrand
        {
            get
            {
                var grp = Result.Groups.Find(g => g.Id.Equals("ITM1_5"));

                var lst = grp.List.Find(l => l.Name.Equals("TSICOD"));

                var itm = lst.Itm[2];

                return itm;
            }
        }

        public string FamilyText
        {
            get
            {
                var grp = Result.Groups.Find(g => g.Id.Equals("ITM1_5"));

                var lst = grp.List.Find(l => l.Name.Equals("ZTSICOD"));

                var itm = lst.Itm[0];

                return itm;
            }
        }

        public string BrandText
        {
            get
            {
                var grp = Result.Groups.Find(g => g.Id.Equals("ITM1_5"));

                var lst = grp.List.Find(l => l.Name.Equals("ZTSICOD"));

                var itm = lst.Itm[1];

                return itm;
            }
        }

        public string SubBrandText
        {
            get
            {
                var grp = Result.Groups.Find(g => g.Id.Equals("ITM1_5"));

                var lst = grp.List.Find(l => l.Name.Equals("ZTSICOD"));

                var itm = lst.Itm[2];

                return itm;
            }
        }

        public string AccCode 
        {
            get => Result.GetValueInGroup("ACCCOD", "ITM5_1");
        }
        
        /// <summary>
        /// AccCode Description
        /// </summary>
        public string ZAccCode
        {
            get => Result.GetValueInGroup("ZACCCOD", "ITM5_1");
        }

       
    }

    public interface ISageItemQuery
    {
        string Reference { get;  }
        string Description { get;  }
        string CategoryCode { get;  }
        int Status { get;  }
        string StatusText { get;  }
        string SearchKey { get;  }

    }

    public class ItemQuery : ISageItemQuery
    {
        public Lin Result { get; set; }

        public string Reference { get => Field("ITMREF").Text; }

        public string Description { get => Field("C2").Text; }

        public string CategoryCode { get => Field("TCLCOD").Text; }

        public int Status { get => Convert.ToInt32(Field("ITMSTA").Text); }

        public string StatusText { get => Field("ITMSTA").MenuLab; }

        public string SearchKey { get => Field("SEAKEY").Text; }

        private Fld Field(string name)
        {
            return Result.Fields.Find(f => f.Name.Equals(name));
        }

    }
}

