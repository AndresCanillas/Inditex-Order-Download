using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Service.Contracts.Infrastructure.Encoding.Tempe
{
    public partial class AllocateEpcsTempe : ITagEncodingProcess, IAllocateEpcsProcess, IPrintPackageDataProcessor, IConfigurable<AllocateEpcsTempeConfig>
    {
        private AllocateEpcsTempeConfig config;
        private IEpcRepositoryTempe repo;
        private IEpcServiceTempe epcService;

        public AllocateEpcsTempe(
            IEpcRepositoryTempe repo,
            IEpcServiceTempe epcService)
        {
            this.repo = repo;
            this.epcService = epcService;
            config = new AllocateEpcsTempeConfig()
            {
                EncodingApi = new EpcApi()
            };
        }

        public bool IsSerialized { get => true; }

        public string EncodeHeader(JObject data, int count, long startingSerial, int tagsPerSheet = 1)
        {
            return "";  // Not implemented yet for TEMPE/Inditex... This could return the Model/Quality/Color/Size components from the variable data...
        }

        public TagEncodingInfo EncodeSample(JObject data)
        {
            // Returns fixed EPC. NOTE: This is required when creating a preview or when using the Print Sample feature
            // Since those options are not really bound to a particular order we cannot invoke the EPC service to generate this EPC.
            var taginfo = new TagEncodingInfo()
            {
                EPC = "09088CD776E981C0000001D398C007E1",
                EPCLock = RFIDLockType.Mask,
                UserLock = RFIDLockType.Mask,
                AccessLock = RFIDLockType.Mask,
                KillLock = RFIDLockType.Mask,
                AccessPassword = null,
                KillPassword = null,
                Barcode = "012345678901",
                SerialNumber = 1,
                WriteAccessPassword = false,
                WriteKillPassword = false,
                WriteLocks = false,
                WriteUserMemory = false,
                TrackingCode = "",
                UnitID = 0,
                UserMemory = null,
                VerifyRFIDWhilePrinting = false
            };

            taginfo.TrackingCode = BarcodeProcessing.ProcessTrackingCodeMask(config.TrackingCodeMask, data, taginfo);

            return taginfo;
        }

        public AllocateEpcsTempeConfig GetConfiguration()
        {
            return config;
        }

        public void SetConfiguration(AllocateEpcsTempeConfig config)
        {
            this.config = config;
            epcService.SetConfiguration(config.WebServiceUrl, config.ApiKeyCoreLab, config.ApiKeyEtiqRfid);
        }

        public List<TagEncodingInfo> Encode(EncodeRequest request)
        {
            var orderStatus = repo.GetOrder(request.OrderID);
            if(orderStatus == null)
                throw new InvalidOperationException($"Order {request.OrderID}/{request.OrderNumber} could not be found in the pre-allocted EPCs table");
            if(orderStatus.AllocationStatus != AllocationStatus.Allocated)
                throw new InvalidOperationException($"Order {request.OrderID}/{request.OrderNumber} has not been validated yet");

            return AllocateProcess(request);
        }

        private List<TagEncodingInfo> AllocateProcess(EncodeRequest request)
        {
            List<AllocatedEpc> epcs;
            if(config.EncodingApi is EpcApi epcApi)
                epcs = AllocateNormalEpcs(epcApi, request);
            else if(config.EncodingApi is PreencodingApi preencodingApi)
                epcs = AllocatePreencodingEpcs(preencodingApi, request);
            else
                throw new NotImplementedException("Unsupported encoding API");

            var locks = repo.GetLockInfo(request.OrderID);
            return CreateTagEncoding(epcs, locks, request.VariableData);
        }

        private List<AllocatedEpc> AllocateNormalEpcs(EpcApi epcApi, EncodeRequest request)
        {
            DetailDto detailDto = new DetailDto
            {
                OrderID = request.OrderID,
                OrderNumber = request.OrderNumber,
                DetailID = request.DetailID,
                Quantity = request.Quantity,
                Model = epcApi.Config.Model.GetValue<int>(request.VariableData),
                Quality = epcApi.Config.Quality.GetValue<int>(request.VariableData),
                Color = epcApi.Config.Color.GetValue<int>(request.VariableData),
                Size = epcApi.Config.Size.GetValue<int>(request.VariableData),
                TagType = epcApi.Config.TagType.GetValue<int>(request.VariableData),
                TagSubType = epcApi.Config.TagSubType.GetValue<int>(request.VariableData),

            };

            return GetOrAllocate(detailDto,
                allocateEpcsBuild: qty => new AllocateEpcsRequest
                {
                    PurchaseOrder = request.OrderNumber,
                    SupplierId = config.SuppliedId,
                    TagType = detailDto.TagType,
                    TagSubType = detailDto.TagSubType,
                    Model = detailDto.Model,
                    Quality = detailDto.Quality,
                    Colors = new List<ColorInfo>
                    {
                        new ColorInfo
                        {
                            ColorCode=detailDto.Color,
                            QuantityBySize=new List<QuantityBySizeInfo>
                            {
                                new QuantityBySizeInfo{ SizeCode=detailDto.Size, Quantity=qty }
                            }
                        }
                    }
                });

        }

        private List<AllocatedEpc> AllocatePreencodingEpcs(PreencodingApi preencodingApi, EncodeRequest request)
        {

            DetailDto detailDto = new DetailDto
            {
                OrderID = request.OrderID,
                OrderNumber = request.OrderNumber,
                DetailID = request.DetailID,
                Quantity = request.Quantity,
                BrandId = preencodingApi.Config.BrandId.GetValue<int>(request.VariableData),
                ProductType = preencodingApi.Config.ProductType.GetValue<int>(request.VariableData),
                Color = preencodingApi.Config.Color.GetValue<int>(request.VariableData),
                Size = preencodingApi.Config.Size.GetValue<int>(request.VariableData),
                TagType = preencodingApi.Config.TagType.GetValue<int>(request.VariableData),
                TagSubType = preencodingApi.Config.TagSubType.GetValue<int>(request.VariableData),

            };


            return GetOrAllocate(detailDto,
                preEncodeBuild: qty => new PreEncodeRequest()
                {
                    BrandId = detailDto.BrandId,
                    ProductTypeCode = detailDto.ProductType,
                    ColorCode = detailDto.Color,
                    QuantityBySize = new List<QuantityBySizeInfo>()
                    {
                        new QuantityBySizeInfo()
                        {
                            SizeCode = detailDto.Size,
                            Quantity = qty
                        }
                    },
                    SupplierId = config.SuppliedId,
                    TagType = detailDto.TagType,
                    TagSubType = detailDto.TagSubType
                });
        }

        private List<TagEncodingInfo> CreateTagEncoding(List<AllocatedEpc> epcs, LockInfo locks, JObject data)
        {
            List<TagEncodingInfo> result = new List<TagEncodingInfo>(epcs.Count);
            ProcessLockOverrides(locks);
            FixMemoryBanks(epcs);
            foreach(var epc in epcs)
            {
                var taginfo = new TagEncodingInfo()
                {
                    EPC = epc.Epc,
                    Barcode = $"",
                    SerialNumber = 0,
                    WriteUserMemory = !String.IsNullOrWhiteSpace(epc.UserMemory),
                    UserMemory = epc.UserMemory,
                    WriteAccessPassword = !String.IsNullOrWhiteSpace(epc.AccessPassword),
                    AccessPassword = epc.AccessPassword,
                    WriteKillPassword = !String.IsNullOrWhiteSpace(epc.KillPassword),
                    KillPassword = epc.KillPassword,
                    WriteLocks = locks.EpcLock != RFIDLockType.Mask ||
                        locks.UserMemoryLock != RFIDLockType.Mask ||
                        locks.KillPasswordLock != RFIDLockType.Mask ||
                        locks.AccessPasswordLock != RFIDLockType.Mask,
                    EPCLock = locks.EpcLock,
                    UserLock = locks.UserMemoryLock,
                    KillLock = locks.KillPasswordLock,
                    AccessLock = locks.AccessPasswordLock,
                    VerifyRFIDWhilePrinting = config.VerifyRFIDWhilePrinting
                };

                taginfo.TrackingCode = BarcodeProcessing.ProcessTrackingCodeMask(config.TrackingCodeMask, data, taginfo);

                result.Add(taginfo);
            }
            return result;
        }

        private void FixMemoryBanks(List<AllocatedEpc> epcs)
        {
            foreach(var epc in epcs)
            {
                if(!config.WriteUserMemory)
                    epc.UserMemory = null;

                if(!String.IsNullOrWhiteSpace(epc.KillPassword) && epc.KillPassword.Length < 8)
                    epc.KillPassword = new String('0', 8 - epc.KillPassword.Length) + epc.KillPassword;

                if(epc.KillPassword.Length > 8)
                    epc.KillPassword = epc.KillPassword.Substring(epc.KillPassword.Length - 8);

                if(epc.KillPassword == "00000000")
                    epc.KillPassword = null;

                if(!String.IsNullOrWhiteSpace(epc.AccessPassword) && epc.AccessPassword.Length < 8)
                    epc.AccessPassword = new String('0', 8 - epc.AccessPassword.Length) + epc.AccessPassword;

                if(epc.AccessPassword.Length > 8)
                    epc.AccessPassword = epc.AccessPassword.Substring(epc.AccessPassword.Length - 8);

                if(epc.AccessPassword == "00000000")
                    epc.AccessPassword = null;

                if(config.KillLockOverride == RFIDLockOverride.MatchAccessLock)
                    epc.KillPassword = epc.AccessPassword;
            }
        }

        private void ProcessLockOverrides(LockInfo locks)
        {
            if(config.AccessLockOverride != RFIDLockOverride.DoNotOverride &&
                config.AccessLockOverride != RFIDLockOverride.MatchAccessLock)
            {
                locks.AccessPasswordLock = (RFIDLockType)((int)config.AccessLockOverride);
            }

            if(config.EpcLockOverride != RFIDLockOverride.DoNotOverride)
            {
                if(config.EpcLockOverride == RFIDLockOverride.MatchAccessLock)
                    locks.EpcLock = locks.AccessPasswordLock;
                else
                    locks.EpcLock = (RFIDLockType)((int)config.EpcLockOverride);
            }

            if(config.UserLockOverride != RFIDLockOverride.DoNotOverride)
            {
                if(config.UserLockOverride == RFIDLockOverride.MatchAccessLock)
                    locks.UserMemoryLock = locks.AccessPasswordLock;
                else
                    locks.UserMemoryLock = (RFIDLockType)((int)config.UserLockOverride);
            }

            if(config.KillLockOverride != RFIDLockOverride.DoNotOverride)
            {
                if(config.KillLockOverride == RFIDLockOverride.MatchAccessLock)
                    locks.KillPasswordLock = locks.AccessPasswordLock;
                else
                    locks.KillPasswordLock = (RFIDLockType)((int)config.KillLockOverride);
            }
        }
    }

    public class AllocateEpcsTempeConfig
    {
        //// Base URL used for both TCORELAB & ETIQRFID. Default url provided below points to their PRE-production environment...
        [Required]
        public string WebServiceUrl { get; set; } = "https://preint-api.inditex.com/";

        [Required]
        public string ApiKeyCoreLab { get; set; }

        [Required]
        public string ApiKeyEtiqRfid { get; set; }

        [Required]
        public int SuppliedId { get; set; } = 4;

        public IEncodingApi EncodingApi { get; set; }

        public bool WriteUserMemory { get; set; }

        public RFIDLockOverride EpcLockOverride { get; set; }

        public RFIDLockOverride UserLockOverride { get; set; }

        public RFIDLockOverride AccessLockOverride { get; set; }

        public RFIDLockOverride KillLockOverride { get; set; }

        // A mask used to create a QR code that can be used in the label to associate information encoded in the RFID Tag with an external system. Can be left null or empty if the client does not use QRCodes or if the QR code is not related to the RFID system. This field works in the same way as the StandardEncodngAlgorithm, see that for more information on how to setup a QRMask.
        public string TrackingCodeMask { get; set; }

        public bool VerifyRFIDWhilePrinting { get; set; }
    }

    public interface IEncodingApi
    {
        // Intentionally empty, this interface is used only to store configuration options
    }

}
