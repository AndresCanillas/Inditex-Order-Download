using SmartdotsPlugins.Inditex.Models;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class SeparatorsInitBase
    {
        public virtual Separators Init(IProject projectData)
        {
            Separators separators = new Separators();

            separators.SECTION_SEPARATOR = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
            separators.SECTION_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            separators.FIBER_SEPARATOR = string.IsNullOrEmpty(projectData.FibersSeparator) ? "\n" : projectData.FibersSeparator;
            separators.FIBER_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            separators.CI_SEPARATOR = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            separators.CI_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;


            return separators;
        }
    }
}
