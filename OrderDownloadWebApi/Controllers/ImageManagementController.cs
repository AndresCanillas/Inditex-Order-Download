using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using OrderDownloadWebApi.Models;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.OrderImages;

namespace OrderDownloadWebApi.Controllers
{
    [Route("image-management")]
    public class ImageManagementController : Controller
    {
        private readonly IFactory factory;
        private readonly ILocalizationService g;
        private readonly IAppLog log;

        public ImageManagementController(IFactory factory, ILocalizationService g, IAppLog log)
        {
            this.factory = factory;
            this.g = g;
            this.log = log;
        }

        [HttpGet("pending")]
        public OperationResult GetPending()
        {
            if (!User.IsInRole(Roles.IDTLabelDesign) && !User.IsInRole(Roles.SysAdmin))
                return OperationResult.Forbid;

            try
            {
                using (var ctx = factory.GetInstance<LocalDB>())
                {
                    var items = ctx.ImageAssets
                        .Where(asset => asset.IsLatest && asset.Status != ImageAssetStatus.InFont)
                        .OrderByDescending(asset => asset.UpdatedDate)
                        .Select(asset => new
                        {
                            asset.ID,
                            asset.Name,
                            asset.Url,
                            asset.Hash,
                            asset.ContentType,
                            Status = asset.Status.ToString(),
                            asset.UpdatedDate
                        })
                        .ToList();

                    return new OperationResult(true, g["OK"], items);
                }
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return OperationResult.InternalError;
            }
        }

        [HttpGet("list")]
        public OperationResult GetList([FromQuery] ImageAssetStatus? status = null)
        {
            if (!User.IsInRole(Roles.IDTLabelDesign) && !User.IsInRole(Roles.SysAdmin))
                return OperationResult.Forbid;

            try
            {
                using (var ctx = factory.GetInstance<LocalDB>())
                {
                    var query = ctx.ImageAssets.AsQueryable()
                        .Where(asset => asset.IsLatest);
                    if (status.HasValue)
                        query = query.Where(asset => asset.Status == status.Value);

                    var items = query
                        .OrderByDescending(asset => asset.UpdatedDate)
                        .Select(asset => new
                        {
                            asset.ID,
                            asset.Name,
                            asset.Url,
                            asset.Hash,
                            asset.ContentType,
                            Status = asset.Status.ToString(),
                            asset.UpdatedDate
                        })
                        .ToList();

                    return new OperationResult(true, g["OK"], items);
                }
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return OperationResult.InternalError;
            }
        }

        [HttpPost("{id:int}/status")]
        public OperationResult UpdateStatus(int id, [FromBody] UpdateImageStatusDto dto)
        {
            if (!User.IsInRole(Roles.IDTLabelDesign) && !User.IsInRole(Roles.SysAdmin))
                return OperationResult.Forbid;

            if (dto == null)
                return new OperationResult(false, g["Invalid request."]);

            try
            {
                using (var ctx = factory.GetInstance<LocalDB>())
                {
                    var asset = ctx.ImageAssets.FirstOrDefault(item => item.ID == id);
                    if (asset == null)
                        return OperationResult.NotFound;

                    asset.Status = dto.Status;
                    asset.UpdatedDate = DateTime.UtcNow;
                    ctx.SaveChanges();
                    return new OperationResult(true, g["OK"]);
                }
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return OperationResult.InternalError;
            }
        }

        [HttpGet("{id:int}/content")]
        public IActionResult GetContent(int id)
        {
            if (!User.IsInRole(Roles.IDTLabelDesign) && !User.IsInRole(Roles.SysAdmin))
                return Forbid();

            try
            {
                using (var ctx = factory.GetInstance<LocalDB>())
                {
                    var asset = ctx.ImageAssets.FirstOrDefault(item => item.ID == id);
                    if (asset == null || asset.Content == null)
                        return NotFound();

                    var contentType = string.IsNullOrWhiteSpace(asset.ContentType)
                        ? "application/octet-stream"
                        : asset.ContentType;
                    return File(asset.Content, contentType);
                }
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return StatusCode(500);
            }
        }
    }

    public class UpdateImageStatusDto
    {
        public ImageAssetStatus Status { get; set; }
    }
}
