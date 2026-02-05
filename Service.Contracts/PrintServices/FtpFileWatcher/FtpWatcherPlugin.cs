using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Service.Contracts.Documents;

namespace Service.Contracts
{
	public interface IFtpWatcherPlugin: IDisposable
	{
		void Execute(FtpFile fileInfo, List<FtpFile> ftpFiles = null);
	}


	public class FtpFile
	{
		public int ProjectID { get; set; }                      // The id of the project that contains the FTP configuration that was used to download this file.
		public int BrandID { get; set; }                        // The id of the brand that contains the project.
		public int CompanyID { get; set; }                      // The id of the company that contains the brand.
		public string ProjectName { get; set; }                 // The name of the project that contains the FTP configuration that was used to download this file.
		public string RootDirectory { get; set; }               // Root directory for the FTP service
		public string CompanyDirectory { get; set; }            // Root directory for the company
		public string BrandDirectory { get; set; }              // Root directory for the brand
		public string ProjectDirectory { get; set; }            // Root directory for the project
		public string FileName { get; set; }					// The name of the file that was downloaded (without path)
		public string DownloadedFilePath { get; set; }			// Path to the downloaded file (the file being processed)
		public string DestinationDirectory { get; set; }		// Directory where the downloaded file should be placed by the system. Initially this is set by the FtpFileWatcherService to be the same as the ProjectDirectory, however the plugin can change the value if neccesary to place the file on a different folder.
		public string Container { get; set; }					// if come from a .zip file, this property contain a full path into FTP to this file else null or empty will be set
		public int ProductionType { get; set; }
		public bool IgnoreFile { get; set; }                    // ignore this file to upload, example use to process images files from ftp inner plugin
		public int FactoryID { get; set; }
		public bool IsStopped { get; set; }
		public bool IsBillable { get; set; } = true;
		public string OrderCategoryClient { get; set; }
		public string MDOrderNumber { get; set; }
        public int RetryCount { get; set; } = 0;
        public FtpItemAction ItemAction { get; set; } = null;
        public int ProviderID { get; set; } = 0;

        public FtpFile Clone()
        {
            return new FtpFile
            {
                ProjectID = this.ProjectID,
                BrandID = this.BrandID,
                CompanyID = this.CompanyID,
                ProjectName = this.ProjectName,
                RootDirectory = this.RootDirectory,
                CompanyDirectory = this.CompanyDirectory,
                BrandDirectory = this.BrandDirectory,
                ProjectDirectory = this.ProjectDirectory,
                FileName = this.FileName,
                DownloadedFilePath = this.DownloadedFilePath,
                DestinationDirectory = this.DestinationDirectory,
                Container = this.Container,
                ProductionType = this.ProductionType,
                IgnoreFile = this.IgnoreFile,
                FactoryID = this.FactoryID,
                IsStopped = this.IsStopped,
                IsBillable = this.IsBillable,
                OrderCategoryClient = this.OrderCategoryClient,
                MDOrderNumber = this.MDOrderNumber,
                RetryCount = this.RetryCount,
                ItemAction = this?.ItemAction?.Clone(),
                ProviderID = this.ProviderID
            };
        }

    }


    public class FtpItemAction
    {
        public FtpActionType Action { get; set; }
        public string Reason { get; set; }
        public TimeSpan? Delay { get; set; }
        public FtpItemAction Clone()
        {
            return new FtpItemAction
            {
                Action = this.Action,
                Reason = this.Reason,
                Delay = this.Delay
            };
        }
    }

    public enum FtpActionType
    {
        Delay = 1,
        Reject = 2,
        Cancel = 3
    }
}
