using SmartdotsPlugins.Inditex.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class SymbolsBuilderBase
    {
        private const string ADDITIONAL = "Additional";
        private const string EXCEPTION = "Exception";
        private const string SYMBOLS = "Symbols";

        public virtual void Build(CompositionDefinition compo,
                                StringBuilder Symbols,
                                Dictionary<string, string> compositionData,
                                Separators separators)
        {

            var symbols = new List<string>();
            foreach(var ci in compo.CareInstructions)
            {
                // ci individual translations
                var langsList = ci.AllLangs
                    .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var translated = langsList.Count > 1 ? string.Join(separators.CI_LANG_SEPARATOR, langsList.Distinct()) : langsList[0];

                // TODO: create enumerate with ci categories or a catalog to avoid harcde category names
                if(ci.Category != ADDITIONAL && ci.Category != EXCEPTION)
                {
                    symbols.Add(ci.Symbol.Trim());


                }
            }
            Symbols.Append(string.Join(",", symbols)); // TODO: save symbol separator inner configuration

            compositionData.Remove(SYMBOLS);
            compositionData.Add(SYMBOLS, Symbols.ToString());
        }
    }
}
