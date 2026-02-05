using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Service.Contracts;

namespace Service.Contracts
{
	public class ATGCEncodingAlgorithm : ITagEncodingAlgorithm, IConfigurable<ArribasEncodingConfig>
	{
		private ATGC encoding;
		private ArribasEncodingConfig config;
		private FixedPassword passwordMethod;

		public ATGCEncodingAlgorithm()
		{
			encoding = new ATGC();
			config = new ArribasEncodingConfig();
			passwordMethod = new FixedPassword();
			config.BarcodeField = "Barcode";
		}

		// DGT does not require any serial sequence because the serial is given directly in the barcode, which is provided by the client.
		public ISerialSequence Sequence
		{
			get { return null; }
		}

		public bool IsSerialized
		{
			get { return true; }
		}


		public List<TagEncodingInfo> Encode(EncodeRequest request)
		{
			var result = new List<TagEncodingInfo>();
			var barcode = request.VariableData.GetValue<string>(config.BarcodeField);
			long value = Convert.ToInt64(barcode);
			for (int i = 0; i < request.Quantity; i++)
			{
				encoding["Barcode"].Value = value.ToString();
				string epc = encoding.GetHexadecimal();
				result.Add(new TagEncodingInfo()
				{
					EPC = epc,
					Barcode = value.ToString(),
					SerialNumber = Convert.ToInt64(barcode),
					WriteUserMemory = false,
					UserMemory = "00000000",
					WriteAccessPassword = false,
					AccessPassword = "", //accessPwd,
					WriteKillPassword = false,
					KillPassword = "", //killPwd,
					WriteLocks = false,
					EPCLock = RFIDLockType.UnLock,
					UserLock = RFIDLockType.UnLock,
					AccessLock = RFIDLockType.UnLock,
					KillLock = RFIDLockType.UnLock
				});
				value++;
			}
			return result;
		}


		public TagEncodingInfo EncodeSample(JObject data)
		{
			var barcode = data.GetValue<string>(config.BarcodeField);
			encoding["Barcode"].Value = barcode;
			string epc = encoding.GetHexadecimal();
			return new TagEncodingInfo()
			{
                EPC = epc,
                Barcode = barcode,
                SerialNumber = Convert.ToInt64(barcode),
				WriteUserMemory = false,
				UserMemory = "00000000",
				WriteAccessPassword = false,
				AccessPassword = "", //accessPwd,
				WriteKillPassword = false,
				KillPassword = "", //killPwd,
				WriteLocks = false,
				EPCLock = RFIDLockType.UnLock,
				UserLock = RFIDLockType.UnLock,
				AccessLock = RFIDLockType.UnLock,
				KillLock = RFIDLockType.UnLock
			};
		}


		public string EncodeHeader(JObject data, int count, long startingSerial, int tagsPerSheet = 1)
		{
			return "";
		}


		public ArribasEncodingConfig GetConfiguration()
		{
			return config;
		}


		public void SetConfiguration(ArribasEncodingConfig config)
		{
			this.config = config;
		}
    }


	public class ArribasEncodingConfig
	{
		public string BarcodeField;                         //Specifies the name of the field that stores the barcode.
	}


	/*
	 * Specification
	 * ==========================
	 * 
	 * This is a customer defined encoding scheme used exclusively for workshops that supply the ATGC (Agrupación de Tráfico de la Guardia Civil)
	 * as is the case with Arribas, Mingo, TEDISA and others.
	 * 
	 * Internal Structure
	 * 
	 * |           Reserved            |      Barcode    |
	 * |-------------------------------|-----------------|
	 * |           52 bits             |      44 bits    |
	 * 
	 * 
	 * Where:
	 * 
	 *	- Reserved is always set to all Zeros
	 *	- Barcode is the 13 digits barcode (Code128 - only numbers are allowed)
	 *	
	 *	
	 * URN format
	 * ==========================
	 * 
	 *  In this case the URN is not standards based, it simply allows to quickly see the most important information in the tag at a glance.
	 *  
	 *		"urn:atgc:Barcode"
	 *		
	 * Example:
	 *		"urn:atgc:1530091903491"
	 */

	public class ATGC : TagEncoding, ITagEncoding
	{
		public override int TypeID
		{
			get { return 13; }
		}

		public override string Name
		{
			get { return "ATGC"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:atgc:Barcode"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:atgc:(?<component>\d{13})$"; }
		}

		public ATGC()
		{
			fields = new BitFieldInfo[]{
				new BitFieldInfo("Reserved",        44, 52,  BitFieldFormat.Decimal, true, true, "0", null),
				new BitFieldInfo("Barcode",         0,  44,  BitFieldFormat.Decimal, true, false, "0", null)
			};
		}
	}
}
