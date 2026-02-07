using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace WebLink.Services.Zebra
{
	public class ZebraRFIDEncoder : IZebraRFIDEncoder
	{
		private IConfigurationContext configContext;
		private ITagEncodingProcess encoding;
		private string lastEpc;
		private long lastSerial;
		private string lastAccPwd;
		private string lastKillPwd;

		public ZebraRFIDEncoder(IConfigurationContext configContext)
		{
			this.configContext = configContext;
		}

		public int OrderID { get; set; }

		public string OrderNumber { get; set; }

		public int DetailID { get; set; }

		public void SetRFIDConfig(IRFIDConfig config)
		{
			if (String.IsNullOrWhiteSpace(config.SerializedConfig))
				throw new InvalidOperationException("Invalid RFID configuration.");
			encoding = configContext.GetInstance<RFIDConfigurationInfo>(config.SerializedConfig).Process;
		}

		public string Encode(IVariableData data)
		{
			if (encoding == null)
				throw new InvalidOperationException("Need to call SetRFIDConfig before calling Encode method.");

			StringBuilder sb = new StringBuilder(1000);
			JObject jo = JObject.FromObject(data.Data);
			var info = encoding.Encode(new EncodeRequest(OrderID, OrderNumber, DetailID, jo, 1, 1))[0];
			lastSerial = info.SerialNumber;
			lastEpc = info.EPC;
			lastAccPwd = info.AccessPassword;
			lastKillPwd = info.KillPassword;
			sb.AppendLine(CreateRFIDCommands(info));
			var zebraCommands = String.Format(
				"{0}\r\n{1}\r\n{2}\r\n",
				sb.ToString(),
				ReadTIDCommand(),
				ReadEPCCommand(info.EPC));
			return zebraCommands;
		}

		private string CreateRFIDCommands(TagEncodingInfo info)
		{
			StringBuilder sb = new StringBuilder(200);

			// Emmit Write EPC command
			sb.Append($"^RS8,,180,2,E,,,3^RFW,H,,,A^FD{info.EPC}^FS");

			// Emmit Write User Memory Command
			if (info.WriteUserMemory)
				sb.Append($"^RFW,H,0,{info.UserMemory.Length / 2},3^FD{info.UserMemory}^FS");

			// Emmit write passwords commands
			if (info.WriteAccessPassword && info.WriteKillPassword)
				sb.Append($"^RFW,H,P^FD{info.AccessPassword},{info.KillPassword}^FS");
			else if(info.WriteAccessPassword)
				sb.Append($"^RFW,H,P^FD{info.AccessPassword}^FS");
			else if (info.WriteKillPassword)
				sb.Append($"^RFW,H,P^FD,{info.KillPassword}^FS");

			// Emmit lock command
			if (info.WriteLocks)
			{
				var killLock = GetLockArg(info.KillLock);
				var accessLock = GetLockArg(info.AccessLock);
				var epcLock = GetLockArg(info.EPCLock);
				var userLock = GetLockArg(info.UserLock);
				sb.Append($"^RLM,{killLock},{accessLock},{epcLock},{userLock}^FS");
			}
			return sb.ToString();
		}

		private string GetLockArg(RFIDLockType lockCode)
		{
			switch (lockCode)
			{
				case RFIDLockType.UnLock: return "U";
				case RFIDLockType.Lock: return "L";
				case RFIDLockType.PermaLock: return "P";
				case RFIDLockType.PermaUnlock: return "O";
				case RFIDLockType.Mask: return "";
				default: throw new InvalidOperationException($"LockType {lockCode} is not supported.");
			}
		}

		private string ReadTIDCommand()
		{
			return "^RFR,H,0,12,2^FN1^FS^HV1,,TID:[,]^FS";
		}

		private string ReadEPCCommand(string epc)
		{
			return $"^RFR,H,2,{epc.Length / 2},1^FN2^FS^HV2,,EPC:[,]^FS";
		}

		public long LastSerial { get { return lastSerial; } }
		public string LastEPC { get { return lastEpc; } }
		public string AccessPassword { get { return lastAccPwd; } }
		public string KillPassword { get { return lastKillPwd; } }
	}
}
