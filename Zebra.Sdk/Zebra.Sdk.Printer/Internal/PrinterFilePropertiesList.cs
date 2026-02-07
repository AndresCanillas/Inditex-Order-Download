using System;
using System.Collections.Generic;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Internal
{
	internal class PrinterFilePropertiesList
	{
		private List<PrinterObjectProperties> fileRecords = new List<PrinterObjectProperties>();

		public PrinterFilePropertiesList()
		{
		}

		public void Add(PrinterObjectProperties printerFileProperties)
		{
			this.fileRecords.Add(printerFileProperties);
		}

		public void AddAll(List<PrinterObjectProperties> printerFileProperties)
		{
			foreach (PrinterObjectProperties printerFileProperty in printerFileProperties)
			{
				this.fileRecords.Add(printerFileProperty);
			}
		}

		public PrinterFilePropertiesList FilterByExtension(string[] extensions)
		{
			PrinterFilePropertiesList printerFilePropertiesList = new PrinterFilePropertiesList();
			for (int i = 0; i < this.fileRecords.Count; i++)
			{
				PrinterObjectProperties item = this.fileRecords[i];
				bool flag = false;
				int num = 0;
				while (num < (int)extensions.Length)
				{
					if (!item.Extension.ToUpper().Equals(extensions[num].ToUpper()))
					{
						num++;
					}
					else
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					printerFilePropertiesList.fileRecords.Add(item);
				}
			}
			return printerFilePropertiesList;
		}

		public PrinterObjectProperties Get(int i)
		{
			return this.fileRecords[i];
		}

		public string[] GetFileNamesFromProperties()
		{
			string[] fullName = new string[this.fileRecords.Count];
			for (int i = 0; i < this.fileRecords.Count; i++)
			{
				fullName[i] = this.fileRecords[i].FullName;
			}
			return fullName;
		}

		public List<PrinterObjectProperties> GetObjectsProperties()
		{
			return this.fileRecords;
		}

		public int Size()
		{
			return this.fileRecords.Count;
		}
	}
}