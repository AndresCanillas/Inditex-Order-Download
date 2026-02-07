using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
	public class ProductionTypeManagerService : IProductionTypeManagerService
	{
		private ISetIDTFactoryStrategy iDTFactoryStrategy;
		private ISetLocalStrategy locaStrategy;
		private ISetLocalAsFirstOptionStrategy localAsFirstOptionStrategy;

		public ProductionTypeManagerService(
			ISetIDTFactoryStrategy iDTFactoryStrategy,
			ISetLocalStrategy locaStrategy,
			ISetLocalAsFirstOptionStrategy localAsFirstOptionStrategy
			)
		{
			this.iDTFactoryStrategy = iDTFactoryStrategy;
			this.locaStrategy = locaStrategy;
			this.localAsFirstOptionStrategy = localAsFirstOptionStrategy;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="SendToCode"> Is string of ClientReference determined by DocumentImportService before to insert a new order </param>
		/// <param name="project"></param>
		/// <param name="articleCode"></param>
		/// <returns></returns>
		public ProductionType GetProductyonType(string sendToCode, IProject project, string articleCode)
		{
			var strategy = GetStrategy(project.ProductionTypeStrategy);

			return strategy.GetProductionType(sendToCode, project, articleCode);
		}

		private IProjectProductionTypeStrategy GetStrategy(ProjectSetProductionType productionTypeStrategy)
		{
			IProjectProductionTypeStrategy selected = null;

			switch (productionTypeStrategy)
			{
				case ProjectSetProductionType.SetIDTFactoryStrategy:
					selected = iDTFactoryStrategy;
					break;

				case ProjectSetProductionType.SetAsLocalStrategy:
					selected = locaStrategy;
					break;

				case ProjectSetProductionType.SetLocalAsFirstStrategy:
					selected = localAsFirstOptionStrategy;
					break;
			}


			return selected;
		}
	}
}
