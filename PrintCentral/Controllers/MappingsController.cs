using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
	[Authorize]
	public class MappingsController : Controller
	{
		private IMappingRepository repo;
        private IUserData userData;
        private ILogService log;
		private ILocalizationService g;

		public MappingsController(
            IMappingRepository repo,
            IUserData userData,
            ILogService log, 
            ILocalizationService g)
		{
			this.repo = repo;
            this.userData = userData;
            this.log = log;
			this.g = g;
		}

		[HttpPost, Route("/mappings/insert")]
		public OperationResult Insert([FromBody]DataImportMapping data)
		{
			try
			{
                if (!userData.Admin_Mappings_CanAdd)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Mappings Created!"], repo.Insert(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/mappings/update")]
		public OperationResult Update([FromBody]DataImportMapping data)
		{
			try
			{
                if (!userData.Admin_Mappings_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Mappings saved!"], repo.Update(data));
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/mappings/delete/{id}")]
		public OperationResult Delete(int id)
		{
			try
			{
                if (!userData.Admin_Mappings_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
				return new OperationResult(true, g["Mappings Deleted!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/mappings/rename/{id}/{name}")]
		public OperationResult Rename(int id, string name)
		{
			try
			{
                if (!userData.Admin_Mappings_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
				return new OperationResult(true, g["Mappings Renamed!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Unexpected error while renaming mappings."]);
			}
		}

		[HttpGet, Route("/mappings/getbyid/{id}")]
		public IDataImportMapping GetByID(int id)
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

		[HttpGet, Route("/mappings/getlist")]
		public List<IDataImportMapping> GetList()
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

		[HttpGet, Route("/mappings/getbyprojectid/{projectid}")]
		public List<IDataImportMapping> GetByProjectID(int projectid)
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

		[HttpGet, Route("/mappings/getcolumns/{id}")]
		public ProcessMappingDTO GetColumns(int id)
		{
			try
			{
				ProcessMappingDTO result = new ProcessMappingDTO();
				var mapping = repo.GetByID(id);
				Reflex.Copy(result, mapping);
				result.Columns = new List<ColMappingDTO>();
				var colMappings = repo.GetColumnMappings(id);
				foreach (var col in colMappings)
				{
					result.Columns.Add(Reflex.Copy(new ColMappingDTO(), col));
				}
				return result;
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpPost, Route("/mappings/addcolumn/{id}")]
		public OperationResult AddColumn(int id)
		{
			try
			{
                if (!userData.Admin_Mappings_CanEdit)
                    return OperationResult.Forbid;
                IDataImportColMapping data = repo.AddColumn(id);
				return new OperationResult(true, g["Column added!"], data);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/mappings/insertcolumn/{id}/{pos}")]
		public OperationResult InsertColumn(int id, int pos)
		{
			try
			{
				if (!userData.Admin_Mappings_CanEdit)
					return OperationResult.Forbid;
				IDataImportColMapping data = repo.InsertColumn(id, pos);
				return new OperationResult(true, g["Column inserted!"], data);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/mappings/movecolumndown/{id}")]
		public OperationResult MoveColumnDown(int id)
		{
			try
			{
				if (!userData.Admin_Mappings_CanEdit)
					return OperationResult.Forbid;
				IDataImportColMapping data = repo.MoveColumnDown(id);
				return new OperationResult(true, g["Column inserted!"], data);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpPost, Route("/mappings/movecolumnup/{id}")]
		public OperationResult MoveColumnUp(int id)
		{
			try
			{
				if (!userData.Admin_Mappings_CanEdit)
					return OperationResult.Forbid;
				IDataImportColMapping data = repo.MoveColumnUp(id);
				return new OperationResult(true, g["Column inserted!"], data);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/mappings/deletecolumn/{colid}")]
		public OperationResult DeleteColumn(int colid)
		{
			try
			{
                if (!userData.Admin_Mappings_CanEdit)
                    return OperationResult.Forbid;
                repo.DeleteColumn(colid);
				return new OperationResult(true, g["Column deleted!"]);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpGet, Route("/mappings/getencodings")]
		public List<EncodingDTO> GetEncodings()
		{
			return repo.GetEncodings();
		}


		[HttpGet, Route("/mappings/getcultures")]
		public List<CultureDTO> GetCultures()
		{
			return repo.GetCultures();
		}

		[HttpPost, Route("/mappings/save")]
		public OperationResult Save([FromBody]ProcessMappingDTO data)
		{
			try
			{
                if (!userData.Admin_Mappings_CanEdit)
                    return OperationResult.Forbid;
                var mapping = Reflex.Copy(new DataImportMapping(), data);
				repo.Update(mapping);
				List<IDataImportColMapping> columns = new List<IDataImportColMapping>();
				foreach (var col in data.Columns)
					columns.Add(Reflex.Copy(new DataImportColMapping(), col));
				repo.UpdateColumnMappings(columns);
				return new OperationResult(true, g["Mappings have been updated!"]);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/mappings/init/{mappingid}/{catalogid}")]
		public OperationResult InitializeFromCatalog(int mappingid, int catalogid)
		{
			try
			{
				if (!userData.Admin_Mappings_CanEdit)
					return OperationResult.Forbid;
				List<IDataImportColMapping> columns = repo.InitializeMappingsFromCatalog(mappingid, catalogid);
				return new OperationResult(true, g["Mappings initialized"], columns);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/mappings/duplicate/{mappingid}/{name}")]
		public OperationResult DuplicateMapping(int mappingid, string name)
		{
			try
			{
				if (!userData.Admin_Mappings_CanAdd)
					return OperationResult.Forbid;
				var mapping = repo.Duplicate(mappingid, name);
				return new OperationResult(true, g["Mappings have been duplicated!"], mapping);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}
	}

	public class ProcessMappingDTO
	{
		public int ID { get; set; }
		public int ProjectID { get; set; }
        public string Name { get; set; }
		public int RootCatalog { get; set; }
		public string SourceType { get; set; }
		public string FileNameMask { get; set; }
		public string SourceCulture { get; set; }
		public string Encoding { get; set; }
		public string LineDelimiter { get; set; }
		public char ColumnDelimiter { get; set; }
		public string QuotationChar { get; set; }
		public bool IncludeHeader { get; set; }
		public string Plugin { get; set; }
		public List<ColMappingDTO> Columns { get; set; }
	}

	public class ColMappingDTO
	{
		public int ID { get; set; }
		public int DataImportMappingID { get; set; }
		public int ColOrder { get; set; }
		public string InputColumn { get; set; }
		public bool? Ignore { get; set; }
		public int? Type { get; set; }
		public bool? IsFixedValue { get; set; }
		public string FixedValue { get; set; }
		public int? MinLength { get; set; }
		public int? MaxLength { get; set; }
		public long? MinValue { get; set; }
		public long? MaxValue { get; set; }
		public DateTime? MinDate { get; set; }
		public DateTime? MaxDate { get; set; }
		public string DateFormat { get; set; }
		public int? DecimalPlaces { get; set; }
		public int? Function { get; set; }
		public string FunctionArguments { get; set; }
        public bool? CanBeEmpty { get; set; }
        public string TargetColumn { get; set; }
    }
}