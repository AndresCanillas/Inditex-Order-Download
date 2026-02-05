namespace Service.Contracts.PrintServices.Plugins
{
    public  class PluginCompoPreviewInputData
    {

        public string[] compoArray {  get; set; }
        public string[] percentArray { get; set; }
        public string[] leatherArray { get; set; }
        public string[] additionalArray { get; set; }
        public int labelLines {  get; set; }
        public  int ID { get; set; }
        public int AdditionalsCompress { get; set; } = 0; 
        public int FiberCompress { get; set; } = 0; 
        
        public string FibersInSpecificLang { get; set; }
        public int ExceptionsLocation { get; set; } = 0;     
        public string[] JustifyCompo { get; set; }  
        public string[] JustifyAdditional { get; set; } 

    }
}
