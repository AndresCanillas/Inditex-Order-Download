using SmartdotsPlugins.Compostion.Abstractions;
using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public  class LeftiesLanguageDictionaryManager :
        InditexLanguageDictionaryManagerBase
    {

        public override Dictionary<CompoCatalogName, IEnumerable<string>> GetInditexLanguageDictionary()
        {
            string[] SectionsLanguage = { "SPANISH", "FRENCH", "English", "PORTUGUESE", "ITALIAN",  "ARABIC", "GALICIAN", "CATALAN", "BASQUE", "TURKISH", "ROMANIAN" };
            string[] FibersLanguage = { "SPANISH", "FRENCH", "English", "PORTUGUESE", "ITALIAN", "ARABIC", "GALICIAN", "CATALAN", "BASQUE", "TURKISH", "ROMANIAN" };

            string[] AdditionalsLanguage = { "Spanish", "English", "French", "Portuguese",  "Arabic",  "Galician", "Catalan", "Basque" };
            string[] ExceptionsLanguage = { "Spanish", "English", "French", "Portuguese", "Arabic", "Galician", "Catalan", "Basque" };

            string[] CareInstructionsLanguage = { "Spanish", "English", "Portuguese" };

            Dictionary<CompoCatalogName, IEnumerable<string>> Language = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            Language.Add(CompoCatalogName.SECTIONS, SectionsLanguage);
            Language.Add(CompoCatalogName.FIBERS, FibersLanguage);
            Language.Add(CompoCatalogName.CAREINSTRUCTIONS, CareInstructionsLanguage);
            Language.Add(CompoCatalogName.EXCEPTIONS, ExceptionsLanguage);
            Language.Add(CompoCatalogName.ADDITIONALS, AdditionalsLanguage);

            return Language;
        }
    }
}
