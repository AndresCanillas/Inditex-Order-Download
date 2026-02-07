
// =========================================================================
// Abstract Composition details Wizard
// =========================================================================

/**
 * Base Version
 * @@param orderSelection
 * @@param wizardStep
 * @@param allSteps
 */
/*
order selection is array of orders selected from OrderGroupIndexView
[{ OrderGroupID: 1, OrderNumber: '8545', Details: [], Orders: [OrderId, OrderID, ....], WizardStepPosition: 0  }]
 */
var CompoDefinition = {
    Quantity: 0,
    ArticleID: 0,
    Definition: {}, // see DefineComposition model
    ArrayData: [],
    Index: -1,
};

var ACompoWizardView = function (options, orderSelection, wizardStep, allSteps) {

    if (!options)
        options = {};

    this.options = ACompoWizardView.GetDefaultOptions(options);

    this.OrderSelection = orderSelection;
    this.FormSelector = '[name="labellingcomposimple-wizard-frm"]';
    this.CurrentTime = (new Date()).valueOf();
    this.CompoArticles = [];
    this.CompoDefined = []; // array of CompoDefinition by the user
    this.Copied = false;
    this.CopiedUnit = -1;
    this.CopiedGroup = -1;
    this.CopyData = {};
    this.ProjectData = {};
    this.Details = [];
    AWizardBaseView.Extend(this, this.options.Title, this.options.ViewUrl, wizardStep, allSteps);
}

ACompoWizardView.Extend = function (target, title, viewUrl, wizardStep, allSteps) {
    ACompoWizardView.call(target, title, viewUrl, wizardStep, allSteps);
    ExtendObj(target, ACompoWizardView.prototype);
}

ACompoWizardView.GetDefaultOptions = function (options) {

    return $.extend({

        ApplyAllText: 'Apply All',
        ApplyText: 'Apply',
        ArticleTitleText: 'Article',
        AssignText: 'Assign',
        CancelText: 'Cancel',
        CopyText: 'Copy',
        CopySuccessText: 'Successfully copied!',
        CopyWarningText: 'No items have been copied.',
        OrderNumberTitleText: 'Order N°',
        OverrideQuestionText: 'Do you want to copy the composition only empty elements in this Order?',
        PasteText: 'Paste',
        Title: 'Define Compostion',
        WithoutLabelText: 'Without Label',

        ViewUrl: '/Validation/Common/LabellingCompoSimpleWizard',
        DefineCompositionViewUrl: '/Validation/Common/DefineCompositionView.js',
        ConfirmDialogTemplate: '/Validation/Common/Components/OverrideCompositionConfirmDialog',

        AllowEmptyCareInstructions: false,
        LabelType: 2, // LabelType.CareLabel = 2
    }, options);

}



ACompoWizardView.prototype = {

    constructor: ACompoWizardView,

    OnLoad: function () {

        let self = this;

        AppContext.Projects.GetByID(self.GetProjectID()).done((result) => {
            self.ProjectData = result;
        });

        self.model.WizardID = self.WizardStep.WizardID;
        self.model.WizardStepID = self.WizardStep.ID;

        self.ShowStatus();
        self.GetCompoArticles().done(() => {

            self.LoadArticles(); // get compo article details
        })

        //$('#modal-image').on('load', function () {
        //    console.log('load a new image')
        //})

    },

    GetProjectID: function () {
        let self = this;
        return self.OrderSelection[0].ProjectID;
    },

    GetPostData: function () {
        let self = this;

        let rq = self.CompoDefined.map((x) => {
            if (x.DataObject) {
                let quantityFieldName = `rq[${x.GroupIdx}].Details[${x.Index}].Quantity`;
                let quantityInput = $(`[name="${quantityFieldName}"]`);

                //let articleFieldName = `rq[${x.GroupIdx}].Details[${x.Index}].ArticleID`;
                //let articleInput = $(`[name="${articleFieldName}"]`);

                x.DataObject.ProjectID = self.ProjectData.ID;
                x.DataObject.Quantity = quantityInput.val();
                //x.DataObject.ArticleID = articleInput.val();
            }
            return x.DataObject;
        });

        return rq;
    },

    // update quantities and Article
    Save: function () {

        let self = this;

        if (!self.model.ValidState()) {
            let d1 = $.Deferred();
            d1.reject();

            return d1.promise();
        };

        let rq = self.GetPostData();

        self.model.CompoDefined = JSON.stringify(self.CompoDefined);
        //var arrayData = $(self.FormSelector).serializeArray();
        return AppContext.Wizard.SaveStateLabellingCompoSimple(rq).then((result) => {

            $(`.compo-filled`, self.FormSelector).removeClass('pending');

            for (let i = 0; i < self.CompoDefined.length; i++) {
                for (let j = 0; j < result.Data.length; j++) {
                    if (result.Data[j].ProductDataID == self.CompoDefined[i].DataObject.ProductDataID) {
                        let found = j;

                        self.AddCompoArticle({
                            DataObject: result.Data[found],
                            Index: self.CompoDefined[i].Index,
                            GroupIdx: self.CompoDefined[i].GroupIdx,
                            Quantity: result.Data[0].Quantity,
                            ArticleID: result.Data[0].ArticleID

                        }, self.CompoDefined[i].Index, self.CompoDefined[i].GroupIdx)
                        break;
                    }
                }

            }

        });

    },

    _solve: function () {
        let self = this;
        let d1 = $.Deferred();
        if (!self.model.ValidState() && !self.AllCompositionAreDefined()) {
            d1.reject();

        } else {

            let rq = self.GetPostData();

            // create OrderGroupSelectionCompositionDTO object
            let selection = self.OrderSelection.map((sel, groupIndex) => {

                sel.Compositions = rq.filter((detail) => {
                    if (detail.OrderGroupID == sel.OrderGroupID)
                        return detail;
                })

                return sel;

            });

            AppContext.Wizard.SaveAndNextLabellingCompoSimple(selection)
                .done((response) => {
                    if (!response.Success)
                        d1.reject();

                    $(`.compo-filled`, self.FormSelector).removeClass('pending');

                    d1.resolve(response);
                })
                .fail(() => {
                    d1.reject();
                    console.log('error save state');
                });

        }

        return d1.promise();
    },

    PrepareCompositionObject: function (arrayData, groupIdx, idx) {
        let quantityFieldName = `rq[${groupIdx}].Details[${idx}].Quantity`;
        let quantityInput = $(`[name="${quantityFieldName}"]`);

        
        //let articleFieldName = `rq[${x.GroupIdx}].Details[${x.Index}]`;
        //let articleInput = $(`[name="${articleFieldName}"]`);

        arrayData.push({
            name: 'Quantity',
            value: quantityInput.val()
        });
        //arrayData.push({
        //    name: 'ArticleID',
        //    value: articleInput.val()
        //});

        return arrayData;
    },

    SaveCompositionDefined: function (arrayData, groupIdx, idx) {
        let self = this;

        postData = self.PrepareCompositionObject(arrayData, groupIdx, idx);
        arrayData.forEach((e) => e.name = 'rq.' + e.name);
        return AppContext.Wizard.SaveCompositionDefined(postData);

    },

    SaveAndNext: function () {

        let self = this;

        if (!self.CanGoNext()) {

            AppContext.ShowError('@g["Please, Define Composition first!"]');

            return;
        }

        let p = self._solve();

        // check if all composition are defined


        p.done(() => {

            self.GetValidator().GetStep(self.WizardStep.Position + 1);

        })

    },

    CanGoNext: function () {

        let compositionArticlesRequired = false;

        let allDefinitionAreFilled = $('.compo-filled').length == $('.compo-filled.yes').length;

        $('[name$="].ArticleID"]').each((j, el) => {
            if ($(el).val() > 0)
                compositionArticlesRequired = true;
        });

        if (!compositionArticlesRequired)
            return true;

        return allDefinitionAreFilled;

    },

    GetCompoArticles: function () {
        let self = this;

        // public enum LabelType.CareLabel = 2
        return AppContext.Wizard.GetArticlesByLabelType({
            Selection: self.OrderSelection,
            LabelType: self.options.LabelType
        }).done((response) => {

            if (!response.Success)
                return;

            self.CompoArticles = response.Data;

        })

    },

    FillCombo: function (comboName, data) {
        let self = this;

        if (!data)
            data = [];

        self.elements[comboName].empty()

        let options = [
            `<option value="0">${self.options.WithoutLabelText}</option>`
        ];

        data.forEach((value, idx, arr) => {
            let opt = `<option value="${value.ID}" data-label-id="${value.LabelID}">${value.Name}</option>`
            options.push(opt);
        });

        let allOptions = options.join('');

        self.elements[comboName].html(allOptions);

    },

    LoadArticles: function () {

        let self = this;

        AppContext.Wizard.GetOrderedLabelsGrouped(self.OrderSelection)
            .done((result) => {
                self.Details = result.Data;
                self._AddRows(result).done(() => {

                    // after add rows, update js objet with compo received to load prev data in form
                    for (let groupIdx = 0; result.Data && groupIdx < result.Data.length; groupIdx++) {

                        for (let idx = 0; result.Data[groupIdx].Details && idx < result.Data[groupIdx].Details.length; idx++) {
                            let detail = result.Data[groupIdx].Details[idx];
                            
                            if (self.options.AllowEmptyCareInstructions) {
                                self.FillWithBlankCareInstructions(detail.Composition.CareInstructions);
                            }

                            self.AddCompoArticle({
                                DataObject: detail.Composition,
                                Index: idx,
                                GroupIdx: groupIdx,
                                Quantity: detail.Quantity,
                                ArticleID: detail.Composition.ArticleID

                            }, idx, groupIdx)
                        }
                    }

                });

            });
    },

    AfterSelectArticle: function (evt) {
        let self = this;

        let el = $(evt.currentTarget);
        let table = el.closest('tbody')[0];
        let rows = table.querySelectorAll(".define-compo");
        for (var row of rows) {
            if (el[0].value == "0")
                row.classList.add("disabled");
            else
                row.classList.remove("disabled");
        }

        let idx = el.data('idx');
        let groupIdx = el.data('group')
        let unitNumber = el.data('item'); // unit number on screen
        //var optSelected = el.find('option:selected');
        //var labelId = optSelected.data('labelId');
        //var preview = $(`[name="preview_${unitNumber}"]`);

        //preview.data('img', `/labels/getpreview/${labelId}?${self.CurrentTime}`);
        //preview.data('caption', optSelected.text());

        // update CompoArticleID
        //self.AddCompoArticle([], idx);
        let found = self.CompoDefined.findIndex((f) => f.Index == idx && f.GroupIdx == groupIdx);
        if (found >= 0)
            self.CompoDefined[found].ArticleID = el.val()

    },

    _AddRows: function (result) {

        let self = this;

        let d1 = $.Deferred()

        //var k = 0;
        let itemNumber = 0;

        let $tbody = self.elements.TableContainer.find('tbody');

        for (let i = 0; i < result.Data.length; i++) {

            let sel = result.Data[i];

            let trGroup = self._GetRowGroupTemplate(sel, i)

            $tbody.append(trGroup);

            for (let j = 0; j < sel.Details.length; j++) {

                let tr = $(self._GetRowTemplate(sel.Details[j], i, itemNumber++, j));

                $tbody.append(tr);

            }

            //k++;

        }

        // bind model and antions inner tbody

        //self.model = AppContext.BindModel(self.ViewContainer, self);
        //AppContext.BindActions($tbody, self);
        //AppContext.BindElements($tbody, self);
        AppContext.BindView($tbody, self).then(() => {

            // AppContext.BindView(self.ViewContainer,self);
            for (let i = 0; i < result.Data.length; i++) {
                //self.FillCombo(`rq[${i}].ArticleID`, self.CompoArticles);

                if (result.Data[i].Details && result.Data[i].Details.length > 0) {
                    $(`[name="rq[${i}].ArticleID"]`).val(result.Data[i].Details[0].ArticleID)
                    $(`[name="rq[${i}].ArticleID"]`).change()

                }
            }

            //$('.popover-preview').click(function (evt) {

            //    evt.preventDefault()

            //    var btn = $(evt.currentTarget);

            //    if (!btn.data('img')) {
            //        console.log('seleccionar opcion en combo');
            //        return;
            //    }

            //    $("#modal-image").attr('src', btn.data('img'));
            //    $("#modal-image").attr('alt', btn.data('caption'));
            //    $('#the-modal').modal('toggle');

            //});

            d1.resolve();

        });

        return d1.promise()

    },

    _GetRowGroupTemplate: function (sel, i) {
        let self = this;

        let tr = `
            <tr class="table-info" data-idx="${i}">
                <td colspan="10">

                    <input model name="rq[${i}].OrderNumber" value="${sel.OrderNumber}" type="hidden" />
                    <input model name="rq[${i}].OrderGroupID" value="${sel.OrderGroupID}" type="hidden" />
                    <input model name="rq[${i}].WizardStepPosition" value="${self.WizardStep.Position}" type="hidden" />
                    <input model name="rq[${i}].ProjectID" value="${sel.ProjectID}" type="hidden" />

                    
                    <div class="input-group input-group-sm wrapper-not-allowed">
                        <div class="col-sm col-form-label">${self.options.OrderNumberTitleText}</label>
                         ${sel.OrderNumber}
                    </div>

                </td>
                <!--
                <td colspan="6">
                    <div class="input-group input-group-sm wrapper-not-allowed">
                        <label class="col-sm col-form-label">${self.options.ArticleTitleText}</label>
                        <select model name="rq[${i}].ArticleID" class="form-control pointer-events-none" label="@g["Select Article"]" no-transform action="change:AfterSelectArticle"></select>
                    </div>
                </td>
                -->
            </tr>
        `;

        return tr;
    },

    _GetRowTemplate: function (detail, groupNumber, itemNumber, unitNumber) {
        let self = this;
        let d = detail;
        let i = groupNumber;
        let j = itemNumber;
        let k = unitNumber;

        let strSize = d.Size || "";

        let strColor = d.Color || "";

        let hasPackCode = d.HasPackCode ? 'has-packcode' : '';

        let combo = ""; //self._ComboTpl(detail, groupNumber, itemNumber, unitNumber); //preguntar a Rafa para que sirve

        let disableCompo = d.Composition.ArticleCode ? '' : 'disabled';

        let strUnitDetails = d.UnitDetails || "";

        let isDefinedValue = self._CompositionIsDefined(detail.Composition);

        let isCompositionFilled = self._CompositionHasData(detail.Composition) ? 'yes' : 'no';

        let isPending = ''; //isDefinedValue ? '' : (isFilled ? 'pending' : '');

        let isCompoDefinedClass = (isDefinedValue == 1 && isCompositionFilled != "no") ? '' : 'd-none';

        let ellipsisDescriptionCss = "width: 150px;overflow: hidden;white-space: nowrap;text-overflow: ellipsis;";

        let tr = `
                <tr data-group="${i}" data-item="${j}" data-unit="${k}">
                    <th scope="row">${(j + 1)}</th>
                    <td>${d.Article}</td>
                    <td><div style="${ellipsisDescriptionCss}" title="${strUnitDetails}">${strUnitDetails}</div></td>
                    <td>${strSize}</td>
                    <td>${strColor}</td>
                    <td>${d.QuantityRequested}</td>
                    <td>

                        <div class="form-inline">
                            <div name="item-${j}" class="input-group input-group-sm ${hasPackCode}" data-packcode="${d.PackCode}" data-pack-quantity="${d.PackConfigQuantity}" data-idx="${j}">
                                <input model name="rq[${i}].IDX[${j}].itemNumber" value="${j}" type="hidden" />
                                <input model name="rq[${i}].IDX[${j}].unitNumber" value="${k}" type="hidden" />
                                <input model name="rq[${i}].Details[${j}].ArticleID" value="${d.ArticleID}" type="hidden" />
                                <input model name="rq[${i}].Details[${k}].Color" value="${d.strColor}" type="hidden" />
                                <input model name="rq[${i}].Details[${k}].OrderID" value="${d.OrderID}" type="hidden" />
                                <input model name="rq[${i}].Details[${k}].ProductDataID" value="${d.ProductDataID}" type="hidden" />
                                <input model name="rq[${i}].Details[${k}].IsDefined" value="${isDefinedValue}" type="hidden" />
                                <input model name="rq[${i}].Details[${k}].Quantity"  readonly="readonly" action="keyup:GoNextField" type="number" value="${d.Quantity}" data-idx="${j}" validation="required;__range(${d.MinAllowed},${d.MaxAllowed})" _max="${d.MaxAllowed}" data-quantity-requested="${d.QuantityRequested}" class="form-control input-sm mr-1 user-entry" no-transform style="min-width: 60px;max-width: 80px;">

                            </div>
                        </div>

                    </td>
                    <!-- <td> ${combo} </td> -->
                    <input model name="rq[${i}].Details[${k}].Definition"  type="hidden" />

                    <td> <span class="compo-filled ${isCompositionFilled} ${isPending}" data-group="${i}" data-idx="${j}" data-item="${k}" ><i class="fa fa-fw fa-times not-defined" style="color:red"></i> <i class="fa fa-fw fa-check defined" style="color:#5FB85E"></i>  </span> </td>
                    <td> <a class="btn btn-sm btn-link ${disableCompo} define-compo" action="click:DefineCompo" data-group="${i}" data-group="${i}" data-idx="${j}" data-item="${k}" >${self.options.AssignText} </td>
                    <td>
                    <a class="btn btn-sm btn-link ${isCompoDefinedClass} clone-action copy" action="click:CopyDefinition" data-group="${i}" data-idx="${j}" data-item="${k}">${self.options.CopyText} <i class="fa fa-fw fa-clone"></i></a>
                    <a class="btn btn-sm btn-link d-none clone-action paste-one" action="click:PasteDefinition" data-group="${i}" data-idx="${j}" data-item="${k}" >${self.options.PasteText} <i class="fa fa-fw fa-clipboard"></i></a>
                    <a class="btn btn-sm btn-link d-none clone-action paste-all" action="click:ApplyAllDefinition" data-group="${i}" data-idx="${j}" data-item="${k}">${self.options.ApplyAllText} <i class="fa fa-fw fa-clipboard"></i></a>
                    </td>
                </tr>`;

        return tr;

    },

    _ComboTpl: function (detail, groupNumber, itemNumber, unitNumber) {
        let self = this;
        let d = detail;
        let i = groupNumber;
        let j = itemNumber;
        let k = unitNumber;

        let combo = `
        <div class="input-group input-group-sm">
            <label class="col-form-label" style="min-width:50px; max-width:60px;">@g["Article"]</label>
            <select model name="rq[${i}].Details[${k}].ArticleID" class="form-control article-selected" label="@g["Article"]" data-group="${i}" data-idx="${k}" data-item="${k}" validation="required;min(1)" no-transform action="change:AfterSelectArticle" style="min-width:80px; max-width:160px;">${self.OptionsTpl}</select>
           <div class="input-group-append">
              <button name="preview_${k}" data-idx="${k}" type="button" class="btn popover-preview" data-img="/images/no_preview.png" data-caption="@g["Select Article"]">
              <i class="fa fa-fw fa-picture-o " aria-hidden="true" ></i>
            </button>
          </div>
        </div>`;

        return combo;

    },

    // https://stackoverflow.com/questions/480735/select-all-contents-of-textbox-when-it-receives-focus-vanilla-js-or-jquery
    // https://stackoverflow.com/questions/5379120/get-the-highlighted-selected-text
    GoNextField: function (evt) {

        let key = evt.keyCode ? evt.keyCode : evt.which;

        if (key == 13) {

            evt.preventDefault();

            let currentPosition = $(evt.target).closest('.input-group').data('index');

            let nextField = $('[data-index="' + (parseInt(currentPosition + 1)) + '"]').find('.user-entry');

            if (nextField.length == 1) {

                nextField.focus();
                let tmpStr = nextField.val();
                nextField.val('');
                nextField.val(tmpStr);
                nextField.get(0).select();
            }

        }

        return true;
    },

    _CompositionIsDefined: function (compositionDefinition) {

        return compositionDefinition.ID > 0;
    },

    _CompositionHasData: function (compositionDefinition) {

        if (compositionDefinition.Sections && compositionDefinition.Sections.length > 0) {
            return 1;
        }

        return 0
    },

    AllCompositionAreDefined: function () {

        let self = this;

        let rt = true;

        // if user has no select a composition label, can skip this step

        let positionNotDefined = [];

        for (let i = 0; self.CompoDefined && i < self.CompoDefined.length; i++) {
            let c = self.CompoDefined[i];
            if (!c.Sections || c.Sections.length < 1) {
                rt = false;
                positionNotDefined.push(i);
            }
        }

        // mark on view undefined Composition Required

        return rt;
    },

    _FigureTemplate: function (imgUrl, alt, caption) {
        return `<figure name="FigureContainer" class="figure">
            <img name="SelectedPreview" src="${imgUrl}" class="figure-img  rounded" alt="${alt}">
            <figcaption class="figure-caption">${caption}</figcaption>
        </figure>`
    },

    _ActionsTemplate: function () { },

    _DefineCompositionCustomOptions: function (options) {
        return options;
    },

    DefineCompo: async function (evt) {
        let self = this;
        let btn = $(evt.currentTarget);
        let table = btn.closest('tbody')[0];
        let idx = btn.data('idx');
        let unitNumber = btn.data('item');
        let groupIdx = btn.data('group');
        let filledMark = btn.closest('tr').find('.compo-filled')
        const detail = self.Details[groupIdx].Details[idx];

        let found = self.CompoDefined.findIndex((e) => e.Index == unitNumber && e.GroupIdx == groupIdx);
        let compoData = null;
        if (found >= 0)
            compoData = self.CompoDefined[found].DataObject;

        let label = await AppContext.HttpGet(`/labels/getbyid/${detail.LabelID}`);// only require if label is for shoes

        btn.addClass("disabled");
        
        AppContext.LoadJS(self.options.DefineCompositionViewUrl).then(() => {

            //Get CodeGama
            AppContext.VariableData.GetByDetail(self.GetProjectID(), compoData.ProductDataID).done((result) => {

                let options = self._DefineCompositionCustomOptions({
                    ProjectID: self.OrderSelection[groupIdx].ProjectID,
                    Selection: self.OrderSelection[groupIdx],
                    CompositionDefined: compoData,
                    Label: label,
                    VariableData: result.Data,
                    UnitNumber: unitNumber
                });

                let compoView = new DefineCompositionView(options, self.ProjectData);

                compoView.ShowDialog(function (userCompo) {

                    // Edit GridView to update selected row
                    btn.removeClass("disabled");

                    if (!userCompo)
                        return;

                    // SaveState for current composition defined
                    self.SaveCompositionDefined(userCompo, groupIdx, unitNumber).then((saveResponse) => {

                        if (saveResponse.Success == false) return;

                        self.AddCompoArticle({
                            DataObject: saveResponse.Data,
                            Index: unitNumber,
                            GroupIdx: groupIdx,
                            Quantity: $(`[name="rq[${groupIdx}].Details[${unitNumber}].Quantity"]`).val(),
                            ArticleID: detail.ArticleID
                        }, unitNumber, groupIdx)

                        // mark as filled
                        filledMark
                            .removeClass('no')
                            .addClass('yes')

                        // Enable CopyButton
                        self.EnableCopyButton(idx, groupIdx);

                        // update ORderID to other coposition inner the sage groupidx
                        self.CompoDefined.forEach((c) => {
                            if (c.GroupIdx != groupIdx)
                                return; //->next

                            c.DataObject["OrderID"] = saveResponse.Data.OrderID
                        })
                    })

                }, "80%");

            });

        });
    },

    // @@compo Object form fields from DefineCompo
    // @@idx int unit position inner group
    // @@groupIdx int group position selected
    AddCompoArticle: function (captureCompo, idx, groupIdx) {
        let self = this;
        // add CompoArticle
        let found = self.CompoDefined.findIndex((e) => e.Index == idx && e.GroupIdx == groupIdx);
        if (found >= 0) {
            // TODO: check this condition ->
            //if (captureCompo.ArrayData.length === 0) // -> return
            //    compo = self.CompoDefined[found].ArrayData

            self.CompoDefined[found] = $.extend({}, captureCompo);
            self.CompoDefined[found].Index = idx;
            self.CompoDefined[found].GroupIdx = groupIdx;

        } else {
            let newCompoArticle = $.extend({}, captureCompo);
            newCompoArticle.Index = idx;
            newCompoArticle.GroupIdx = groupIdx;

            self.CompoDefined.push(newCompoArticle);
        }

    },

    CopyDefinition: function (evt) {
        let self = this;
        let unitNumber = $(evt.currentTarget).data('item');
        let groupIdx = $(evt.currentTarget).data('group');

        self._CopyDefinition(unitNumber, groupIdx);

    },

    _CopyDefinition: function (unitNumber, groupIdx) {
        let self = this;

        self.CopiedUnit = unitNumber;
        self.CopiedGroup = groupIdx;
        self.CopyData = self.CompoDefined.find((compoArticle) => compoArticle.Index == unitNumber && compoArticle.GroupIdx == groupIdx);

        self.EnablePasteButtons();

    },

    PasteDefinition: function (evt) {

        let self = this;
        let targetUnit = $(evt.currentTarget).data('item');
        let targetGroup = $(evt.currentTarget).data('group');

        self._PasteDefinition(targetUnit, targetGroup);
    },

    _PasteDefinition: function (targetUnit, targetGroup) {

        let self = this;
        // already exist CompoArticle -> replace, not exist, add new one
        let found = self.CompoDefined.findIndex((e) => e.Index == targetUnit && e.GroupIdx == targetGroup);
        if (found >= 0) {

            self.CompoDefined[found].DataObject.ArticleCode = self.CopyData.DataObject.ArticleCode;
            self.CompoDefined[found].DataObject.ArticleID = self.CopyData.DataObject.ArticleID;
            self.CompoDefined[found].DataObject.TargetArticle = self.CopyData.DataObject.TargetArticle;
            self.CompoDefined[found].DataObject.Sections = JSON.parse(JSON.stringify(self.CopyData.DataObject.Sections));
            self.CompoDefined[found].DataObject.CareInstructions = $.extend([], self.CopyData.DataObject.CareInstructions);
            self.CompoDefined[found].DataObject.WrTemplate = self.CopyData.DataObject.WrTemplate;
            self.CompoDefined[found].DataObject.Type = self.CopyData.DataObject.Type;
            self.CompoDefined[found].DataObject.Required = self.CopyData.DataObject.Required;
        } else {
            throw '@g["Error:No composition found."]';
        }

        $(`.compo-filled[data-item="${targetUnit}"][data-group="${targetGroup}"]`, self.FormSelector).removeClass('no').addClass('yes').addClass('pending');

        // TODO: enabel Copy Button

    },

    // enable for every valid compo
    EnableCopyButton: function (index, groupIdx) {

        let copyBtn = $(`.copy[data-idx="${index}"][data-group="${groupIdx}"]`);
        //var td = copyBtn.closest('td')
        //var pasteAll = td.find('.paste-all')

        // hide all and enable paste over all
        //$('.clone-action.paste-one,.clone-action.paste-all', self.FormSelector).removeClass('d-none').addClass('d-none')

        copyBtn.removeClass('d-none');
        //pasteAll.removeClass('d-none')

    },

    EnablePasteButtons: function () {
        let self = this;

        // enable all paste action
        $('.clone-action.paste-one', self.FormSelector).removeClass('d-none');

        // disble all past all action
        $('.clone-action.paste-all', self.FormSelector).removeClass('d-none').addClass('d-none');

        // hide current index
        $(`.clone-action.paste-one[data-item="${self.CopiedUnit}"][data-group="${self.CopiedGroup}"]`, self.FormSelector).addClass('d-none');

        $(`.clone-action.paste-all[data-item="${self.CopiedUnit}"][data-group="${self.CopiedGroup}"]`, self.FormSelector).removeClass('d-none');

        $('.clone-action.copy').text(self.options.CopyText);

        $(`.clone-action.copy[data-item="${self.CopiedUnit}"][data-group="${self.CopiedGroup}"]`).text(self.options.CopyText + '*');
    },
    _UpdateCompoDefinedByFilter: function (CopyCompoArticle, GroupToPaste) {
        let self = this;

        for (var i = 0; i < GroupToPaste.length; i++) {
            self.CompoDefined[GroupToPaste[i]].DataObject.ArticleCode = CopyCompoArticle.DataObject.ArticleCode;
            self.CompoDefined[GroupToPaste[i]].DataObject.ArticleID = CopyCompoArticle.DataObject.ArticleID;
            self.CompoDefined[GroupToPaste[i]].DataObject.TargetArticle = CopyCompoArticle.DataObject.TargetArticle;
            self.CompoDefined[GroupToPaste[i]].DataObject.Sections = JSON.parse(JSON.stringify(CopyCompoArticle.DataObject.Sections)); //Object.assign({}, self.CopyData.DataObject.Sections); //{ ...self.CopyData.DataObject.Sections }; //$.extend({}, self.CopyData.DataObject.Sections);
            self.CompoDefined[GroupToPaste[i]].DataObject.CareInstructions = $.extend([], CopyCompoArticle.DataObject.CareInstructions);
            self.CompoDefined[GroupToPaste[i]].DataObject.WrTemplate = CopyCompoArticle.DataObject.WrTemplate;
            self.CompoDefined[GroupToPaste[i]].DataObject.Type = CopyCompoArticle.DataObject.Type;
            self.CompoDefined[GroupToPaste[i]].DataObject.Required = CopyCompoArticle.DataObject.Required;
            $(`.compo-filled[data-idx="${self.CompoDefined[GroupToPaste[i]].Index}"][data-group="${CopyCompoArticle.GroupIdx}"]`, self.FormSelector).removeClass('no').addClass('yes');
        }
    },

    ApplyAllDefinition: function (evt) {
        var self = this;
        var CopyIndex = $(evt.currentTarget).data('item');
        var CopyGroupIdx = $(evt.currentTarget).data('group');

        self._CopyDefinition(CopyIndex, CopyGroupIdx);

        var confirm = new ConfirmDialog(self.options.OverrideQuestionText, self.options.ApplyText, self.options.CancelText, self.options.ConfirmDialogTemplate);

        confirm.ShowDialog((result) => {

            if (!result) {
                return;
            }

            let uniqueArticleIDs = new Set(self.CompoDefined.map(c => c.ArticleID));
            let uniqueArticleCount = uniqueArticleIDs.size;

            if (uniqueArticleCount > 1) {
                AppContext.ShowError("You cannot apply compositions to two different items. You must create a separate composition for each item");
                return;
            }

            let appliedCounter = 0;
            self.CompoDefined.forEach(function (c, index) {

                if (result.Override == false && c.DataObject.Sections.length > 0) {
                    return; // continue
                }

                if (c.GroupIdx == CopyGroupIdx && c.Index != CopyIndex) {
                    self._PasteDefinition(c.Index, c.GroupIdx);
                    appliedCounter++;
                }
            });

            if (appliedCounter > 0) {
                AppContext.ShowSuccess(`${appliedCounter} ` + self.options.CopySuccessText);
            } else {
                AppContext.ShowWarning(self.options.CopyWarningText, 5000);
            }

            return;
        });
    },

    FillWithBlankCareInstructions: function (currentCareInstructions) {

        if (!Array.isArray(currentCareInstructions) && currentCareInstructions.length < 1) {
            return;
        }

        var blank = [
            {
                "ID": 0,
                "Instruction": 0,
                "Code": null,
                "Category": "Wash",
                "CompositionID": 0,
                "AllLangs": null,
                "SymbolType": null,
                "Symbol": null
            },
            {
                "ID": 0,
                "Instruction": 0,
                "Code": null,
                "Category": "Bleach",
                "CompositionID": 0,
                "AllLangs": null,
                "SymbolType": null,
                "Symbol": null
            },
            {
                "ID": 0,
                "Instruction": 0,
                "Code": null,
                "Category": "Dry",
                "CompositionID": 0,
                "AllLangs": null,
                "SymbolType": null,
                "Symbol": null
            },
            {
                "ID": 0,
                "Instruction": 0,
                "Code": null,
                "Category": "Iron",
                "CompositionID": 0,
                "AllLangs": null,
                "SymbolType": null,
                "Symbol": null
            },
            {
                "ID": 0,
                "Instruction": 0,
                "Code": null,
                "Category": "DryCleaning",
                "CompositionID": 0,
                "AllLangs": null,
                "SymbolType": null,
                "Symbol": null
            }
        ];


        for (const blankCi of blank) {
            var found = currentCareInstructions.find(f => f.Category == blankCi.Category);

            if (!found)
                currentCareInstructions.push(blankCi); // Add element if it is not found
        }

        //return currentCareInstructions;
    }

};
