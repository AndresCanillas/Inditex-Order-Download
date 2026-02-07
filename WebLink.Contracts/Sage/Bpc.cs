using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace WebLink.Contracts.Sage
{
    public interface ISageBpc
    {
        IXmlTransfer Result { get; set; }
        string Reference { get; }
        string Name { get; }
        string ShortDescription { get; }
        List<ISageAddress> ConfiguredAddresses { get; }
        //List<ISageAddress> ShippingAddresses { get; }
        //List<ISageAddress> Addresses { get; set; }
        string CountryCode { get; }
        string Country { get; }
        string LangCode { get; }
        string Lang { get; }
        string MainContact { get; }
    }

    public interface ISageAddress
    {
        string BcpReference { get; set; }
        string BpcName { get; set; }
        string Reference { get; set; }
        string Description { get; set; }
        string CountryCode { get; set; }
        string Country { get; set; }
        string Line1 { get; set; }
        string Line2 { get; set; }
        string Line3 { get; set; }
        string ZipCode { get; set; }
        string City { get; set; }
        string ProvinceCode { get; set; }
        SageAddresType Type { get; set; }
        bool IsDefault { get; set; }
        string Email1 { get; set; }
        string Email2 { get; set; }
        bool ConfiguredAsShippingAddress { get; set; }
        bool IsDefaultShippingAddress { get; set; }
        DateTime CreationDate { get; set; }
        DateTime UpdateDate { get; set; }
        string Telephone1 { get; set; }
        string Telephone2 { get; set; }
        string BusinessName1 { get; set; }
        string BusinessName2 { get; set; }
    }

    public enum SageAddresType
    {
        Unknow,
        Billing,
        Shipping
    }


    // Customer

    /*
      <FLD NAME="BPCNUM" TYPE="Char">000036</FLD>
    <FLD NAME="BPCNAM" TYPE="Char">TRUBEN, S.L.</FLD>
    <FLD NAME="BPCSHO" TYPE="Char">TRUBEN</FLD>
    <FLD MENULAB="Normal" MENULOCAL="401" NAME="BPCTYP" TYPE="Integer">1</FLD>
    <FLD NAME="POSCOD" TYPE="Char">41007</FLD>
    <FLD NAME="PTE" TYPE="Char">PAG60</FLD> 
     direcciones        BPAC_1
     direcciones entrega     BPC4_1
     */
    public class Bpc : ISageBpc
    {
        public IXmlTransfer Result { get; set; }
        public string Reference { get { return Result.GetValueInGroup("BPCNUM", "BPC0_1"); } }
        public string Name { get { return Result.GetValueInGroup("BPCNAM", "BPC0_1"); } }
        public string ShortDescription { get { return Result.GetValueInGroup("BPRSHO", "BPRC_1"); } }
        public string CountryCode { get { return Result.GetValueInGroup("CRY", "BPRC_1"); } }
        public string Country { get { return Result.GetValueInGroup("ZCRY", "BPRC_1"); } }
        public string LangCode { get { return Result.GetValueInGroup("LAN", "BPRC_1"); } }
        public string Lang { get { return Result.GetValueInGroup("ZLAN", "BPRC_1"); } }

        public string MainContact
        {
            get
            {
                if (ConfiguredAddresses == null || ConfiguredAddresses.Count < 1)
                {
                    return string.Empty;
                }

                return !string.IsNullOrEmpty(ConfiguredAddresses.OrderByDescending(o => o.IsDefault).First().Email1) ? ConfiguredAddresses.OrderByDescending(o => o.IsDefault).First().Email1 : string.Empty;
            }
        }

        public List<ISageAddress> ConfiguredAddresses
        {
            get
            {
                List<ISageAddress> adds = new List<ISageAddress>();
                var tab = Result.Tables.Find(f => f.Id.Equals("BPAC_1"));
                //var fiscals = List<string>() { "fiscal", "Fiscal"}

                foreach(var line in tab.Lines)
                {
                    var add = new Add();
                   
                    add.Reference = line.Fields.Find(f => f.Name.Equals("CODADR")).Text;
                    add.Description = line.Fields.Find(f => f.Name.Equals("BPADES")).Text;
                    add.CountryCode = line.Fields.Find(f => f.Name.Equals("BPACRY")).Text;
                    add.Country = line.Fields.Find(f => f.Name.Equals("CRYNAM")).Text;
                    add.Line1 = line.Fields.Find(f => f.Name.Equals("ADDLIG1")).Text;
                    add.Line2 = line.Fields.Find(f => f.Name.Equals("ADDLIG2")).Text;
                    add.Line3 = line.Fields.Find(f => f.Name.Equals("ADDLIG3")).Text;
                    add.City = line.Fields.Find(f => f.Name.Equals("CTY")).Text;
                    add.ZipCode = line.Fields.Find(f => f.Name.Equals("POSCOD")).Text;
                    add.ProvinceCode = line.Fields.Find(f => f.Name.Equals("SAT")).Text;
                    add.IsDefault = line.Fields.Find(f => f.Name.Equals("BPAADDFLG")).Text.Equals("2");// TODO: Default Is a Fiscal Address - Smartdots business rule
                    add.Email1 = line.Fields.Find(f => f.Name.Equals("WEB1")).Text;
                    add.Email2 = line.Fields.Find(f => f.Name.Equals("WEB2")).Text;
                    add.Telephone1 = line.Fields.Find(f => f.Name.Equals("TEL1")).Text;
                    add.Telephone2 = line.Fields.Find(f => f.Name.Equals("TEL2")).Text;
                    //add.Type = add.Description.Contains("fiscal", StringComparison.InvariantCultureIgnoreCase) ? SageAddresType.Billing : SageAddresType.Unknow;
                    //add.Type = add.Description.ToLower().Contains("fiscal") ? SageAddresType.Billing : SageAddresType.Unknow;
                    add.Type = !string.IsNullOrEmpty(add.Description) && add.Description.ToLower().IndexOf("fiscal", StringComparison.OrdinalIgnoreCase) >= 0 ? SageAddresType.Billing : SageAddresType.Unknow;

                    adds.Add(add);
                }

                // Configured for Delivery in Sage
                /**
                *  En sage las direcciones de entrega son datos adicionales que agregan a las direcciones
                *  para la web deberia ser sufuciente con saber cuales son las que estan configuradas como entrega
                *  y esto se hace a traves del codigo de la direccion
                *  en el tab de direcciones es el campo CODADR
                *  en el tab de cliente-entrega es el campo BPAADD
                */
                var tabBPC4 = Result.Tables.Find(f => f.Id.Equals("BPC4_1"));

                //var shippingAddresses = new List<ISageAddress>();

                foreach (var line in tabBPC4.Lines)
                {
                    var fld = line.Fields.Find(f => f.Name.Equals("BPAADD"));

                    var a = adds.FirstOrDefault(f => f.Reference.Equals(fld.Text));

                    if (a != null)
                    {
                        // TODO: asignar Type = SageAddresType.Shipping esto es incongruente, con el flag ConfiguredAsShippingAddress se resuelve mejor
                        // la direccion fiscal tambien puede ser direccion de entrega, estaria sobreescribiendo el tipo
                        // agregar un flag que diga si es fiscal o no eliminar la propiedad Type

                        //a.Type = SageAddresType.Shipping; 
                        a.ConfiguredAsShippingAddress = true;
                        a.IsDefaultShippingAddress = line.Fields.Find(f => f.Name.Equals("BPDADDFLG")).Text == "2";
                        a.BusinessName1 = line.Fields.Find(f => f.Name.Equals("BPDNAM0")).Text;
                        a.BusinessName2 = line.Fields.Find(f => f.Name.Equals("BPDNAM1")).Text;
                        //shippingAddresses.Add(a);

                    }

                }

                return adds;
            }
        }
    }


    /*
		<FLD NAME="CODADR" TYPE="Char">003</FLD>
			<FLD NAME="BPADES" TYPE="Char">Prueba</FLD>
			<FLD NAME="BPACRY" TYPE="Char">MX</FLD>
			<FLD NAME="CRYNAM" TYPE="Char">México</FLD>
			<FLD NAME="ADDLIG1" TYPE="Char">Linea 1</FLD>
			<FLD NAME="ADDLIG2" TYPE="Char">Linea 2</FLD>
			<FLD NAME="ADDLIG3" TYPE="Char">Linea 3</FLD>
			<FLD NAME="POSCOD" TYPE="Char">66036</FLD>
			<FLD NAME="CTY" TYPE="Char">GARCIA</FLD>
			<FLD NAME="SAT" TYPE="Char"/>
			<FLD NAME="TEL1" TYPE="Char"/>
			<FLD NAME="TEL2" TYPE="Char"/>
			<FLD NAME="TEL3" TYPE="Char"/>
			<FLD NAME="TEL4" TYPE="Char"/>
			<FLD NAME="TEL5" TYPE="Char"/>
			<FLD NAME="WEB1" TYPE="Char"/>
			<FLD NAME="WEB2" TYPE="Char"/>
			<FLD NAME="WEB3" TYPE="Char"/>
			<FLD NAME="WEB4" TYPE="Char"/>
			<FLD NAME="WEB5" TYPE="Char"/>
			<FLD NAME="FCYWEB" TYPE="Char">https://google.com</FLD>
			<FLD NAME="EXTNUM" TYPE="Char">9988</FLD>
			<FLD MENULAB="No" MENULOCAL="1" NAME="BPAADDFLG" TYPE="Integer">1</FLD>
			<FLD NAME="FLMOD" TYPE="Integer">0</FLD>
			<FLD MENULAB="No" MENULOCAL="1" NAME="ADRVAL" TYPE="Integer">1</FLD>
			<FLD NAME="FLMODFONC" TYPE="Integer">0</FLD>

    */
    public class Add : ISageAddress
    {
        public string BcpReference { get; set; }
        public string BpcName { get; set; }
        // <FLD NAME="CODADR" TYPE="Char">001</FLD>
        public string Reference { get; set; }
        //<FLD NAME="BPADES" TYPE="Char">DIRECCION FISCAL</FLD>
        public string Description { get; set; }
        //<FLD NAME = "BPACRY" TYPE="Char">ES</FLD>
        public string CountryCode { get; set; }
		//<FLD NAME = "CRYNAM" TYPE="Char">España</FLD>
        public string Country { get; set; }
        //<FLD NAME = "ADDLIG1" TYPE="Char">C/.LA OROTAVA,118</FLD>
        public string Line1 { get; set; }
        //<FLD NAME = "ADDLIG2" TYPE="Char">PLG.IND.SAN LUIS</FLD>
        public string Line2 { get; set; }
        //<FLD NAME = "ADDLIG3" TYPE= "Char" />
        public string Line3 { get; set; }
        //  < FLD NAME= "POSCOD" TYPE= "Char" > 29006 </ FLD >
        public string ZipCode { get; set; }
        //< FLD NAME= "CTY" TYPE= "Char" > MALAGA </ FLD >
        public string City { get; set; }
        // Si es españa, agrega codigo de provincia
        // BPACRY: ES -> CRYNAM: España
        //<FLD NAME = "SAT" TYPE="Char">29</FLD>
        public string ProvinceCode { get; set; }
        public bool IsDefault { get; set; }
        public string Email1 { get; set; }
        public string Email2 { get; set; }
        public string Telephone1 { get; set; }
        public string Telephone2 { get; set; }
        public string BusinessName1 { get; set; }
        public string BusinessName2 { get; set; }
        public SageAddresType Type { get; set; }
        public bool ConfiguredAsShippingAddress { get; set; }
        public bool IsDefaultShippingAddress { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}
