using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers.Wizards
{
    [Authorize]
    public class CompanyCertificationsController : Controller
    {

        private ILogService log;
        private IUserData userData;
        private ICompanyCertificationRepository repo;
        private ILocalizationService g;
        public CompanyCertificationsController(ILogService log, IUserData userData, ICompanyCertificationRepository repo, ILocalizationService g)
        {
            this.log = log;
            this.userData = userData;
            this.repo = repo;
            this.g = g;
        }

        [HttpPost, Route("/companycertification/insert")]
        public OperationResult Insert([FromBody] CompanyCertificationDTO data)
        {
            try
            {
                return new OperationResult(true, g["Company Certification Created!"], repo.Insert(CompanyCertification.FromDto(data)));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                if(ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPut, Route("/companycertification/update")]
        public OperationResult Update([FromBody] CompanyCertification data)
        {
            try
            {
                return new OperationResult(true, g["Company Certification Saved!"], repo.Update(data));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                if(ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/companycertification/delete")]
        public OperationResult Delete([FromBody] CompanyCertification data)
        {
            try
            {
                return new OperationResult(true, g["Company Certification Deleted!"], repo.Update(data));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpGet, Route("/companycertification/all")]
        public List<CompanyCertificationDTO> All()
        {
            try
            {
                return repo.All().Where(certification => !certification.IsDeleted)
                     .Select(certification => new CompanyCertificationDTO
                     {
                         ID = certification.ID,
                         CompanyID = certification.CompanyID,
                         IsDeleted = certification.IsDeleted,
                         SupplierReference = certification.SupplierReference,
                         CertificateNumber = certification.CertificateNumber,
                         CertifyingCompany = certification.CertifyingCompany,
                         CertificationExpiration = certification.CertificationExpiration
                     })
                     .ToList();
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/companycertification/getbyid/{id}")]
        public ICompanyCertification GetByID(int id)
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

        [HttpPost, Route("/companycertification/save")]

        public OperationResult Save([FromBody] IEnumerable<CompanyCertificationDTO> data)
        {
            try
            {
                return new OperationResult(true, g["Company Certification Saved!"], repo.SaveRange(data));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpGet, Route("/companycertification/getbyvendorid/{vendorID}")]
        public List<CompanyCertificationDTO> GetByVendorID(int vendorID)
        {
            try
            {
                return repo.All().Where(certification => certification.CompanyID == vendorID && !certification.IsDeleted)
                     .Select(certification => new CompanyCertificationDTO
                     {
                         ID = certification.ID,
                         CompanyID = certification.CompanyID,
                         IsDeleted = certification.IsDeleted,
                         SupplierReference = certification.SupplierReference,
                         CertificateNumber = certification.CertificateNumber,
                         CertifyingCompany = certification.CertifyingCompany,
                         CertificationExpiration = certification.CertificationExpiration
                     })
                     .ToList();
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }


    }
}
