using SmartdotsPlugins.Compostion.Abstractions;
using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class PBLanguageDictionaryManager : InditexLanguageDictionaryManagerBase
    {
        public override Dictionary<CompoCatalogName, IEnumerable<string>> GetInditexLanguageDictionary()
        {
            string[] SectionsLanguage = { "SPANISH", "English", "FRENCH",  "PORTUGUESE", "DUTCH", "GREEK", "GERMAN", "ITALIAN", "SLOVENIAN", "CHINESE", "INDONESIAN", "JAPANESE", "KOREAN", "ARABIC", "GALICIAN", "CATALAN", "BASQUE", "DANISH", "SLOVAK", "CROATIAN" };
            string[] FibersLanguage = { "SPANISH", "English", "FRENCH", "PORTUGUESE", "DUTCH", "GREEK", "GERMAN", "ITALIAN", "SLOVENIAN", "CHINESE", "INDONESIAN", "JAPANESE", "KOREAN", "ARABIC", "GALICIAN", "CATALAN", "BASQUE", "DANISH", "SLOVAK", "CROATIAN" };

            string[] AdditionalsLanguage = { "Spanish", "English", "French", "Portuguese", "Polish", "Romanian", "Indonesian", "Arabic", "Galician", "Catalan", "Basque" };
            string[] ExceptionsLanguage = { "Spanish", "English", "French",  "Portuguese", "Dutch", "Italian", "Greek", "Japanese", "German", "Danish", "Slovenian", "Chinese", "Korean", "Indonesian", "Arabic", "Galician", "Catalan", "Basque", "SLOVAK", "CROATIAN" };

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
