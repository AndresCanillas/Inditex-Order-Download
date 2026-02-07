using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Contracts.Models
{
	public class MappingRepository : GenericRepository<IDataImportMapping, DataImportMapping>, IMappingRepository
	{
		private IDBConnectionManager connManager;
		private IAppInfo appInfo;

		public MappingRepository(
			IFactory factory,
			IDBConnectionManager connManager,
			IAppInfo appInfo
			)
			: base(factory, (ctx) => ctx.DataImportMappings)
		{
			this.connManager = connManager;
			this.appInfo = appInfo;
		}


		protected override string TableName { get => "DataImportMappings"; }


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, DataImportMapping actual, IDataImportMapping data)
		{
			actual.Name = data.Name;
			actual.RootCatalog = data.RootCatalog;
			actual.SourceType = data.SourceType;
			actual.FileNameMask = data.FileNameMask;
			actual.SourceCulture = data.SourceCulture;
			actual.Encoding = data.Encoding;
			actual.LineDelimiter = data.LineDelimiter;
            actual.ColumnDelimiter = data.ColumnDelimiter;
			actual.QuotationChar = data.QuotationChar;
			actual.IncludeHeader = data.IncludeHeader;
			actual.Plugin = data.Plugin;
		}


		protected override void BeforeDelete(PrintDB ctx, IUserData userData, DataImportMapping actual, out bool cancelOperation)
		{
			cancelOperation = false;
			ctx.Database.ExecuteSqlCommand("delete from DataImportColMapping where DataImportMappingID = @mappingId", new SqlParameter("mappingId", actual.ID));
		}


		public List<IDataImportMapping> GetByProjectID(int projectid)
		{
			using(var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByProjectID(ctx, projectid);
			}
		}


		public List<IDataImportMapping> GetByProjectID(PrintDB ctx, int projectid)
		{
			var result = All(ctx).Where(p => p.ProjectID == projectid).AsNoTracking();
			return new List<IDataImportMapping>(result);
		}


		public List<IDataImportColMapping> GetColumnMappings(int id)
		{
			using(var ctx = factory.GetInstance<PrintDB>())
			{
				return GetColumnMappings(ctx, id);
			}
		}


		public List<IDataImportColMapping> GetColumnMappings(PrintDB ctx, int id)
		{
			var row = GetByID(id);
			if (row != null)
			{
				var mappings = ctx.DataImportColMapping
					.Where(p => p.DataImportMappingID == row.ID)
					.OrderBy(p => p.ColOrder).AsNoTracking().ToList();

				return new List<IDataImportColMapping>(mappings);
			}
			else return null;
		}


		public IDataImportColMapping AddColumn(int id)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return AddColumn(ctx, id);
			}
		}


		public IDataImportColMapping AddColumn(PrintDB ctx, int id)
		{
			DataImportColMapping col = new DataImportColMapping();
			col.DataImportMappingID = id;
			var count = ctx.DataImportColMapping.Where(c => c.DataImportMappingID == id).Count();
			col.ColOrder = count + 1;
			ctx.DataImportColMapping.Add(col);
			ctx.SaveChanges();
			return col;
		}


		public IDataImportColMapping InsertColumn(int id, int pos)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return InsertColumn(ctx, id, pos);
			}
		}


		public IDataImportColMapping InsertColumn(PrintDB ctx, int id, int pos)
		{
			ctx.Database.ExecuteSqlCommand("update DataImportColMapping set ColOrder = ColOrder+1 where DataImportMappingID = @id and ColOrder >= @pos", new SqlParameter("@id", id), new SqlParameter("@pos", pos));
			DataImportColMapping col = new DataImportColMapping();
			col.DataImportMappingID = id;
			col.ColOrder = pos;
			ctx.DataImportColMapping.Add(col);
			ctx.SaveChanges();
			return col;
		}


		public IDataImportColMapping MoveColumnDown(int colid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return MoveColumnDown(ctx, colid);
			}
		}


		public IDataImportColMapping MoveColumnDown(PrintDB ctx, int colid)
		{
			var col = ctx.DataImportColMapping.Where(c => c.ID == colid).FirstOrDefault();
			var nextCol = ctx.DataImportColMapping.Where(c => c.DataImportMappingID == col.DataImportMappingID && c.ColOrder > col.ColOrder && c.ID != col.ID).OrderBy(c => c.ColOrder).Take(1).FirstOrDefault();
			if (nextCol != null)
			{
				col.ColOrder++;
				nextCol.ColOrder--;
				ctx.SaveChanges();
			}
			return col;
		}


		public IDataImportColMapping MoveColumnUp(int colid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return MoveColumnUp(ctx, colid);
			}
		}


		public IDataImportColMapping MoveColumnUp(PrintDB ctx, int colid)
		{
			var col = ctx.DataImportColMapping.Where(c => c.ID == colid).FirstOrDefault();
			var prevCol = ctx.DataImportColMapping.Where(c => c.DataImportMappingID == col.DataImportMappingID && c.ColOrder < col.ColOrder && c.ID != col.ID).OrderByDescending(c => c.ColOrder).Take(1).FirstOrDefault();
			if (prevCol != null)
			{
				col.ColOrder--;
				prevCol.ColOrder++;
				ctx.SaveChanges();
			}
			return col;
		}


		public void DeleteColumn(int colid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				DeleteColumn(ctx, colid);
			}
		}


		public void DeleteColumn(PrintDB ctx, int colid)
		{
			var col = ctx.DataImportColMapping.Where(c => c.ID == colid).AsNoTracking().FirstOrDefault();
			ctx.Database.ExecuteSqlCommand("update DataImportColMapping set ColOrder = ColOrder-1 where DataImportMappingID = @id and ColOrder >= @pos",
				new SqlParameter("@id", col.DataImportMappingID),
				new SqlParameter("@pos", col.ColOrder));
			ctx.Database.ExecuteSqlCommand("delete from DataImportColMapping where ID = @id", new SqlParameter("@id", colid));
		}


		public void UpdateColumnMappings(List<IDataImportColMapping> columns)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				UpdateColumnMappings(ctx, columns);
			}
		}


		public void UpdateColumnMappings(PrintDB ctx, List<IDataImportColMapping> columns)
		{
			if (columns.Count > 0)
			{
				var id = columns[0].DataImportMappingID;
				var mappings = ctx.DataImportColMapping.Where(p => p.DataImportMappingID == id).ToList();
				foreach (var map in mappings)
				{
					var val = columns.FirstOrDefault(p => p.ID == map.ID);
					if (val != null)
						Reflex.Copy(map, val);
				}
				ctx.SaveChanges();
			}
		}


		private static List<EncodingDTO> encodings = new List<EncodingDTO>();


		public List<EncodingDTO> GetEncodings()
		{
			if (encodings.Count == 0)
			{
				lock (encodings)
				{
					if (encodings.Count == 0)
					{
						var encodingsFile = Path.Combine(appInfo.AssemblyDir, "encodings.json");
						var json = File.ReadAllText(encodingsFile);
						encodings.AddRange(JsonConvert.DeserializeObject<List<EncodingDTO>>(json));
					}
				}
			}
			return encodings;
		}


		public List<CultureDTO> GetCultures()
		{
			var list = new List<CultureDTO>();
			var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
			foreach (var culture in cultures)
			{
				list.Add(new CultureDTO() { Name = culture.Name, DisplayName = culture.DisplayName });
			}
			list.Sort((a, b) => String.Compare(a.DisplayName, b.DisplayName, true));
			return list;
		}


		public IDataImportMapping Duplicate(int mappingid, string name)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return Duplicate(ctx, mappingid, name);
			}
		}


		public IDataImportMapping Duplicate(PrintDB ctx, int mappingid, string name)
		{
			var mapping = ctx.DataImportMappings.Where(m => m.ID == mappingid).AsNoTracking().FirstOrDefault();
			if (mapping == null)
				throw new Exception($"Duplicate Operation: Mapping {mappingid} could not be found.");

			mapping.ID = 0;
			mapping.Name = name;
			ctx.DataImportMappings.Add(mapping);
			ctx.SaveChanges();

			var originalColMappings = GetColumnMappings(ctx, mappingid);
			foreach (var originalCol in originalColMappings)
			{
				var col = (DataImportColMapping)originalCol;
				col.ID = 0;
				col.DataImportMappingID = mapping.ID;
				ctx.DataImportColMapping.Add(col);
			}
			ctx.SaveChanges();
			events.Send(new EntityEvent(mapping.GetCompanyID(ctx), mapping, DBOperation.Insert));
			return mapping;
		}


		public List<IDataImportColMapping> InitializeMappingsFromCatalog(int mappingid, int catalogid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
				return InitializeMappingsFromCatalog(ctx, mappingid, catalogid);
		}


		public List<IDataImportColMapping> InitializeMappingsFromCatalog(PrintDB ctx, int mappingid, int catalogid)
		{
			var catalog = ctx.Catalogs.Where(c => c.ID == catalogid).FirstOrDefault();
			
			if (catalog == null)
				throw new Exception($"Could not find catalog (ID:{catalogid})");
			
			List<DataImportColMapping> columns;
			
			if (catalog.Name == "Orders")
				columns = InitColMappingsForOrders(ctx, catalog, mappingid);
			else
				columns = InitColMappingsFromCatalog(catalog, mappingid);
			
			foreach (var col in columns)
				ctx.DataImportColMapping.Add(col);
			
			ctx.SaveChanges();
			
			return new List<IDataImportColMapping>(columns);
		}


		private List<DataImportColMapping> InitColMappingsForOrders(PrintDB ctx, Catalog catalog, int mappingid)
		{
			var detailCatalog = ctx.Catalogs.Where(c => c.Name.Equals("OrderDetails") && c.ProjectID.Equals(catalog.ProjectID)).FirstOrDefault();
			var variableDataCatalog = ctx.Catalogs.Where(c => c.Name.Equals("VariableData") && c.ProjectID.Equals(catalog.ProjectID)).FirstOrDefault();

			var columns = InitColMappingsFromCatalog(catalog, mappingid);
			columns.AddRange(InitColMappingsFromCatalog(detailCatalog, mappingid));
			columns.AddRange(InitColMappingsFromCatalog(variableDataCatalog, mappingid));

			// fix Index
			int sort = 1;
			foreach (var col in columns)
				col.ColOrder = sort++;

			return columns;
		}


		private List<DataImportColMapping> InitColMappingsFromCatalog(Catalog catalog, int mappingid)
		{
			var fields = catalog.Fields.ToList();
			var columns = new List<DataImportColMapping>(fields.Count);
			int idx = 1;
			foreach (var f in fields)
			{
				if (f.Name == "ID" || f.Type == ColumnType.Set || f.Type == ColumnType.Reference) continue;
				var c = InitColMappingFromField(catalog, f, idx);
				if (f.Name == "OrderNumber" && catalog.Name == "Orders")
					c.TargetColumn = "#OrderNumber";
				c.DataImportMappingID = mappingid;
				columns.Add(c);
				idx++;
			}
			return columns;
		}


		private DataImportColMapping InitColMappingFromField(Catalog catalog, FieldDefinition f, int index)
		{
			var col = new DataImportColMapping();
			col.ColOrder = index;
			col.InputColumn = f.Name;
			col.Ignore = false;
			col.Type = (int)GetDocumentColumnType(f.Type);
			col.IsFixedValue = false;
			col.MinLength = (f.CanBeEmpty ? 0 : 1);
			col.MaxLength = f.Length;
			col.MinValue = f.MinValue;
			col.MaxValue = f.MaxValue;
			col.MinDate = f.MinDate;
			col.MaxDate = f.MaxDate;
			col.CanBeEmpty = f.CanBeEmpty;
			col.TargetColumn = f.Name;
			return col;
		}


		private DocumentColumnType GetDocumentColumnType(ColumnType type)
		{
			switch (type)
			{
				case ColumnType.Int:
					return DocumentColumnType.Int32;
				case ColumnType.Long:
					return DocumentColumnType.Int64;
				case ColumnType.Decimal:
					return DocumentColumnType.Decimal;
				case ColumnType.Date:
					return DocumentColumnType.DateTime;
				case ColumnType.Bool:
					return DocumentColumnType.Boolean;
				case ColumnType.String:
					return DocumentColumnType.Text;
				default:
					throw new Exception($"Invalid data type: Cannot create document import mapping for a field of type {type}");
			}
		}


		public DocumentImportConfiguration GetDocumentImportConfiguration(string userName, int projectid, IFSFile file)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetDocumentImportConfiguration(ctx, userName, projectid, file);
			}
		}


		public DocumentImportConfiguration GetDocumentImportConfiguration(PrintDB ctx, string userName, int projectid, IFSFile file)
		{
			var mapping = GetMappingsByFileMask(ctx, projectid, file.FileName);
			var rootcatalog = (from c in ctx.Catalogs where c.ProjectID == projectid && c.ID == mapping.RootCatalog select c).SingleOrDefault();
			if (rootcatalog == null)
				throw new Exception($"Could not find catalog with ID {mapping.RootCatalog} in project {projectid}.");
			var processMapping = CreateProcessMapping(ctx, mapping, projectid);
			return CreateDocumentImportConfigurationAsync(ctx, processMapping, projectid, userName, rootcatalog, file).Result;
		}


		public DocumentImportConfiguration GetDocumentImportConfiguration(string userName, int projectid, string catalogName, IFSFile file)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetDocumentImportConfiguration(ctx, userName, projectid, catalogName, file);
			}
		}


		public DocumentImportConfiguration GetDocumentImportConfiguration(PrintDB ctx, string userName, int projectid, string catalogName, IFSFile file)
		{
			var rootcatalog = (from c in ctx.Catalogs where c.ProjectID == projectid && c.Name == catalogName select c).SingleOrDefault();
			if (rootcatalog == null)
				throw new Exception($"There is no mapping for {catalogName} in project {projectid}.");
			var mapping = GetMappings(ctx, projectid, rootcatalog.ID, userName, file.FileName);
			return CreateDocumentImportConfigurationAsync(ctx, mapping, projectid, userName, rootcatalog, file).Result;
		}


		public async Task<DocumentImportConfiguration> GetDocumentImportConfigurationAsync(string userName, int projectid, string catalogName, IFSFile file)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return await GetDocumentImportConfigurationAsync(ctx, userName, projectid, catalogName, file);
			}
		}

		public async Task<DocumentImportConfiguration> GetDocumentImportConfigurationAsync(PrintDB ctx, string userName, int projectid, string catalogName, IFSFile file)
		{
			var rootcatalog = await (from c in ctx.Catalogs where c.ProjectID == projectid && c.Name == catalogName select c).SingleOrDefaultAsync();
			if (rootcatalog == null)
				throw new MappingNotFoundException($"There is no mapping for {catalogName} in project {projectid}.");
			var mapping = await GetMappingsAsync(ctx, projectid, rootcatalog.ID, userName, file.FileName);
			return await CreateDocumentImportConfigurationAsync(ctx, mapping, projectid, userName, rootcatalog, file);
		}


		private async Task<DocumentImportConfiguration> CreateDocumentImportConfigurationAsync(PrintDB ctx, IProcessMappings mapping, int projectid, string userName, Catalog catalog, IFSFile file)
		{
			var project = await ctx.Projects.FirstAsync(p => p.ID == projectid);
			var brand = await ctx.Brands.FirstAsync(b => b.ID == project.BrandID);
            //var colDelimiter = String.IsNullOrEmpty(mapping.Data.ColumnDelimiter) ? ',' : mapping.Data.ColumnDelimiter[0];
            var colDelimiter = mapping.Data.ColumnDelimiter;
            var quoteChar = String.IsNullOrEmpty(mapping.Data.QuotationChar) ? '"' : mapping.Data.QuotationChar[0];
			var config = new DocumentImportConfiguration()
			{
				JobID = Guid.NewGuid().ToString(),
				User = userName,
				CompanyID = brand.CompanyID,
				BrandID = brand.ID,
				ProjectID = projectid,
				FileName = file.FileName,
				FileGUID = file.FileGUID,
				Input = new InputConfiguration()
				{
					SourceType = mapping.Data.SourceType,
					SourceCulture = mapping.Data.SourceCulture,
					Encoding = mapping.Data.Encoding,
					LineDelimiter = mapping.Data.LineDelimiter,
					ColumnDelimiter = colDelimiter.GetValueOrDefault(),
					QuotationChar = quoteChar,
					IncludeHeader = mapping.Data.IncludeHeader,
					Plugin = mapping.Data.Plugin
				},
				Output = new OutputConfiguration()
				{
					TargetDB = "CatalogDB",
					CatalogID = catalog.CatalogID,
					Mappings = mapping.Columns
				}
			};
			return config;
		}


		public DocumentImportConfiguration GetBatchFileImportConfiguration(string userName, int projectid, IFSFile file)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetBatchFileImportConfiguration(ctx, userName, projectid, file);
			}
		}


		public DocumentImportConfiguration GetBatchFileImportConfiguration(PrintDB ctx, string userName, int projectid, IFSFile file)
		{
			var project = ctx.Projects.First(p => p.ID == projectid);
			var brand = ctx.Brands.First(b => b.ID == project.BrandID);
			DocumentImportConfiguration config = new DocumentImportConfiguration();
			config.FileName = file.FileName;
			config.FileGUID = file.FileGUID;
			config.User = userName;
			config.CompanyID = brand.CompanyID;
			config.BrandID = brand.ID;
			config.ProjectID = projectid;
			config.Input.SourceCulture = "es-ES";
			config.Input.SourceType = "Excel";
			config.Input.IncludeHeader = true;
			config.Output.Mappings.Add(new DocumentColMapping()
			{
				CanBeEmpty = false,
				Ignore = false,
				InputColumn = "EAN13",
				TargetColumn = "Barcode",
				Type = DocumentColumnType.Text,
				Function = (int)ValidationFunction.EAN13
			});
			config.Output.Mappings.Add(new DocumentColMapping()
			{
				CanBeEmpty = false,
				Ignore = false,
				InputColumn = "Modelo_Label",
				TargetColumn = "ArticleCode",
				Type = DocumentColumnType.Text,
				Function = (int)ValidationFunction.SetLookup,
				FunctionArguments = GetProjectArticleCodes(ctx, projectid)
			});
			config.Output.Mappings.Add(new DocumentColMapping()
			{
				CanBeEmpty = false,
				Ignore = false,
				InputColumn = "Cantidad",
				TargetColumn = "Quantity",
				Type = DocumentColumnType.Int32,
				MinValue = 1,
				MaxValue = 100000
			});
			return config;
		}


		public DocumentImportConfiguration GetCatalogImportConfiguration(string userName, int catalogid, IFSFile file)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetCatalogImportConfiguration(ctx, userName, catalogid, file);
			}
		}


		public DocumentImportConfiguration GetCatalogImportConfiguration(PrintDB ctx, string userName, int catalogid, IFSFile file)
		{
			var catalog = (from c in ctx.Catalogs where c.CatalogID == catalogid select c).SingleOrDefault();
			if (catalog == null)
				throw new Exception($"There is no mapping for the specified catalog.");
			var project = ctx.Projects.First(p => p.ID == catalog.ProjectID);
			var brand = ctx.Brands.First(b => b.ID == project.BrandID);
			List<FieldDefinition> fields = catalog.Fields.ToList();
			DocumentImportConfiguration config = new DocumentImportConfiguration();
			config.FileName = file.FileName;
			config.FileGUID = file.FileGUID;
			config.CompanyID = brand.CompanyID;
			config.BrandID = brand.ID;
			config.ProjectID = catalog.ProjectID;
			config.User = userName;
			var ext = Path.GetExtension(file.FileName).ToLower();
			if (ext == ".csv" || ext == ".txt")
				GetInputConfigForTextFile(config, fields, file);
			else
				GetInputConfigForExcelFile(config);
			config.Output.CatalogID = catalog.CatalogID;
			config.Output.TargetDB = "CatalogDB";
			CreateMappingsFromCatalog(config, fields);
			return config;
		}


		public void CreateMappingsFromCatalog(DocumentImportConfiguration config, List<FieldDefinition> fields)
		{
			foreach(var f in fields)
			{
				if (f.IsHidden) continue;
				if (f.Type == ColumnType.Reference || f.Type == ColumnType.Set) continue;
				config.Output.Mappings.Add(new DocumentColMapping()
				{
					CanBeEmpty = f.CanBeEmpty,
					DecimalPlaces = 0,
					Ignore = false,
					InputColumn = f.Name,
					TargetColumn = f.Name,
					Type = GetMappingTypeFromColumnType(f.Type)
				});
			}
		}

		public DocumentColumnType GetMappingTypeFromColumnType(ColumnType type)
		{
			switch (type)
			{
				case ColumnType.Bool: return DocumentColumnType.Boolean;
				case ColumnType.Date: return DocumentColumnType.DateTime;
				case ColumnType.Decimal: return DocumentColumnType.Int64;
				case ColumnType.Int: return DocumentColumnType.Int32;
				case ColumnType.Long: return DocumentColumnType.Int64;
				case ColumnType.String: return DocumentColumnType.Text;
				default: return DocumentColumnType.Text;
			}
		}

		private void GetInputConfigForExcelFile(DocumentImportConfiguration config)
		{
			config.Input.SourceCulture = "es-ES";
			config.Input.SourceType = "Excel";
			config.Input.IncludeHeader = true;
		}


		private void GetInputConfigForTextFile(DocumentImportConfiguration config, List<FieldDefinition> fields, IFSFile file)
		{
			int rb;
			var buffer = new byte[5000];
			using (Stream s = file.GetContentAsStream())
			{
				rb = s.Read(buffer, 0, buffer.Length);
			}
			var text = Encoding.UTF8.GetString(buffer);
			int index1 = text.IndexOf('\r');
			int index2 = text.IndexOf('\n');
			if (index1 > 0)
			{
				if (index1 + 1 == index2)
					config.Input.LineDelimiter = "\r\n";
				else
					config.Input.LineDelimiter = "\r";
			}
			else if (index2 > 0)
			{
				if (index2 + 1 == index1)
					config.Input.LineDelimiter = "\n\r";
				else
					config.Input.LineDelimiter = "\n";
			}
			config.Input.QuotationChar = '"';
			var field = fields.Where(p => p.Type == ColumnType.String).FirstOrDefault();
			if (field != null)
			{
				index1 = text.IndexOf(field.Name, 0, 200);
				config.Input.IncludeHeader = (index1 >= 0);
				if (config.Input.IncludeHeader)
					config.Input.ColumnDelimiter = text[index1 + field.Name.Length];
				else
					config.Input.ColumnDelimiter = GetColumnDelimiterFromText(text);
			}
			else
			{
				config.Input.ColumnDelimiter = GetColumnDelimiterFromText(text);
				config.Input.IncludeHeader = true;
			}
			config.Input.Encoding = "UTF-8";
			config.Input.SourceType = "Delimited";
			config.Input.SourceCulture = "es-ES";
		}


		private char GetColumnDelimiterFromText(string text)
		{
			if (text.IndexOf(',') >= 0)
				return ',';
			else if (text.IndexOf(';') >= 0)
				return ';';
			else if (text.IndexOf('\t') >= 0)
				return '\t';
			else
				return ',';
		}


		private DataImportMapping GetMappingsByFileMask(PrintDB ctx, int projectid, string fileName)
		{
			var mappings = ctx.DataImportMappings
			.Where(p => p.ProjectID == projectid)
			.Include(p => p.Mappings)
			.AsNoTracking().ToList();
			if (mappings.Count == 1)
			{
				return mappings[0];
			}
			else
			{
				foreach (var element in mappings)
				{
					if (!String.IsNullOrWhiteSpace(element.FileNameMask))
					{
						Regex r = new Regex(element.FileNameMask.Trim(), RegexOptions.IgnoreCase);
						if (r.IsMatch(fileName))
							return element;
					}
				}
			}
			throw new Exception("Could not find any mappings to process this file.");
		}


		private IProcessMappings GetMappings(PrintDB ctx, int projectid, int rootcatalog, string userName, string fileName)
		{
			var mappings = ctx.DataImportMappings
			.Where(p => p.ProjectID == projectid && p.RootCatalog == rootcatalog)
			.Include(p => p.Mappings)
			.AsNoTracking().ToList();
			return GetProcessMappings(ctx, mappings, projectid, userName, fileName, false);
		}


		private async Task<IProcessMappings> GetMappingsAsync(PrintDB ctx, int projectid, int rootcatalog, string userName, string fileName)
		{
			var mappings = await ctx.DataImportMappings
			.Where(p => p.ProjectID == projectid && p.RootCatalog == rootcatalog)
			.Include(p => p.Mappings)
			.AsNoTracking().ToListAsync();
			return GetProcessMappings(ctx, mappings, projectid, userName, fileName, false);
		}


		private ProcessMappings GetProcessMappings(PrintDB ctx, List<DataImportMapping> mappings, int projectid, string userName, string fileName, bool ignoreMask)
		{
			StringBuilder sb = new StringBuilder(50);
			DataImportMapping match = null;
			if (ignoreMask && mappings.Count > 0)
			{
				match = mappings[0];
			}
			else
			{
				if(fileName.Contains('\\'))
					fileName = Path.GetFileName(fileName);

				foreach (var e in mappings)
				{
					sb.Append($"{e.ID} - {e.Name}\r\n");
					if (!String.IsNullOrWhiteSpace(e.FileNameMask))
					{
						Regex r = new Regex(e.FileNameMask.Trim(), RegexOptions.IgnoreCase);
						if (r.IsMatch(fileName))
						{
							match = e;
							break;
						}
					}
				}
			}
			if (match == null)
				throw new MappingNotFoundException($"Could not find any configuration to handle file: {Path.GetFileName(fileName)}. Tested mappings:\r\n{sb.ToString()}", fileName);
			return CreateProcessMapping(ctx, match, projectid);
		}


		private ProcessMappings CreateProcessMapping(PrintDB ctx, DataImportMapping match, int projectid)
		{
			/* Intake Workflow Change:
			 * 
			 * NOTE: Supplier (BillTo/SendTo) and ArticleCode validations should no longer be done on the document service.
			 *       Instead those validations will be done in the IntakeWorkflow. Because of that, we need to remove the
			 *       code below.
			 *
			 */

			//string providers = "";
			//var articleCodeMapping = match.Mappings.FirstOrDefault(p => !String.IsNullOrWhiteSpace(p.TargetColumn) && p.TargetColumn.EndsWith("ArticleCode"));
			//var billToMapping = match.Mappings.FirstOrDefault(p => !String.IsNullOrWhiteSpace(p.TargetColumn) && p.TargetColumn == "BillTo");
			//var sendToMapping = match.Mappings.FirstOrDefault(p => !String.IsNullOrWhiteSpace(p.TargetColumn) && p.TargetColumn == "SendTo");
			//if (articleCodeMapping != null && (articleCodeMapping.Function ?? 0) == 0)
			//{
			//	articleCodeMapping.Function = (int)ValidationFunction.SetLookup;
			//	articleCodeMapping.FunctionArguments = GetProjectArticleCodes(ctx, projectid);
			//}
			//if (billToMapping != null || sendToMapping != null)
			//	providers = GetCompanyProviders(ctx, projectid);
			//if (billToMapping != null)
			//{
			//	// IMPORTANT: BillTo and SendTo mappings MUST have a value mapping and cannot have any other function configured. They can have FixedValues (if needed).
			//	billToMapping.Function = (int)ValidationFunction.ValueMapping;
			//	billToMapping.FunctionArguments = providers;
			//}
			//if (sendToMapping != null)
			//{
			//	// IMPORTANT: BillTo and SendTo mappings MUST have a value mapping and cannot have any other function configured. They can have FixedValues (if needed).
			//	sendToMapping.Function = (int)ValidationFunction.ValueMapping;
			//	sendToMapping.FunctionArguments = providers;
			//}

			match.Mappings = match.Mappings.OrderBy(p => p.ColOrder).ToList();
			var result = new ProcessMappings(match);
			return result;
		}




		private string GetProjectArticleCodes(PrintDB ctx, int projectid)
		{
			var names = ctx.Articles
			.Where(p => p.ProjectID == projectid)
			.Select(p => new { Name = p.ArticleCode })
			.Union(
				ctx.Articles
				.Where(p => p.ProjectID == null)
				.Select(p => new { Name = p.ArticleCode }))
			.Union(
				ctx.Packs
				.Where(p => p.ProjectID == projectid)
				.Select(p => new { Name = p.PackCode }))
			.AsNoTracking()
			.ToList();

			StringBuilder sb = new StringBuilder(500);
			foreach (var label in names)
				sb.Append(label).Append(',');
			if (sb.Length > 1)
				sb.Remove(sb.Length - 1, 1);
			return sb.ToString();
		}


		// This generates a ValueMap (key1=value1, key2=value2, etc) containing all valid options for the BillTo and SendTo fields.
		// This value map is used to ensure that the BillTo and SendTo fields of an order are properly validated. 
		//
		// Example: Assume the issuing company has CompanyCode = "SMD-0001", and that this company has only one provider configured:
		// CompanyCode = "SMD-0002". This means that the values in the BillTo and SendTo fields MUST be either "SMD-0001" (to bill or
		// send to itself) or "SMD-0002" (to bill or send to the provider). 
		//
		// However, the internal CompanyCode used by IDT is rarely sent by a client in an order, instead they use their own
		// "refereces" (ClientReference Field), so the value map also includes the configured client references as valid options.
		// If the company "SMD-0001" has ClientReference "A", and company "SMD-0002" has ClientReference "X", then either 
		// of those values will be recognized as valid, and the Value Map will take care of turning the ClientReferences into the
		// correct CompanyCodes required to process the order.
		private string GetCompanyProviders(PrintDB ctx, int projectid)
		{
			StringBuilder sb = new StringBuilder(100);

			var companyid = (from p in ctx.Projects
							 join b in ctx.Brands on p.BrandID equals b.ID
							 where p.ID == projectid
							 select b.CompanyID).Single();

			var client = ctx.Companies.Where(p => p.ID == companyid).AsNoTracking().FirstOrDefault();
			if (client != null)
			{
				// Append the issuing company (itself) to the ValueMap
				sb.Append($"{client.CompanyCode}={client.CompanyCode};{client.ClientReference}={client.CompanyCode};");

				// Append all providers to the ValueMap
				var qry = (from a in ctx.CompanyProviders
						   join b in ctx.Companies on a.ProviderCompanyID equals b.ID
						   where a.CompanyID == companyid
						   select new { b.CompanyCode, a.ClientReference }).AsNoTracking().ToList();

				foreach (var cc in qry)
				{
					sb.Append($"{cc.CompanyCode}={cc.CompanyCode};{cc.ClientReference}={cc.CompanyCode};");
				}

				if (sb.Length > 1)
					sb.Remove(sb.Length - 1, 1);
				return sb.ToString();
			}
			else return "";
		}
    }


	public class ProcessMappings : IProcessMappings
	{
		public IDataImportMapping Data { get; set; }
		public List<DocumentColMapping> Columns { get; set; }


		public ProcessMappings(DataImportMapping data)
		{
			CultureInfo ci = new CultureInfo(data.SourceCulture);
			Data = data;
			var columns = new List<DocumentColMapping>();
			foreach (var col in data.Mappings)
				columns.Add(Reflex.Copy(new DocumentColMapping(), col));
			Columns = columns;
		}
	}
}
