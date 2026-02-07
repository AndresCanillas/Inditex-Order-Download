using Miracle.FileZilla.Api;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;
using System.Reflection.Emit;
using System.Security.Policy;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using WebLink.Contracts.Sage;
using System.Linq;
namespace SmartdotsPlugins.MangoJson.PackPlugins
{


    //     * CB000C82 -> si agregará la CB000C82X siempre que el campo Line tenga una de estas 12 fam line descriptions
    //     * CB000B82 -> si agregará la CB000B82X siempre que el campo Line tenga una de estas 12 fam line descriptions
    //     * CB000KD3 -> si agregará la CB000KD3X siempre que el campo Line tenga una de estas 12 fam line descriptions
    //     * CB000KI2 -> si agregará la CB000KI2X siempre que el campo Line tenga una de estas 12 fam line descriptions
    //     * CB000YI3 -> si agregará la CB000YI3X siempre que el campo Line tenga una de estas 12 fam line descriptions


    //Se debe agregar una etiqueta extra siempre y cuando la familia es una de estas abajo(la familia correcta es HOME, aún que en los adjuntos diga “HOME UNISEX / HOME KIDS”).

    //KIDS BOY
    //KIDS GIRL
    //KIDS UX
    //BABY BOY
    //BABY GIRL
    //BABY UX
    //NEWBORN B.
    //NEWBORN G.
    //NEWBORN UX
    //HOME

    //Deben de entrar las mismas cantidades que las etiquetas de barcode/compo, y siempre que haya cambios de cantidades, deberán de también si ver afectadas por eses cambios, pues tendremos de producir las mismas cantidades de etiquetas extras que las restantes de compo.

    //en caso de HOME las familias afectadas utilizan la PRO por lo que la excepcion será para la C82.Para el resto de etiquetas se añadirá la etiqueta EXTRA.
    internal class CodeLabelPackConfig
    {
        public List<string> ExtraArticles;
        public List<string> EnableGenders;
        public string GenderInputField;
    }

    [FriendlyName("MangoJson - Code Labels Extras")]
    [Description("MangoJson - Code Labels Extras")]
    public class MangoJsonLabelsExtrasPackPlugin : AbstractPackArticlesPlugin, IPackArticlesPlugin
    {
        static string GenderInputField = "Line";

        // only 12 families
        private static List<string> EnableGenders = new List<string> {
            "KIDS BOY", "KIDS GIRL", "KIDS UX",
            "BABY BOY", "BABY GIRL", "BABY UX",
            "NEWBORN B.", "NEWBORN G.", "NEWBORN UX",
            "HOME", "KIDS", "BABY","NEWBORN"
            //"TEEN BOY", "TEEN GIRL", "TEEN UX"


        };

        private static Dictionary<string, CodeLabelPackConfig> map = new Dictionary<string, CodeLabelPackConfig>() {
            { "CB000C82", new CodeLabelPackConfig { ExtraArticles = new List<string> { "CB000C82X" }, EnableGenders = EnableGenders , GenderInputField = GenderInputField} },
            { "GI000YI3", new CodeLabelPackConfig { ExtraArticles = new List<string> { "GI000YI3X" }, EnableGenders = EnableGenders , GenderInputField = GenderInputField} },
            { "GI000HO3", new CodeLabelPackConfig { ExtraArticles = new List<string> { "GI000HO3X" }, EnableGenders = EnableGenders , GenderInputField = GenderInputField} },
            { "GI004KI2", new CodeLabelPackConfig { ExtraArticles = new List<string> { "GI004KI2X" }, EnableGenders = EnableGenders , GenderInputField = GenderInputField} },
            { "CB000B82", new CodeLabelPackConfig { ExtraArticles = new List<string> { "CB000B82X" }, EnableGenders = EnableGenders , GenderInputField = GenderInputField} },
            { "GI000KD3", new CodeLabelPackConfig { ExtraArticles = new List<string> { "GI000KD3X" }, EnableGenders = EnableGenders , GenderInputField = GenderInputField} },
        };


        public MangoJsonLabelsExtrasPackPlugin()
        {

        }

        //No se pondrá la etiqueta EXTRA en las siguientes familias ya que pondrán la alarma con bolsita.
            //FALDA PIJAMA 922
            //CAMISA PIJAMA 281
            //CAMISETA PIJAMA 282
            //SUDADERA PIJAMA 284
            //TOP PIJAMA 285
            //CARDIGAN PIJAMA 286
            //PETO PIJAMA 472
            //PANTALON PIJAMA 508
            //PIJAMA PACK 509
            //PIJAMA 628
            //GORRO 677 / 695
            //BUFANDA 647 / 697
            //GUANTES 627 / 670



        public override void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {
            var detailsArticleCodeColumn = data.GetTargetColumnByName("Details.ArticleCode");
            var detailsQuantityColumn = data.GetTargetColumnByName("Details.Quantity");

            var NotIncludeThisFamilies = new string[] {"510","512","574","577","585","593","682","922","281","282","284","285","286","472","508","509","628","677","695","647","697","627","670"};

            data.ForEach((r) =>
            {
                var articleCode = r.GetValue(detailsArticleCodeColumn).ToString().ToUpper();
                var quantity = r.GetValue(detailsQuantityColumn).ToString().ToUpper();

                if(!map.TryGetValue(articleCode, out var config)) return; // TODO: maybe require trhow an exception

                var lineColumn = data.GetInputColumnByName(config.GenderInputField);

                var ageColumn = data.GetInputColumnByName("Age");
                var famCodeColumn = data.GetInputColumnByName("ProductTypeCodeLegacy");

                
                if(NotIncludeThisFamilies.Contains(r.GetValue(famCodeColumn).ToString())) return;

                if(r.GetValue(lineColumn).ToString().ToUpper() != "KIDS" ) return; // the families (NotIncludeThisFamilies) don't need the extra for line KIDS

                
                AddArticles(map[articleCode].ExtraArticles, articleCodes, Convert.ToInt32(quantity));

            });

        }

        public override void Dispose() { }
    }
}
