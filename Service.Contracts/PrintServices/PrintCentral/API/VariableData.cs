using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;


namespace Service.Contracts.PrintCentral
{
	// This interface is used to supply variable data used for printing or previewing labels. Variable data might also be used when generating RFID encodings.
	public interface IVariableData
	{
		int ID { get; }
		string Barcode { get; }
		string this[string property] { get; }
		Dictionary<string, string> Data { get; }
	}

	public class VariableData : IVariableData
	{
		public VariableData() { }

		public VariableData(Dictionary<string, string> data)
		{
			this.Data = data;
		}

		public Dictionary<string, string> Data { get; set; }

		public int ID
		{
			get { return Convert.ToInt32(this["ID"]); }
		}

		public string Barcode
		{
			get { return this["Barcode"]; }
		}

		public string this[string property]
		{
			get
			{
				if (!Data.TryGetValue(property, out var value))
					throw new Exception($"Property {property} could not be found in the current object.");
				else
					return value;
			}
		}

		public static IVariableData FromJson(string data)
		{
			Dictionary<string, string> values = new Dictionary<string, string>();
			JObject o = JObject.Parse(data);
			foreach (var p in o.Properties())
			{
				values[p.Name] = o.GetValue<string>(p.Name);
			}
			return new VariableData(values);
		}
	}
}
