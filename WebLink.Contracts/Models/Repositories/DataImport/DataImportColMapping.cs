using System;

namespace WebLink.Contracts.Models
{
    public class DataImportColMapping : IDataImportColMapping
    {
        public int ID { get; set; }
        public int? DataImportMappingID { get; set; }
        public int ColOrder { get; set; }
        public string InputColumn { get; set; }
        public bool? Ignore { get; set; }
        public int? Type { get; set; }
        public bool? IsFixedValue { get; set; }
        public string FixedValue { get; set; }
        public int? MaxLength { get; set; }
        public int? MinLength { get; set; }
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

