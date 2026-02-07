using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebLink.Contracts.Models
{
    public class OrderPool : IOrderPool
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public Project Project { get; set; }
        [MaxLength(20)]
        public string OrderNumber { get; set; }
        [MaxLength(20)]
        public string Seasson { get; set; }
        public int Year { get; set; }
        [MaxLength(20)]
        public string ProviderCode1 { get; set; }
        [MaxLength(50)]
        public string ProviderName1 { get; set; }
        [MaxLength(20)]
        public string ProviderCode2 { get; set; }
        [MaxLength(50)]
        public string ProviderName2 { get; set; }
        [MaxLength(10)]
        public string Size { get; set; }
        [MaxLength(10)]
        public string ColorCode { get; set; }
        [MaxLength(30)]
        public string ColorName { get; set; }
        public string Price1 { get; set; }
        public string Price2 { get; set; }
        public int Quantity { get; set; }
        [MaxLength(30)]
        public string ArticleCode { get; set; }
        [MaxLength(10)]
        public string CategoryCode1 { get; set; } // modelo
        [MaxLength(100)]
        public string CategoryText1 { get; set; } // seccion
        [MaxLength(10)]
        public string CategoryCode2 { get; set; } // calidad
        [MaxLength(100)]
        public string CategoryText2 { get; set; } // sistema_tallaje
        [MaxLength(10)]
        public string CategoryCode3 { get; set; } //cod_familia
        [MaxLength(100)]
        public string CategoryText3 { get; set; }  //familia
        [MaxLength(10)]
        public string CategoryCode4 { get; set; } //cod_subfamilia
        [MaxLength(100)]
        public string CategoryText4 { get; set; } // subfamilia
        [MaxLength(10)]
        public string CategoryCode5 { get; set; }
        [MaxLength(100)]
        public string CategoryText5 { get; set; } // Origen
        [MaxLength(10)]
        public string CategoryCode6 { get; set; }
        [MaxLength(100)]
        public string CategoryText6 { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public DateTime? ExpectedProductionDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string ProcessedBy  { get; set; }
        public string DeletedBy { get; set; }     
        public DateTime? DeletedDate { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string ExtraData { get; set; }
    }
}

