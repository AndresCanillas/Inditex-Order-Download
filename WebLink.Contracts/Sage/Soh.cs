using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WebLink.Contracts.Sage
{
	public interface ISageOrder
	{
		IXmlTransfer Param { get; set; }
		string Reference { get; set; }
		string CreatedFrom { get; set; }
		string SalesFactory { get; set; }
		string ExpeditionFactory { get; set; }
		string RevisionNum { get; set; }
		string OrderDate { get; set; }
		string CustomerReference { get; set; }
		string CustomerOrderReference { get; set; }
		string ProyectSuffix { get; set; }
		string DeliveryDate { get; set; }
		string ShipmentDate { get; set; }
		string ExpeditionAddressReference { get; set; }
		string Currency { get; set; }
		string Signed { get; set; }
		string SignedText { get; }
		string OrderStatus { get; set; }
		string OrderStatusText { get; }
		string Allocation { get; set; }
		string AllocationText { get; }
		string Delivered { get; set; }
		string DeliveredText { get; }
		string Invoice { get; set; }
		string InvoiceText { get; }
		string Credit { get; set; }
		string CreditText { get; }
		string Hold { get; set; }
		string HoldText { get; }
		string DeliveryBillingCode { get; set; }

		void AddItem(ISageRequestItem item);
		IEnumerable<SageRequestItem> GetItems();
		void SetBillingAddress(ISageAddress address);
		void SetDeliveryAddress(ISageAddress address);
        bool ExistItem(string fieldName, string fieldValue);
        int MaxNumberItemLines(string ywsref);  
	}

	/**
		<FLD NAME="ITMREF" TYPE="Char">ETAD000117</FLD>
		<FLD NAME="SAU" TYPE="Char">MIL</FLD>
		<FLD NAME="YSUPED" TYPE="Char">SC HO 2020-C000-04486</FLD>
		<FLD NAME="QTY" TYPE="Decimal">7.773</FLD>
	 */
	public interface ISageRequestItem
	{
		List<Fld> Fields { get; set; }

		void SetCustomerRef(string reference);
		void SetQuantity(int quantity, SalesOfUnit salesOfUnit = SalesOfUnit.MIL);
		void SetReference(string reference);
        void SetWsReference(string reference);// is a unique field, generated for PrintWeb
	}


	public enum SalesOfUnit
	{
		UN = 0,
		MIL = 1
	}

	public class SageRequestItem : ISageRequestItem
	{
		public List<Fld> Fields { get; set; }

		public void SetReference( string reference)
		{
			AddField(new Fld() { Name = "ITMREF", Text = reference });
		}

		public void SetQuantity (int quantity, SalesOfUnit salesOfUnit = SalesOfUnit.MIL)
		{
			double coefficient = 1.0;
			int decimals = 0;

			switch(salesOfUnit)
			{
				case SalesOfUnit.UN:
					coefficient = 1.0;
					decimals = 0;
					break;

				case SalesOfUnit.MIL:
					coefficient = 1000.0;
					decimals = 3;
					break;

				default:
					coefficient = 1.0;
					decimals = 0;
					break;
			}

			var q = Math.Round(Convert.ToDouble(quantity) / coefficient, decimals); ;

			NumberFormatInfo nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";
			nfi.NumberGroupSeparator = "";

			AddField(new Fld() { Name = "QTY", Text = q.ToString(nfi) });
			AddField(new Fld() { Name = "SAU", Text = salesOfUnit.ToString() });
		}

		public void SetCustomerRef (string reference)
		{
			AddField(new Fld() { Name = "YSUPED", Text = reference });
		}

        /// <summary>
        /// Custom field added to de SAGE to track lines inner Sage Orders to support duplicated on Iris System
        /// </summary>
        /// <param name="reference"></param>
        public void SetWsReference(string reference)
        {
            AddField(new Fld() { Name = "YWSREF", Text = reference });
        }

        public SageRequestItem()
		{
			Fields = new List<Fld>();
		}

		private void AddField(Fld field)
		{
			var fld = Fields.Find(f => f.Name.Equals(field.Name));

			if (fld == null)
			{
				fld = new Fld() { Name = field.Name };
				Fields.Add(fld);
			}

			fld.Text = field.Text;
		}
	}



    public class Soh : ISageOrder
    {
        public IXmlTransfer Param { get; set; }

        // Number
        public string Reference
        {
            get => Param.GetValueInGroup("SOHNUM", "SOH0_1");
            set => Param.SetValueInGroup("SOHNUM", value, "SOH0_1");
        }

        // use WEB from PrintCentral
        // SOHTYP
        public string CreatedFrom
        {
            get => Param.GetValueInGroup("SOHTYP", "SOH0_1");
            set => Param.SetValueInGroup("SOHTYP", value, "SOH0_1");
        }

        // SALFCY
        public string SalesFactory
        {
            get => Param.GetValueInGroup("SALFCY", "SOH0_1");
            set => Param.SetValueInGroup("SALFCY", value, "SOH0_1");
        }

        // STOFCY
        public string ExpeditionFactory
        {

            get => Param.GetValueInGroup("STOFCY", "SOH2_1");
            set => Param.SetValueInGroup("STOFCY", value, "SOH2_1");
        }

        // REVNUM -> use 0 always
        public string RevisionNum
        {
            get => Param.GetValueInGroup("REVNUM", "SOH0_1");
            set => Param.SetValueInGroup("REVNUM", value, "SOH0_1");
        }

        // ORDDAT ->yyyyMMdd
        public string OrderDate
        {
            get => Param.GetValueInGroup("ORDDAT", "SOH0_1");
            set => Param.SetValueInGroup("ORDDAT", value, "SOH0_1");
        }

        // BPCORD -> Sage Reference
        public string CustomerReference
        {
            get => Param.GetValueInGroup("BPCORD", "SOH0_1");
            set => Param.SetValueInGroup("BPCORD", value, "SOH0_1");
        }

        // CUSORDREF -> order num received from customer 
        public string CustomerOrderReference
        {
            get => Param.GetValueInGroup("CUSORDREF", "SOH0_1");
            set => Param.SetValueInGroup("CUSORDREF", value, "SOH0_1");
        }

        // PJT -> attached files suffix
        public string ProyectSuffix
        {
            get => Param.GetValueInGroup("PJT", "SOH1_2");
            set => Param.SetValueInGroup("PJT", value, "SOH1_2");
        }

        // DEMDLVDAT -> yyyyMMdd
        public string DeliveryDate
        {
            get => Param.GetValueInGroup("DEMDLVDAT", "SOH2_2");
            set => Param.SetValueInGroup("DEMDLVDAT", value, "SOH2_2");
        }

        // SHIDAT -> 
        public string ShipmentDate
        {
            get => Param.GetValueInGroup("SHIDAT", "SOH2_2");
            set => Param.SetValueInGroup("SHIDAT", value, "SOH2_2");
        }

        // YSTOADR -> Expedition Address
        public string ExpeditionAddressReference
        {
            get => Param.GetValueInGroup("YSTOADR", "SOH2_1");
            set => Param.SetValueInGroup("YSTOADR", value, "SOH2_1");
        }

        // SOH1_4, CUR
        public string Currency
        {
            get => Param.GetValueInGroup("CUR", "SOH1_4");
            set => Param.SetValueInGroup("CUR", value, "SOH1_4");
        }

        public string Signed
        {
            get => Param.GetValueInGroup("APPFLG", "SOH1_5");
            set => Param.SetValueInGroup("APPFLG", value, "SOH1_5");
        }

        public string SignedText
        {
            get {
                var fld = Param.GetFieldFromGroup("APPFLG", "SOH1_5");
                return fld != null ? fld.MenuLab : string.Empty;
            }
        }

        public string OrderStatus
        {
            get => Param.GetValueInGroup("ORDSTA", "SOH1_5");
            set => Param.SetValueInGroup("ORDSTA", value, "SOH1_5");
        }

        public string OrderStatusText
        {
            get
            {
                var fld = Param.GetFieldFromGroup("ORDSTA", "SOH1_5");
                return fld != null ? fld.MenuLab : string.Empty;
            }
        }

        public string Allocation
        {
            get => Param.GetValueInGroup("ALLSTA", "SOH1_5");
            set => Param.SetValueInGroup("ALLSTA", value, "SOH1_5");
        }

        public string AllocationText
        {
            get
            {
                var fld = Param.GetFieldFromGroup("ALLSTA", "SOH1_5");
                return fld != null ? fld.MenuLab : string.Empty;
            }
        }

        public string Delivered
        {
            get => Param.GetValueInGroup("DLVSTA", "SOH1_5");
            set => Param.SetValueInGroup("DLVSTA", value, "SOH1_5");
        }

        public string DeliveredText
        {
            get
            {
                var fld = Param.GetFieldFromGroup("DLVSTA", "SOH1_5");
                return fld != null ? fld.MenuLab : string.Empty;
            }
        }

        public string Invoice
        {
            get => Param.GetValueInGroup("INVSTA", "SOH1_5");
            set => Param.SetValueInGroup("INVSTA", value, "SOH1_5");
        }

        public string InvoiceText
        {
            get
            {
                var fld = Param.GetFieldFromGroup("INVSTA", "SOH1_5");
                return fld != null ? fld.MenuLab : string.Empty;
            }
        }

        // En curso - estatus crediticio del cliente
        public string Credit
        {
            get => Param.GetValueInGroup("CDTSTA", "SOH1_5");
            set => Param.SetValueInGroup("CDTSTA", value, "SOH1_5");
        }

        public string CreditText
        {
            get
            {
                var fld = Param.GetFieldFromGroup("CDTSTA", "SOH1_5");
                return fld != null ? fld.MenuLab : string.Empty;
            }
        }

        public string Hold
        {
            get => Param.GetValueInGroup("HLDSTA", "SOH1_5");
            set => Param.SetValueInGroup("HLDSTA", value, "SOH1_5");
        }

        public string HoldText
        {
            get
            {
                var fld = Param.GetFieldFromGroup("HLDSTA", "SOH1_5");
                return fld != null ? fld.MenuLab : string.Empty;
            }
        }

        public string DeliveryBillingCode
        {
            get => Param.GetValueInGroup("BPAADD", "SOH1_1");
            set => Param.SetValueInGroup("BPAADD", value, "SOH1_1");
        }

        public void SetDeliveryAddress(ISageAddress address)
        {
            SetAddress(address, "ADB2_1");
        }

        public void SetBillingAddress(ISageAddress address)
        {
            SetAddress(address, "ADB3_1");
        }

        private void SetAddress(ISageAddress address, string addressId) {

            var grp = new Grp() { Id = addressId };

            grp.Fields.Add(new Fld() { Name = "CRY", Text = address.CountryCode });
            grp.Fields.Add(new Fld() { Name = "POSCOD", Text = address.ZipCode });
            grp.Fields.Add(new Fld() { Name = "CTY", Text = address.City });
            grp.Fields.Add(new Fld() { Name = "SAT", Text = address.ProvinceCode });

            if (!string.IsNullOrEmpty(address.Reference))
            {
                grp.Fields.Add(new Fld() { Name = "BPAADD", Text = address.Reference });
            }

            grp.List.Add(new Lst()
            {
                Name = "BPAADDLIG",
                Size = "3",
                Type = "Char",
                Itm = new List<string>()
                {
                    address.Line1,
                    address.Line2,
                    address.Line3
                }
            });

            grp.List.Add(new Lst()
            {
                Name = "BPRNAM",
                Size = "2",
                Type = "Char",
                Itm = new List<string>()
                {
                    address.BusinessName1,
                    address.BusinessName2
                }
            });

            var found = Param.Groups.Find(g => g.Id.Equals(addressId));

            if (found == null)
            {
                Param.Groups.Add(grp);
            } else
            {
                found = grp;
            }
        }

        public void AddItem(ISageRequestItem item) {

            var tbl = Param.Tables.Find(t => t.Id.Equals("SOH4_1"));
            var currentRef = item.Fields.Find(f => f.Name.Equals("YWSREF")).Text;

            if (tbl == null)
            {
                tbl = new Tab() { Id = "SOH4_1", Dimension = "1000" };
                Param.Tables.Add(tbl);
            }

            var ln = tbl.Lines.Find(L => L.Fields.Any(f => {
                return f.Name.Equals("YWSREF") && f.Text.Equals(currentRef);
            }));

            if (ln == null)
            {
                ln = new Lin() { Num = (tbl.Lines.Count() + 1).ToString() };
                tbl.Lines.Add(ln);
                tbl.Size = tbl.Lines.Count.ToString();
            }

            // update fields
            ln.Fields = item.Fields;
        }

        public void AddItem(string sagereference, int quantity, SalesOfUnit salesOfUnit, string customerItemRef)
        {
            SageRequestItem item = new SageRequestItem();
            item.SetReference(sagereference);
            item.SetQuantity(quantity, salesOfUnit);
            item.SetCustomerRef(customerItemRef);

            AddItem(item);
        }

        /// <summary>
        /// return a clone of items, not a reference - 
        /// </summary>
        /// <returns>inmutable itemlist</returns>
        public IEnumerable<SageRequestItem> GetItems()
        {
            var tbl = Param.Tables.Find(t => t.Id.Equals("SOH4_1"));
            // clone item list
            var items = tbl.Lines.Select(line => new SageRequestItem() { Fields = new List<Fld>(line.Fields) });

            return items;
        }


        public int GetItemsCount()
        {
            var tbl = Param.Tables.Find(t => t.Id.Equals("SOH4_1"));

            return tbl.Lines.Count();
        }

        public bool ExistItem(string fieldName, string fieldValue)
        {

            var items = GetItems();

            foreach(var itm in items)
            {
                if(itm.Fields.Count(fld => fld.Name == fieldName && fld.Text == fieldValue) > 0)
                {
                    return true;
                }
            }

            return false;
        }
        public int MaxNumberItemLines(string ywsref)
        {
            int maxNumberLine = 0;
            var tbl = Param.Tables.Find(t => t.Id.Equals("SOH4_1"));
            // clone item list
            var lineList = tbl.Lines.Where(L => L.Fields.Any(f => {
                return f.Name.Equals("YWSREF") && f.Text.Contains(ywsref);
            }));

            if (!lineList.Any())
            {
                return maxNumberLine; 
            }

            if (lineList.Any(l => l.Fields.Any(f => f.Name.Equals("YWSREF") && f.Text.Contains(ywsref)
             && f.Text.Contains("-"))))
            {
             var linesWithYWSERField= lineList.Where(l => l.Fields.Any(f => f.Name.Equals("YWSREF") && f.Text.Contains(ywsref)
             && f.Text.Contains("-")));
               
                foreach (var line in linesWithYWSERField)
                {
                    var stringFieldNumber = line.Fields.First(field => field.Name.Equals("YWSREF")).Text.Split("-").Last(); 
                    int.TryParse (stringFieldNumber, out var number); 
                    if (maxNumberLine<number)
                        maxNumberLine = number;
                }

            } else
            {
                return maxNumberLine; 
            }
            
            return maxNumberLine;
        }


	}

}
