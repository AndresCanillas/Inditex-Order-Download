using LinqKit;
using Service.Contracts.PrintCentral;
using SmartdotsPlugins.Inditex.Models;
using SmartdotsPlugins.Inditex.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class FiberListBuilderBase
    {
        public class FiberListConfig
        {
            public CompositionDefinition Compo { get; set; }
            public Separators Separators { get; set; }
            public int FillingWeightId { get; set; }
            public string FillingWeightText { get; set; }
            public bool IsSeparatedPercentage { get; set; }
            public List<string> FibersLanguage { get; set; }
            public string Language { get; set; }

            public int ExceptionsLocation { get; set; } = 0;
            public int OrderId { get; internal set; }

            public List<ExceptionComposition> ExceptionsComposition { get; set; }
            public bool UsesFreeExceptionComposition { get; set; }

            public FiberConcatenation FiberConcatenation { get; set; }  
        }

        public virtual List<CompositionTextDTO> Build(FiberListConfig config)
        {
            List<CompositionTextDTO> list = new List<CompositionTextDTO>();

            if(config.ExceptionsComposition != null && config.ExceptionsComposition.Any())
            {
                foreach(var line in config.ExceptionsComposition)
                {
                    IEnumerable<string> langsListSection = null;
                    switch(line.Type)
                    {
                        case "Section":
                            var section = config.Compo.Sections.FirstOrDefault(s => s.Code == line.SectionID);
                            if(section != null)
                            {
                               langsListSection = section.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                               var sectionValue = langsListSection.Count() > 1 ? string.Join(config.Separators.SECTION_LANG_SEPARATOR, langsListSection.Distinct()) : langsListSection.First();
                                var sectionText = ReArrangeSection(sectionValue);
                                list.Add(new CompositionTextDTO
                                {
                                    Percent = string.Empty,
                                    Text = sectionText.FirstOrDefault(),
                                    FiberType = string.Empty,
                                    TextType = TextType.CareInstruction,
                                    Langs = langsListSection.ToList()
                                });
                            }
                            break;
                        case "Fiber":

                            var sectionFiber = config.Compo.Sections.FirstOrDefault(s => s.Code == line.SectionID);
                            var fiber = sectionFiber.Fibers.FirstOrDefault(f => f.Code == line.FiberID);
                            var langsListFiber = fiber.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                            var langsAllListFiber = fiber.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                            var fiberValue = langsListFiber.Count() > 1 ? string.Join(config.Separators.FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First();
                            list.Add(new CompositionTextDTO
                            {
                                Percent = config.IsSeparatedPercentage ? fiber.Percentage + "%" : string.Empty,
                                Text = config.IsSeparatedPercentage ? fiberValue : $"{fiber.Percentage+ "%"} {fiberValue}",
                                FiberType = string.Empty,
                                TextType = TextType.CareInstruction,
                                Langs = langsListSection?.ToList()
                                
                            });
                            break;
                        case "Exception":
                            var exception = config
                                                .Compo
                                                .CareInstructions
                                                .FirstOrDefault(w => w.Category == CareInstructionCategory.EXCEPTION && w.Instruction == line.ExceptionID);
                            if(exception != null)
                            {

                                var langsList = exception.AllLangs
                                                         .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                                                         .Distinct();

                                var translated = langsList.Count() > 1 ? string.Join(config.Separators.CI_LANG_SEPARATOR, langsList) : langsList.First();
                                if(config.FillingWeightId != 1 && config.FillingWeightId == exception.Instruction)
                                {
                                    translated = $"{config.FillingWeightText} {translated}";
                                }
                                list.Add(new CompositionTextDTO
                                {
                                    Percent = string.Empty,
                                    Text = translated,
                                    FiberType = string.Empty,
                                    TextType = TextType.CareInstruction,
                                    Langs = langsList.ToList()
                                });



                            }
                            break;
                        default:
                            break;
                    }
                }

                return list;
            }


            for(var i = 0; i < config.Compo.Sections.Count; i++)
            {
                var langsListSection = config.Compo.Sections[i].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();

                var sectionValue = langsListSection.Count() > 1 ? string.Join(config.Separators.SECTION_LANG_SEPARATOR, langsListSection.Distinct()) : langsListSection.First();

                //if composition have one section
                if(config.Compo.Sections.Count > 1)
                {
                    var sectionText = ReArrangeSection(sectionValue);
                    if(sectionText.Count == 1)
                    {
                        list.Add(new CompositionTextDTO
                        {
                            Percent = string.Empty,
                            Text = sectionText.First(),
                            FiberType = "TITLE",
                            TextType = TextType.Title,
                            Langs = langsListSection.ToList(),
                            SectionFibersText = GetSectionFibers(config.Compo.Sections[i], config.Separators),
                            TextSelectedLanguage = GetSectionNameByLanguage(config.Compo.Sections[i], config.Language, config.FibersLanguage)
                        });
                    }
                    else
                    {
                        bool first = true;
                        foreach(var section in sectionText)
                        {
                            if(first)
                            {
                                list.Add(new CompositionTextDTO
                                {
                                    Percent = string.Empty,
                                    Text = section,
                                    FiberType = "TITLE",
                                    TextType = TextType.Title,
                                    Langs = langsListSection.ToList(),
                                    SectionFibersText = GetSectionFibers(config.Compo.Sections[i], config.Separators),
                                    TextSelectedLanguage = GetSectionNameByLanguage(config.Compo.Sections[i], config.Language, config.FibersLanguage)
                                });
                                first = false;
                            }
                            else
                            {
                                list.Add(new CompositionTextDTO
                                {
                                    Percent = string.Empty,
                                    Text = section,
                                    FiberType = "MERGETITLE",
                                    TextType = TextType.MergeTitle,
                                    Langs = langsListSection.ToList(),
                                    SectionFibersText = GetSectionFibers(config.Compo.Sections[i], config.Separators),
                                    TextSelectedLanguage = GetSectionNameByLanguage(config.Compo.Sections[i], config.Language, config.FibersLanguage)
                                });
                            }
                        }
                    }
                }

                for(var f = 0; f < config.Compo.Sections[i].Fibers.Count; f++)
                {
                    var langsListFiber = config.Compo.Sections[i].Fibers[f].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                    var langsAllListFiber = config.Compo.Sections[i].Fibers[f].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

                    var fiberValue = langsListFiber.Count() > 1 ? string.Join(config.Separators.FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First();

                    list.Add(new CompositionTextDTO
                    {
                        Percent = config.IsSeparatedPercentage ? config.Compo.Sections[i].Fibers[f].Percentage + "%" : string.Empty,
                        Text = config.IsSeparatedPercentage ? fiberValue : $"{config.Compo.Sections[i].Fibers[f].Percentage + "%"} {fiberValue}",
                        FiberType = config.Compo.Sections[i].Fibers[f].FiberType,
                        TextType = TextType.Fiber,
                        Langs = langsListFiber.ToList(),
                        TextSelectedLanguage = $"{config.Compo.Sections[i].Fibers[f].Percentage + "%"} {langsAllListFiber.Select(l => l.ToUpper()).ToList().ElementAtOrDefault(config.FibersLanguage.IndexOf(config.Language))}"

                    });
                }

                // Add Exceptions only to the first section
                if(i == config.ExceptionsLocation)
                {
                    if(config.ExceptionsComposition == null ||   !config.ExceptionsComposition.Any())
                    {
                        config.Compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.EXCEPTION).ForEach(ci =>
                        {

                            var langsList = ci.AllLangs
                            .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                            .Distinct();

                            var translated = langsList.Count() > 1 ? string.Join(config.Separators.CI_LANG_SEPARATOR, langsList) : langsList.First();
                            if(config.FillingWeightId != 1 && config.FillingWeightId == ci.Instruction)
                            {
                                translated = $"{config.FillingWeightText} {translated}";
                            }
                            list.Add(new CompositionTextDTO
                            {
                                Percent = string.Empty,
                                Text = translated,
                                FiberType = string.Empty,
                                TextType = TextType.CareInstruction,
                                Langs = langsList.ToList()
                            });


                        });

                        if(config.FiberConcatenation != null && config.FiberConcatenation.SectionID != null && config.FiberConcatenation.FiberID != null)
                        {
                            var sectionFiber = config.Compo.Sections.FirstOrDefault(s => s.Code == config.FiberConcatenation.SectionID);
                            var fiber = sectionFiber.Fibers.FirstOrDefault(f => f.Code == config.FiberConcatenation.FiberID);
                            var langsListFiber = fiber.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                            var langsAllListFiber = fiber.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                            var fiberValue = langsListFiber.Count() > 1 ? string.Join(config.Separators.FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First();
                            list.Add(new CompositionTextDTO
                            {
                                Percent = string.Empty,
                                Text = fiberValue,
                                FiberType = string.Empty,
                                TextType = TextType.Fiber,
                                Langs = langsListSection?.ToList()
                            });
                        }
                    }
                    else
                    {
                        foreach(var line in config.ExceptionsComposition)
                        {
                            switch(line.Type)
                            {
                                case "Section":
                                    var section = config.Compo.Sections.FirstOrDefault(s => s.Code == line.SectionID);
                                    if(section != null)
                                    {
                                        langsListSection = section.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                                        sectionValue = langsListSection.Count() > 1 ? string.Join(config.Separators.SECTION_LANG_SEPARATOR, langsListSection.Distinct()) : langsListSection.First();
                                        var sectionText = ReArrangeSection(sectionValue);
                                        list.Add(new CompositionTextDTO
                                        {
                                            Percent = string.Empty,
                                            Text = sectionText.FirstOrDefault(),
                                            FiberType = string.Empty,
                                            TextType = TextType.CareInstruction,
                                            Langs = langsListSection.ToList()
                                        });
                                    }
                                    break;
                                case "Fiber":

                                    var sectionFiber = config.Compo.Sections.FirstOrDefault(s => s.Code == line.SectionID);
                                    var fiber = sectionFiber.Fibers.FirstOrDefault(f => f.Code == line.FiberID);
                                    var langsListFiber = fiber.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                                    var langsAllListFiber = fiber.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                                    var fiberValue = langsListFiber.Count() > 1 ? string.Join(config.Separators.FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First();
                                    list.Add(new CompositionTextDTO
                                    {
                                        Percent = string.Empty,
                                        Text = fiberValue,
                                        FiberType = string.Empty,
                                        TextType = TextType.CareInstruction,
                                        Langs = langsListSection.ToList()
                                    });
                                    break;
                                case "Exception":
                                    var exception = config
                                                        .Compo
                                                        .CareInstructions
                                                        .FirstOrDefault(w => w.Category == CareInstructionCategory.EXCEPTION && w.Instruction == line.ExceptionID);
                                    if(exception != null)
                                    {

                                        var langsList = exception.AllLangs
                                                                 .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                                                                 .Distinct();

                                        var translated = langsList.Count() > 1 ? string.Join(config.Separators.CI_LANG_SEPARATOR, langsList) : langsList.First();
                                        if(config.FillingWeightId != 1 && config.FillingWeightId == exception.Instruction)
                                        {
                                            translated = $"{config.FillingWeightText} {translated}";
                                        }
                                        list.Add(new CompositionTextDTO
                                        {
                                            Percent = string.Empty,
                                            Text = translated,
                                            FiberType = string.Empty,
                                            TextType = TextType.CareInstruction,
                                            Langs = langsList.ToList()
                                        });

                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                }
            }

            return list;
        }

        public virtual List<CompositionTextDTO> Build(CompositionDefinition compo,
                                            Separators separators,
                                            int fillingWeightId = -1,
                                            string fillingWeightText = "",
                                            bool isSeparatedPercentage = true)
        {
            List<CompositionTextDTO> list = new List<CompositionTextDTO>();

            for(var i = 0; i < compo.Sections.Count; i++)
            {
                var langsListSection = compo.Sections[i].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();

                var sectionValue = langsListSection.Count() > 1 ? string.Join(separators.SECTION_LANG_SEPARATOR, langsListSection.Distinct()) : langsListSection.First();

                //if composition have one section
                if(compo.Sections.Count > 1)
                {
                    var sectionText = ReArrangeSection(sectionValue);
                    if(sectionText.Count == 1)
                    {
                        list.Add(new CompositionTextDTO
                        {
                            Percent = string.Empty,
                            Text = sectionText.First(),
                            FiberType = "TITLE",
                            TextType = TextType.Title,
                            Langs = langsListSection.ToList(),
                            SectionFibersText = GetSectionFibers(compo.Sections[i], separators)
                        });
                    }
                    else
                    {
                        bool first = true;
                        foreach(var section in sectionText)
                        {
                            if(first)
                            {
                                list.Add(new CompositionTextDTO
                                {
                                    Percent = string.Empty,
                                    Text = section,
                                    FiberType = "TITLE",
                                    TextType = TextType.Title,
                                    Langs = langsListSection.ToList(),
                                    SectionFibersText = GetSectionFibers(compo.Sections[i], separators)
                                });
                                first = false;
                            }
                            else
                            {
                                list.Add(new CompositionTextDTO
                                {
                                    Percent = string.Empty,
                                    Text = section,
                                    FiberType = "MERGETITLE",
                                    TextType = TextType.MergeTitle,
                                    Langs = langsListSection.ToList(),
                                    SectionFibersText = GetSectionFibers(compo.Sections[i], separators)
                                });
                            }

                        }
                    }
                }

                for(var f = 0; f < compo.Sections[i].Fibers.Count; f++)
                {
                    var langsListFiber = compo.Sections[i].Fibers[f].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();

                    var fiberValue = langsListFiber.Count() > 1 ? string.Join(separators.FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First();

                    list.Add(new CompositionTextDTO
                    {
                        Percent = isSeparatedPercentage ? compo.Sections[i].Fibers[f].Percentage + "%" : string.Empty,
                        Text = isSeparatedPercentage ? fiberValue : $"{compo.Sections[i].Fibers[f].Percentage + "%"} {fiberValue}",
                        FiberType = compo.Sections[i].Fibers[f].FiberType,
                        TextType = TextType.Fiber,
                        Langs = langsListFiber.ToList()
                    });
                }

                // Add Exceptions only to the first section
                if(i == compo.ExceptionsLocation) // i == ExceptionsLocation 
                {
                    compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.EXCEPTION).ForEach(ci =>
                    {

                        var langsList = ci.AllLangs
                        .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                        .Distinct();

                        var translated = langsList.Count() > 1 ? string.Join(separators.CI_LANG_SEPARATOR, langsList) : langsList.First();
                        if(fillingWeightId != 1 && fillingWeightId == ci.Instruction)
                        {
                            translated = $"{fillingWeightText} {translated}";
                        }
                        list.Add(new CompositionTextDTO
                        {
                            Percent = string.Empty,
                            Text = translated,
                            FiberType = string.Empty,
                            TextType = TextType.CareInstruction,
                            Langs = langsList.ToList()
                        });
                    });
                }

            }
            return list;
        }

        protected string GetSectionNameByLanguage(Section section, string lang, List<string> languages)
        {
            var text = string.Empty;
            if(string.IsNullOrEmpty(lang) || section == null || !languages.Any())
            {
                return text;
            }

            var index = languages.Select(l => l.ToUpper()).ToList().IndexOf(lang.ToUpper());
            text = section.AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct().ElementAtOrDefault(index);
            return text;
        }
        protected List<string> GetSectionFibers(Section section, Separators separators)
        {
            var list = new List<string>();
            for(var f = 0; f < section.Fibers.Count; f++)
            {
                var langsListFiber = section.Fibers[f].AllLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Distinct();
                var fiberValue = langsListFiber.Count() > 1 ? string.Join(separators.FIBER_LANG_SEPARATOR, langsListFiber) : langsListFiber.First();
                list.Add(fiberValue);
            }
            return list;
        }

        private List<string> ReArrangeSection(string sectionValue)
        {
            if(sectionValue.IndexOf('/') < 0)
            {
                return new List<string>() { sectionValue };
            }

            List<string> sections = new List<string>();
            string[] outerArray = sectionValue.Split('-');

            foreach(string part in outerArray)
            {
                var wordsOfSection = part.Split('/');
                int countOfSections = 0;
                foreach(string word in wordsOfSection)
                {
                    if(sections.Count < countOfSections + 1)
                    {
                        sections.Add(word.Trim());
                    }
                    else
                    {
                        sections[countOfSections] += "- " + word.Trim();
                    }
                    countOfSections++;
                }
            }

            return sections;

        }
    }


}
