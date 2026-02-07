using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;


namespace WebLink.Controllers
{
    [Authorize]
    public class CatalogsController : Controller
    {
        private ICatalogRepository repo;
        private IUserData userData;
        private ILocalizationService g;
        private ILogService log;

        public CatalogsController(
            ICatalogRepository repo,
            IUserData userData,
            ILocalizationService g,
            ILogService log
            )
        {
            this.repo = repo;
            this.userData = userData;
            this.g = g;
            this.log = log;
        }

        [HttpPost, Route("/catalogs/insert")]
        public OperationResult Insert([FromBody]Catalog data)
        {
            try
            {
                if (!userData.Admin_Catalogs_CanAdd)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Catalog Created!"], repo.Insert(data));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/catalogs/update")]
        public OperationResult Update([FromBody]Catalog data)
        {
            try
            {
                if (!userData.Admin_Catalogs_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Catalog saved!"], repo.Update(data));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/catalogs/delete/{id}")]
        public OperationResult Delete(int id)
        {
            try
            {
                if (!userData.Admin_Catalogs_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
                return new OperationResult(true, g["Catalog Deleted!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/catalogs/rename/{id}/{name}")]
        public OperationResult Rename(int id, string name)
        {
            try
            {
                if (!userData.Admin_Catalogs_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
                return new OperationResult(true, g["Catalog Renamed!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Unexpected error while renaming Catalog."]);
            }
        }

        [HttpGet, Route("/catalogs/getbyid/{id}")]
        public ICatalog GetByID(int id)
        {
            try
            {
                return repo.GetByID(id);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/catalogs/getbycatalogid/{catalogid}")]
        public ICatalog GetByCatalogID(int catalogid)
        {
            try
            {
                return repo.GetByCatalogID(catalogid);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/catalogs/getlist")]
        public List<ICatalog> GetList()
        {
            try
            {
                return repo.GetList();
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/catalogs/getbyproject/{projectid}")]
        public List<ICatalog> GetByProjectID(int projectid)
        {
            try
            {
                return repo.GetByProjectID(projectid);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpPost, Route("/catalogs/getbyname/{projectid}")]
        public OperationResult GetByName(int projectid, [FromBody]string name)
        {
            try
            {
                return new OperationResult(true, "", repo.GetByName(projectid, name));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpGet, Route("/catalogs/getbyprojectwithroles/{projectid}")]
        public List<ICatalog> GetByProjectIDWithRoles(int projectid)
        {
            try
            {
                return repo.GetByProjectIDWithRoles(projectid);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpPost, Route("/catalogs/assignroles")]
        public OperationResult AssignRoles([FromBody]CatalogRolesDTO data)
        {
            try
            {
                repo.AssignCatalogRoles(data.CatalogID, data.Roles);
                return new OperationResult(true, g["Catalog roles where updated!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }
    }


    public class CatalogRolesDTO
    {
        public int CatalogID { get; set; }
        public string Roles { get; set; }
    }
}