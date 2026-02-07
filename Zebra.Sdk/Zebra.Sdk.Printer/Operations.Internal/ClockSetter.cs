using System;
using System.Globalization;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class ClockSetter : PrinterOperationBase<object>
	{
		private string dateTime;

		private ClockSetter.DateTime formattedDateTime;

		public ClockSetter(string dateTime, Connection connection, PrinterLanguage language) : base(connection, language)
		{
			this.dateTime = dateTime;
		}

		public override object Execute()
		{
			this.formattedDateTime = this.FormatDateTime(this.dateTime);
			this.SelectStatusChannelIfOpen();
			this.SetClock();
			return null;
		}

		private ClockSetter.DateTime FormatDateTime(string input)
		{
			System.DateTime dateTime;
			string str = null;
			string str1 = null;
			string[] strArrays = this.SplitDateTimeFormat(input);
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string str2 = strArrays[i];
				if (str2.Contains("-"))
				{
					try
					{
						dateTime = System.DateTime.ParseExact(str2, new string[] { "MM-dd-yy", "MM-dd-yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
					}
					catch (Exception)
					{
						throw new ZebraIllegalArgumentException(string.Concat("Invalid Date: \"", str2, "\""));
					}
					str = dateTime.ToString("MM-dd-yyyy");
				}
				else if (str2.Contains(":"))
				{
					try
					{
						System.DateTime.ParseExact(str2, "HH:mm:ss", CultureInfo.InvariantCulture);
					}
					catch (Exception)
					{
						throw new ZebraIllegalArgumentException(string.Concat("Invalid Time: \"", str2, "\""));
					}
					str1 = str2;
				}
			}
			return new ClockSetter.DateTime(str, str1);
		}

		private void SetClock()
		{
			if (!this.ShouldUseJson(this.connection, this.printerLanguage))
			{
				if (this.formattedDateTime.date != null)
				{
					SGD.SET("rtc.date", this.formattedDateTime.date, this.connection);
				}
				if (this.formattedDateTime.time != null)
				{
					SGD.SET("rtc.time", this.formattedDateTime.time, this.connection);
				}
			}
			else
			{
				if (this.formattedDateTime.date != null)
				{
					this.connection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes(string.Concat("{}{\"rtc.date\":\"", this.formattedDateTime.date, "\"}")), this.connection.MaxTimeoutForRead, this.connection.TimeToWaitForMoreData, new JsonValidator());
				}
				if (this.formattedDateTime.time != null)
				{
					this.connection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes(string.Concat("{}{\"rtc.time\":\"", this.formattedDateTime.time, "\"}")), this.connection.MaxTimeoutForRead, this.connection.TimeToWaitForMoreData, new JsonValidator());
					return;
				}
			}
		}

		private bool ShouldUseJson(Connection printerConnection, PrinterLanguage printerLanguage)
		{
			bool flag = printerLanguage == PrinterLanguage.LINE_PRINT;
			bool flag1 = !(printerConnection is StatusConnection);
			if (printerConnection is StatusConnection)
			{
				return true;
			}
			if (!flag1)
			{
				return false;
			}
			return !flag;
		}

		private string[] SplitDateTimeFormat(string inputString)
		{
			string[] strArrays = inputString.Split(new char[] { ' ' });
			if (!inputString.Contains("-") && !inputString.Contains(":"))
			{
				throw new ZebraIllegalArgumentException(string.Concat("Invalid Date/Time: \"", inputString, "\""));
			}
			return strArrays;
		}

		internal class DateTime
		{
			public string date;

			public string time;

			public DateTime(string date, string time)
			{
				this.date = date;
				this.time = time;
			}
		}
	}
}