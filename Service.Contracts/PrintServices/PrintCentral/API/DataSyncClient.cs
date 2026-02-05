using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.PrintCentral
{
	public interface IDataSyncClient
	{
		string Url { get; set; }
		string Token { get; set; }
		bool Authenticated { get; }
		void Login(string loginUrl, string userName, string password);
		Task LoginAsync(string loginUrl, string userName, string password);
		Task<SyncResult> SyncEncodedLabels(int factoryid, List<EncodedLabelDTO> data);
		Task<SyncResult> SyncLabelsRfidData(int factoryid, List<EncodedLabelDTO> data);
        Task<SyncResult> SyncUpdateEncodedDevice(UpdateEncodedLabelsSync data);

    }

	public class SyncResult
	{
		public bool Success { get; set; }
		public string ErrorMessage { get; set; }
	}

	public class EncodedLabelsSync
	{
		public int FactoryID { get; set; }
		public List<EncodedLabelDTO> Data { get; set; }
	}
    public class UpdateEncodedLabelsSync
    {
        public int OrderID { get; set; }
        public int EncodeDeviceID { get; set; }
        public int? InlayConfigID { get; set; }
        public string InlayConfigDescription { get; set; }
        public int FactoryID { get; set; }
    }

    public class DataSyncClient: BaseServiceClient, IDataSyncClient
	{
		public Task<SyncResult> SyncEncodedLabels(int factoryid, List<EncodedLabelDTO> data)
		{
			var rq = new EncodedLabelsSync()
			{
				FactoryID = factoryid,
				Data = data
			};
			return InvokeAsync<EncodedLabelsSync, SyncResult>("/api/sync/encodedlabels", rq);
		}
		public Task<SyncResult> SyncLabelsRfidData(int factoryid, List<EncodedLabelDTO> data)
		{
			var rq = new EncodedLabelsSync()
			{
				FactoryID = factoryid,
				Data = data
			};
			return InvokeAsync<EncodedLabelsSync, SyncResult>("/api/sync/labelsrfiddata", rq);
		}

        public Task<SyncResult> SyncUpdateEncodedDevice(UpdateEncodedLabelsSync data)
        {
            return InvokeAsync<UpdateEncodedLabelsSync, SyncResult>("/api/sync/updateencodeddevice", data);
        }
    }
}
