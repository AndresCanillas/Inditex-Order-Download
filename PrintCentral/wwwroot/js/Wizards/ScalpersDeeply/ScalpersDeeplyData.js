//# sourceURL=https://js/Wizards/ScalpersDeeply/ScalpersDeeplyData.js
var ScalpersDeeply = (function () {
    console.log("include scalpers y deeply data");

    var SCS = function () {

        var data =
        {
            Lines: [],
            Sections: [],
            Gamas: [],
            ArticleCategories: [],
            Articles: [],
            Configs: [],
        }

        var self = {
            constructor: SCS,
            get Lines() { return data.Lines; },
            get Sections() { return data.Sections; },
            get Gamas() { return data.Gamas; },
            get Articles() { return data.Articles; },
            get ArticleCategories() { return data.ArticleCategories; },
            get ArticlesByOrderData() { return data.Configs; },

            // helpers
            // return empty object if not found, to avoid null exceptions
            FindGama: function (newVal) {
                var gama = ScalpersDeeply.Gamas.find((item) => item.Name == newVal);
                if (!gama) {
                    return { ID: 0, Name: '-' }
                }
                return gama;
            },

            GetWorkData: function (sectionName, lineName) {

                let section = data.Sections.find((item) => item.Name.toUpperCase() == sectionName.toUpperCase())

                let scLine = this.Lines.find((item) => item.Name.toUpperCase() == lineName.toUpperCase())

                let workData = this.ArticlesByOrderData.filter((item) => item.SectionID == section.ID);

                workData = workData.filter(item => item.LineID == scLine.ID);

                return workData;
            },

            GetGamasByCode: function (sectionName, lineName) {

                let workData = this.GetWorkData(sectionName, lineName)

                let gamaIds = workData.map((w) => w.GamaID);

                let gamas = ScalpersDeeply.Gamas.filter((item) => gamaIds.indexOf(item.ID) >= 0);

                return gamas;
            },

            UpdateArticles: function (d) {
                data.Articles = d;
            },

            GetCatalogs: function (projectID,catalogs) {
                var self = this;
                var deferred = $.Deferred();

                $.when(
                    AppContext.Catalogs.GetByName(projectID, catalogs.LinesCatalog),
                    AppContext.Catalogs.GetByName(projectID, catalogs.SectionsCatalog),
                    AppContext.Catalogs.GetByName(projectID, catalogs.GamasCatalog),
                    AppContext.Catalogs.GetByName(projectID, catalogs.CategoriesCatalog),
                    AppContext.Catalogs.GetByName(projectID, catalogs.ArticleConfigsCatalog),
                    AppContext.Catalogs.GetByName(projectID, catalogs.ArticleCatalog)
                ).then((linesResult, sectionsResult, gamasResult, categoriesResult, articlesConfigsResult, articlesResult) => {
                    if (!linesResult || !sectionsResult || !gamasResult || !categoriesResult || !articlesConfigsResult || !articlesResult) {
                        AppContext.ShowError('@g["Verify that the following catalogs exist in the project: LinesScalpers, SectionsScalpers, GamasScalpers, CategoriesScalpers, ArticlesScalpers and ArticleConfigs."]');
                        return;
                    }

                    self.LinesDefinition = linesResult[0].Data;
                    self.SectionsDefinition = sectionsResult[0].Data;
                    self.GamasDefinition = gamasResult[0].Data;
                    self.CategoriesDefinition = categoriesResult[0].Data;
                    self.ArticlesConfigsDefinition = articlesConfigsResult[0].Data;
                    self.ArticlesScalpersDefinition = articlesResult[0].Data;

                    $.when(
                        AppContext.CatalogData.GetByCatalogID(self.LinesDefinition.CatalogID, null, null, false),
                        AppContext.CatalogData.GetByCatalogID(self.SectionsDefinition.CatalogID, null, null, false),
                        AppContext.CatalogData.GetByCatalogID(self.GamasDefinition.CatalogID, null, null, false),
                        AppContext.CatalogData.GetByCatalogID(self.CategoriesDefinition.CatalogID, null, null, false),
                        AppContext.CatalogData.GetByCatalogID(self.ArticlesConfigsDefinition.CatalogID, null, null, false),
                        AppContext.CatalogData.GetByCatalogID(self.ArticlesScalpersDefinition.CatalogID, null, null, false)
                    ).then((linesData, sectionsData, gamasData, categoriesData, articlesConfigsData, articlesData) => {
                        function safeParse(d, name) {
                            try {
                                if (d && d[0]) return JSON.parse(d[0]);
                                AppContext.ShowError(`@g["No data for ${name} Catalog"]`);
                                return [];
                            } catch (e) {
                                AppContext.ShowError(`@g["Error parsing ${name}: Catalog"]`);
                                return [];
                            }
                        }

                        data.Lines = safeParse(linesData, 'Lines');
                        data.Sections = safeParse(sectionsData, 'Sections');
                        data.Gamas = safeParse(gamasData, 'Gamas');
                        data.ArticleCategories = safeParse(categoriesData, 'Categories');
                        data.Articles = safeParse(articlesData, 'Articles');
                        data.Articles.forEach(a => { a.ID = Number(a.Code); });
                        data.Configs = safeParse(articlesConfigsData, 'Configs');
                        
                        deferred.resolve(true);

                    }).fail(deferred.reject);
                }).fail(deferred.reject);

                return deferred.promise();
            },

        }

        return self;

    }

    // return single
    return new SCS();
})();