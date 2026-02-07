using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;
using Zebra.Sdk.Settings.Internal;

namespace Zebra.Sdk.Util.Internal
{
	internal class MultipartFileSender
	{
		private Connection connection;

		private List<PrinterFileDescriptor> fileDescriptors;

		public MultipartFileSender(Connection connection, List<PrinterFileDescriptor> fileDescriptors)
		{
			this.connection = connection;
			this.fileDescriptors = fileDescriptors;
		}

		private MultipartFileSender.Entity BuildEntity()
		{
			Dictionary<string, Stream> strs = new Dictionary<string, Stream>();
			foreach (PrinterFileDescriptor fileDescriptor in this.fileDescriptors)
			{
				strs.Add(fileDescriptor.Name, fileDescriptor.SourceStream);
			}
			return new MultipartFileSender.Entity()
			{
				Boundary = this.GenerateBoundary(),
				FileObjects = strs
			};
		}

		private string GenerateBoundary()
		{
			string str = Guid.NewGuid().ToString();
			return string.Concat(str, str).Substring(0, 36);
		}

		public static List<PrinterObjectProperties> GetPrinterObjectPropertiesFromJsonData(byte[] jsonData)
		{
			List<PrinterObjectProperties> printerObjectProperties = new List<PrinterObjectProperties>();
			if (jsonData != null && jsonData.Length != 0)
			{
				try
				{
					foreach (MpfPrinterResponse obj in JArray.Parse(Encoding.UTF8.GetString(jsonData)).ToObject<List<MpfPrinterResponse>>())
					{
						PrinterFilePath printerFilePath = FileUtilities.ParseDriveAndExtension(obj.Filename);
						printerObjectProperties.Add(new PrinterFilePropertiesZpl(string.Concat(printerFilePath.Drive, ":"), printerFilePath.FileName, printerFilePath.Extension.Substring(1), obj.Size, obj.Crc32));
					}
				}
				catch (Exception)
				{
				}
			}
			return printerObjectProperties;
		}

		private List<PrinterObjectProperties> GetPrinterResponse()
		{
			return MultipartFileSender.GetPrinterObjectPropertiesFromJsonData(this.connection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes("\r\n"), this.connection.MaxTimeoutForRead, this.connection.TimeToWaitForMoreData, new JsonValidator()));
		}

		public static PrinterObjectProperties Send(Connection connection, PrinterFileDescriptor printerFileDescriptor)
		{
			List<PrinterObjectProperties> printerObjectProperties = MultipartFileSender.Send(connection, new List<PrinterFileDescriptor>()
			{
				printerFileDescriptor
			});
			if (printerObjectProperties.Count < 1)
			{
				throw new ConnectionException("No printer response to MPF storage request");
			}
			return printerObjectProperties[0];
		}

		public static List<PrinterObjectProperties> Send(Connection connection, List<PrinterFileDescriptor> fileDescriptors)
		{
			return (new MultipartFileSender(connection, fileDescriptors)).Send();
		}

		private List<PrinterObjectProperties> Send()
		{
			if (this.fileDescriptors == null || this.fileDescriptors.Count == 0)
			{
				throw new ArgumentException("No files to send");
			}
			this.SendToPrinter(this.BuildEntity());
			return this.GetPrinterResponse();
		}

		private void SendToPrinter(MultipartFileSender.Entity entityData)
		{
			try
			{
				using (BinaryWriter printerOutputStream = new MultipartFileSender.PrinterOutputStream(this.connection))
				{
					printerOutputStream.Write(Encoding.UTF8.GetBytes("{}"));
					foreach (string key in entityData.FileObjects.Keys)
					{
						printerOutputStream.Write(Encoding.UTF8.GetBytes(entityData.GetContentHeader(key)));
						Stream item = entityData.FileObjects[key];
						if (item.CanSeek)
						{
							item.Position = (long)0;
						}
						byte[] numArray = new byte[item.Length];
						for (int i = item.Read(numArray, 0, (int)numArray.Length); i > 0; i = item.Read(numArray, 0, (int)numArray.Length))
						{
							printerOutputStream.Write(numArray, 0, i);
						}
						printerOutputStream.Write(Encoding.UTF8.GetBytes("\r\n"));
					}
					printerOutputStream.Write(Encoding.UTF8.GetBytes(string.Format("--{0}--\r\n", entityData.Boundary)));
					printerOutputStream.Flush();
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new ConnectionException(exception.Message, exception);
			}
		}

		private class Entity
		{
			private string boundary;

			private Dictionary<string, Stream> fileObjects;

			public string Boundary
			{
				get
				{
					return this.boundary;
				}
				set
				{
					this.boundary = value;
				}
			}

			public Dictionary<string, Stream> FileObjects
			{
				get
				{
					return this.fileObjects;
				}
				set
				{
					this.fileObjects = value;
				}
			}

			public Entity()
			{
			}

			public string GetContentHeader(string fileName)
			{
				return string.Format("--{0}\r\nContent-Disposition: form-data; name=\"files\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\nContent-Transfer-Encoding: binary\r\n\r\n", this.boundary, fileName);
			}
		}

		private class PrinterOutputStream : BinaryWriter
		{
			private Connection connection;

			public PrinterOutputStream(Connection connection)
			{
				this.connection = connection;
			}

			public override void Write(byte[] b, int off, int length)
			{
				try
				{
					this.connection.Write(b, off, length);
				}
				catch (ConnectionException connectionException1)
				{
					ConnectionException connectionException = connectionException1;
					throw new IOException(connectionException.Message, connectionException);
				}
			}

			public override void Write(int arg0)
			{
				throw new IOException("Unsupported Operation");
			}

			public override void Write(byte[] b)
			{
				this.Write(b, 0, (int)b.Length);
			}
		}
	}
}