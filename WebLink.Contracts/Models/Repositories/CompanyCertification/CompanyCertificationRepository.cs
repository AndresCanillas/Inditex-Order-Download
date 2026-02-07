using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Service.Contracts.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Models
{
    public class CompanyCertificationRepository : GenericRepository<ICompanyCertification, CompanyCertification>, ICompanyCertificationRepository
    {
        private IProviderRepository _providerRepo;
        public CompanyCertificationRepository(IFactory factory, IProviderRepository providerRepo) : base(factory, (ctx) => ctx.CompanyCertifications)
        {
            _providerRepo = providerRepo;
        }

        protected override string TableName { get => "CompanyCertifications"; }



        protected override void UpdateEntity(PrintDB ctx, IUserData userData, CompanyCertification actual, ICompanyCertification data)
        {
            if(userData.IsIDT || userData.Principal.IsAnyRole(Roles.CompanyAdmin, Roles.ProdManager))
            {
                actual.CertificationExpiration = data.CertificationExpiration;
                actual.SupplierReference = data.SupplierReference;
                actual.CertificateNumber = data.CertificateNumber;
                actual.CertifyingCompany = data.CertifyingCompany;
                actual.CompanyID = data.CompanyID;
                actual.IsDeleted = data.IsDeleted;
            }
        }
        public IEnumerable<CompanyCertification> SaveRange(IEnumerable<CompanyCertificationDTO> data)
        {
            var entities = data.Select(CompanyCertification.FromDto).ToList();

            foreach(var entity in entities)
            {
                if(entity.ID == 0)
                    this.Insert(entity);
                else
                    this.Update(entity);
            }

            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return ctx.CompanyCertifications.Where((c) => !c.IsDeleted).ToList();
            }
        }

    }
}
