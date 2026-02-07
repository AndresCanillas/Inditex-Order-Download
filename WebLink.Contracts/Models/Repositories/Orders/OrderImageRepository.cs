using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebLink.Contracts.Models.Repositories
{

    public class ImageOrderMetadata : ImageMetadataBase
    {
        public int OrderID { get; set; }
        public ImageOrderMetadata() { }
    }
    public class OrderImageRepository : IOrderImageRepository
    {
        private IFactory factory;
        private IRemoteFileStore store;
        private IOrderRepository orderRepo;

        public OrderImageRepository(
            IFactory factory,
            IFileStoreManager storeManager,
            IOrderRepository orderRepo
            )
        {
            this.factory = factory;
            this.orderRepo = orderRepo;
            this.store = storeManager.OpenStore("OrderStore");
        }


        public IEnumerable<ImageOrderMetadata> GetListByOrderID(int orderID)
        {
            var project = orderRepo.GetByID(orderID);  // NOTE: Done just to verify that the calling user has permissions to access the specified project

            if (!store.TryGetFile(orderID, out var container))
                return null;

            var result = new List<ImageOrderMetadata>();
            var images = container.GetAttachmentCategory("Images") as IRemoteAttachmentCollection;
            var metadata = images.GetAllAttachmentMetadata<ImageOrderMetadata>();
            foreach (var imageMeta in metadata)
            {
                if (imageMeta == null) continue;   // NOTE: Attachments with no metadata are thumbnails or should be ignored.
                result.Add(imageMeta);
            }
            return result;
        }


        public void UpdateImageMetadata(ImageOrderMetadata data)
        {
            var userData = factory.GetInstance<IUserData>();
            if (data == null) return;
            var project = orderRepo.GetByID(data.OrderID);  // NOTE: Done just to verify that the calling user has permissions to access the specified project

            if (!store.TryGetFile(data.OrderID, out var container))
                throw new Exception($"Image container for project {data.OrderID} was not found.");

            var images = container.GetAttachmentCategory("Images");
            if (!images.TryGetAttachment(data.FileName, out var image))
                throw new Exception("Image could not be found");
            var meta = image.GetMetadata<ImageOrderMetadata>();
            meta.UpdateDate = DateTime.Now;
            meta.UpdateUser = userData.UserName;
            meta.Description = data.Description;
            image.SetMetadata(meta);
        }


        public ImageOrderMetadata UploadImage(int orderID, string fileName, Stream content)
        {
            var userData = factory.GetInstance<IUserData>();
            var project = orderRepo.GetByID(orderID);  // NOTE: Done just to verify that the calling user has permissions to access the specified project

            if (!store.TryGetFile(orderID, out var container))
                container = store.GetOrCreateFile(orderID, Project.FILE_CONTAINER_NAME);

            if (fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);

            var images = container.GetAttachmentCategory("Images");
            var image = images.GetOrCreateAttachment(fileName);
            image.SetContent(content);

            var result = image.GetMetadata<ImageOrderMetadata>();
            if (String.IsNullOrWhiteSpace(result.CreateUser))
            {
                result.CreateUser = userData.UserName;
                result.CreateDate = DateTime.Now;
            }
            result.OrderID = orderID;
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


        public Stream GetImage(int orderID, string fileName)
        {
            if (!store.TryGetFile(orderID, out var container))
                throw new Exception($"Image container for project {orderID} was not found.");
            if (fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);
            var images = container.GetAttachmentCategory("Images");
            if (!images.TryGetAttachment(fileName, out var image))
                return null;
            return image.GetContentAsStream();
        }


        public Stream GetThumbnail(int orderID, string fileName)
        {
            if (!store.TryGetFile(orderID, out var container))
                throw new Exception($"Image container for project {orderID} was not found.");
            if (fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);
            var images = container.GetAttachmentCategory("Images");
            var thumb = GetThumbFileName(fileName);
            if (!images.TryGetAttachment(thumb, out var thumbImage))
                return null;
            return thumbImage.GetContentAsStream();
        }


        public void DeleteImage(int orderID, string fileName)
        {
            var order = orderRepo.GetByID(orderID);  // NOTE: Done just to verify that the calling user has permissions to access the specified project

            if (!store.TryGetFile(orderID, out var container))
                throw new Exception($"Image container for project {orderID} was not found.");
            if (fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);
            var images = container.GetAttachmentCategory("Images");
            if (images.TryGetAttachment(fileName, out var image))
                images.DeleteAttachment(image);
            var thumbFileName = GetThumbFileName(fileName);
            if (images.TryGetAttachment(thumbFileName, out var thumb))
                images.DeleteAttachment(thumb);
        }
    }
}
