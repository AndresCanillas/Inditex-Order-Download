using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

namespace Service.Contracts.Infrastructure.Encoding.Tempe
{
	public interface IEpcServiceTempe
	{
		void SetConfiguration(string url, string apiKeyCoreLab, string apiKeyEtiqRfid);
		ValidateOrderResponse ValidateOrder(ValidateOrderRequest request);
		IEnumerable<AllocateEpcsResponse> AllocateEpcs(AllocateEpcsRequest request);
		GetEpcsResponse GetEpcs(GetEpcsRequest request);
		IEnumerable<EpcLogError> LogEpcs(IEnumerable<EpcLogItem> epcs);
		DecodedEpc DecodeEpcs(string epcHex);
		PreEncodeResponse PreEncode(PreEncodeRequest request);
	}

	public class ValidateOrderRequest
	{
		public string PurchaseOrder { get; set; }
		public int Model { get; set; }
		public int Quality { get; set; }
	}

	public class ValidateOrderResponse
	{
		public List<ErrorInfo> Errors { get; set; }
		public string PurchaseOrder { get; set; }
		public int Model { get; set; }
		public int Quality { get; set; }
		public List<ColorInfo> Colors { get; set; }

		public bool HasErrors() => Errors != null && Errors.Count > 0;

		public string GetErrors()
		{
			if (!HasErrors()) return "";
			var sb = new StringBuilder(1000);
			foreach (var e in Errors)
				sb.AppendLine($"{e.Code}: {e.Message} - {e.Description}");
			return sb.ToString();
		}
	}

	public class ErrorInfo
	{
		public string Code { get; set; }
		public string Description { get; set; }
		public string Level { get; set; }
		public string Message { get; set; }
	}

	public class ColorInfo
	{
		public int ColorCode { get; set; }
		public List<QuantityBySizeInfo> QuantityBySize { get; set; }
	}

	public class QuantityBySizeInfo
	{
		public int SizeCode { get; set; }
		public int Quantity { get; set; }
	}

	public class AllocateEpcsRequest
	{
		public string PurchaseOrder { get; set; }
		public int Model { get; set; }
		public int Quality { get; set; }
		public List<ColorInfo> Colors { get; set; }
		public int SupplierId { get; set; }
		public int TagType { get; set; }
		public int TagSubType { get; set; }
	}

	public class AllocateEpcsResponse
	{
		public string PurchaseOrder { get; set; }
		public int Model { get; set; }
		public int Quality { get; set; }
		public int ColorCode { get; set; }
		public int RfidRequestId { get; set; }
	}

	public class GetEpcsRequest
	{
		public int RfidRequestId { get; set; }
		public int Offset { get; set; }
		public int Limit { get; set; }
	}

	public class GetEpcsResponse
	{
		public MetadataTagPage MetadataTagPage { get; set; }
		public List<EpcInfo> Results { get; set; }
	}

	public class MetadataTagPage
	{
		public ResultSet ResultSet { get; set; }
		public string Version { get; set; }
		public SecurityBits SecurityBits { get; set; }
	}

	public class ResultSet
	{
		public int Count { get; set; }
		public int Offset { get; set; }
		public int Limit { get; set; }
		public int Total { get; set; }
	}

	public class SecurityBits
	{
		public int EpcLock { get; set; }
		public int EpcPermaLock { get; set; }
		public int UserMemoryLock { get; set; }
		public int UserMemoryPermaLock { get; set; }
		public int KillPasswordLock { get; set; }
		public int KillPasswordPermaLock { get; set; }
		public int AccessPasswordLock { get; set; }
		public int AccessPasswordPermaLock { get; set; }
	}

	public class EpcInfo
	{
		public int SizeCode { get; set; }
		public string EpcHex { get; set; }
		public string AccessPasswordHex { get; set; }
		public string KillPasswordHex { get; set; }
		public string UserMemoryHex { get; set; }
	}

	public class EpcLogItem
	{
		public string EpcHex { get; set; }
		public string AccessPasswordHex { get; set; }
		public DateTime EncodingDate { get; set; }
		public int SupplierId { get; set; }
		public string DeviceSerialNumber { get; set; }
		public string TidHex { get; set; }
	}

	public class EpcLogError
	{
		public string EpcHex { get; set; }
		public string Error { get; set; }
	}

	public class DecodedEpc
	{
		public string Version { get; set; }
		public int BrandId { get; set; }
		public int SectionId { get; set; }
		public int ProductTypeCode { get; set; }
		public int ActiveTag { get; set; }
		public int EncodeCheck { get; set; }
		public int InventoryTag { get; set; }
		public int SupplierId { get; set; }
		public int Free { get; set; }
		public int SizeCode { get; set; }
		public int Color { get; set; }
		public int Quality { get; set; }
		public int Model { get; set; }
		public int ProductCompositionId { get; set; }
		public int TagType { get; set; }
		public int TagSubType { get; set; }
		public int SerialNumber { get; set; }
		public int BarCode { get; set; }
		public int Eas { get; set; }
		public string AccessPassword { get; set; }
		public string KillPassword { get; set; }
		public string UserMemory { get; set; }
	}

	public class PreEncodeRequest
	{
		public int BrandId { get; set; }
		public int ProductTypeCode { get; set; }
		public int ColorCode { get; set; }
		public List<QuantityBySizeInfo> QuantityBySize { get; set; }
		public int SupplierId { get; set; }
		public int TagType { get; set; }
		public int TagSubType { get; set; }
	}

	public class PreEncodeResponse
	{
		public int RfidRequestId { get; set; }
	}
}
