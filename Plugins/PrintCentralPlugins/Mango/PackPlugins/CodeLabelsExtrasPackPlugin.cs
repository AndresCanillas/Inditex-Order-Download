using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartdotsPlugins.PackPlugins.Mango
{

    /**
         * 
         *   ID	Priority	Title	Iteration Path	State	Remaining Work	Effort	Assigned To
         *   4999	2	Plugin Packs adicionales automaticas para Etiquetas de código	Print\230828 - 230908	To Do	4	4	Rafael Guerrero
         *   
         *   RI3 -> si agregará la RI3X siempre que el campo 16 tenga una de estas 12 fam line descriptions
         *   C82 -> si agregará la C82X siempre que el campo 13 tenga una de estas 12 fam line descriptions
         *   B82 -> si agregará la B82X siempre que el campo 13 tenga una de estas 12 fam line descriptions
         *   KD3 -> si agregará la KD3X siempre que el campo 13 tenga una de estas 12 fam line descriptions
         *   KI2 -> si agregará la KI2X siempre que el campo 13 tenga una de estas 12 fam line descriptions
         *   
         *   
BABY BOY
BABY GIRL
BABY UX
BOYS
GIRLS
HE
HOME
KIDS BOY
KIDS GIRL
MAN
NEWBORN B.
NEWBORN G.
NEWBORN UX
TEEN BOY
TEEN GIRL
WOMAN
        */
    internal class CodeLabelPackConfig
    {
        public List<string> ExtraArticles;
        public List<string> EnableGenders;
        public string GenderInputField;
    }

    [FriendlyName("Mango - Code Labels Extras")]
    [Description("Mango - Code Labels Extras")]
    public class CodeLabelsExtrasPackPlugin : AbstractPackArticlesPlugin, IPackArticlesPlugin
    {
        // only 12 families
        private static List<string> EnableGenders = new List<string> {
            "KIDS BOY", "KIDS GIRL", "KIDS UX",
            "BABY BOY", "BABY GIRL", "BABY UX",
            "NEWBORN B.", "NEWBORN G.", "NEWBORN UX",
            "HOME",
            //"TEEN BOY", "TEEN GIRL", "TEEN UX"


        };

        private static Dictionary<string, CodeLabelPackConfig> map = new Dictionary<string, CodeLabelPackConfig>() {
            { "C82", new CodeLabelPackConfig { ExtraArticles = new List<string> { "C82X" }, EnableGenders = EnableGenders , GenderInputField = "fam_line_description"} },
            { "B82", new CodeLabelPackConfig { ExtraArticles = new List<string> { "B82X" }, EnableGenders = EnableGenders , GenderInputField = "fam_line_description"} },
            //{ "RI3", new CodeLabelPackConfig { ExtraArticles = new List<string> { "RI3X" }, EnableGenders = EnableGenders , GenderInputField = "fam_line_description"} },
            { "KD3", new CodeLabelPackConfig { ExtraArticles = new List<string> { "KD3X" }, EnableGenders = EnableGenders , GenderInputField = "fam_line_description"} },
            { "KI2", new CodeLabelPackConfig { ExtraArticles = new List<string> { "KI2X" }, EnableGenders = EnableGenders , GenderInputField = "fam_line_description"} },
            { "YI3", new CodeLabelPackConfig { ExtraArticles = new List<string> { "YI3X" }, EnableGenders = EnableGenders , GenderInputField = "fam_line_description"} },
        };


        public CodeLabelsExtrasPackPlugin()
        {

        }
        
        public override void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {
            var detailsArticleCodeColumn = data.GetTargetColumnByName("Details.ArticleCode");
            var detailsQuantityColumn = data.GetTargetColumnByName("Details.Quantity");
            

            data.ForEach((r) => {
                var articleCode = r.GetValue(detailsArticleCodeColumn).ToString().ToUpper();
                var quantity = r.GetValue(detailsQuantityColumn).ToString().ToUpper();

                if (!map.TryGetValue(articleCode, out var config)) return; // TODO: maybe require trhow an exception

                var famLinDescriptionColumn = data.GetInputColumnByName(config.GenderInputField);

                if(r.GetValue(famLinDescriptionColumn).ToString().ToUpper() == "HOME" && articleCode == "C82") return; // the article C82 don't need the extra for line HOME

                if (!config.EnableGenders.Contains(r.GetValue(famLinDescriptionColumn).ToString().ToUpper())) return; // Some families line descriptions don't need the extra

                AddArticles(map[articleCode].ExtraArticles, articleCodes, Convert.ToInt32(quantity));

            });

        }

        public override void Dispose() { }
    }
}
