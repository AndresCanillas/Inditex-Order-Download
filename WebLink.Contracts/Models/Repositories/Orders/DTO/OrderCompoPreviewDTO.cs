using Service.Contracts.PrintCentral;
using System.Collections.Generic;

namespace WebLink.Contracts.Models.Repositories.Orders.DTO
{
    public class OrderCompoPreviewDTO
    {
        public string[] Compo { get; set; }
        public string[] Percent { get; set; }
        public string[] Leather { get; set; }
        public string[] Additionals{ get; set; }
        public int ProjectID { get; set; }
        public int OrderID { get; set; }
        public int OrderGroupID { get; set; }
        public  int MaxLines { get; set; }
        public int Id { get; set; }
        public int AdditionalsCompress { get; set; } = 0;
        public int FiberCompress { get; set; } = 0; 
        public int Filling_Weight_Id { get; set; }   
        public string Filling_Weight_Text { get; set; }  
        
        public string FibersInSpecificLang { get; set; }
        public int ExceptionsLocation { get; set; } = 0;     
        public List<ExceptionComposition> ExceptionsComposition { get; set; } = new List<ExceptionComposition>();
		public string[] JustifyCompo { get; set; }

        public string[] JustifyAdditional { get; set; }

        public bool UsesFreeExceptionComposition { get; set; }  

        public FiberConcatenation FiberConcatenation { get; set; }  
    }
}
