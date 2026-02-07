using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{

    public class ImageMetadataBase
    {
        public string FileName { get; set; }
        public string Description { get; set; }
        public long SizeBytes { get; set; }
        public string HumanSize { get { return FileSizeFormatter.FormatSize(SizeBytes); } }
        public string CreateUser { get; set; }
        public DateTime CreateDate { get; set; }
        public string UpdateUser { get; set; }
        public DateTime UpdateDate { get; set; }

    }
    public class ImageMetadata:ImageMetadataBase 
    {
        public int ProjectID { get; set; }
        public ImageMetadata() { }
    }

    public static class FileSizeFormatter
    {
        static readonly string[] suffixes =
        { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        public static string FormatSize(long bytes)
        {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }
    }

    public class ProjectImageRepository : IProjectImageRepository
    {
		private IFactory factory;
		private IRemoteFileStore store;
        private IProjectRepository projectRepo;

        public ProjectImageRepository(
            IFactory factory,
            IFileStoreManager storeManager,
            IProjectRepository projectRepo
            )
        {
            this.factory = factory;
            this.projectRepo = projectRepo;
            this.store = storeManager.OpenStore("ProjectStore");
        }


        public IEnumerable<ImageMetadata> GetListByProjectID(int projectID)
        {
            var project = projectRepo.GetByID(projectID);  // NOTE: Done just to verify that the calling user has permissions to access the specified project

            if (!store.TryGetFile(projectID, out var container))
                return null;

            var result = new List<ImageMetadata>();
            var images = container.GetAttachmentCategory("Images") as IRemoteAttachmentCollection;
			var metadata = images.GetAllAttachmentMetadata<ImageMetadata>();
            foreach (var imageMeta in metadata)
            {
                if (imageMeta == null) continue;   // NOTE: Attachments with no metadata are thumbnails or should be ignored.
                result.Add(imageMeta);
            }
            return result;
        }

        public Stream GetImage(int projectID, string fileName)
        {
            if(!store.TryGetFile(projectID, out var container))
                throw new Exception($"Image container for project {projectID} was not found.");
            if(fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);
            var images = container.GetAttachmentCategory("Images");
            if(!images.TryGetAttachment(fileName, out var image))
                return null;
            return image.GetContentAsStream();
        }

        public ImageMetadata GetImageMetadata(int projectID, string fileName)
        {
            if(!store.TryGetFile(projectID, out var container))
                return null;

            if(fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);

            var images = container.GetAttachmentCategory("Images") as IRemoteAttachmentCollection;
            if(!images.TryGetAttachment(fileName, out var image))
                return null;

            return image.GetMetadata<ImageMetadata>();
        }


        public void UpdateImageMetadata(ImageMetadata data)
        {
			var userData = factory.GetInstance<IUserData>();
            if (data == null) return;
            var project = projectRepo.GetByID(data.ProjectID);  // NOTE: Done just to verify that the calling user has permissions to access the specified project

            if (!store.TryGetFile(data.ProjectID, out var container))
                throw new Exception($"Image container for project {data.ProjectID} was not found.");

            var images = container.GetAttachmentCategory("Images");
            if (!images.TryGetAttachment(data.FileName, out var image))
                throw new Exception("Image could not be found");
            var meta = image.GetMetadata<ImageMetadata>();
            meta.UpdateDate = DateTime.Now;
            meta.UpdateUser = userData.UserName;
            meta.Description = data.Description;
            image.SetMetadata(meta);
        }


        public ImageMetadata UploadImage(int projectID, string fileName, Stream content)
        {
			var userData = factory.GetInstance<IUserData>();
			var project = projectRepo.GetByID(projectID);  // NOTE: Done just to verify that the calling user has permissions to access the specified project

            if (!store.TryGetFile(projectID, out var container))
                container = store.GetOrCreateFile(projectID, Project.FILE_CONTAINER_NAME);

            if (fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);

            var images = container.GetAttachmentCategory("Images");
			var image = images.GetOrCreateAttachment(fileName);
            image.SetContent(content);

			var result = image.GetMetadata<ImageMetadata>();
			if(String.IsNullOrWhiteSpace(result.CreateUser))
			{
				result.CreateUser = userData.UserName;
				result.CreateDate = DateTime.Now;
			}
			result.ProjectID = projectID;
			result.FileName = fileName;
			result.SizeBytes = image.FileSize;
			result.UpdateUser = userData.UserName;
			result.UpdateDate = DateTime.Now;
			image.SetMetadata(result);

			if (Path.GetExtension(fileName).ToLower() != ".svg")
                CreateThumbnail(images, image);

            return result;
        }


        private void CreateThumbnail(IAttachmentCollection images, IAttachmentData image)
        {
            using (var src = image.GetContentAsStream())
            {
                var thumbContent = ImageProcessing.CreateThumb(src, 150, 150);
                var thumbName = GetThumbFileName(image.FileName);
                if (!images.TryGetAttachment(thumbName, out var thumb))
                    thumb = images.CreateAttachment(thumbName);
                thumb.SetContent(thumbContent);
            }
        }


        private string GetThumbFileName(string fileName)
        {
            string fnwe = Path.GetFileNameWithoutExtension(fileName);
            return $"{fnwe}._thumb_.png";
        }


        


        public Stream GetThumbnail(int projectID, string fileName)
        {
            if (!store.TryGetFile(projectID, out var container))
                throw new Exception($"Image container for project {projectID} was not found.");
            if (fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);
            var images = container.GetAttachmentCategory("Images");
            var thumb = GetThumbFileName(fileName);
            if (!images.TryGetAttachment(thumb, out var thumbImage))
                return null;
            return thumbImage.GetContentAsStream();
        }


        public void DeleteImage(int projectID, string fileName)
        {
            var project = projectRepo.GetByID(projectID);  // NOTE: Done just to verify that the calling user has permissions to access the specified project

            if (!store.TryGetFile(projectID, out var container))
                throw new Exception($"Image container for project {projectID} was not found.");
            if (fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);
            var images = container.GetAttachmentCategory("Images");
            if (images.TryGetAttachment(fileName, out var image))
                images.DeleteAttachment(image);
            var thumbFileName = GetThumbFileName(fileName);
            if (images.TryGetAttachment(thumbFileName, out var thumb))
                images.DeleteAttachment(thumb);
        }

        //protected virtual async Task AuthorizeOperationAsync(PrintDB ctx, IUserData userData, IProject data)
        //{
           
        //}


    }
}
