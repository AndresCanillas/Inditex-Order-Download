using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       A utility class used to wrap and send SGD commands to a connection.
	///       </summary>
	public class SGD
	{
		/// <summary>
		///   <markup>
		///     <include item="SMCAutoDocConstructor">
		///       <parameter>Zebra.Sdk.Printer.SGD</parameter>
		///     </include>
		///   </markup>
		/// </summary>
		public SGD()
		{
		}

		/// <summary>
		///       Constructs an SGD DO command and sends it to the printer.
		///       </summary>
		/// <param name="setting">the SGD setting</param>
		/// <param name="value">the setting's value</param>
		/// <param name="printerConnection">the connection to send the command to</param>
		/// <returns>The response from the SGD DO command</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs</exception>
		public static string DO(string setting, string value, Connection printerConnection)
		{
			return SGD.DO(setting, value, printerConnection, printerConnection.MaxTimeoutForRead, printerConnection.TimeToWaitForMoreData);
		}

		/// <summary>
		///       Constructs an SGD DO command and sends it to the printer.
		///       </summary>
		/// <param name="responseData">output stream to receive the response.</param>
		/// <param name="setting">the SGD setting</param>
		/// <param name="value">the setting's value</param>
		/// <param name="printerConnection">the connection to send the command to</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs</exception>
		public static void DO(Stream responseData, string setting, string value, Connection printerConnection)
		{
			SGD.DO(responseData, setting, value, printerConnection, printerConnection.MaxTimeoutForRead, printerConnection.TimeToWaitForMoreData);
		}

		/// <summary>
		///       Constructs an SGD DO command and sends it to the printer.
		///       </summary>
		/// <param name="setting">the SGD setting</param>
		/// <param name="value">the setting's value</param>
		/// <param name="printerConnection">the connection to send the command to</param>
		/// <param name="maxTimeoutForRead">the maximum time, in milliseconds, to wait for a response from the printer</param>
		/// <param name="timeToWaitForMoreData">the maximum time, in milliseconds, to wait in-between reads after the initial data is received</param>
		/// <returns>The response from the SGD DO command</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs</exception>
		public static string DO(string setting, string value, Connection printerConnection, int maxTimeoutForRead, int timeToWaitForMoreData)
		{
			string item = "";
			Connection connection = ConnectionUtil.SelectConnection(printerConnection);
			if (!(connection is StatusConnection))
			{
				string str = string.Concat(new string[] { "! U1 do \"", setting, "\" \"", value, "\"", StringUtilities.CRLF });
				byte[] numArray = connection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes(str), maxTimeoutForRead, timeToWaitForMoreData, new SgdValidator());
				item = StringUtilities.StripQuotes(Encoding.UTF8.GetString(numArray, 0, (int)numArray.Length));
			}
			else
			{
				byte[] numArray1 = JsonHelper.BuildSetCommand(new Dictionary<string, string>()
				{
					{ setting, value }
				});
				byte[] numArray2 = connection.SendAndWaitForValidResponse(numArray1, maxTimeoutForRead, timeToWaitForMoreData, new JsonValidator());
				try
				{
					item = JsonHelper.ParseGetResponse(numArray2)[setting];
				}
				catch (ZebraIllegalArgumentException)
				{
				}
				if (item == null)
				{
					item = "";
				}
			}
			return item;
		}

		/// <summary>
		///       Constructs an SGD DO command and sends it to the printer.
		///       </summary>
		/// <param name="responseData">output stream to receive the response.</param>
		/// <param name="setting">the SGD setting</param>
		/// <param name="value">the setting's value</param>
		/// <param name="printerConnection">the connection to send the command to</param>
		/// <param name="maxTimeoutForRead">the maximum time, in milliseconds, to wait for a response from the printer</param>
		/// <param name="timeToWaitForMoreData">the maximum time, in milliseconds, to wait in-between reads after the initial data is received</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs</exception>
		public static void DO(Stream responseData, string setting, string value, Connection printerConnection, int maxTimeoutForRead, int timeToWaitForMoreData)
		{
			Connection connection = ConnectionUtil.SelectConnection(printerConnection);
			if (!(connection is StatusConnection))
			{
				SGD.QuoteRemovingOutputStream quoteRemovingOutputStream = new SGD.QuoteRemovingOutputStream(responseData);
				string str = string.Concat(new string[] { "! U1 do \"", setting, "\" \"", value, "\"", StringUtilities.CRLF });
				connection.SendAndWaitForValidResponse(quoteRemovingOutputStream, new BinaryReader(new MemoryStream(Encoding.UTF8.GetBytes(str))), maxTimeoutForRead, timeToWaitForMoreData, new SgdValidator());
			}
			else
			{
				byte[] numArray = JsonHelper.BuildSetCommand(new Dictionary<string, string>()
				{
					{ setting, value }
				});
				byte[] numArray1 = connection.SendAndWaitForValidResponse(numArray, maxTimeoutForRead, timeToWaitForMoreData, new JsonValidator());
				string item = null;
				try
				{
					item = JsonHelper.ParseGetResponse(numArray1)[setting];
				}
				catch (ZebraIllegalArgumentException)
				{
				}
				if (item == null)
				{
					item = "";
				}
				try
				{
					byte[] bytes = Encoding.UTF8.GetBytes(item);
					responseData.Write(bytes, 0, (int)bytes.Length);
				}
				catch (Exception exception)
				{
					throw new ConnectionException(exception.Message);
				}
			}
		}

		/// <summary>
		///       Constructs an SGD GET command and sends it to the printer.
		///       </summary>
		/// <param name="setting">the SGD setting</param>
		/// <param name="printerConnection">the connection to send the command to</param>
		/// <returns>the setting's value</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs</exception>
		public static string GET(string setting, Connection printerConnection)
		{
			return SGD.GET(setting, printerConnection, printerConnection.MaxTimeoutForRead, printerConnection.TimeToWaitForMoreData);
		}

		/// <summary>
		///       Constructs an SGD GET command and sends it to the printer.
		///       </summary>
		/// <param name="setting">the SGD setting</param>
		/// <param name="printerConnection">the connection to send the command to</param>
		/// <param name="maxTimeoutForRead">the maximum time, in milliseconds, to wait for a response from the printer</param>
		/// <param name="timeToWaitForMoreData">the maximum time, in milliseconds, to wait in-between reads after the initial data is received</param>
		/// <returns>the setting's value</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs</exception>
		public static string GET(string setting, Connection printerConnection, int maxTimeoutForRead, int timeToWaitForMoreData)
		{
			string str = "";
			Connection connection = ConnectionUtil.SelectConnection(printerConnection);
			if (!(connection is StatusConnection))
			{
				string str1 = string.Concat("! U1 getvar \"", setting, "\"", StringUtilities.CRLF);
				byte[] numArray = connection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes(str1), maxTimeoutForRead, timeToWaitForMoreData, new SgdValidator());
				str = StringUtilities.StripQuotes(Encoding.UTF8.GetString(numArray, 0, (int)numArray.Length));
			}
			else
			{
				byte[] numArray1 = JsonHelper.BuildQuery(new List<string>()
				{
					setting
				});
				byte[] numArray2 = connection.SendAndWaitForValidResponse(numArray1, maxTimeoutForRead, timeToWaitForMoreData, new JsonValidator());
				try
				{
					JsonHelper.ParseGetResponse(numArray2).TryGetValue(setting, out str);
				}
				catch (ZebraIllegalArgumentException)
				{
				}
				if (str == null)
				{
					str = "";
				}
			}
			return str;
		}

		/// <summary>
		///       Constructs an SGD SET command and sends it to the printer.
		///       </summary>
		/// <param name="setting">the SGD setting</param>
		/// <param name="value">the setting's value</param>
		/// <param name="printerConnection">the connection to send the command to</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs</exception>
		public static void SET(string setting, int value, Connection printerConnection)
		{
			SGD.SET(setting, value.ToString(), printerConnection);
		}

		/// <summary>
		///       Constructs an SGD SET command and sends it to the printer.
		///       </summary>
		/// <param name="setting"></param>
		/// <param name="value"></param>
		/// <param name="printerConnection"></param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs</exception>
		public static void SET(string setting, string value, Connection printerConnection)
		{
			Connection connection = ConnectionUtil.SelectConnection(printerConnection);
			if (connection is StatusConnection)
			{
				byte[] numArray = JsonHelper.BuildSetCommand(new Dictionary<string, string>()
				{
					{ setting, value }
				});
				connection.SendAndWaitForValidResponse(numArray, connection.MaxTimeoutForRead, connection.TimeToWaitForMoreData, new JsonValidator());
				return;
			}
			string str = string.Concat(new string[] { "! U1 setvar \"", setting, "\" \"", value, "\"", StringUtilities.CRLF });
			connection.Write(Encoding.UTF8.GetBytes(str));
		}

		private class QuoteRemovingOutputStream : BinaryWriter
		{
			public QuoteRemovingOutputStream(Stream outputStream) : base(outputStream)
			{
			}

			public override void Write(byte[] buffer)
			{
				byte[] numArray = buffer;
				for (int i = 0; i < (int)numArray.Length; i++)
				{
					byte num = numArray[i];
					if (Encoding.UTF8.GetBytes("\"")[0] != num)
					{
						base.Write(num);
					}
				}
			}
		}
	}
}