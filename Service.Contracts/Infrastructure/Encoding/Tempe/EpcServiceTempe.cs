using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using Services.Core;

namespace Service.Contracts.Infrastructure.Encoding.Tempe
{
	public class EpcServiceTempe : BaseServiceClient, IEpcServiceTempe
	{
		private readonly ILogService log;
		private string apiKeyCoreLab;
		private string apiKeyEtiqRfid;

		public EpcServiceTempe(ILogService log)
		{
			this.log = log;
			UseCamelCase = true;
		}

		public void SetConfiguration(string url, string apiKeyCoreLab, string apiKeyEtiqRfid)
		{
			this.Url = url;
			this.apiKeyCoreLab = apiKeyCoreLab;
			this.apiKeyEtiqRfid = apiKeyEtiqRfid;
		}

		public ValidateOrderResponse ValidateOrder(ValidateOrderRequest request)
		{
			try
			{
				return Post<ValidateOrderRequest, ValidateOrderResponse>(
					"tcorelab-provider/api/v1/product/order/validation",
					request,
					new Dictionary<string, string>() { { "itx-apiKey", apiKeyCoreLab } });
			}
			catch
			{
				var json = JsonConvert.SerializeObject(request);
				log.LogMessage($"TempeApi Error when sending ValidateOrder request: {json}");
				throw;
			}
		}

		public IEnumerable<AllocateEpcsResponse> AllocateEpcs(AllocateEpcsRequest request)
		{
			try
			{
				return Post<AllocateEpcsRequest, List<AllocateEpcsResponse>>(
					"tcorelab-provider/api/v1/product/order/tags",
					request,
					new Dictionary<string, string>() { { "itx-apiKey", apiKeyCoreLab } });
			}
			catch
			{
				var json = JsonConvert.SerializeObject(request);
				log.LogMessage($"TempeApi Error when sending AllocateEpcs request: {json}");
				throw;
			}
		}

		public GetEpcsResponse GetEpcs(GetEpcsRequest request)
		{
			try
			{
				return Get<GetEpcsResponse>(
					$"etiqrfid-rfid-provider/api/v1/product/execution/{request.RfidRequestId}/tags?offset={request.Offset}&limit={request.Limit}",
					new Dictionary<string, string>() { { "itx-apiKey", apiKeyEtiqRfid } });
			}
			catch
			{
				var json = JsonConvert.SerializeObject(request);
				log.LogMessage($"TempeApi Error when sending GetEpcs request: {json}");
				throw;
			}
		}

		public IEnumerable<EpcLogError> LogEpcs(IEnumerable<EpcLogItem> epcs)
		{
			try
			{
				return Post<IEnumerable<EpcLogItem>, List<EpcLogError>>(
					"etiqrfid-rfid-provider/api/v1/product/epc/log",
					epcs,
					new Dictionary<string, string>() { { "itx-apiKey", apiKeyEtiqRfid } });
			}
			catch
			{
				var json = JsonConvert.SerializeObject(epcs);
				if (json.Length > 800)
					json = json.Substring(0, 800) + "...";

				log.LogMessage($"TempeApi Error when sending LogEpcs request: {json}");
				throw;
			}
		}

		public DecodedEpc DecodeEpcs(string epcHex)
		{
			try
			{
				return Get<DecodedEpc>(
					$"etiqrfid-rfid-provider/api/v1/product/epc/{epcHex}/decode",
					new Dictionary<string, string>() { { "itx-apiKey", apiKeyEtiqRfid } });
			}
			catch
			{
				log.LogMessage($"TempeApi Error when sending DecodeEpcs request: {epcHex}");
				throw;
			}
		}

		public PreEncodeResponse PreEncode(PreEncodeRequest request)
		{
			try
			{
				return Post<PreEncodeRequest, PreEncodeResponse>(
					"tcorelab-provider/api/v1/product/preencode/tags",
					request,
					new Dictionary<string, string>() { { "itx-apiKey", apiKeyCoreLab } });
			}
			catch
			{
				var json = JsonConvert.SerializeObject(request);
				log.LogMessage($"TempeApi Error when sending PreEncode request: {json}");
				throw;
			}
		}
	}
}
