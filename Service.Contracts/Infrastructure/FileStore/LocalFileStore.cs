using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Service.Contracts
{
    class LocalFileStore : ILocalFileStore
    {
        private string workDir;
        private int containerLevels = 3;
        private string[] attachmentCategories;
        private IAppConfig config;
        private ILogService log;
        private readonly IFileStoreMttoService mtto;
        private ITempFileService tempFileSrv;


        public LocalFileStore(IAppConfig config, ILogService log, IFileStoreMttoService mtto, ITempFileService tempFileSrv)
        {
            // NOTE: containerLevels must be between 1 and 3, also it MUST NOT be changed after files are stored, 
            // as this changes how files are distributed across subfolder.
            if (containerLevels < 1 || containerLevels > 3)
                throw new Exception("Invalid ammount of container levels.");
            this.config = config;
            this.log = log;
            this.mtto = mtto;
            this.tempFileSrv = tempFileSrv;
        }


        public string BaseDirectory
        {
            get { return workDir; }
            set
            {
                if (value != workDir)
                {
                    if (workDir != null)
                        mtto.Unregister(workDir);
                    if (String.IsNullOrWhiteSpace(value))
                        throw new Exception("Invalid FileStore.BaseDirectory");
                    workDir = value;
                    if (!Directory.Exists(workDir))
                        Directory.CreateDirectory(workDir);
                    mtto.Register(workDir);
                }
            }
        }


        public int ContainerLevels
        {
            get => containerLevels;
            set
            {
                if (value < 1 || value > 3)
                    throw new Exception("Invalid ammount of container levels, value must be in the range [1,3].");
                containerLevels = value;
            }
        }


        public void SetCategories(params string[] attachmentCategories)
        {
            this.attachmentCategories = attachmentCategories;
        }


        public void Configure(string storeName)
        {
            var storeConfig = config.Bind<FileStoreConfiguration>($"FileStores.{storeName}")
				?? throw new InvalidOperationException($"Missing configuration for file store {storeName}");
            Configure(storeConfig.WorkDirectory, storeConfig.AttachmentCategories, storeConfig.ContainerLevels);
        }


        public void Configure(string workDir, string attachmentCategories, int containerLevels)
        {
            if (String.IsNullOrWhiteSpace(workDir))
                throw new InvalidOperationException("WorkDirectory parameter cannot be empty");

            BaseDirectory = workDir;

            if (!String.IsNullOrWhiteSpace(attachmentCategories))
            {
                var categories = attachmentCategories.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                SetCategories(categories);
            }

            if (containerLevels == 0)
                containerLevels = 3;

            ContainerLevels = containerLevels;
        }


        public IEnumerable<string> Categories
        {
            get
            {
                return this.attachmentCategories;
            }
            set
            {
                var list = new List<string>(value);
                this.attachmentCategories = list.ToArray();
            }
        }


        public IFileData GetOrCreateFile(int id, string filename)
        {
            if (TryGetFile(id, out var file))
            {
                return file;
            }
            else
            {
                file = CreateFile(id, filename);
                return file;
            }
        }


        public bool TryGetFile(int id, out IFileData file)
        {
            file = null;
            var fileDir = GetFilePath(id);
            if (Directory.Exists(fileDir))
            {
                file = FileData.ValidateFile(id, fileDir, attachmentCategories);
                if (file != null)
                    return true;
            }
            return false;
        }


        public IFileData CreateFile(int id, string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException("argument fileName cannot be null");

            if (fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);

            IFileData file;
            var fileDir = GetFilePath(id);
            if (Directory.Exists(fileDir))
            {
                file = FileData.ValidateFile(id, fileDir, attachmentCategories);
                if (file != null)
                    throw new InvalidOperationException($"File ID {id} already exists.");
            }
            else
            {
                Directory.CreateDirectory(fileDir);
                file = FileData.CreateFile(id, fileDir, fileName, attachmentCategories);
            }
            return file;
        }


        public void DeleteFile(int id)
        {
            var fileDir = GetFilePath(id);
            if (Directory.Exists(fileDir))
                FileStoreHelper.DeleteDirectory(fileDir, true);
        }


        public string GetFilePath(int fileid)
        {
            return FileStoreHelper.GetFilePhysicalPath(workDir, containerLevels, fileid);
        }


        public int GetFileID(string path)
        {
            if (!path.Contains(workDir))
                throw new Exception("The given path does not correspond to this file store.");
            path = path.Replace(workDir, "");
            if (path.StartsWith("\\"))
                path = path.Substring(1);
            var tokens = Path.GetDirectoryName(path).Split('\\');
            if (tokens.Length < containerLevels + 1)
                throw new Exception("The given path does not correspond to this file store or does not have the expected directory levels.");
            return Convert.ToInt32(tokens[containerLevels]);
        }
    }

    public class FileStoreConfiguration
    {
        public string WorkDirectory;
        public string AttachmentCategories;
        public int ContainerLevels;
    }

    #region common exceptions
    [Serializable]
    public class LocalStorageNotFoundException : Exception
    {
        public LocalStorageNotFoundException()
        {
        }

        public LocalStorageNotFoundException(string message) : base(message)
        {
        }

        public LocalStorageNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected LocalStorageNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
    #endregion
}
