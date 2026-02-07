using System;
using System.Collections.Generic;
using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using Services.Core;

namespace SmartdotsPlugins.OrderPlugins
{
    [FriendlyName("Scalpers - Pack Articles Plugin")]
    [Description("gets articles from pack.")]
    public class ScalpersPackArticlesPlugin : AbstractPackArticlesPlugin, IPackArticlesPlugin
    {
        private ILogSection log;
        public List<Seasons> model = new List<Seasons>();
        private List<string> WOMAN_SECTION = new List<string> { "MUJER" };
        private List<string> WOMAN_GAMMA = new List<string> { "BOLSO", "CALZADO", "PRENDA" };
        private List<string> WOMAN_ARTICLES = new List<string> { "MARCHAMO_SCALPERS_BLACK" };

        private List<string> HNN_SECTION = new List<string> { "HOMBRE", "NIÑO", "NIÑA" };
        private List<string> HNN_GAMMA = new List<string> { "PRENDA", "CALZADO" };
        private List<string> HNN_ARTICLES = new List<string> { "MARCHAMO_NAVY" };

        private List<string> NIÑA_SECTION = new List<string> { "NIÑA" };
        private List<string> NIÑA_ARTICLES = new List<string> { "SCGIRLSCOLLECTION" };
        private List<string> NIÑA_SEASON = new List<string> { "SS20 / NOS CONTINUIDAD" };

        private List<string> JV_SECTION_1 = new List<string> { "JORGE_VAZQUEZ" };
        private List<string> JV_GAMMA_1 = new List<string> { "PRENDA" };
        private List<string> JV_ARTICLES_1 = new List<string> { "JVAZQUEZHT", "JVAZQUEZLOOP" };

        private List<string> JV_SECTION_2 = new List<string> { "JORGE_VAZQUEZ" };
        private List<string> JV_GAMMA_2 = new List<string> { "ACC" };
        private List<string> JV_ARTICLES_2 = new List<string> { "JVAZQUEZHT_ACC", "JVAZQUEZLOOP" };

        private List<string> MIM_SECTION = new List<string> { "MIM" };
        private List<string> MIM_GAMMA = new List<string> { "CALZADO" };
        private List<string> MIM_ARTICLES = new List<string> { "MIMSHOESHT", "MARCHAMO_NAVY" };

        public ScalpersPackArticlesPlugin(ILogService log)
        {
            this.log = log.GetSection("Scalpers-PackArticlesPlugin");

            //this.model.AddRange(new List<Seasons>()
            //{
            //    new Seasons() { Section = new List<string> { "MUJER" }, Gamma = { "BOLSO", "CALZADO", "PRENDA" }, Articles = { "MARCHAMO_SCALPERS_BLACK" } },
            //    new Seasons() { Section = new List<string> { "HOMBRE", "NIÑO", "NIÑA" }, Gamma = { "PRENDA", "CALZADO" }, Articles = { "MARCHAMO_NAVY" } },
            //    new Seasons() { Section = new List<string> { "NIÑA"  }, Season = { "SS20 / NOS CONTINUIDAD" }, Articles = { "SCGIRLSCOLLECTION" } },
            //    new Seasons() { Section = new List<string> { "JORGE_VAZQUEZ" }, Gamma = { "PRENDA" }, Articles = { "JVAZQUEZHT", "JVAZQUEZLOOP" } },
            //    new Seasons() { Section = new List<string> { "JORGE_VAZQUEZ" }, Gamma = { "ACC" }, Articles = { "JVAZQUEZHT_ACC", "JVAZQUEZLOOP" } },
            //    new Seasons() { Section = new List<string> { "MIM" }, Gamma = { "CALZADO" }, Articles = { "MIMSHOESHT", "MARCHAMO_NAVY" } }
            //});
        }

        public override void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {
			var codseccionColumn = data.GetColumnByName("Details.Product.IsBaseData.codseccion");
			var codgamaColumn = data.GetColumnByName("Details.Product.IsBaseData.codgama");
			var temporadaColumn = data.GetColumnByName("Details.Product.IsBaseData.temporada");
			var quantityColumn = data.GetColumnByName("Details.Quantity");

			data.ForEach(row =>
            {
				var field1 = row.GetValue(codseccionColumn).ToString().ToUpper();
                var field2 = row.GetValue(codgamaColumn).ToString().ToUpper();
                var field3 = row.GetValue(temporadaColumn).ToString().ToUpper();
                var quantity = Convert.ToInt32(row.GetValue(quantityColumn));


                //Evaluates Woman Section
                if (WOMAN_SECTION.Contains(field1) && WOMAN_GAMMA.Contains(field2))
                {
                    AddArticles(WOMAN_ARTICLES, articleCodes, quantity);
                }

                //Evluates Hombre, Niño Niña - Prenda, Calzado
                else if (HNN_SECTION.Contains(field1) && HNN_GAMMA.Contains(field2))
                {
                    AddArticles(HNN_ARTICLES, articleCodes, quantity);
                }

                //Evluates JV - Prenda
                else if (JV_SECTION_1.Contains(field1) && JV_GAMMA_1.Contains(field2))
                {
                    AddArticles(JV_ARTICLES_1, articleCodes, quantity);
                }

                //Evluates JV - ACC
                else if (JV_SECTION_2.Contains(field1) && JV_GAMMA_2.Contains(field2))
                {
                    AddArticles(JV_ARTICLES_2, articleCodes, quantity);
                }

                //Evluates MIM - CLZADO
                else if (MIM_SECTION.Contains(field1) && MIM_GAMMA.Contains(field2))
                {
                    AddArticles(MIM_ARTICLES, articleCodes, quantity);
                }



                //Evluates Niña - SS20 / NOS CONTINUIDAD
                if (NIÑA_SECTION.Contains(field1) && NIÑA_SEASON.Contains(field3))
                {
                    AddArticles(NIÑA_ARTICLES, articleCodes, quantity);
                }

            });
        }
        
        public override void Dispose()
        {
        }

    }

    public class Seasons
    {
        public List<string> Section = new List<string>();
        public List<string> Gamma = new List<string>();
        public List<string> Articles = new List<string>();
        public List<string> Season = new List<string>();
    }
}
