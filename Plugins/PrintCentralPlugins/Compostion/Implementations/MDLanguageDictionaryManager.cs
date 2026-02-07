using SmartdotsPlugins.Compostion.Abstractions;
using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class MDLanguageDictionaryManager: InditexLanguageDictionaryManagerBase
    {

        public override Dictionary<CompoCatalogName, IEnumerable<string>> GetInditexLanguageDictionary()
        {
            string[] SectionsLanguage = { "SPANISH", "FRENCH", "English", "PORTUGUESE", "DUTCH", "ITALIAN", "GREEK", "JAPANESE", "GERMAN", "DANISH", "SLOVENIAN", "CHINESE", "KOREAN", "INDONESIAN", "GALICIAN", "CATALAN", "BASQUE", "SLOVAK", "CROATIAN", "ARABIC" };
            string[] FibersLanguage = { "SPANISH", "FRENCH", "English", "PORTUGUESE", "DUTCH", "ITALIAN", "GREEK", "JAPANESE", "GERMAN", "DANISH", "SLOVENIAN", "CHINESE", "KOREAN", "INDONESIAN", "GALICIAN", "CATALAN", "BASQUE", "SLOVAK", "CROATIAN", "ARABIC" };

            string[] AdditionalsLanguage = { "Spanish", "English", "French", "Portuguese", "Polish", "Romanian", "Indonesian", "Galician", "Catalan", "Basque", "Arabic" };
            string[] ExceptionsLanguage = { "Spanish", "French", "English", "Portuguese", "Dutch", "Italian", "Greek", "Japanese", "German", "Danish", "Slovenian", "Chinese", "Korean", "Indonesian", "Galician", "Catalan", "Basque", "SLOVAK", "CROATIAN", "Arabic" };

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
