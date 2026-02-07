using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
    [Authorize]
    public class CompaniesController : Controller
    {
        private ICompanyRepository repo;
        private IERPCompanyLocationRepository erpConfigRepo;
        private IFtpAccountRepository ftpRepo;
        private IUserData userData;
        private ILocalizationService g;
        private ILogService log;

        public CompaniesController(
            ICompanyRepository repo,
            IERPCompanyLocationRepository erpConfigRepo,
            IFtpAccountRepository ftpRepo,
            IUserData userData,
            ILocalizationService g,
            ILogService log)
        {
            this.repo = repo;
            this.erpConfigRepo = erpConfigRepo;
            this.ftpRepo = ftpRepo;
            this.userData = userData;
            this.g = g;
            this.log = log;
        }


        [HttpPost, Route("/companies/insert")]
        public OperationResult Insert([FromBody] Company data)
        {
            try
            {
                if(!userData.Admin_Companies_CanAdd)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Company Created!"], repo.Insert(data));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                if(ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }


        [HttpPost, Route("/companies/update")]
        public OperationResult Update([FromBody] Company data)
        {
            try
            {
                if(!userData.Admin_Companies_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Company Saved!"], repo.Update(data));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                if(ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/companies/updateordersorting")]
        public OperationResult UpdateOrderSorting([FromBody] List<Company> companies)
        {
            try
            {
                if(!userData.CanSeeVMenu_Printers)
                    return OperationResult.Forbid;
                repo.UpdateOrderSorting(companies);
                return new OperationResult(true, g["Configuration Updated!"], null);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }


        [HttpPost, Route("/companies/delete/{id}")]
        public OperationResult Delete(int id)
        {
            try
            {
                if(!userData.Admin_Companies_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
                return new OperationResult(true, g["Company Deleted!"]);
            }
            catch(Exception ex)
            {

                var msg = g["Operation could not be completed."];

                if(userData.IsIDT)
                    msg = ex.Message;

                log.LogException(ex);
                return new OperationResult(false, msg);
            }
        }

        [HttpPost, Route("/companies/rename/{id}/{name}")]
        public OperationResult Rename(int id, string name)
        {
            try
            {
                if(!userData.Admin_Companies_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
                return new OperationResult(true, g["Company Renamed!"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                if(ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpGet, Route("/companies/getbyid/{id}")]
        public ICompany GetByID(int id)
        {
            try
            {
                return repo.GetByID(id);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/companies/getlist")]
        public List<ICompany> GetList()
        {
            try
            {
                if(userData.IsIDTExternal)
                {
                    return repo.GetListForExternalManager(userData.LocationID).ToList(); ;
                }
                else
                {
                    return repo.GetList();
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/companies/getall")]
        public List<ICompany> GetAll()
        {
            try
            {

                if(userData.IsIDTExternal)
                {
                    return repo.GetListForExternalManager(userData.LocationID).ToList(); ;
                }
                else
                {
                    return repo.GetAll();
                }

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [Route("/companies/logo/{id}")]
        public IActionResult GetLogo(int id)
        {
            try
            {
                Response.Headers[HeaderNames.CacheControl] = "no-cache";
                var logo = repo.GetLogo(id);
                if(logo != null)
                    return File(logo, "image/png");
                else
                    return File("/images/no_logo.png", "image/png");
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return File("/images/no_logo.png", "image/png");
            }
        }

        [Route("/companies/uploadlogo/{id}")]
        public IActionResult SetLogo(int id)
        {
            try
            {
                if(!userData.Admin_Companies_CanEditLogo)
                    return Forbid();
                if(Request.Form.Files != null && Request.Form.Files.Count == 1)
                {
                    var file = Request.Form.Files[0];
                    if(".png,.jpg,.jpeg,.gif".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                        return Content($"{{\"success\":false, \"message\":\"{g["Can only accept .png, .jpg, .jpeg and .gif files"]}\"}}");
                    using(MemoryStream ms = new MemoryStream())
                    {
                        using(Stream src = file.OpenReadStream())
                        {
                            src.CopyTo(ms, 4096);
                        }
                        repo.UpdateLogo(id, ms.ToArray());
                        return Content($"{{\"success\":true, \"message\":\"\", \"FileID\":{id}}}");
                    }
                }
                else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return Content($"{{\"success\":false, \"message\":\"{g["Unexpected error while uploading image file."]}\"}}");
            }
        }

        [HttpGet, Route("/companies/getftpaccount/{id}")]
        public FtpAccountInfo GetFtpAccount(int id)
        {
            try
            {
                if(!userData.Admin_Companies_CanEditFTPSettings)
                    return null;
                return ftpRepo.GetCompanyFtpAccount(id, true);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpPost, Route("/companies/SaveFtpAccount")]
        public OperationResult SaveFtpAccount([FromBody] FtpAccountInfo account)
        {
            try
            {
                if(!userData.Admin_Companies_CanEditFTPSettings)
                    return OperationResult.Forbid;
                ftpRepo.SaveCompanyFtpAccount(account);
                return new OperationResult(true, g["FTP account saved!"]);
            }
            catch(FtpAccountTakenException)
            {
                return new OperationResult(false, g["FTP User is already taken, please choose a different user name."]);
            }
            catch(FtpPasswordTooWeakException)
            {
                return new OperationResult(false, g["FTP Password is too weak, make sure it is at least 8 characters long and includes lower case and upper case characters as well as numbers."]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpGet, Route("/companies/getproviderslist/{companyid}")]
        public List<ICompany> GetProvidersList(int companyid)
        {
            try
            {
                if(userData.CanSeeCompanyFilter)
                    return repo.GetProvidersList(companyid);
                else
                    return new List<ICompany>() { repo.GetByID(companyid) };
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/companies/getprojectcompany/{projectid}")]
        public ICompany GetProjectCompany(int projectid)
        {
            try
            {
                return repo.GetProjectCompany(projectid);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpPost, Route("/companies/assignrfidconfig/{companyid}/{configid}")]
        public OperationResult AssignRFIDConfig(int companyid, int configid)
        {
            try
            {
                if(!userData.Admin_Companies_CanEditRFIDSettings)
                    return OperationResult.Forbid;
                repo.AssignRFIDConfig(companyid, configid);
                return new OperationResult(true, g["RFID configuration updated!"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["RFID configuration could not be updated."]);
            }
        }

        [HttpGet, Route("/companies/checkisbroker/{companyid}")]
        public OperationResult CheckIsBroker(int companyid)
        {
            try
            {
                var company = repo.GetByID(companyid, true);
                return new OperationResult(true, g["OK"], new { IsBroker = company.IsBroker, CompanyID = companyid });
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpGet, Route("/companies/geterpconfig/{companyid}")]
        public OperationResult GetERPConfig(int companyid)
        {
            try
            {
                return new OperationResult(true, g["OK"], erpConfigRepo.GetByCompany(companyid));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/companies/saveerpconfig")]
        public OperationResult SaveERPConfig([FromBody] UpdateERPConfigRequest rq)
        {
            try
            {
                erpConfigRepo.SaveERPConfiguration(rq);
                return new OperationResult(true, g["OK"], rq);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/companies/delerpconfig/{erpConfigID}")]
        public OperationResult DelERPConfig(int erpConfigID)
        {
            try
            {
                var deleted = erpConfigRepo.DeleteErpConfig(erpConfigID);
                return new OperationResult(true, g["OK"], deleted);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost]
        [Route("/companies/filterbyname")]
        public IList<ICompany> Filter([FromBody] string filterbyname)
        {


            try
            {
                return repo.FilterByName(filterbyname);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }
    }
}