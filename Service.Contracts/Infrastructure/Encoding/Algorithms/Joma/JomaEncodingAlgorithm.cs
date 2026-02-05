using Newtonsoft.Json.Linq;
using Service.Contracts.Database;
using Service.Contracts.Infrastructure.Encoding.SerialSequences;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Contracts.Infrastructure.Encoding.Algorithms.Joma
{
    public class JomaEncodingAlgorithm : ITagEncodingAlgorithm, IConfigurable<JomaEncodingConfig>
    {
        private JomaEncodingConfig config;
        private IJomaSerialSequence sequence;

        public JomaEncodingAlgorithm(IJomaSerialSequence sequence)
        {
            config = new JomaEncodingConfig();
            this.sequence = sequence;
        }

        public ISerialSequence Sequence
        {
            get { return null; }
        }
        
        public bool IsSerialized
        {
            get { return config.IsSerialized; }
        }

        public List<TagEncodingInfo> Encode(EncodeRequest request)
        {
            //use a dictionary for assure unique EPC values
            var result = new Dictionary<string, TagEncodingInfo>(60000000);

            var provider = config.Provider.ToString("D2");
            
            var serials = sequence.AcquireMultiple(request.Quantity);

             foreach(var serial in serials)
            {
                string epc = $"{provider}{serial:D14}";
                var tag = new TagEncodingInfo()
                {
                    EPC = epc,
                    Barcode = "",
                    SerialNumber = serial,
                    WriteUserMemory = false,
                    UserMemory = "00000000",
                    WriteAccessPassword = false,
                    AccessPassword = "", //accessPwd,
                    WriteKillPassword = false,
                    KillPassword = "", //killPwd,
                    WriteLocks = false,
                    EPCLock = RFIDLockType.UnLock,
                    UserLock = RFIDLockType.UnLock,
                    AccessLock = RFIDLockType.UnLock,
                    KillLock = RFIDLockType.UnLock,
                    VerifyRFIDWhilePrinting = config.VerifyRFIDWhilePrinting
                };

                tag.TrackingCode = BarcodeProcessing.ProcessTrackingCodeMask(config.TrackingCodeMask, request.VariableData, tag);

                result.Add(epc,tag);
            }
         
            return result.Values.ToList();
        }


        public TagEncodingInfo EncodeSample(JObject data)
        {
            var provider = config.Provider.ToString("D2");
            var date = DateTime.Now;
            var seq = new Random().Next(100).ToString("D2");

            string serial = $"{date:yyyyMMddHHmm}{seq}";

            return new TagEncodingInfo()
            {
                EPC = $"{provider}{serial:D14}",
                Barcode = "",
                SerialNumber = Convert.ToInt64(serial), 
                WriteUserMemory = false,
                UserMemory = "00000000",
                WriteAccessPassword = false,
                AccessPassword = "", //accessPwd,
                WriteKillPassword = false,
                KillPassword = "", //killPwd,
                WriteLocks = false,
                EPCLock = RFIDLockType.UnLock,
                UserLock = RFIDLockType.UnLock,
                AccessLock = RFIDLockType.UnLock,
                KillLock = RFIDLockType.UnLock
            };
        }

        public string EncodeHeader(JObject data, int count, long startingSerial, int tagsPerSheet = 1)
        {
            return "";
        }

        JomaEncodingConfig IConfigurable<JomaEncodingConfig>.GetConfiguration()
        {
            return config;
        }

        public void SetConfiguration(JomaEncodingConfig config)
        {
            this.config = config;
        }
    }

    public class JomaEncodingConfig
    {
        public int Provider;
        public bool IsSerialized;
        public string TrackingCodeMask;
        public bool VerifyRFIDWhilePrinting;
    }
}