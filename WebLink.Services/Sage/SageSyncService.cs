using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Sage;

namespace WebLink.Services.Sage
{
	public class SageSyncService : ISageSyncService
	{
		private IFactory factory;
		private ISageClientService sageClient;
		private IEventQueue events;
		private IArticleRepository articleRepo;
		private IArtifactRepository artifactRepo;
		private ICompanyRepository companyRepo;
		private IAddressRepository addressRepo;
		private ICountryRepository countryRepo;
		private ILogService log;


		public SageSyncService(
			IFactory factory,
			IArticleRepository articleRepo,
			IArtifactRepository artifactRepo,
			ISageClientService sageClient,
			ICompanyRepository companyRepo,
			IAddressRepository addressRepo,
			ICountryRepository countryRepo,
			IEventQueue events,
			ILogService log)
		{
			this.factory = factory;
			this.articleRepo = articleRepo;
			this.artifactRepo = artifactRepo;
			this.sageClient = sageClient;
			this.companyRepo = companyRepo;
			this.addressRepo = addressRepo;
			this.countryRepo = countryRepo;
			this.events = events;
			this.log = log;
		}



		public async Task<IArticle> SyncItemAsync(int articleId, string sageReference)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return await SyncItemAsync(ctx, articleId, sageReference);
			}
		}


		public async Task<IArticle> SyncItemAsync(PrintDB ctx, int articleId, string sageReference)
		{
			var item = await sageClient.GetItemDetail(sageReference);
			return SyncItem(ctx, articleId, item);
		}


		private IArticle SyncItem(PrintDB ctx, int articleId, ISageItem item)
		{
			var article = articleRepo.GetByID(ctx, articleId);
			// TODO: este codigo se repite en varios lugares, centralizar los mappings de objetos con sage
			article.BillingCode = item.Reference;
			article.Name = !string.IsNullOrEmpty(item.SeaKey) ? item.SeaKey : article.Name;
			article.Description = item.Descripcion1;
			article.SyncWithSage = true;
			article.SageRef = item.Reference;

			var articleUpdated = articleRepo.Update(ctx, article);

			if (item.HasImage)
			{
				articleRepo.SetArticlePreview(articleId, item.Image);
			}

			return articleUpdated;
		}


		public void ImportItems(IEnumerable<string> references)
		{

		}

		public async Task<string> ImportItemsAsync(IEnumerable<string> items, int projectID, string identifier, string family, string brand)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return await ImportItemsAsync(ctx, items, projectID, identifier, family, brand);
			}
		}

		public async Task<string> ImportItemsAsync(PrintDB ctx, IEnumerable<string> items, int projectID, string identifier, string family, string brand)
		{
			// item already exist  or create new
			int maxLoad = 5;
			List<Task> pendintTasks = new List<Task>();
			int totalItems = items.Count();
			int processed = 0;
			object locker = new Object();
			var counter = 0;

			foreach (var reference in items)
			{
				ArticleImportParam param = new ArticleImportParam()
				{
					Reference = reference,
					ProjectID = projectID,
					Brand = brand,
					Identifier = identifier,
					Position = counter
				};

				counter++;

				var task = DoArticleImport(ctx, param).ContinueWith(t =>
				{
					//log.LogMessage($"reference {processed} faul: '{t.IsFaulted}', exception: '{t.Exception}', finalized: '{t.IsCompleted}'");

					lock (locker)
					{
						processed++;
						events.Send(new SageSyncArticleProcessEvent(param.Identifier, totalItems, processed, string.Empty, string.Empty));
					}
				});

				pendintTasks.Add(task);

				if (pendintTasks.Count >= maxLoad)
				{
					await Task.WhenAll(pendintTasks);
					pendintTasks.Clear();
				}
			}

			if (pendintTasks.Count >= 0)
			{
				await Task.WhenAll(pendintTasks);
				pendintTasks.Clear();
			}

			events.Send(new SageSyncItemImportsEndEvent(identifier, processed));

			return identifier;

		}


		public async Task<ICompany> SyncCompanyAsync(int companyID, string sageReference)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return await SyncCompanyAsync(ctx, companyID, sageReference);
			}
		}


		public async Task<ICompany> SyncCompanyAsync(PrintDB ctx, int companyID, string sageReference)
		{
			var langOptions = factory.GetInstance<RequestLocalizationOptions>();

			var btc = await sageClient.GetCustomerDetail(sageReference);
			log.LogMessage("sage customer data ready");
			var company = companyRepo.GetByID(ctx, companyID, true);
			bool sageRefChange = company.SageRef != sageReference;
			string cultureUI = "es-ES";
			var cultureInfoFound = langOptions.SupportedUICultures.FirstOrDefault(f => f.ThreeLetterISOLanguageName.Contains(btc.Lang));

			if (cultureInfoFound != null)
			{
				cultureUI = cultureInfoFound.Name;
			}

			log.LogMessage("cultuire UI {0}", cultureUI);

			company.Name = btc.Name;
			company.MainContact = btc.MainContact;// Nombre del contacto, colocando el mismo email
			company.MainContactEmail = btc.MainContact;
			company.SyncWithSage = true;
			company.SageRef = sageReference;
			company.Culture = cultureUI;

			if (string.IsNullOrEmpty(company.CompanyCode))
			{
				company.CompanyCode = RandomCode(ctx, btc.Name);
			}

			var companyUpdated = companyRepo.Update(ctx, company);

			log.LogMessage("provider updated with sage");

			var currentAddresses = addressRepo.GetByCompany(ctx, companyID);

			btc.ConfiguredAddresses.Where(w => !string.IsNullOrEmpty(w.CountryCode)).ToList().ForEach(a =>
			{

				try
				{
					bool isNewAddress = false;

					IAddress found = currentAddresses.FirstOrDefault(f => f.SageRef == a.Reference);

					if (found == null)
					{
						isNewAddress = true;
						found = new Address();
					}
						// TODO: move mapping to method
						found.Name = a.Description;
					found.AddressLine1 = a.Line1;
					found.AddressLine2 = a.Line2;
					found.AddressLine3 = a.Line3;
					found.CityOrTown = a.City;
					found.ZipCode = a.ZipCode;
					found.Country = a.Country;
					found.CountryID = countryRepo.GetByAlpha2(a.CountryCode).ID;
					found.Default = a.IsDefaultShippingAddress;
					found.Telephone1 = a.Telephone1;
					found.Telephone2 = a.Telephone2;
					found.Email1 = a.Email1;
					found.Email2 = a.Email2;
					found.BusinessName1 = a.BusinessName1;
					found.BusinessName2 = a.BusinessName2;

					found.SyncWithSage = true;
					found.SageRef = a.Reference;
					found.SageProvinceCode = a.ProvinceCode;

					int currentAddressID = 0;

					if (isNewAddress)
					{
						var inserted = addressRepo.Insert(ctx, found);
						addressRepo.AddToCompanyAddress(ctx, companyID, inserted.ID);
						currentAddressID = inserted.ID;
					}
					else
					{
						var updated = addressRepo.Update(ctx, found);
						currentAddressID = updated.ID;
					}

					if (found.Default == true)
					{
						addressRepo.SetDefaultAddress(ctx, companyID, currentAddressID, true);
					}
				}
				catch (Exception _ex)
				{
					log.LogException($"Import direction fail  Address SageRef: {a.Reference}, CompanyID: {companyID}", _ex);
						// TODO: notify ui
					}

			});

			return companyUpdated;

		}



		private async Task DoArticleImport(PrintDB ctx, ArticleImportParam param)
		{
			try
			{
				ISageItem fullItem = await sageClient.GetItemDetail(param.Reference);

				if (!string.IsNullOrEmpty(param.Brand) && fullItem.Brand.Equals(param.Brand))
				{
					IArticle article = articleRepo.GetBySageReference(ctx, param.Reference, param.ProjectID);

					if (article == null)
					{
						article = new Article()
						{
							Name = !string.IsNullOrEmpty(fullItem.SeaKey) ? fullItem.SeaKey : param.Reference,
							ArticleCode = param.Reference,
							ProjectID = param.ProjectID
						};

						article = articleRepo.Insert(ctx, article);
					}

					var artUpdated = SyncItem(ctx, article.ID, fullItem);

				}
			}
			catch (Exception _ex)
			{
				log.LogException($"Error to import item Reference {param.Reference}", _ex);
			}
		}


		private string RandomCode(PrintDB ctx, string from, int length = 3, int loop = 0)
		{
			Random rd = new Random();
			RegEx rgx = new RegEx("[A-Za-z]([A-Za-z0-9]+)?");

			var ret = string.Empty;
			var maxTry = 50;
			var currentIt = 1;

			// select [length] valid character 
			while (ret.Length < length)
			{

				if (currentIt++ >= maxTry)
				{
					return string.Empty;// user must set manually code
				}

				int next = rd.Next(0, from.Length);
				var selectedChart = from[next];

				if (rgx.Match(ret + selectedChart))
				{
					ret = ret + selectedChart;
				}

			}

			ret = ret.ToUpperInvariant();

			// if company code exist, generate a new of [length+1] characters length
			if (companyRepo.GetByCompanyCode(ctx, ret) != null)
			{
				if (loop > 2)
				{
					return string.Empty;// user must set manually code
				}

				ret = RandomCode(ctx, from, length + 1, loop + 1);
			}

			return ret;
		}



		public async Task<IArtifact> SyncArtifact(int id, string reference)
		{
			using(var ctx = factory.GetInstance<PrintDB>())
			{
                return await SyncArtifact(ctx, id, reference);
			}
		}


		public async Task<IArtifact> SyncArtifact(PrintDB ctx, int id, string reference)
        {
            var data = artifactRepo.GetByID(ctx, id);
            var item = await sageClient.GetItemDetail(reference);

            data.SageRef = reference;
            data.SyncWithSage = true;
            data.Description = item.Descripcion1;

            var artifactupdated = artifactRepo.Update(ctx, data);

            return artifactupdated;
        }
    }


    internal class ArticleImportParam
    {
        public int Position { get; set; }
        public string Reference { get; set; }
        public int ProjectID { get; set; }
        public string Identifier { get; set; }
        public string Family { get; set; }
        public string Brand { get; set; }
    }
}
