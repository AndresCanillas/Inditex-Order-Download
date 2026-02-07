using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace WebLink.Controllers
{
    [Authorize]
	public class ProvidersController : Controller
	{
		private IProviderRepository repo;
		private IArticleRepository articleRepository;
		private IArticleDetailRepository articleDetailRepository;
		private ILocalizationService g;
		private ILogService log;
        private IUserData userData;
        private INotificationRepository notificationRepository;
        private IUserRepository userRepository;
        private IOrderEmailService orderEmailService;
        private IProviderGetLocationService providerGetLocationService;
        private IProjectRepository projectRepository;
        public ProvidersController(
            IProviderRepository repo,
            ILocalizationService g,
            ILogService log,
            IUserData userData,
            IArticleRepository articleRepository,
            IArticleDetailRepository articleDetailRepository,
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            IOrderEmailService orderEmailService,
            IProviderGetLocationService providerGetLocationService,
            IProjectRepository projectRepository)
        {
            this.repo = repo;
            this.g = g;
            this.log = log;
            this.userData = userData;
            this.articleRepository = articleRepository;
            this.articleDetailRepository = articleDetailRepository;
            this.notificationRepository = notificationRepository;
            this.userRepository = userRepository;
            this.orderEmailService = orderEmailService;

            this.providerGetLocationService = providerGetLocationService;
            this.projectRepository = projectRepository;
        }


        [HttpGet, Route("/providers/getbycompanyid/{companyid}")]
		public List<ProviderDTO> GetByCompanyID(int companyid)
		{
			try
			{
                // ignore companyID is not IDT
                if(!userData.IsIDT )
                    companyid = userData.CompanyID;

                return repo.GetByCompanyIDME(companyid);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

        [HttpGet, Route("/providers/getbycompanyidme/{companyid}")]
        public List<ProviderDTO> GetByCompanyIDME(int companyid)
        {
            try
            {
                return repo.GetByCompanyIDME(companyid);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/providers/getarticlesbycompanyid/{companyid}")]
        public List<ArticleInfoDTO> GetArticlesByCompanyID(int companyid)
        {
            try
            {
				var lista = articleRepository.GetArticlesByCompanyId(companyid).ToList();
				return lista.Where(a=>a.LabelId != null && a.LabelId >0).ToList(); 
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/providers/getarticlesdetailbyproviderid/{providerid}/{companyid}")]
        public IEnumerable<ArticleDetailDTO> GetArticlesDetailByProviderID(int providerid, int companyid)
        {
            try
            {
                var lista = articleDetailRepository.GetByProviderId(providerid, companyid);
                return lista;
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }


        [HttpPost, Route("/providers/update")]
		public OperationResult UpdateProvider([FromBody]ProviderDTO provider)
		{
			try
			{
                if (!userData.Admin_Companies_CanEditProviders)
                    return OperationResult.Forbid;
                repo.UpdateProvider(provider);
				return new OperationResult(true, g["Provider was added!"]);
			}
            catch(DbUpdateException dbEx)
            {
                var msg = dbEx.Message;
                if(dbEx.InnerException != null)
                    msg = dbEx.InnerException.Message;

                if(msg.Contains("Cannot insert duplicate key"))
                {
                    msg = g["Provider reference [{0}] is duplicated", provider.ClientReference];

                    return new OperationResult(false, msg);
                }

                return new OperationResult(false, g["Operation could not be completed."]);
            }
            catch(Exception ex)
			{
                log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpPost, Route("/providers/addprovider/{companyid}")]
		public OperationResult AddProviderToCompany(int companyid, [FromBody]ProviderDTO provider)
		{
			try
			{
                if (!userData.Admin_Companies_CanEditProviders)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Provider was added!"], repo.AddProviderToCompany(companyid, provider));
			}
            catch(DbUpdateException dbEx)
            {
                var msg = dbEx.Message;
                if(dbEx.InnerException != null)
                    msg = dbEx.InnerException.Message;

                if(msg.Contains("Cannot insert duplicate key"))
                {
                    msg = g["Provider reference [{0}] is duplicated",provider.ClientReference];

                    return new OperationResult(false, msg);
                }

                return new OperationResult(false, g["Operation could not be completed."]);
            }
			catch(Exception ex)
			{
                log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpPost, Route("/providers/removeprovider/{providerid}")]
		public OperationResult RemoveProviderFromCompany(int providerid)
		{
			try
			{
                if (!userData.Admin_Companies_CanEditProviders)
                    return OperationResult.Forbid;
                repo.RemoveProviderFromCompany(providerid);
				return new OperationResult(true, g["Provider was removed!"]);
			}
            catch(DbUpdateException dbEx)
            {
                var msg = dbEx.Message;
                if(dbEx.InnerException != null)
                    msg = dbEx.InnerException.Message;

                if(msg.ToLower().Contains("reference constraint"))
                {
                    msg = g["Provider can't be remove because has orders"];

                    return new OperationResult(false, msg);
                }

                return new OperationResult(false, g["Operation could not be completed."]);
            }
			catch(Exception ex)
			{
                log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

        [HttpPost, Route("/providers/removearticlefromprovider/{articledetailid}")]
        public OperationResult RemoveArticleFromProvider(int articledetailid)
        {
            try
            {
				articleDetailRepository.Delete(articledetailid); 
                return new OperationResult(true, g["Article was removed!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }
        [HttpPost, Route("/providers/addarticlefromprovider/{articleid}/{providerid}")]
        public OperationResult AddArticleFromProvider(int articleid, int providerid )
        {
            try
            {
                var article =  articleDetailRepository.AddArticleDetail(providerid, articleid); 
                return new OperationResult(true, g["Article was added!"], article);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }
        [HttpPost, Route("/providers/addarticlesfromprovider/{articleid}/{providerid}")]
        public OperationResult AddArticlesFromProvider(string[] articleid, int providerid)
        {
            try
            {
                var articles = new List<ArticleDetailDTO>();
                foreach (var item in articleid) 
                {
                    var arrayIds = item.Split(',');
                    foreach (var sId in arrayIds)
                    {
                        int.TryParse(sId, out int id);
                        var article = articleDetailRepository.AddArticleDetail(providerid, id);
                        articles.Add(article);
                    }
                }
                return new OperationResult(true, g["Article was added!"], articles);

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/providers/removeallarticlefromprovider/{companyid}/{providerid}")]
        public OperationResult RemoveAllArticlesFromProvider (int companyid, int providerid)
        {
            try
            {
                articleDetailRepository.DeleteByCompanyId(companyid, providerid);   
                return new OperationResult(true, g["All article was removed!"]);
            }
            catch (Exception ex)
            { 
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);

            }
        }
        [HttpPost, Route("/providers/emailiferrorsupplier/{projectid}")]
        public async Task<OperationResult> SendEmailIfErrorSupplier([FromBody] List<SupplierErrors> errorsSuppliers, int projectID)
        {
            try
            {
                var emails = new List<string>();
//              var customers = notificationRepository.GetIDTStakeholders(projectID,null); 
                var customers = projectRepository.GetCustomerEmails(projectID);
                var emailTitle = "Error with Suppliers";
                var body = string.Empty;

                foreach(var customer in customers)
                {
                    var customerRepository = userRepository.GetByID(customer);
                    if(customerRepository == null)
                    {
                        log.LogException($"This customer ID:{customer} not exists!");
                        return new OperationResult(false, g["Operation could not be completed."]);
                    }
                    emails.Add(customerRepository?.Email);
                    foreach(var supplier in errorsSuppliers)
                    {
                        //body += $"The same supplier has been received with changed values. " +
                        //    $"The supplier name was <b>{supplier.ActualName}</b> and the supplier has been received with the name <b>{supplier.NewName}</b>." +
                        //    $" Please review and correct the supplier information.\n";
                        body = supplier.Message; 
                    }

                    await orderEmailService.SendMessage(string.Join(';',emails), emailTitle,body,null);
                }

                return new OperationResult(true, g["Operation OK! "]);
            }
            catch(Exception ex) 
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]); ;
            }
        }

        [HttpGet, Route("/providers/getproviderlocation/{companyid}/{projectid}/{sendtocountry}/{catalogname}/{filterfield}/{selectfield}")]
        public async Task<int> GetProviderLocatation(int companyid, int projectid,  string sendToCountry, string catalogName, string filterField, string selectField)
        {
            try
            {
                return  providerGetLocationService.GetLocation(companyid, projectid, sendToCountry, catalogName, filterField, selectField);
            }
            catch(Exception ex)
            {
                log.LogException(ex); 
                return 0;    
            }
            
        }
    }
}