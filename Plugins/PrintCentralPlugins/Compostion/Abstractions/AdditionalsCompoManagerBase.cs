using SmartdotsPlugins.Inditex.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class AdditionalsCompoManagerBase
    {
        private const string ADDITIONAL_PAGE = "AdditionalPage";
        private const string ADDITIONALS_NUMBER = "AdditionalsNumber";
        private const string ADDITIONAL = "Additional";
        private const string EXCEPTION = "Exception";
        private const string FULL_ADDITIONALS = "FullAdditionals";
        private IOrderUtilService orderUtilService;

        protected AdditionalsCompoManagerBase(IOrderUtilService orderUtilService)
        {
            this.orderUtilService = orderUtilService;
        }

        public virtual void GenerateAdditional(ArticleCompoConfig article,
                                                CompositionDefinition compo,
                                                StringBuilder careInstructions,
                                                StringBuilder additionals,
                                                StringBuilder Symbols,
                                                Dictionary<string, string> compositionData,
                                                int projectId,
                                                int compoId,
                                                int totalPages,
                                                int allowedLinesByPage,
                                                Separators separators)
        {
            if(article.IsSimpleAdditional)
            {
                AdditionalSimple(compo, careInstructions, additionals, Symbols, compositionData, separators);
            }
            else
            {
                ClearAdditionalPages(projectId, compoId, totalPages);
                AdditionalByPage(compo, careInstructions, additionals, Symbols, compositionData, allowedLinesByPage);
            }
        }

        private void AdditionalByPage(CompositionDefinition compo, StringBuilder careInstructions, StringBuilder additionals, StringBuilder symbols, Dictionary<string, string> compositionData, int allowedLinesByPage)
        {
            throw new NotImplementedException();
        }

        public void ClearAdditionalPages(int projectId, int compoId, int totalpages)
        {
            var compositionData = new Dictionary<string, string>();

            for(int j = 0; j < totalpages; j++)
            {
                var page = ADDITIONAL_PAGE + (j + 1);

                compositionData.Add(page, string.Empty);
            }

            compositionData.Add(ADDITIONALS_NUMBER, string.Empty);
            orderUtilService.SaveComposition(projectId, compoId, compositionData, string.Empty, string.Empty);
        }

        private void AdditionalSimple(CompositionDefinition compo, StringBuilder careInstructions, StringBuilder additionals, StringBuilder Symbols, Dictionary<string, string> compositionData, Separators separators)
        {
            foreach(var ci in compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.ADDITIONAL))
            {
                var langsList = ci.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

                if(ci.Symbol != string.Empty)
                {
                    //Symbols.Append(ci.Symbol); // TODO: now, always use FONT 
                    Symbols.Append(ci.Symbol + ",");
                }

                var translations = langsList.Length > 1 ? string.Join(separators.CI_LANG_SEPARATOR, langsList) : langsList[0];

                if(ci.Category != ADDITIONAL && ci.Category != EXCEPTION)
                {
                    careInstructions.Append(translations);
                    careInstructions.Append(separators.CI_SEPARATOR);
                }
                else
                {
                    additionals.Append(translations);
                    additionals.Append(separators.CI_SEPARATOR);
                }
            }

            //set Additionals only Zara
            compositionData.Add(FULL_ADDITIONALS, additionals.ToString().TrimEnd(separators.CI_SEPARATOR.ToCharArray()));

        }
    }
}
