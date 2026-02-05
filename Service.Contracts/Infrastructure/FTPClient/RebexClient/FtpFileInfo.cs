using System;

namespace RebexFtpLib.Client
{
    public class FtpFileInfo
    {
        public string FileName { get; internal set; }
        public long FileSize { get; internal set; }
        public FtpFileType FileType { get; internal set; }
        public DateTime LastUpdated { get; internal set; }
    }
}