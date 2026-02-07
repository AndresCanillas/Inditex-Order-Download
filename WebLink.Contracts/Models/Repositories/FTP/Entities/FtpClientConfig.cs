namespace WebLink.Contracts.Models
{
    public class FtpClientConfig
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public FTPMode Mode { get; set; }
        public bool AllowInvalidCert { get; set; }
        public string KeyFile { get; set; }  // Actual content of the key file!!
        public string KeyFilePassword { get; set; }
        public string WorkDirectory { get; set; }
        public string FileMask { get; set; }
        public string DefaultFactory { get; set; }
        public bool RemoveFilesAfterDownload { get; set; }
    }
}

