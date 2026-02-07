
// =========================================================================
// Abstract Define Composition
// =========================================================================
// #region Define Composition

/*
Object options {
OrderID: INT
ProjectID : INT,
CompositionDefined: {
Sections: [],
CareInstructions:[]
},

OrderSelection: {
OrderGroupID: 1,
OrderNumber: '8545',
Details: [],
Orders:
[OrderId, OrderID, ....],
WizardStepPosition: 0 ,
ProjectID: INT
},

ConfirmCloseWindowText: 'You will lose any unsaved changes, are you sure to close ?',
ConfirmButtonText : 'Confirm'
}
 */
var ADefineCompositionView = function (options) {

    if (!options)
        options = {};

    this.options = ADefineCompositionView.GetDefaultOptions(options);
    this.SectionsCatalog = null;
    this.FibersCatalog = null;
    this.WashingRulesCatalog = null;
    this.TemplatesCatalog = null;
    this.CareInstructionsCatalog = null;
    this.SectionData = [];
    this.FibersData = [];
    this.WashinRulesData = [];
    this.TemplatesData = [];
    this.CareInstructionsData = [];
    this.CareInstructions = [];
    this.RelData = [];
    this.ProjectData = this.options.ProjectData;
    this.Additional = [];
    this.MaxPercentage = 0;
    this.CountriesData = [];
    this.FibersTypeData = [];
    this.CareSymbols = "";
    this.CareInstructionsCategory = this.options.CareInstructionsCategory;
    // TODO: number of sections and parts must be configured by client or project
    this.MaxFibers = this.options.MaxFibers;
    this.MaxSections = this.options.MaxSections;
    this.FormSelector = '[name="composition-wizard-frm"]';
    this.FiberCodeSelector = 'select.fiber-code';
    this.FiberCodeWeightSelector = '.fiber-weight';
    FormView.Extend(this, this.options.Title, this.options.ViewUrl);
    //ExtendObj(this, ADefineCompositionView.prototype);// override base class methods - uggly version, but simple

    this.InitialData = "";
    this.EnableEvents = false;
    this.ShoesSectionConfig = [{
        Code: 1,
        Sort: 0
    }, {
        Code: 2,
        Sort: 1
    }, {
        Code: 3,
        Sort: 2
    }];
}

ADefineCompositionView.Extend = function (target, options) {
    ADefineCompositionView.call(target, options);
    ExtendObj(target, ADefineCompositionView.prototype);
}

ADefineCompositionView.GetDefaultOptions = function (options) {
    return $.extend({

        ViewUrl: '/Validation/Common/DefineCompositionView',

        // Text
        ConfirmCloseWindowText: 'You will lose any unsaved changes, are you sure to close ?',
        ConfirmButtonText: 'Confirm',
        WarningRemoveSectionText: "Section will removed, this action can't undo.Please confirm this action",
        LeatherText: 'Leather',
        TextileText: 'Textile',
        SyntheticText: 'Synthetic', // @Html.Raw(g["Synthetic"])
        DefaultOptionText: 'Select an Option', // @Html.Raw(g["Select an Option"])
        Shoes3SectionsOptionsText: 'Shoes 3 Sections',
        Shoes2SectionsOptionsText: 'Shoes 2 Sections',
        GenericOptionText: 'Generic',
        CompoForMexicoOptionText: 'Composition Mexico',
        SectionLabelText: 'Section',
        FiberTypesPlaceHolderText: 'Fiber Type',
        CountryOfOriginLabelText: 'Origin',
        FiberLabelText: 'Fiber',
        AddFiberActionText: 'Add Fiber',
        IsBlankFieldText: 'Without Fibers',
        IsMainTitleText: 'Is Main Title',
        ErrorToLoadCareInstructionsText: 'Fail to get care instructions data',
        ErrorToLoadTemplatesData: 'Error trying get Templates data.',
        UserErrorSectionsRepeat: 'Sections cannot be repeated',
        UserErrorFibersMinWeigth: 'All fibers must have more than 0 percent',
        UserErrorFiberRepeat: 'Cannot repeat a fiber within a section',
        UserErrorFibersMaxWeigth: 'All fibers on a section together must add up to 100 percent',
        UserErrorCountryOfOriginRequired: 'Country is required for Leather Fiber Type',
        UserErrorFiberTypeRequired: 'Fiber Type is Required',
        UserErrorFibersAndSectionsAreNotCompleted: 'Please complete the sections and fibers',
        UserErrorCareInstructionsRequired: 'Please select all Care Instrucctions',

        // session user
        UserLang: 'English',
        DefaultLangField: 'English',
        Languages: [{ "Culture": "en-US", "Lang": "English", "APICode": "en" }],
        SectionHasDisplay: false,
        FiberHasDisplay: false,
        //Languages:     [{ "Culture": "es-ES", "Lang": "Spanish" }, { "Culture": "ca-ES", "Lang": "Catalan" }, { "Culture": "en-US", "Lang": "English" }, { "Culture": "fr-FR", "Lang": "French" }, { "Culture": "tr-TR", "Lang": "English" }],

        // proyect configuration dependency
        ProjectID: 0,
        WrSymbolCssClass: 'common-wr-symbol',
        MaxFibers: 10,
        MaxSections: 10,
        SectionCatalogName: 'Sections', // "@Catalog.BRAND_SECTIONS_CATALOG"
        FibersCatalogName: 'Fibers', //"@Catalog.BRAND_FIBERS_CATALOG"
        WashingRulesCatalogName: 'CareInstructions', //"@Catalog.BRAND_CAREINSTRUCTIONS_CATALOG"
        TemplateCatalogName: 'Templates', //"@Catalog.BRAND_CAREINSTRUCTIONS_TEMPLATES_CATALOG"
        ProjectData: {},
        ForceToSelectWrOption: true,
        EnableRepeatSections: false,
        CareInstructionsCategory: ['Wash', 'Bleach', 'Dry', 'Iron', 'DryCleaning'],
        ShoesSectionConfig: [{
            Code: -1,
            Sort: 0
        }, {
            Code: -2,
            Sort: 1
        }, {
            Code: -3,
            Sort: 2
        }]

    }, options)
}

ADefineCompositionView.prototype = {

    constructor: ADefineCompositionView,

    // #region VIRTUAL METHODS, only override this methods in child class
    AllowToAddMoreFibersRow: function (section) {

        let self = this;

        // count fibers inner current Section
        var fibers = section.find('.fiber');
        var fiberIdx = fibers.length;

        if (fiberIdx < self.options.MaxFibers) {
            return true;
        }

        return false;
    },
    SectionDataFiltered: function () {
        return this.SectionData.filter(f => f.IsActive == 1);
    },

    FibersDataFiltered: function () {
        return this.FibersData.filter(f => f.IsActive == 1);
    },

    FibersTypeDataFiltered: function () {
        return this.FibersTypeData.filter(_f => _f.IsActive == 1);
    },

    CareInstructionsDataFiltered: function () {
        return this.CareInstructionsData.filter(f => f.IsActive == 1);
    },

    CareInstructionsTemplateData: function () {
        return this.TemplatesData;
    },
    // #endregion VIRTUAL METHODS


    OnLoad: async function () {

        var self = this;

        self.elements.Title.html(this.options.Title);

        self.LoadCompoOptions();

        //Load Shoes Composition catalogs
        if (self.ProjectData.EnableShoeComposition)
            await self.LoadShoeCompoCatalogs();

        await self.LoadCareInstructions();

        self.LoadCatalogs().done(() => {

            self.LoadOrderData();

            self.EnableEvents = true;

            self.model['TargetArticle'] = this.options.CompositionDefined.TargetArticle; // fire change event

            self.InitialData = $(self.FormSelector).serializeArray();

        })

        self.JavascriptUIEvents();
    },

    Cancel: function () {

        var self = this;

        var ctrls = $(self.FormSelector).serializeArray();

        if (JSON.stringify(self.InitialData) !== JSON.stringify(ctrls)) {

            var confirm = new ConfirmDialog(self.options.ConfirmCloseWindowText, self.options.ConfirmButtonText);

            confirm.ShowDialog((result) => {
                if (!result)
                    return;

                FormView.prototype.Close.call(this);

            });
        } else {
            FormView.prototype.Close.call(this);
        }

    },

    Save: function () {

        var self = this;

        var deffered = $.Deferred();

        if (self.model.ValidState() && self.ValidateSections() && self.ValidateShoesCombos()) {

            var arrayData = $(self.FormSelector).serializeArray();

            deffered.resolve(arrayData);

            self.Close(arrayData);
        } else {
            deffered.reject();
        }

        return deffered.promise();

    },

    _solve: function () {
        let self = this;
        let deffered = $.Deferred();
        let additionals = false;
        let exceptions = false;

        if (self.model.ValidState() && self.ValidComposition() && self.ValidateSections() && self.ValidateShoesCombos() && self.ValidCareInstruccions()) {

            var ciAddend = 0;
            self.elements.SortedCareInstructions.text(''); // clean
            $('input.wr-code[id], select.swr-code', self.FormSelector).each((pos, e) => {
                let val = 0;
                let el = $(e);
                let category = e.id;

                if (el.hasClass('wr-code')) {

                    //if (parseInt(el.val()) > 0) {


                    var i = `<input model name="CareInstructions[${ciAddend}].Category" value="${category}">
                                 <input model name="CareInstructions[${ciAddend}].Instruction" value="${el.val()}">`;
                    self.elements.SortedCareInstructions.append(i);
                    ciAddend++;
                    //}


                }

                if (el.hasClass('swr-code')) {

                    additionals = el.hasClass('swr-additionals');
                    exceptions = el.hasClass('swr-exceptions');

                    if (Array.isArray(el.val())) {
                        el.data('kendoMultiSelect').value().forEach((v) => {

                            var idx = ciAddend++;
                            var i = `<input model name="CareInstructions[${idx}].Instruction" value="${v}">`;

                            if (additionals) {
                                var j = `<input model hidden name="CareInstructions[${idx}].Category" value="Additional" />`;
                            } else {
                                var j = `<input model hidden name="CareInstructions[${idx}].Category" value="Exception" />`;
                            }

                            self.elements.SortedCareInstructions.append(i);
                            self.elements.SortedCareInstructions.append(j);
                        })
                    }
                }
            })

            var arrayData = $(self.FormSelector).serializeArray();
            //fix Exceptions and Additionals
            deffered.resolve(arrayData);
        } else {
            deffered.reject();
        }

        return deffered.promise();

    },

    SaveDefinition: function () {

        var self = this;

        var p = self._solve();

        p.done((arrayData) => {

            self.Close(arrayData);

        })

    },

    LoadCatalogs: function () {

        let self = this;

        let sectionCatalogName = self.options.SectionCatalogName;
        let fiberCatalogName = self.options.FibersCatalogName;
        let washingRulesCatalogName = self.options.WashingRulesCatalogName;

        let d1 = $.Deferred();

        let overlay = null;
        overlay = $('<div><div id="overlay"></div><div id="overlaymessage">Processing...</div></div>');
        overlay.appendTo(document.body)

        let endFn = () => {
            overlay.remove();
        };

        $.when(
            AppContext.Catalogs.GetByName(self.options.ProjectID, sectionCatalogName, null, null, false, false),
            AppContext.Catalogs.GetByName(self.options.ProjectID, fiberCatalogName, null, null, false, false),
            AppContext.Catalogs.GetByName(self.options.ProjectID, washingRulesCatalogName, null, null, false, false)).fail(function () {
                endFn();
                d1.reject();

            }).then(function (sectionsCatalog, fibersCatalog, washingRulesCatalog) {

                self.SectionsCatalog = sectionsCatalog[0].Data;
                self.FibersCatalog = fibersCatalog[0].Data;
                self.WashingRulesCatalog = washingRulesCatalog[0].Data;

                $.when(
                    AppContext.CatalogData.GetByCatalogID(self.SectionsCatalog.CatalogID, null, null, false),
                    AppContext.CatalogData.GetByCatalogID(self.FibersCatalog.CatalogID, null, null, false),
                    AppContext.CatalogData.GetByCatalogID(self.WashingRulesCatalog.CatalogID, null, null, false)).then(function (sectionData, fibersData, washingRulesData) {

                        self.SectionData = JSON.parse(sectionData[0]);
                        self.FibersData = JSON.parse(fibersData[0]);
                        self.WashinRulesData = JSON.parse(washingRulesData[0]);


                        let textField = self.GetLang();
                        
                        if (self.SectionData.length && self.SectionData[0].hasOwnProperty('Details')) {
                            self.options.SectionHasDisplay = true;
                            self.AddDetailToCatalogData(self.SectionData, textField);
                        }


                        if (self.FibersData.length && self.FibersData[0].hasOwnProperty('Details')) {
                            self.options.FiberHasDisplay = true;
                            self.AddDetailToCatalogData(self.FibersData, textField);
                        }
                        
                        /* mover a un sitio mas legible evento fijo de normas de lavado*/

                        d1.resolve();

                    })
                    .always(function () {
                        endFn();
                    })
                    .fail(function (a, b, c, d) {

                        console.log('fail to get data', a, b, c, d)
                        d1.reject();

                    })
            })

        return d1.promise();

    },


    AddDetailToCatalogData: function (catalogData, textField) {

        catalogData.forEach(f => {

            let fDetails = '';
            if (f.Details && f.Details.length > 0)
                fDetails = ` (${f.Details})`;

            f.Display = `${f[textField]}${fDetails}`;

        })

    },


    LoadOrderData: function () {
        let self = this;
        let d1 = $.Deferred()

        let compo = this.options.CompositionDefined;

        if (compo.ID == 0) { // ensure default values for shoes labels was already validated in this:Onload:self.LoadCompoOptions();

            compo.TargetArticle = self.elements.TargetArticle.val();
            compo.WrTemplate = self.elements.WrTemplate.val();

        }

        self.LoadData(compo);

        self.LoadUserdata();

        d1.resolve(compo);

        return d1.promise();

    },

    GetComboOptionsTpl: function (dataList, selectFirstOptionText, valueField, textField) {
        var self = this;

        if (!self.CompoArticles)
            self.CompoArticles = [];

        var options = [];

        if (selectFirstOptionText) {
            options.push(`<option value="0">${selectFirstOptionText}</option>`);
        }

        if (!textField)
            textField = valueField;

        if (!Array.isArray(textField))
            textField = [textField];

        dataList.forEach((value, idx, arr) => {

            var optText = [];
            textField.forEach((fldName) => {
                optText.push(value[fldName]);
            })

            var opt = `<option value="${value[valueField]}">${optText.join(' / ')}</option>`
            options.push(opt);
        });

        var allOptions = options.join('');

        return allOptions;

    },

    AddSectionEvent: function (evt) {
        var self = this;
        var btn = $(evt.currentTarget);

        self.AddSection(true);
    },

    AddSection: function (addFirstFiber) {

        var self = this;
        var n = $('.section').length; // section index

        if (addFirstFiber == undefined)
            addFirstFiber = false;

        if (n >= self.options.MaxSections) {
            console.log('cannot add more sections');
            return;
        }

        var sectionHtml = self.SectionTpl(n);

        self.elements.Composition.append(sectionHtml);

        var sectionAddedSelector = self.SectionSelector(n);

        var sectionContainer = $(sectionAddedSelector);

        self.model = AppContext.BindModel(self.ViewContainer, self);
        AppContext.BindActions(sectionContainer, self);

        if (n > 0) {
            var prevSectionContainer = $(self.SectionSelector(n - 1), self.FormSelector)
            self.RemoveSectionButtonEnable(prevSectionContainer.find('.remove-section-action>button'), false);
        }

        // eventos

        let lang = self.GetLang();
        let textField = self.options.SectionHasDisplay ? "Display" : lang;
        let template = `#:data.Code# - #:data.${textField}#`; // #:data.Code# - #:data.English#
        let cmbConfig = {
            dataSource: self.SectionDataFiltered(),
            dataTextField: textField,
            dataValueField: 'ID',
            template: template,
            filter: "contains",
            change: function (e) {

                // only allow select elemets in datasource
                if (e.sender.selectedIndex == -1)
                    e.sender.select(-1);
            }
        }

        $('.section-list', sectionAddedSelector).kendoComboBox(cmbConfig);


        self.ManageCompoOptions();

        if (addFirstFiber)
            self.AddFiber(n);
        return n;
    },

    // {  Percentage: 1-100, SectionID: int, Code: string  }
    SetSectionData: function (sectionIdx, data) {

        let self = this;;

        let kcombo = $('select.section-list', self.SectionSelector(sectionIdx)).data('kendoComboBox');

        kcombo.select((dataItem) => dataItem.ID == data.SectionID);

        // Weigh

        // IsBlank
        self.EnableEvents = false;
        self.model[`Sections[${sectionIdx}].IsBlank`] = data.IsBlank;// simulate click when this funtions is called from script
        self.model[`Sections[${sectionIdx}].IsMainTitle`] = data.IsMainTitle;// simulate click when this funtions is called from script
        self.EnableEvents = true;
        self.SetAsBlankSection(sectionIdx, data.IsBlank);

    },

    AddFiberEvent: function (evt) {

        var self = this;

        var btn = $(evt.currentTarget);

        if (btn.prop('disabled') == true) {
            evt.preventDefault();
            return;
        }

        var sectionIdx = btn.data('idx');

        self.AddFiber(sectionIdx);

    },




    AddFiber: function (sectionIdx) {

        var self = this;

        var section = $(self.SectionSelector(sectionIdx));
        var addFiberBtn = $('.add-fiber', section)

        var fibers = section.find('.fiber');
        var fiberIdx = fibers.length;
        var fiberTpl = self.FiberTpl(sectionIdx, fiberIdx);

        if (self.AllowToAddMoreFibersRow(section) == false) {
            return fiberIdx;
        }

        addFiberBtn.before(fiberTpl)

        var fiberContainer = $(self.FiberContainerSelector(fiberIdx), section);

        self.model = AppContext.BindModel(self.ViewContainer, self);
        AppContext.BindActions(fiberContainer, self);

        // set automatically a percentage value
        if (fiberIdx == 0) {
            $('.fiber-weight', section).val(100);
            self.RemoveFiberButtonEnable(fiberContainer.find('.remove-fiber-action>button'), false);
        } else {
            // subsctrac 1 point to the max value and set 1 point to the new fiber

            // only enable to remove the last fiber
            var prevFibercontainer = $(self.FiberContainerSelector(fiberIdx - 1), section)
            self.RemoveFiberButtonEnable(prevFibercontainer.find('.remove-fiber-action>button'), false);
        }

        if (fiberIdx >= self.options.MaxFibers) {
            var btn = $(`.add-fiber[data-idx="${sectionIdx}"]`)
            btn.prop('disabled', true);

        }

        //load shoes composition extra fields
        var countriesData = self.CountriesData.map(x => {
            return {
                ID: x.ID,
                Name: "From " + x.Name
            }
        });

        $('.country-code', fiberContainer).kendoComboBox({
            dataSource: countriesData,
            dataTextField: 'Name',
            dataValueField: 'ID',
            change: function (e) {
                // only allow select elemets in datasource
                if (e.sender.selectedIndex == -1)
                    e.sender.select(-1);
            }

        });

        $('.fiber-type', fiberContainer).kendoComboBox({
            dataSource: self.FibersTypeDataFiltered(),
            dataTextField: 'Name',
            dataValueField: 'ID',
            template: self.GetFiberTypeComboHTMLTemplate(),
            change: function (e) {
                var input = e.sender.element[0];
                var span = e.sender.element[0].parentNode.firstChild;
                if (!input.value) {
                    span.style.borderColor = 'red';
                    response = false;
                } else {
                    span.style.borderColor = '#d5d5d5';

                    self.SetFiberTypeIcon(e.sender.element[0]);
                }

                // only allow select elemets in datasource
                if (e.sender.selectedIndex == -1)
                    e.sender.select(-1);
            }
        });

        self.ManageCompoOptions();

        // widgets
        let lang = self.GetLang();
        let fiberTextField = self.options.FiberHasDisplay ? "Display" : lang;
        let template = `#:data.Code# - #:data.${fiberTextField}#`; // #:data.Code# - #:data.English#
        let cmbConfig = {
            dataSource: self.FibersDataFiltered(),
            dataTextField: fiberTextField,
            dataValueField: 'ID',
            template: template,
            filter: "contains",
            change: function (e) {

                // only allow select elemets in datasource
                if (e.sender.selectedIndex == -1)
                    e.sender.select(-1);
            }
        }

        $(self.FiberCodeSelector, fiberContainer).kendoComboBox(cmbConfig);

        return fiberIdx;

    },

    SetFiberTypeIcon: function (input) {

        var c = "";
        if (input.value == "1") {
            c = "A"
        } else if (input.value == "2") {
            c = "B"
        } else if (input.value == "3") {
            c = "C"
        }

        var icon = document.getElementById($(input).prop('name'));
        icon.innerText = c;
    },

    SetFiberData: function (fiberIdx, sectionIdx, data) {
        var self = this;
        var kcombo = $(self.FiberSelector(fiberIdx, sectionIdx), self.SectionSelector(sectionIdx)).data('kendoComboBox');
        kcombo.select((dataItem) => dataItem.ID == data.FiberID);

        if (self.ProjectData.EnableShoeComposition) {
            var kfibertype = $(`select[name='Sections[${sectionIdx}].Fibers[${fiberIdx}].FiberType']`).data('kendoComboBox');
            kfibertype.select((dataItem) => dataItem.ID == data.FiberType);
            self.SetFiberTypeIcon(kfibertype.element[0]);
        }

        $('.fiber-weight', `.fiber[data-jdx="${fiberIdx}"][data-idx="${sectionIdx}"]`).val(data.Percentage);

    },

    SetCareInstructionData: function (categoryID, ci) {
        var self = this;
        var htmlEl = $(`#${categoryID}`)
        //console.log('verificando existencia ->', categoryID , $(`#${categoryID}`));
        kcombo = htmlEl.data('kendoComboBox');
        //console.log('combo kendo ->', kcombo);
        kcombo.select((dataItem) => dataItem.ID == ci.Instruction);
    },

    SetCareSymbols: function () {

        var self = this;
        this.CareSymbols = "";

        self.CareInstructionsCategory.forEach((categoryID) => {
            var kcombo = $(`#${categoryID}`).data('kendoComboBox');
            var symbolData = self.CareInstructionsData.filter(x => x.ID == kcombo.value());

            if (symbolData.length > 0) {
                this.CareSymbols += symbolData[0].Symbol + " ";
            }

        });

        $(`div[name='wsymbols']`).text(self.CareSymbols);
    },

    SetMultiselectData: function (categoryID, keys) {
        var kmultis = $(`#${categoryID}`, self.FormSelector).data("kendoMultiSelect");
        kmultis.value(keys);
    },

    RemoveFiberEvent: function (evt) {
        let self = this;

        let btn = $(evt.currentTarget)

        let sectionIdx = btn.data('idx')

        let fiberIdx = btn.data('jdx')

        //let fibers = $('.fiber', self.SectionSelector(sectionIdx));

        if (btn.prop('disabled') == false || fiberIdx > 0) {
            self.RemoveFiber(sectionIdx, fiberIdx);
            evt.preventDefault();
        }

    },

    RemoveFiber: function (sectionIdx, fiberIdx) {

        let self = this;

        let fiberContainer = $(self.FiberContainerSelector(fiberIdx), self.SectionSelector(sectionIdx));

        fiberContainer.remove()

        // enable last button
        fiberIdx = fiberIdx - 1;

        if (fiberIdx > 0) {
            fiberContainer = $(self.FiberContainerSelector(fiberIdx), self.SectionSelector(sectionIdx));
            self.RemoveFiberButtonEnable(fiberContainer.find('.remove-fiber-action > button'), true);

        }

    },

    RemoveSectionEvent: function (evt, idx) {
        var self = this;

        if (evt) {
            var btn = $(evt.currentTarget);
            idx = btn.data('idx');
        }

        self.RemoveSection(idx);
    },

    RemoveSection: function (sectionIdx, autoConfirm) {
        var self = this;

        var section = $(self.SectionSelector(sectionIdx), self.FormSelector)

        var sections = $('.section', self.FormSelector);

        var btn = section.find('.remove-section-action > button');

        if (btn.prop('disabled') == true || sections.length == 1) {
            return;
        }

        if (autoConfirm === undefined) {
            autoConfirm = false;
        }

        var fn = (result) => {
            if (!result)
                return;

            section.remove();

            var lastSection = $(self.SectionSelector(sectionIdx - 1), self.FormSelector)

            if (lastSection.length > 0) {
                var btn = lastSection.find('.remove-section-action > button');
                self.RemoveSectionButtonEnable(btn, true);
            }
        };

        if (autoConfirm) {
            fn(true);
        } else {
            var confirm = new ConfirmDialog(self.options.WarningRemoveSectionText, self.options.ConfirmButtonText);
            confirm.ShowDialog(fn);
        }

    },

    //UseTemplate: function (evt) {
    //    var value = $(evt.currentTarget).val();

    //    if (value == 0) {
    //        $('.wr-code:input').data("kendoAutoComplete").value('');
    //    } else {
    //        $('.wr-code').each((idx, kendoAuto) => {
    //            $(kendoAuto).data("kendoAutoComplete").value(parseInt(value)+idx);
    //        })
    //    }
    //},

    SectionTpl: function (sectionIdx) {

        var self = this;

        var i = sectionIdx;

        var addFiberBtnTpl = self.FibberButtonTpl(sectionIdx);

        var margin = i == 0 ? 'mt-4' : 'mt-4';

        //Add weight by section
        var weightExtraFields = self.ProjectData.EnableSectionWeight ?
            `<input model action="change:ValidateSection" name='Sections[${i}].Weight' placeholder='' no-transform class='form-control form-control-sm text-right section-weight' style='min-width: 40px;max-width: 40px;' validation='regex(^([1-9]?[0-9]|100)$)' />

            <div class='input-group-prepend section-weight'>
                <span class='input-group-text' style='border-left: 0;border-right: 0;'>%</span>
            </div>` : '';

        let blankSection = `<div class="form-check mr-1">
                <input class="form-check-input" type="checkbox" model name="Sections[${i}].IsBlank" type="bool" value="true" data-idx="${i}"  id="DefineCompo_SetAsBlankSection-${sectionIdx}" action="change:SetAsBlankSection_Event">
                <label class="form-check-label" for="DefineCompo_SetAsBlankSection-${sectionIdx}">
                    ${self.options.IsBlankFieldText}
                </label>
            </div>`;

        let bolderSection = `<div class="form-check mr-1">
                <input class="form-check-input" type="checkbox" model name="Sections[${i}].IsMainTitle" type="bool" value="true" data-idx="${i}"  id="DefineCompo_SetAsMaintTitleSection-${sectionIdx}">
                <label class="form-check-label" for="DefineCompo_SetAsMaintTitleSection-${sectionIdx}">
                    ${self.options.IsMainTitleText}
                </label>
            </div>`;

        let tpl = `<div class="section ${margin} border-bottom p-2 " data-idx="${i}">
                    <div class="form-group row" style="margin-bottom:5px;">
                        <label class="col-sm col-form-label">${self.options.SectionLabelText}</label>
                        <div class="input-group input-group-sm col-sm">
                            ${weightExtraFields}
                            <select model name="Sections[${i}].SectionID" data-idx="${i}" validation="required;range(0, 1000)" no-transform class="form-control form-control-sm section-list"></select>
                            <div class="input-group-append remove-section-action">
                                <button class="btn btn-sm btn-danger mr-0" type="button" action="click:RemoveSectionEvent" data-idx="${i}"><i class="fa fa-fw fa-trash-o"></i></button>
                            </div>
                            <div class="icons-compo scalpers-shoes-fiber" id="Sections[${i}]"> </div>

                            ${blankSection}

                            ${bolderSection}

                        </div>
                    </div>

                    ${addFiberBtnTpl}
                </div>`

        return tpl;
    },

    FiberTpl: function (sectionIdx, fiberIdx) {
        var self = this;
        var i = sectionIdx;
        var j = fiberIdx;

        //Add shoes extra fields by section
        var shoesExtraFields = self.ProjectData.EnableShoeComposition ?
            `<div style="display: inline; margin-left:10px;"><select model name="Sections[${i}].Fibers[${j}].FiberType" placeholder="${self.options.FiberTypesPlaceHolderText}" no-transform class="form-control form-control-sm fiber-type"></select></div><div class="icons-compo scalpers-shoes-fiber fiber-type-icon" id="Sections[${i}].Fibers[${j}].FiberType"> </div>
             <div style="display: inline; margin-left:10px;"><select model name="Sections[${i}].Fibers[${j}].CountryOfOrigin" placeholder="${self.options.CountryOfOriginLabelText}" no-transform class="form-control form-control-sm country-code"></select></div>` : '';

        var tpl = `<div class="form-group row fiber" style="margin-bottom:5px;" data-idx="${i}" data-jdx="${j}">
                        <label class="col-sm col-form-label">${self.options.FiberLabelText}</label>
                        <div class="input-group input-group-sm col-sm">
                            <input model name="Sections[${i}].Fibers[${j}].Percentage"   placeholder="0" no-transform class="form-control form-control-sm text-right fiber-weight" style="min-width: 40px;max-width: 40px;" validation="range(1, 100);regex(^([1-9]?[0-9]|100)$)" />
                            <div class="input-group-prepend">
                                <span class="input-group-text" style="border-left: 0;border-right: 0;">%</span>
                            </div>
                            <select model name="Sections[${i}].Fibers[${j}].FiberID"  data-idx="${i}" data-jdx="${j}" no-transform class="form-control form-control-sm fiber-code" ></select>

                            <div class="input-group-append remove-fiber-action">
                                <button class="btn btn-sm btn-warning mr-0" type="button" action="click:RemoveFiberEvent" data-idx="${i}" data-jdx="${j}"><i class="fa fa-fw fa-minus-circle"></i></button>
                            </div>

                            ${shoesExtraFields}
                        </div>
                    </div>`

        return tpl;
    },

    FibberButtonTpl: function (sectionIdx) {

        var i = sectionIdx;
        var tpl = `<div name="add-fiber-${i}" class="form-group row col-sm add-fiber" style="margin-bottom:5px;" data-idx="${i}">
                        <button type="button" class="btn btn-sm btn btn-link" action="click:AddFiberEvent" data-idx="${i}">${this.options.AddFiberActionText} <i class="fa fa-fw fa-plus"></i></button>
                    </div>`;

        return tpl;
    },

    RemoveFiberButtonEnable: function (btn, enable) {
        if (enable) {
            btn.prop('disabled', false)
                .addClass('btn-warning')
                .removeClass('btn-secondary')
        } else {
            btn.prop('disabled', true)
                .removeClass('btn-warning')
                .addClass('btn-secondary')
        }
    },

    RemoveSectionButtonEnable: function (btn, enable) {
        if (enable) {
            btn.prop('disabled', false)
                .addClass('btn-danger')
                .removeClass('btn-secondary')
        } else {
            btn.prop('disabled', true)
                .removeClass('btn-danger')
                .addClass('btn-secondary')
        }
    },

    SectionSelector: function (sectionIdx) {
        return `.section[data-idx="${sectionIdx}"]`;
    },

    FiberSelector: function (fiberIdx, sectionIdx) {
        return `${this.FiberCodeSelector}[data-jdx="${fiberIdx}"][data-idx="${sectionIdx}"]`;
    },

    FiberContainerSelector: function (fiberIdx) {
        return `.fiber[data-jdx="${fiberIdx}"]`;
    },

    JavascriptUIEvents: function () {
        var self = this;

        self.elements.wsymbols.addClass(self.options.WrSymbolCssClass);

    },

    /*
    GetSection: function (sectionIdx) { return $(`.section[data-idx="${sectionIdx}"]`); },
    GetFiberContainer: function (fiberIdx, sectionIdx) { return this.GetSection(sectionIdx).find(`.fiber[data-jdx="${fiberIdx}"]`); },
    GetFiberCode: function (fiberIdx, sectionIdx) { return this.GetFiberContainer(fiberIdx, sectionIdx).find('.fiber-code'); },
    GetFiberWeight: function (fiberIdx, sectionIdx) { return this.GetFiberContainer(fiberIdx, sectionIdx).find('.fiber-weight'); },
     */

    LoadCareInstructions: async function () {

        let self = this;
        //var d1 = $.Deferred()

        if (self.options.Label.IncludeCareInstructions == false)
            return;


        self.elements.CareInstructionsContainer.removeClass('d-none')
        self.elements.CareInstructionsContainer.show();// compatible with custom views

        try {

            self.CareInstructionsCatalog = await AppContext.Catalogs.GetByName(self.options.ProjectID, "CareInstructions", null, null, false, false);

            if (self.CareInstructionsCatalog.Data) {

                let result = await AppContext.CatalogData.GetByCatalogID(self.CareInstructionsCatalog.Data.CatalogID, null, null, false);
                self.CareInstructionsData = JSON.parse(result);

                //Load Templates configuration
                await self.GetTemplatesConfiguration();

                //Load Exceptions configuration
                self.GetExceptionsConfiguration();

                //Load Additional configuration
                self.GetAdditionalsConfiguration();

            }

            // return empty
            //d1.resolve();

        } catch (e) {

            AppContext.ShowError(self.options.ErrorToLoadCareInstructionsText);
            //d1.reject();
        }

        //return d1.promise();
    },

    //Templates config
    GetTemplatesConfiguration: async function () {

        var self = this;

        //Fill combo Template
        var defaultValue = self.GetDefaultOption();


        try {

            self.TemplatesCatalog = await AppContext.Catalogs.GetByName(self.options.ProjectID, self.options.TemplateCatalogName, null, null, false, false);

            if (self.TemplatesCatalog.Data) {
                const results = await Promise.all([
                    AppContext.CatalogData.GetByCatalogID(self.TemplatesCatalog.Data.CatalogID, null, null, false),
                    AppContext.CatalogData.GetFullSubset(self.TemplatesCatalog.Data.CatalogID, self.options.WashingRulesCatalogName, null)
                ]);

                self.RelData = JSON.parse(results[1]);

                self.TemplatesData = JSON.parse(results[0]);
                self.TemplatesData.unshift(defaultValue);

                AppContext.LoadComboBox(self.elements.WrTemplate, self.CareInstructionsTemplateData().filter(f => f.IsActive == true), "ID", "Name");

            }

            //Fill default care instructions values (empty)
            $('.wr-code', self.FormSelector).kendoComboBox({
                dataTextField: self.GetLang(),
                dataValueField: 'ID',
                template: self.GetCareInstructionComboHTMLTemplate(),
                change: function (e) {
                    if (e.sender.selectedIndex > -1) {
                        var value = e.sender.dataSource.options.data ? e.sender.dataSource.options.data[e.sender.selectedIndex] : e.sender.dataSource.options;
                        e.sender.input.val(value[self.GetLang()]);
                        e.sender.Code = value["Code"];
                        e.sender.element[0].value = value.ID;
                    }

                    if (e.sender.selectedIndex == -1)
                        e.sender.select(-1);

                    self.SetCareSymbols();
                }
            });

            switch (self.ProjectData.TemplateConfiguration) {

                case 0: // CITemplateConfig.Disabled
                    self.LoadCategories();
                    break;

                case 1: //CITemplateConfig.Enabled
                    self.elements.TemplatesContainer.show();
                    self.LoadCategories();
                    break;

                case 2: //CITemplateConfig.Forced

                    self.elements.TemplatesContainer.show();
                    self.LoadCategories();
                    self.DisableComboElements();

                    break;
                default:
                    break;
            }

        } catch (e) {
            console.log(e)
            AppContext.ShowError(self.options.ErrorToLoadTemplatesData);
        }

    },

    //Exceptions config
    GetExceptionsConfiguration: function () {

        var self = this;

        if (self.ProjectData.AllowExceptions) {
            this.elements.ExceptionsContainer.removeClass('d-none')
            self.LoadExceptions();
        }
    },

    //Additionals config
    GetAdditionalsConfiguration: function () {

        var self = this;

        if (self.ProjectData.AllowAdditionals) {
            this.elements.AdditionlasContainer.removeClass('d-none')
            self.LoadAdditional();
        }
    },

    //Additionals config
    GetMadeInCompoShoesFiberConfiguration: function () {

        var self = this;

        if (!self.ProjectData.AllowMadeInCompoShoesFiber) {
            this.elements.AllowMadeInCompoShoesFiber.hide();
        }

    },

    FilterElements: function (e) {

        var self = this;

        if (!self.EnableEvents) return;

        var id = parseInt(e.target.value);

        //clear invalid class
        $("input[name*='Instruction_input']").parent().removeClass("comboArticlesNull");

        if (id > 0) {
            self.LoadTemplateCategories(id);
        } else {

            // CITemplateConfig.Forced = 2
            if (self.ProjectData.TemplateConfiguration == 2) {
                self.ClearCareInstructionsCombo();
            } else {
                self.LoadCategories();
                self.SetCareSymbols();
            }

        }
    },

    //Load categories combo boxes by template id
    LoadTemplateCategories: function (id) {
        var self = this;

        var elements = self.model.ModelElements;
        var requiredCi = $('#' + id);

        //Fill combo Template
        var defaultValue = self.GetEmptyOptionForCareInstruction();

        for (var e of elements) {
            if (e.matches('.wr-code')) {
                var combobox = $("#" + e.id).data("kendoComboBox");
                var category = self.RelData.find(x => x.SourceID == id && x.Category.toLowerCase() == e.id.toLowerCase());

                if (category) {
                    //var dataSource = new kendo.data.DataSource({
                    //    data: [category]
                    //});

                    combobox.setDataSource(category);
                    combobox.refresh();

                    //combobox.Default = true;
                    combobox.value(category.Code);
                    combobox.selectedIndex = 0;
                } else {

                    let newEmptySource = [];

                    if (self.options.ForceToSelectWrOption == false) {
                        newEmptySource.push(defaultValue)
                    }

                    combobox.setDataSource(newEmptySource);

                    if (self.options.ForceToSelectWrOption == false) {
                        combobox.select(0);
                    } else {

                        combobox.selectedIndex = -1;
                        combobox.value("");
                    }
                }


                combobox.trigger("change");
            }

            // TODO: additional and exception kendoMultiSelect are not Initialized 
            // use project configuration to check it
            if (e.matches('.swr-code') && requiredCi != undefined) {
                var multiselect = requiredCi.data('kendoMultiSelect');
                if (!multiselect) continue;

                var newData = self.CareInstructionsData.filter(x => self.RelData.find(f => f.SourceID == id && f.ID == x.ID && x.Category.toLowerCase() == ($(e).data("category") != undefined ? $(e).data("category").toLowerCase() : "")));

                if (self.options.ForceToSelectWrOption == false) {
                    newData.unshift(defaultValue)
                }

                // how to change datasource
                // https://docs.telerik.com/kendo-ui/api/javascript/ui/multiselect/methods/setdatasource
                multiselect.setDataSource(newData);

                multiselect.refresh();

                var dataIds = newData.map((v) => v.ID);
                //SET SELECTED
                self.SetMultiselectData(e.id, dataIds)

            }
        }
    },

    LoadCategories: function () {

        var self = this;
        var elements = self.model.ModelElements;

        //Fill combo Template
        var defaultValue = self.GetEmptyOptionForCareInstruction();

        for (var e of elements) {
            if (e.matches('.wr-code')) {
                var combobox = $("#" + e.id).data("kendoComboBox");
                combobox.value("");
                combobox.text("");
                combobox.selectedIndex = -1

                var category = self.CareInstructionsData.filter(x => x.Category.toLowerCase() == e.id.toLowerCase());



                if (self.options.ForceToSelectWrOption == false /*&& $("#" + e.id).hasClass('ignore')*/) {
                    category.unshift(defaultValue)
                }

                if (category.length > 0) {
                    combobox.setDataSource(category);

                    if (self.options.ForceToSelectWrOption == false) {
                        combobox.select(0);
                    }

                    combobox.template = "#:data.Code# - #:data." + self.GetLang() + "#";
                    combobox.Code = null;

                    combobox.refresh();
                }
            }
        }
    },

    DisableComboElements: function () {

        var self = this;
        var elements = self.model.ModelElements;

        for (var e of elements) {
            if (e.matches('.wr-code')) {
                $("#" + e.id).data("kendoComboBox").readonly(true);
                $("#" + e.id).parent().parent().addClass("pointer-events-none");
            }
        }

        $('#Additionals').parent().parent().addClass("pointer-events-none");
        $('#Exceptions').parent().parent().addClass("pointer-events-none");

    },

    ClearCareInstructionsCombo: function () {

        var self = this;

        var elements = self.model.ModelElements;

        for (var e of elements) {
            if (e.matches('.wr-code')) {
                var combobox = $("#" + e.id).data("kendoComboBox");

                combobox.setDataSource([]);
                combobox.selectedIndex = -1;
                combobox.value("");

            }
        }

        this.CareSymbols = "";
        $(`div[name='wsymbols']`).text(self.CareSymbols);

    },

    LoadAdditional: function () {

        var self = this;
        var additionalData = self.CareInstructionsData.filter(x => x.Category === 'Additional');

        $('#Additionals', self.FormSelector).kendoMultiSelect({
            dataSource: additionalData,
            dataTextField: self.GetLang(),
            dataValueField: 'ID',
            template: "#:data.Code# - #:data." + self.GetLang() + "#",
            height: 200,
            filter: "contains"
            /*change: function (e) {
            self.CareInstructions = self.CareInstructions.filter(x => { return x.Category !== "Additional" });
            for (var item = 0; item < e.sender._old.length; item++) {
            self.CareInstructions.push({
            Code: e.sender._old[item],
            Position: item,
            Category: 'Additional'
            }
            );

            }

            self.Sort();
            }*/
        })
        $("#Additionals").parent().css("height", "auto");
    },

    LoadExceptions: function () {

        var self = this;
        var exceptionData = self.CareInstructionsData.filter(x => x.Category === 'Exception');

        $('#Exceptions', self.FormSelector).kendoMultiSelect({
            dataSource: exceptionData,
            dataTextField: self.GetLang(),
            dataValueField: 'ID',
            template: "#:data.Code# - #:data." + self.GetLang() + "#",
            height: 200,
            change: function (e) {
                var controlException = document.getElementsByName($(this.element).prop('name'));
                self.Exceptions = $(controlException).val();
            },
            filter: "contains"
            //change: function (e) {
            //    self.CareInstructions = self.CareInstructions.filter(x => { return x.Category !== "Exception"});
            //    for (var item = 0; item < e.sender._old.length; item++) {
            //        self.CareInstructions.push(
            //            {
            //                Code: e.sender._old[item],
            //                Position: item,
            //                Category: 'Exceptions'
            //            }
            //        );

            //    }

            //    self.Sort();
            //}
        })
        $("#Exceptions").parent().css("height", "auto");
    },

    //Sort Care Instructions array by Position
    Sort: function () {
        let self = this;
        let exceptions = self.CareInstructions.filter(x => {
            return x.Category === "Exceptions"
        });
        let additional = self.CareInstructions.filter(x => {
            return x.Category === "Additional"
        });

        //Sort Additionals
        let length = 6;

        if (additional.length > 0) {
            //var position = additionalLength + length;
            for (let a = 0; a < additional.length; a++) {
                additional[a].Position = length + a;
            }
        }

        //Sort Exceptions
        length = 6 + additional.length;

        if (exceptions.length > 0) {
            //var position = exceptionsLength + length;
            for (let a = 0; a < exceptions.length; a++) {
                exceptions[a].Position = length + a;
            }
        }
    },

    //LoadCareInstuctionsValues: function () {
    //    var self = this;
    //    var elements = self.model.ModelElements;

    //    for (var e of elements) {
    //        if (e.matches('.wr-code')) {
    //            var selectElement = $("#" + e.id).data("kendoComboBox");
    //            var code = selectElement['Code'] || 'Do not ' + e.id;
    //            self.CareInstructions.push(
    //                {
    //                    Code: code,
    //                    Position: selectElement.element.data('idx'),
    //                    Category: e.id
    //                }
    //            );
    //        }
    //    }

    //    self.CareInstructions = self.CareInstructions.sort((a, b) => { return (a.Position > b.Position) ? 1 : -1 })

    //    self.model.CareInstructionsSorted = JSON.stringify(self.CareInstructions);
    //},

    // change between footwear and garment composition
    ChangeTargetArticle_Event: function (evt) {

        let self = this;

        if (!self.EnableEvents) {
            evt.preventDefault();
            return false;
        }

        let value = document.getElementsByName("TargetArticle")[0].value;

        self.ChangeTargetArticle(value);

    },

    // change sections source
    // 1 -> garment, 2 -> footwear
    ChangeTargetArticle: function (articleType) {

        let self = this;

        if (articleType == 2) // shoes 3 sections
            self.SetShoes3Sections();
        if (articleType == 3) // shoes 2 sections
            self.SetShoes2Sections();
        else {
            // articleType = 1 or unknow
            // add defautl section if it's empty
            if ($('.section').length == 0) {
                self.AddSection(true);
            }

            $('select.section-list').each(function (e) {
                let combo = $(this).data('kendoComboBox');
                combo.setDataSource(self.SectionDataFiltered());
            });
        }

    },

    ManageCompoOptions: function (e) {

        var self = this;
        var value = document.getElementsByName("TargetArticle")[0].value;
        var hidden = false;
        var isShoes = [2, 3].indexOf(parseInt(value)) != -1;

        //Manage weight options only allowed for mexican compo
        var weightElements = document.getElementsByClassName("section-weight");
        hidden = parseInt(value) === 1 ? false : true;
        for (var element of weightElements) {
            element.hidden = hidden;
        }

        //Manage shoes options fibers only allowed for shoes
        var shoeCompoElementsFibers = document.querySelectorAll(".fiber-type , .fiber-type-icon");

        hidden = isShoes ? false : true;
        for (var element of shoeCompoElementsFibers) {
            element.hidden = hidden;
        }

        //Manage shoes options fiber countries madein only allowed for shoes
        var shoeCompoElementsCountries = document.querySelectorAll(".country-code");

        hidden = isShoes && self.ProjectData.AllowMadeInCompoShoesFiber ? false : true;
        for (var element of shoeCompoElementsCountries) {
            element.hidden = hidden;
        }

        //self.SetSourceSections();

    },

    ValidateSection: function (e) {
        var self = this;

        self.MaxPercentage = 0;
        var elements = self.model.ModelElements;
        for (var e of elements) {
            if (e.matches('.section-weight')) {
                self.MaxPercentage += (e.value ? parseInt(e.value) : 0);
            }
        }
    },

    //EnableComposition: function (e) {
    //    var value = parseInt(e.target.value) == 0 ? true : false;

    //    var nodes = document.getElementsByName("Composition")[0].getElementsByTagName('*');
    //    for (var i = 0; i < nodes.length; i++) {
    //        nodes[i].disabled = value;

    //        if (nodes[i].matches('.k-select')) {
    //            nodes[i].hidden = value;
    //        }

    //        if (nodes[i].matches('.k-icon')) {
    //            nodes[i].hidden = value;
    //        }

    //    }

    //    document.getElementsByName("TargetArticle")[0].disabled = value;
    //    document.getElementsByClassName("add-section")[0].disabled = value;

    //},

    //Enable or disable Washing rules section
    //EnableWashingRules: function (e) {
    //    var value = parseInt(e.target.value) == 0 ? true : false;

    //    let sections = ['WashinRules', 'TemplatesRightZone'];

    //    for (let section of sections) {
    //        var nodes = document.getElementsByName(section)[0].getElementsByTagName('*');
    //        for (var i = 0; i < nodes.length; i++) {
    //            nodes[i].disabled = value;

    //            if (nodes[i].matches('.k-select')) {
    //                nodes[i].hidden = value;
    //            }

    //        }
    //    }

    //    $("#Additional").data("kendoMultiSelect").enable(!value);
    //    $("#Exceptions").data("kendoMultiSelect").enable(!value);

    //},

    // TODO:  find a better way to set options for Combo "TargetArticle"
    LoadCompoOptions: function () {

        var self = this;
        var data = [];

        if (self.ProjectData.EnableShoeComposition && self.options.Label.ShoeComposition) {
            data.push({
                ID: "2",
                Value: self.options.Shoes3SectionsOptionsText
            });
            data.push({
                ID: "3",
                Value: self.options.Shoes2SectionsOptionsText
            });

            //AppContext.LoadComboBox(self.elements.TargetArticle, data, "ID", "Value");
            self.FillCombo(self.elements.TargetArticle, data, "ID", "Value", "2") // default is compo with 3 sections


        } else {

            data.push(self.GetDefaultOption());

            if (self.ProjectData.EnableSectionWeight) {
                data.push({
                    ID: "1",
                    Value: self.options.CompoForMexicoOptionText
                });
            }

            //AppContext.LoadComboBox(self.elements.TargetArticle, data, "ID", "Value");
            self.FillCombo(self.elements.TargetArticle, data, "ID", "Value", "0");
        }

        if (self.elements.TargetArticle.find('option').length > 1)
            self.elements.CompoOptionsSection.removeClass('d-none')

    },

    LoadShoeCompoCatalogs: async function () {

        var self = this;

        //Load countries catalog
        self.CountriesData = await AppContext.HttpGet(`/countries/getlist/`);

        //Load fibers catalog
        self.FibersTypeData.unshift({
            ID: "0",
            Name: '',
            Symbol: "",
            IsActive: 0
        }); // for locked fiber types to pass validation
        self.FibersTypeData.unshift({
            ID: "1",
            Name: self.options.LeatherText,
            Symbol: "A",
            IsActive: 1
        });
        self.FibersTypeData.unshift({
            ID: "2",
            Name: self.options.TextileText,
            Symbol: "B",
            IsActive: 1
        });
        self.FibersTypeData.unshift({
            ID: "3",
            Name: self.options.SyntheticText,
            Symbol: "C",
            IsActive: 1
        });
    },

    //Validate sections anf fibers
    ValidateSections: function () {

        var self = this;
        var response = true;

        var fibersZero = 0;
        var allSections = [];
        var uniqueFibers = true;
        let fiberWeightTotal = true
        let sectionWeightTotal = true

        // mexican compo, sections with weight
        // TODO: count sections weight total here, dont use self.maxPercentage
        var weightElements = document.getElementsByClassName("section-weight");
        if (self.elements.TargetArticle[0].value == 1 && self.ProjectData.EnableSectionWeight && self.MaxPercentage != 100) {
            sectionWeightTotal = false;
        }

        if (sectionWeightTotal) {
            for (var element of weightElements) {
                element.classList.remove('is-invalid');
            }
        } else {
            for (var element of weightElements) {
                element.classList.add('is-invalid');
            }
        }

        //TODO: identify duplicated fibers inner sections, section with problem, empty fibers weight, empty fibers value
        // { Code: SectionCode, RepetedFibers: [{ Value, Elements:[]  }, ...], EmptyWeight: [Element1,...], EmptyValue: [Element1...], IsEmptyValue, IsEmptyWeight, MoreThatOnce }
        //let sectionsWithBadFibbers = [];

        //Validate fibers
        var sections = Array.from(document.getElementsByClassName('section'));
        //        for (var section of sections) {
        sections.forEach((section, sectionIdx) => {

            // skip on disable section
            let combo = $('select.section-list', section);
            let sectionRow = self.SectionData.find(f => f.ID == combo.val())
            let isBlank = self.model[`Sections[${sectionIdx}].IsBlank`]

            if ((combo.is('[readonly]') && sectionRow.Code == '0') || isBlank)
                return; // ignore section validation

            let ctrl = $('input.section-list', section).val(); //user interface text value
            allSections.push(ctrl);

            let fibers = section.getElementsByClassName('fiber-weight');
            let fibersTotal = 0;

            for (var fiber of fibers) {
                if ($(fiber).prop('readonly') == false && (fiber.value == "" || fiber.value == "0"))
                    fibersZero++; // zero value found
                let fiberValue = fiber.value ? parseInt(fiber.value) : 0
                fibersTotal += fiberValue;
            }

            if (fibersTotal != 100) {
                fiberWeightTotal = false;
                for (var fiber of fibers) {
                    fiber.classList.add('is-invalid');
                }
            } else {
                for (var fiber of fibers) {
                    fiber.classList.remove('is-invalid');
                }
            }

            // unique fibers
            let fibersInSection = $(self.FiberCodeSelector, self.SectionSelector(sectionIdx))

            let values = Array.from(fibersInSection).map(f => f.value);
            let uniqueFiberCodes = [...new Set(values)];

            if (values.length != uniqueFiberCodes.length) {
                uniqueFibers = false;
            }

        });

        let uniqueSections = [...new Set(allSections)];
        if (!self.options.EnableRepeatSections && allSections.length != uniqueSections.length) {
            AppContext.ShowError(self.options.UserErrorSectionsRepeat);
            response = false;
        }

        if (fibersZero > 0) {
            AppContext.ShowError(self.options.UserErrorFibersMinWeigth);
            response = false;
        }

        if (!uniqueFibers) {
            AppContext.ShowError(self.options.UserErrorFiberRepeat);
            response = false;
        }

        if (!fiberWeightTotal || !sectionWeightTotal) {
            AppContext.ShowError(self.options.UserErrorFibersMaxWeigth);
            response = false;
        }

        return response;
    },

    //Validate sections shoes combos
    ValidateShoesCombos: function () {

        var self = this;
        var response = true;
        var countryResponse = true;

        //Validate fibers
        if (self.ProjectData.EnableShoeComposition && self.options.Label.ShoeComposition) {

            var sections = document.getElementsByClassName('section');
            for (var section of sections) {
                var fibers = section.getElementsByClassName('fiber-type');
                var countries = section.getElementsByClassName('country-code');

                var i = 0;
                for (var fiber of fibers) {

                    if (i != fibers.length && fiber.matches('.k-widget')) {
                        var span = fiber.childNodes[0];
                        var input = fiber.childNodes[1];

                        if (!input.value) {
                            span.style.borderColor = 'red';
                            response = false;
                        } else {
                            span.style.borderColor = '#d5d5d5';

                            //select country when Fiber-Type 1 (Leather) is selected
                            if (input.value == 1 && self.ProjectData.AllowMadeInCompoShoesFiber) {
                                var countryCombo = countries[i];
                                var countrySpan = countryCombo.childNodes[0];
                                var countryInput = countryCombo.childNodes[1];

                                if (!countryInput.value) {
                                    countrySpan.style.borderColor = 'red';
                                    countryResponse = false;
                                    response = false;
                                } else {
                                    countrySpan.style.borderColor = '#d5d5d5';
                                }
                            }

                        }
                    }

                    i++;

                }
            }
        }

        if (!countryResponse && !response)
            AppContext.ShowError(self.options.UserErrorCountryOfOriginRequired);
        else if (!response)
            AppContext.ShowError(self.options.UserErrorFiberTypeRequired);

        return response;
    },

    GetCareInstructionComboHTMLTemplate: function () {
        var self = this;
        return `<span class="${self.options.WrSymbolCssClass}">#:data.Symbol#</span> - #:data.${self.GetLang()}#`
    },

    GetFiberTypeComboHTMLTemplate: function () {
        // TODO: check with designer team the fonts for shoes
        return `<span class="scalpers-shoes-fiber">#:data.Symbol#</span> - #:data.Name#`
    },

    ValidComposition: function () {

        var self = this;
        var emptyInputs = [];

        //remove class
        $("input[name*='SectionID_input']").parent().removeClass("comboArticlesNull");
        $("input[name*='FiberID']").parent().removeClass("comboArticlesNull");

        let allSectionElements = $("select.section-list");
        let sectionsInputsUI = $("input[name*='SectionID_input']");
        let allFiberElements = $(self.FiberCodeSelector);
        let fibersInputUI = $("input[name*='FiberID']");

        allSectionElements.each((index, section) => {
            if (section.value == '') {
                emptyInputs.push(sectionsInputsUI.get(index));
            }
        })

        allFiberElements.each((index, fiber) => {
            if (fiber.value == '')
                emptyInputs.push(fibersInputUI.get(index));
        })

        if (emptyInputs.length > 0) {
            emptyInputs.forEach((input, idx, arr) => {
                $(input).parent().addClass("comboArticlesNull");
            });

            AppContext.ShowError(self.options.UserErrorFibersAndSectionsAreNotCompleted);
            return false;
        } else {
            return true;
        }
    },

    ValidCareInstruccions: function () {
        let self = this;
        let emptyInputs = [];

        //remove class
        $("input[name*='Instruction_input']").parent().removeClass("comboArticlesNull");


        if (self.options.Label.IncludeCareInstructions == false) return true;

        // ['Wash', 'Bleach'..] --> ['#Wash', '#Bleach'..]
        let ciList = Array.from(self.options.CareInstructionsCategory.map(arrItem => `#${arrItem}`))

        ciList.forEach((ciElement, ciIdx) => {
            let $ci = $(ciElement)

            if ($ci.hasClass('ignore'))
                return;

            let combo = $ci.data('kendoComboBox')
            if (!combo.value())
                emptyInputs.push(ciElement);
        })

        if (emptyInputs.length > 0) {
            emptyInputs.forEach((input, idx, arr) => {

                let name = $(input).attr('name')
                let inputui = $(`[name="${name}_input"]`)
                $(inputui).parent().addClass("comboArticlesNull");
            });

            AppContext.ShowError("Care Instrucctions are mandatory");
            return false;
        } else {
            return true;
        }

    },

    GetLang: function () {

        var self = this;
        var langSys = self.options.UserLang;

        return self.options.Languages.find(x => x.Culture == langSys) ? self.options.Languages.find(x => x.Culture == langSys).Lang : self.options.DefaultLangField;

    },

    FillCombo: function (comboName, data, valueField, textField, currentValue) {
        let self = this;

        if (!data)
            data = [];

        let combo = AppContext.GetContainerElement(comboName);
        let options = [];

        combo.empty();

        data.forEach((value, idx, arr) => {

            let opValue = valueField ? value[valueField] : value.ID ? value.ID : value.Value ? item.Value : 0;
            let opText = textField ? value[textField] : value.Name ? value.Name : value.Text ? value.Text : "";

            let selected = '';

            if (currentValue !== undefined && opValue == currentValue)
                selected = 'selected'

            var opt = `<option value="${opValue}" ${selected}>${opText}</option>`
            options.push(opt);
        });

        let allOptions = options.join('');

        combo.html(allOptions);

    },

    SetShoes3Sections: function () {
        let self = this;
        self.SetShoesSections();

        // change datasource for first section
        let topSource = self.SectionDataFiltered().filter(_f => _f.Code == this.options.ShoesSectionConfig[0].Code);
        let topCombo = $('select.section-list:nth(0)').data('kendoComboBox');
        topCombo.setDataSource(topSource);
        topCombo.select(0);

        // unlock middle section
        let middleSource = self.SectionData.filter(_f => _f.Code === this.options.ShoesSectionConfig[1].Code); // get empty value
        let middleCompo = $('select.section-list:nth(1)').data('kendoComboBox');
        middleCompo.setDataSource(middleSource);
        middleCompo.select(0);
        middleCompo.readonly(true);

        // unlock fibers
        let section = $(self.SectionSelector(1));
        let fibers = section.find('.fiber');

        if (self.options.CompositionDefined && self.options.CompositionDefined.Sections.length > 1) { }

        fibers.each((index, fiberContainer) => {
            let fiberCombo = $(self.FiberCodeSelector, fiberContainer).data('kendoComboBox');
            fiberCombo.setDataSource(self.FibersDataFiltered())
            fiberCombo.value("");
            fiberCombo.readonly(false);

        });

        $('.fiber-weight', section).each((index, fiberWeight) => {
            $(fiberWeight)
                .prop('readonly', false)
                .attr('validation', 'range(1, 100);regex(^([1-9]?[0-9]|100)$)')

        });

        let typeCombo = section.find('select.fiber-type').data('kendoComboBox');
        let typeSource = self.FibersTypeDataFiltered();
        typeCombo.setDataSource(typeSource);
        typeCombo.readonly(false);
        typeCombo.select(-1);

        //disable add more fibers
        // TODO add-fiber-? element not are binding, use jquery
        let addFiberButon = $('[name="add-fiber-1"]').find('button');
        addFiberButon.prop('disabled', false)
    },

    /// 2 sections are the same from 3 sections, only lock milldle section
    // TODO: update this method for more simple algorithm to real requirement defined at 2022-11-24 -> copy the same source for the top
    SetShoes2Sections: function () {
        let self = this;

        self.SetShoesSections();

        let topSource = self.SectionDataFiltered().filter(_f => _f.Code == this.options.ShoesSectionConfig[0].Code);
        // change datasource for first section
        let topCombo = $('select.section-list:nth(0)').data('kendoComboBox');
        topCombo.setDataSource(topSource);
        topCombo.select(0);

        // lock middle section
        var emptySectionSource = self.SectionData.filter(_f => _f.Code === '0'); // get empty value
        var middleCompo = $('select.section-list:nth(1)').data('kendoComboBox');
        middleCompo.setDataSource(emptySectionSource);
        middleCompo.select(0);
        middleCompo.readonly(true);

        // only keep one fiber, with empty fiber value
        let section = $(self.SectionSelector(1));
        let fibersLen = section.find('.fiber').length;

        if (fibersLen > 1) {
            while (fibersLen > 1) {
                self.RemoveFiber(1, fibersLen - 1);
                fibersLen--;
            }
        }

        let fiberContainer = $(self.FiberContainerSelector(0), section);
        let fiberCombo = $(self.FiberCodeSelector, fiberContainer).data('kendoComboBox');
        let emptyFiberSource = self.FibersData.filter(_f => _f.Code === '0')
        fiberCombo.setDataSource(emptyFiberSource)
        fiberCombo.select(0);
        fiberCombo.readonly(true);

        let fiberWeight = $('.fiber-weight', fiberContainer);
        fiberWeight.val(0);
        fiberWeight.prop('readonly', true);

        // remove validation
        fiberWeight.attr('validation', '');

        let typeCombo = section.find('select.fiber-type').data('kendoComboBox');
        let typeSource = self.FibersTypeData.filter(_f => _f.ID == '0');
        typeCombo.setDataSource(typeSource)
        typeCombo.select(0);
        typeCombo.readonly(true);

        //disable add more fibers
        // TODO add-fiber-? element not are binding, use jquery
        let addFiberButon = $('[name="add-fiber-1"]').find('button');
        addFiberButon.prop('disabled', true)

    },

    SetShoesSections: function () {

        let self = this;

        let sectionsDefined = self.options.CompositionDefined.Sections;
        let sectionLength = $('.section').length;

        // if not sections added, add 3 sections for shoes
        if (sectionLength < 3) {
            while (sectionLength < 3) {
                self.AddSection(true);
                sectionLength = $('.section').length;
            }

        }
        while (sectionLength > 3) {
            self.RemoveSection(sectionLength - 1, true);
            sectionLength--;
        }

        // lock last section
        var source = self.SectionData.filter(_f => _f.Code === this.options.ShoesSectionConfig[2].Code);
        var lastSection = $('select.section-list:nth(2)').data('kendoComboBox');
        lastSection.setDataSource(source);
        lastSection.select(0);
        lastSection.readonly(true);
    },



    LoadUserdata: function () {

        let self = this;

        let compo = this.options.CompositionDefined;

        let sections = compo.Sections;
        let ciList = compo.CareInstructions
        // +-------------------------------+
        // | fill form
        // +-------------------------------+

        // fill sections
        if (sections)
            $(sections).each((sectionIdx, s) => {

                if ($(self.SectionSelector(sectionIdx)).length < 1) { // check if secctionIdx already exist
                    self.AddSection();
                }

                self.SetSectionData(sectionIdx, s);

                //  fill fiber for every section
                if (s.Fibers)
                    $(s.Fibers).each((fiberIdx, f) => {

                        let $fiberContainer = $(self.FiberSelector(fiberIdx, sectionIdx));

                        if ($fiberContainer.length < 1) { // check if fiberIdx already exist
                            self.AddFiber(sectionIdx);
                        }

                        self.SetFiberData(fiberIdx, sectionIdx, f);
                    });

            });


        // has careinstruction
        if (self.options.Label.IncludeCareInstructions == false || !ciList)
            return;

        self.CareInstructionsCategory.forEach((categoryID) => {
            let ci = ciList.find((f) => f.Category.toLowerCase() == categoryID.toLowerCase())

            if (ci)
                self.SetCareInstructionData(categoryID, ci);
            else
                if (!ci && self.options.ForceToSelectWrOption == false) {
                    $(`#${categoryID}`).addClass('ignore');
                    //let comboCi = $(`#${categoryID}`).data('kendoComboBox')
                    //let src = comboCi.dataSource.options.data;
                    //let emptyOption = { ID: 0, English: 'Leave Blank', Spanish: 'Sin selección', Code: '', Symbol: '', Instruction: 0 };
                    let emptyOption = self.GetEmptyOptionForCareInstruction();
                    //src.unshift(emptyOption)
                    //comboCi.setDataSource(src);
                    self.SetCareInstructionData(categoryID, emptyOption);
                }

        });

        //set Symbols
        self.SetCareSymbols();

        if (self.ProjectData.AllowAdditionals) {
            let additionals = ciList.filter((f) => f.Category.toLowerCase() == 'Additional'.toLowerCase());
            let additionalsIds = additionals.map((v) => v.Instruction);
            if (additionalsIds.length > 0)
                self.SetMultiselectData('Additionals', additionalsIds)
        }

        if (self.ProjectData.AllowExceptions) {
            let exceptions = ciList.filter((f) => f.Category.toLowerCase() == 'Exception'.toLowerCase());
            let exceptionsIdx = exceptions.map((v) => v.Instruction);
            if (exceptionsIdx.length > 0)
                self.SetMultiselectData('Exceptions', exceptionsIdx)
        }

        
    },

    GetDefaultOption: function () {
        //Fill combo Template
        let defaultValue = {
            ID: 0,
            Code: '-',
            English: this.options.DefaultOptionText,
            Spanish: this.options.DefaultOptionText,
            IsActive: true
        };

        return defaultValue;
    },

    GetEmptyOptionForCareInstruction: function () {
        return { ID: 0, English: 'Leave Blank', Spanish: 'Sin selección', Code: '', Symbol: '', Instruction: 0 };
    },


    SetAsBlankSection_Event: function (evt) {

        let self = this;

        if (!self.EnableEvents) {
            evt.preventDefault();
            return false;
        }

        let chk = $(evt.currentTarget)

        let sectionIdx = chk.data('idx')

        let isBlank = chk.prop('checked');

        self.SetAsBlankSection(sectionIdx, isBlank)

    },

    SetAsBlankSection: function (sectionIdx, isBlank) {
        
        let self = this;

        var fibers = $('.fiber', self.SectionSelector(sectionIdx));

        let addFiberBtn = $('.add-fiber', self.SectionSelector(sectionIdx));
        // hide
        //fibers.toggle();
        if (isBlank) {
            fibers.remove();
            addFiberBtn.hide();
        } else {
            self.AddFiber(sectionIdx);
            addFiberBtn.show();
        }

    }

    // #region fill form


    // #endregion fill form

};

// #endregion Define Composition