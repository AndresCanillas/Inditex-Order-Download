using System.Collections.Generic;

namespace SmartdotsPlugins.Barsa.WizardPlugins
{
    public class BarsaComposition
    {
        public string FullComposition { get; set; }
        public string FullCareInstructions { get; set; }
        public string Symbols { get; set; }
        public IList<string> CareInstructionsSplit { get; set; }
        public bool CareInstructionsWarning { get { return CareInstructionsSplit.Count > 3; } }
    }
}
