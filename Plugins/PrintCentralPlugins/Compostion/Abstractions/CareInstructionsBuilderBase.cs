using SmartdotsPlugins.Inditex.Models;
using SmartdotsPlugins.Inditex.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public class CareInstructionsBuilderBase
    {

        public virtual List<CompositionTextDTO> Build(CompositionDefinition compo, Separators separators)
        {

            List<CompositionTextDTO> list = new List<CompositionTextDTO>();

            foreach(var ci in compo.CareInstructions)
            {

                var langsList = ci.AllLangs
                        .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                        .Distinct();

                var translated = langsList.Count() > 1 ? string.Join(separators.CI_LANG_SEPARATOR, langsList) : langsList.First();

                list.Add(new CompositionTextDTO
                {
                    Percent = string.Empty,
                    Text = translated,
                    FiberType = ci.Category,
                    TextType = TextType.CareInstruction,
                    Langs = langsList.ToList()
                });
            }
            return list;
        }
    }
}
