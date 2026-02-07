using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class InditexLanguageDictionaryManagerBase
    {
        public virtual Dictionary<CompoCatalogName, IEnumerable<string>> GetInditexLanguageDictionary()
        {
            string[] SectionsLanguage = { "SPANISH", "FRENCH", "English", "PORTUGUESE", "DUTCH", "ITALIAN", "GREEK", "JAPANESE", "GERMAN", "DANISH", "SLOVENIAN", "CHINESE", "KOREAN", "INDONESIAN", "ARABIC", "GALICIAN", "CATALAN", "BASQUE", "SLOVAK", "CROATIAN" };
            string[] FibersLanguage = { "SPANISH", "FRENCH", "English", "PORTUGUESE", "DUTCH", "ITALIAN", "GREEK", "JAPANESE", "GERMAN", "DANISH", "SLOVENIAN", "CHINESE", "KOREAN", "INDONESIAN", "ARABIC", "GALICIAN", "CATALAN", "BASQUE", "SLOVAK", "CROATIAN" };

            string[] AdditionalsLanguage = { "Spanish", "English", "French", "Portuguese", "Polish", "Romanian", "Indonesian", "Arabic", "Galician", "Catalan", "Basque" };
            string[] ExceptionsLanguage = { "Spanish", "French", "English", "Portuguese", "Dutch", "Italian", "Greek", "Japanese", "German", "Danish", "Slovenian", "Chinese", "Korean", "Indonesian", "Arabic", "Galician", "Catalan", "Basque", "SLOVAK", "CROATIAN" };

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
