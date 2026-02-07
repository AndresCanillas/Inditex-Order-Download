using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public class EntityCacheService
    {
        private readonly InMemoryEntityCache<ICompany> companyCache;
        private readonly InMemoryEntityCache<IBrand> brandCache;
        private readonly InMemoryEntityCache<IProject> projectCache;

        public EntityCacheService(
            IEventQueue events,
            ICompanyRepository companyRepository,
            IBrandRepository brandRepository,
            IProjectRepository projectRepository)
        {
            companyCache = new InMemoryEntityCache<ICompany>(events);
            brandCache = new InMemoryEntityCache<IBrand>(events);
            projectCache = new InMemoryEntityCache<IProject>(events);

            companyCache.Initialize(companyRepository.All(), (id) => companyRepository.GetByID(id));
            brandCache.Initialize(brandRepository.All(), (id) => brandRepository.GetByID(id));
            projectCache.Initialize(projectRepository.All(), (id) => projectRepository.GetByID(id));
        }

        public InMemoryEntityCache<ICompany> CompanyCache => companyCache;
        public InMemoryEntityCache<IBrand> BrandCache => brandCache;
        public InMemoryEntityCache<IProject> ProjectCache => projectCache;
    }
}
