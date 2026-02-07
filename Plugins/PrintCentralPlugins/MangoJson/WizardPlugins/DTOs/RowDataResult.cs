using System.Collections.Generic;



namespace SmartdotsPlugins.MangoJson.WizardPlugins
{
    partial class MangoJsonCompoPlugin
    {
        public class RowDataResult
        {
            public string BaseArticleCode { get; set; }
            public DataResult CompositionDataResult { get; set; } = new DataResult();
            public DataResult CaresDataResult { get; set; } = new DataResult();
            public List<string> CareSymbols { get; set; } = new List<string>();
        }
    }
}
