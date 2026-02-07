using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public class DataImportMapping : IDataImportMapping, ICompanyFilter<DataImportMapping>, ISortableSet<DataImportMapping>
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public Project Project { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        public int RootCatalog { get; set; }        // The ID of the catalog that will be used as the target for this import operation.
        [MaxLength(20)]
        public string SourceType { get; set; }      // The IInputSource object that should be used to parse the file content. Right now the supported values are: 'Excel'  |  'Delimited'  |  'Fixed'  (not implemented yet)
        [MaxLength(400)]
        public string FileNameMask { get; set; }    // This mappings will apply only if the file name matches the FileNameMask regular expression.
        [MaxLength(10)]
        public string SourceCulture { get; set; }   // The culture used to parse numbers and dates. Ex. 'es-ES', 

        // The following apply only for Delimited (and or Fixed files)
        [MaxLength(20)]
        public string Encoding { get; set; }        // Possible Values: 'Default' | 'ASCII' | 'UTF-7' | 'UTF-8' | 'UTF-32' | 'Unicode' | 'UTF-16BE' ... or any of the other 140 encodings in existance... See "encodings.txt" under Documents project.
        [MaxLength(5)]
        public string LineDelimiter { get; set; }   // Sequence of characters used to delimit rows (usually \r\n)
        [MaxLength(1)]
        public char? ColumnDelimiter { get; set; } // Character used to delimit columns (Usually ,  ; or \t)
        [MaxLength(1)]
        public string QuotationChar { get; set; }   // Character used to enclose column values when the value might include the delimiter (Usually " or ')  NOTE: We assume that if the value of a column includes the quotation character, then it will be scaped by making that character appear twice. Example: Asuming " is the quotation character, then "Hello" y escaped as ""Helo"".
        public bool IncludeHeader { get; set; }     // True = The first line of the file will have the column names. False = No header, only data should be expected in the file.

        public string Plugin { get; set; }

        // The following is a list of the columns expected in the file and how to handle each of them.
        public IList<DataImportColMapping> Mappings { get; set; }

        public int GetCompanyID(PrintDB db) =>
            (from p in db.Projects
             join b in db.Brands on p.BrandID equals b.ID
             where p.ID == ProjectID
             select b.CompanyID).Single();

        public async Task<int> GetCompanyIDAsync(PrintDB db) => await
            (from p in db.Projects
             join b in db.Brands on p.BrandID equals b.ID
             where p.ID == ProjectID
             select b.CompanyID).SingleAsync();

        public IQueryable<DataImportMapping> FilterByCompanyID(PrintDB db, int companyid) =>
            from m in db.DataImportMappings
            join p in db.Projects on m.ProjectID equals p.ID
            join b in db.Brands on p.BrandID equals b.ID
            where b.CompanyID == companyid
            select m;

        public IQueryable<DataImportMapping> ApplySort(IQueryable<DataImportMapping> qry) => qry.OrderBy(m => m.Name);
    }
}

