using System;

namespace WebLink.Contracts.Models
{
    public class ArticleCompositionConfig : IArticleCompositionConfig
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int ProjectID { get; set; }
        public int ArticleID { get; set; }
        public string ArticleCode { get; set; }

        public float HeightInInches { get; set; }
        public float WidthInches { get; set; } = 0;
        public int LineNumber { get; set; }
        public bool WithSeparatedPercentage { get; set; }
        public int DefaultCompresion { get; set; }
        public int PPI { get; set; }
        public bool IsSimpleAdditional { get; set; }
        public float WidthAdditionalInInches { get; set; }
        public string SelectedLanguage { get; set; }

        public ArticleCompostionCalculationType ArticleCompositionCalculationType { get; set; } 
        public int MaxPages { get; set; }
        // En el caso en que el calculo del articulo permita añadir los adicionales a la pagina de la composicion
        // se debe indicar el numero maximo de lineas que debe tener esa compo 
        public int MaxLinesToIncludeAdditional { get; set; }    

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

