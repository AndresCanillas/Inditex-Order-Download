using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class TemplateInfo
	{
		public bool isLocalToComputer;

		public string contents;

		public string pathOnPrinter;

		public FieldDescriptionData[] variableFields;

		public TemplateInfo()
		{
		}

		public void Acquire(string destinationDevice, string templateFilename)
		{
			this.ReadTemplate(destinationDevice, templateFilename);
			this.pathOnPrinter = FormatUtilZpl.ExtractDFName(this.contents);
			this.variableFields = FormatUtilZpl.GetVariableFieldsS(this.contents);
		}

		private void GetTemplateFromPrinter(string destinationDevice, string templateFilename)
		{
			if (destinationDevice != null)
			{
				Connection connection = null;
				try
				{
					try
					{
						connection = ConnectionBuilderInternal.Build(destinationDevice);
						connection.Open();
						ZebraPrinter instance = ZebraPrinterFactory.GetInstance(connection);
						this.contents = Encoding.UTF8.GetString(instance.RetrieveFormatFromPrinter(templateFilename));
						this.isLocalToComputer = false;
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						throw new ArgumentException(exception.Message, exception);
					}
				}
				finally
				{
					try
					{
						if (connection != null)
						{
							connection.Close();
						}
					}
					catch (ConnectionException)
					{
					}
				}
			}
		}

		private void ReadTemplate(string destinationDevice, string templateFilename)
		{
			this.isLocalToComputer = true;
			try
			{
				this.contents = FileReader.ToString(templateFilename);
			}
			catch (IOException)
			{
				this.contents = "";
			}
			if (this.contents.Equals(""))
			{
				this.GetTemplateFromPrinter(destinationDevice, templateFilename);
			}
			if (this.contents.Equals(""))
			{
				throw new ArgumentException(string.Concat("Template file (", templateFilename, ") does not exist or is empty."));
			}
		}
	}
}