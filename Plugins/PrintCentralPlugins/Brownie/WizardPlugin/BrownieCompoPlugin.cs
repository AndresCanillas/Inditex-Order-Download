using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using SmartdotsPlugins.Brownie.WizardPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace Smartdots
{
    [FriendlyName("Brownie - Composition Text Plugin")]
	[Description("Concatenate Brownie Composition Sections and Care Instructions.")]
	public class BrownieCompoPlugin : IWizardCompositionPlugin
	{
		private IEventQueue events;
		private ILogService log;
		private IOrderUtilService orderUtilService;
		private readonly int MAX_SHOES_FIBERS = 5;
		private readonly int MAX_SHOES_SECTIONS = 3;
		private readonly string EMPTY_CODE = "0";
		private readonly IOrderRepository orderRepo;
		private readonly IFactory factory;
		private IConnectionManager connManager;
		private ICatalogRepository catalogRepo;

		private string SECTION_SEPARATOR;
		private string SECTION_LANGUAGE_SEPARATOR;
		private string FIBERS_SEPARATOR;
		private string FIBER_LANGUAGE_SEPARATOR;
		private string CAREINSTRUCTION_SEPARATOR;
		private string CAREINSTRUCTION_LANGUAGE_SEPARATOR;
		private string COMPOSITION_SEPARATOR = ";";
		private readonly string ARTICLES_TABLENAME = "Articles";
		private readonly string BASEDATA_TABLENAME = "BaseData";

		string[] CareInstructionsLanguageSortedAll = { "English", "Spanish", "French", "Portuguese", "Italian", "Hebrew" };

		public BrownieCompoPlugin(IEventQueue events, ILogService log, IOrderUtilService orderUtilService, IConnectionManager connManager, ICatalogRepository catalogRepo, IOrderRepository orderRepo = null, IFactory factory = null)
		{
			this.events = events;
			this.log = log;
			this.orderUtilService = orderUtilService;
			this.connManager = connManager;
			this.catalogRepo = catalogRepo;
			this.orderRepo = orderRepo;
			this.factory = factory;
		}

		public void GenerateCompositionText(List<OrderPluginData> orderData)
		{
			log.LogMessage($"GenerateCompositionText for OrderGroupID: {orderData[0].OrderGroupID}");
			bool addedArticleTable = false;
			GetSeparators(orderData[0].ProjectID);

			foreach (var od in orderData)
			{
				var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, null, COMPOSITION_SEPARATOR);

				addedArticleTable = RunsThroughCompositions(orderData, addedArticleTable, od, compositions);
			}
		}

		private bool RunsThroughCompositions(List<OrderPluginData> orderData, bool addedArticleTable, OrderPluginData od, IList<CompositionDefinition> compositions)
		{
			foreach (var c in compositions)
			{
				var compositionData = new Dictionary<string, string>();

				//List<string> ShoesSymbols;
				//var validSymbolsValue = new Dictionary<char, string> { { '1', "A" }, { '2', "B" }, { '3', "C" } };
				var sb = new StringBuilder();

				GenerateSections(c, compositionData, sb, orderData[0].ProjectID);

				compositionData.Add("FullComposition", sb.ToString().Trim());

				sb.Clear();

				var careInstructionsAndSymbols = ConcatenateCareInstructionsAndSymbols(c.CareInstructions);

				foreach (var careInstructionLanguage in careInstructionsAndSymbols.Item3)
					compositionData.Add(careInstructionLanguage.Key, careInstructionLanguage.Value);

				if (!addedArticleTable)
				{
					AddCompositionArticleTable(orderData[0].ProjectID, orderData[0].OrderID, orderData[0].OrderGroupID);
					addedArticleTable = true;
				}

				log.LogMessage($"Save Generic Compo for OrderGroupID: {od.OrderGroupID}, ( CompositionLabelID: {c.ID} )");

				orderUtilService.SaveComposition(orderData[0].ProjectID, c.ID, compositionData, careInstructionsAndSymbols.Item1, careInstructionsAndSymbols.Item2);
			}

			return addedArticleTable;
		}

		private void GenerateSections(CompositionDefinition c, Dictionary<string, string> compositionData, StringBuilder sb, int projectID)
		{
			for (var i = 0; i < c.Sections.Count; i++)
			{
				var title = "title_" + (i + 1);
				//var symbolShoe = "symbols_shoes_" + (i + 1);

				//ShoesSymbols = new List<string>() { "", "", "", "", "" };

				var langsList = c.Sections[i].AllLangs.Split(COMPOSITION_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
				var titleValue = AddSectionTitle(c.Sections[i].Code, langsList);

				compositionData.Add(title, titleValue);
				sb.Append(titleValue).Append(SECTION_SEPARATOR);

				var fibers = c.Sections[i].Fibers != null ? c.Sections[i].Fibers : new List<Fiber>();
				var fiber = "fibers_" + (i + 1);
				var fiberValue = string.Empty;
				var fibersStrings = new StringBuilder();

				fiberValue = ConcatenateFibers(fibers, langsList, fiberValue, projectID);

				fibersStrings.Append(fiberValue);

				AddSectionTitleSeparator(sb, c.Sections.Count, i, fiberValue, fibersStrings);

				compositionData.Add(fiber, fiberValue);

				// only 3 first sections, fibers contains shoes symbols
				/*if (i < MAX_SHOES_SECTIONS)
                    compositionData.Add(symbolShoe, string.Join(";", ShoesSymbols));*/

			}
		}

		private void GetSeparators(int projectID)
		{
			var projectData = orderUtilService.GetProjectById(projectID);

			SECTION_SEPARATOR = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
			SECTION_LANGUAGE_SEPARATOR = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
			FIBERS_SEPARATOR = string.IsNullOrEmpty(projectData.FibersSeparator) ? "\n" : projectData.FibersSeparator;
			FIBER_LANGUAGE_SEPARATOR = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
			CAREINSTRUCTION_SEPARATOR = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
			CAREINSTRUCTION_LANGUAGE_SEPARATOR = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;

		}

		private string AddSectionTitle(string sectionCode, string[] langsList)
		{
			var titleValue = string.Empty;
			if (sectionCode != EMPTY_CODE)
				titleValue = langsList.Length > 1 ? $" {String.Join(SECTION_LANGUAGE_SEPARATOR, langsList)} " : langsList[0];
			return titleValue;
		}
		private string ConcatenateFibers(IList<Fiber> fibers, string[] langsList, string fiberValue, int projectID)
		{
			var code = string.Empty;

			for (var f = 0; f < fibers.Count; f++)
			{
				/*if (!string.IsNullOrEmpty(fibers[f].FiberType) && validSymbolsValue.TryGetValue(fibers[f].FiberType[0], out var keyLetter))
                {
                    if (f < MAX_SHOES_FIBERS)
                        ShoesSymbols[f] = keyLetter;
                }*/

				langsList = fibers[f].AllLangs.Split(COMPOSITION_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

				if (langsList.Length > 0)
					code = AddFiberInitials(langsList[0], projectID);

				if (fibers[f].Code != EMPTY_CODE)
				{
					fiberValue += $"{fibers[f].Percentage}% {code} " + (langsList.Length > 1 ? $" {String.Join(FIBER_LANGUAGE_SEPARATOR, langsList)}" : langsList[0]);

					if (f < fibers.Count - 1) // is not last, add fibers separator too
					{
						fiberValue += string.Format("\u200F{0}\u200E", FIBERS_SEPARATOR);
					}

				}
			}
			return fiberValue;
		}

		private string AddFiberInitials(string fiberValueEnglish, int projectID)
		{
			var catalogs = catalogRepo.GetByProjectID(projectID, true);

			var fibersCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_FIBERS_CATALOG));

			using (var dynamicDB = connManager.OpenDB("CatalogDB"))
			{
				var fiberEntity = dynamicDB.SelectOne<FiberEntity>($@"SELECT f.Id,f.Code FROM {fibersCatalog.TableName} f WHERE f.English = '{fiberValueEnglish}'");

				return fiberEntity?.Code;
			}
		}

		private void AddSectionTitleSeparator(StringBuilder stringBuilder, int totalNumberOfSections, int actualSectionNumber, string fiberValue, StringBuilder fibersStrings)
		{
			if (actualSectionNumber < totalNumberOfSections - 1)
			{
				fiberValue += SECTION_SEPARATOR;
				fibersStrings.Append(SECTION_SEPARATOR);
			}

			stringBuilder.Append(fibersStrings);
		}

		private (string, string, Dictionary<string, string>) ConcatenateCareInstructionsAndSymbols(IEnumerable<CareInstruction> careInstructions)
		{

			StringBuilder careInstructionsBuilder = new StringBuilder();
			StringBuilder symbolsBuilder = new StringBuilder(string.Empty, 10);
			var careInstructionsLanguages = new Dictionary<string, string>();

			foreach (var careInstruction in careInstructions)
			{
				var langsList = careInstruction.AllLangs.Split(COMPOSITION_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
				ConcatenateSymbols(careInstruction, symbolsBuilder);

				string translations = AddTranslationsCareInstructions(careInstructionsLanguages, langsList);

				careInstructionsBuilder.Append(translations);
				careInstructionsBuilder.Append(CAREINSTRUCTION_SEPARATOR);
			}
			return (careInstructionsBuilder.ToString(), symbolsBuilder.ToString(), careInstructionsLanguages);
		}

		private string AddTranslationsCareInstructions(Dictionary<string, string> careInstructionsLanguages, string[] langsList)
		{
			for (int i = 0; i < langsList.Length; i++)
			{
				if (!careInstructionsLanguages.ContainsKey(CareInstructionsLanguageSortedAll[i]))
					careInstructionsLanguages.Add(CareInstructionsLanguageSortedAll[i], langsList[i]);
				else
					careInstructionsLanguages[CareInstructionsLanguageSortedAll[i]] += string.Format("{0}{1}", CAREINSTRUCTION_LANGUAGE_SEPARATOR, langsList[i]);

			}

			var translations = langsList.Length > 1 ? string.Join(CAREINSTRUCTION_LANGUAGE_SEPARATOR, langsList) : langsList[0];
			return translations;
		}

		private void ConcatenateSymbols(CareInstruction careInstruction, StringBuilder symbolsBuilder)
													=> symbolsBuilder.Append(careInstruction.Symbol);

		private void AddCompositionArticleTable(int projectId, int orderId, int orderGroupId)
		{
			var order = orderRepo.GetByID(orderId);
			var jsonArticle = string.Empty;
			var jsonOldArticle = string.Empty;

			using (PrintDB ctx = factory.GetInstance<PrintDB>())
			{
				ICatalog ordersCatalog, detailCatalog, variableDataCatalog, compositionLabelCatalog, articlesCatalog, baseDataCatalog;
				GetCatalogs(projectId, out ordersCatalog, out detailCatalog, out variableDataCatalog, out compositionLabelCatalog, out articlesCatalog, out baseDataCatalog);

				var relField = ordersCatalog.Fields.First(w => w.Name == "Details");


				using (var dynamicDb = connManager.OpenDB("CatalogDB"))
				{
					var variableDataItem = dynamicDb.SelectOne<VariableDataCompleteItem>(
										$@"SELECT v.TXT1, v.HasComposition, v.MadeIn,v.CIF, bd.Articulo as ClientArticle
                                        FROM {ordersCatalog.TableName} o
                                        INNER JOIN [dbo].[REL_{ordersCatalog.CatalogID}_{detailCatalog.CatalogID}_{relField.FieldID}] rel ON o.ID = rel.SourceID
                                        INNER JOIN {detailCatalog.TableName} d ON rel.TargetID = d.ID
                                        INNER JOIN {variableDataCatalog.TableName} v ON d.Product = v.ID
                                        INNER JOIN {compositionLabelCatalog.TableName} c ON v.HasComposition = c.ID
                                        INNER JOIN {baseDataCatalog.TableName} bd ON bd.ID = v.IsBaseData
                                        WHERE o.ID = {order.OrderDataID}");

                    //string articleCode = GetArticleCodeBaseDataTable(baseDataCatalog, order.OrderNumber, dynamicDb);
                    string articleCode = variableDataItem.ClientArticle;

					ArticleDto article = SearchArticle(orderGroupId, articlesCatalog, dynamicDb, variableDataItem, articleCode);

					//jsonArticle = Newtonsoft.Json.JsonConvert.SerializeObject(article);
					_ = dynamicDb.ExecuteQuery($"UPDATE {articlesCatalog.TableName} " +
						$"SET [CIF] = @cif," +
						$"[OrderGroupID] = @orderGroupID," +
						$"[HasComposition] = @hasComposition," +
						$"[IsMadeIn] = @isMadeIn," +
						$"[CreatedAt] = @createdAt " +
						$"WHERE [ArticleCode] = @articleCode"
						, variableDataItem.CIF, orderGroupId, variableDataItem.HasComposition, variableDataItem.MadeIn, DateTime.Now, article.ArticleCode);

				}

			}
		}
        //private string CleanOrderNumber(string orderNumber)
        //{
        //    if(string.IsNullOrEmpty(orderNumber))
        //        return orderNumber;

        //    // Pattern to match: ends with "-" followed by one or more digits followed by "R"
        //    var pattern = @"-\d+R$";

        //    if(Regex.IsMatch(orderNumber, pattern))
        //    {
        //        return Regex.Replace(orderNumber, pattern, "");
        //    }

        //    return orderNumber;
        //}

  //      private string GetArticleCodeBaseDataTable(ICatalog baseDataCatalog, string orderNumber, IDBX dynamicDb)
		//{
  //          orderNumber = CleanOrderNumber(orderNumber);
		//	var articleCodeBD = dynamicDb.SelectOne<BaseData>($@"SELECT bd.Articulo AS ArticleCode FROM BaseData_{baseDataCatalog.CatalogID} bd WHERE bd.[Pedido] = @orderNumber", orderNumber);

		//	return articleCodeBD.ArticleCode;

		//}

		private ArticleDto SearchArticle(int orderGroupId, ICatalog articlesCatalog, IDBX dynamicDb, VariableDataCompleteItem variableDataItem, string articleCode)
		{
			var article = dynamicDb.SelectOne<ArticleDto>(
									$@"SELECT a.* FROM {articlesCatalog.TableName} a WHERE a.[ArticleCode] = '{articleCode}' and a.[Active] = 1;");

			article.CIF = variableDataItem.CIF;
			article.OrderGroupID = orderGroupId;
			article.HasComposition = variableDataItem.HasComposition;
			article.IsMadeIn = variableDataItem.MadeIn;
			article.CreatedAt = DateTime.Now;
			return article;
		}

		private void GetCatalogs(int projectId, out ICatalog ordersCatalog, out ICatalog detailCatalog, out ICatalog variableDataCatalog, out ICatalog compositionLabelCatalog, out ICatalog articlesCatalog, out ICatalog baseDataCatalog)
		{
			var catalogs = catalogRepo.GetByProjectID(projectId, true);
			ordersCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
			detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
			variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
			compositionLabelCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));
			articlesCatalog = catalogs.First(catalog => catalog.Name.Contains(ARTICLES_TABLENAME));
			baseDataCatalog = catalogs.First(f => f.Name.Equals(BASEDATA_TABLENAME));
		}

		public void Dispose()
		{
		}

		public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
		{
			throw new NotImplementedException();
		}

		public void SaveCompoPreview(OrderPluginData od, string[] compoArray, string[] percentArray, string[] leahterArray, string[] additionalArray, int labelLines, int ID)
		{

		}

		public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets)
		{

		}

		public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data)
		{

		}
	}
}
