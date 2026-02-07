
using Service.Contracts;

namespace WebLink.Contracts
{
    public class FtpFileReceivedEvent : EQEventInfo
    {
        public UploadOrderDTO OrderData { get; set; }

        public int FtpFileReceivedID { get; set; }
    }
}
