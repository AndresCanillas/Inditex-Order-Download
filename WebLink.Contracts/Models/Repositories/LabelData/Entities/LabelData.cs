using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class LabelData : ILabelData, ISortableSet<LabelData>
    {
        public int ID { get; set; }
        public int? ProjectID { get; set; }
        public Project Project { get; set; }
        [MaxLength(100)]
        public string FileName { get; set; }
        [MaxLength(50)]
        public string Name { get; set; }
        public string Comments { get; set; }
        public bool EncodeRFID { get; set; }            // Indicates if this label will include RFID code or not
        public bool DoubleSide { get; set; }            // Indicates if the label has two sides (front and back)
        public bool IsSerialized { get; set; }          // Indicates if this label is serialized (contains data that makes each printed label unique)
        public string PreviewData { get; set; }         // The product data with which the preview is generated (values for the variables defined in the label)
        public LabelType Type { get; set; }
        public int? MaterialID { get; set; }
        public Material Material { get; set; }
        public bool? LabelsAcross { get; set; }
        public int? Rows { get; set; }
        public int? Cols { get; set; }
        public string Mappings { get; set; }
        public string GroupingFields { get; set; } = "{\"GroupingFields\":\"\",\"DisplayFields\":\"\"}";
        public bool? RequiresDataEntry { get; set; }    // If null or false, then no data entry is required for this label.
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string ComparerField { get; set; }
        public bool DenyPartialExport { get; set; }
        public bool ShoeComposition { get; set; }/*
		public bool IsForComposition { get; set; }*/
        public bool IsDataBound { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public void Rename(string name) => Name = name;
        public IQueryable<LabelData> ApplySort(IQueryable<LabelData> qry) => qry.OrderBy(p => p.Name);
        public bool IncludeComposition { get; set; }
        public string UpdatedFileBy { get; set; }
        public DateTime UpdatedFileDate { get; set; }
        public bool IncludeCareInstructions { get; set; }
        public string IsValidBy { get; set; }
        public DateTime IsValidDate { get; set; }
        public bool IsValid { get; set; }

    }
}

