// =========================================================================
// Abstract Wizard Base
// =========================================================================
// #region WizardBase

/**
 * @requires FormView from wwwroot/js/Appcontext.js
 * @requires BreadCrumbsUI from wwwroot/js/Appcontext.UI.js
 * @requires ValidatorHelper from Pages/Common/js/ValidatorHelper.js
 *
 *
 *
 * @param int wizardStep, position number
 * @param Array allSteps, all steps list
 *
 */

// WizardType
/*
{
NotDefine = 0,
Quantity = 10,
Labelling = 15,
Extras = 30,
ShippingAddress = 80,
Review = 90
}
 */
// WizardStep Interface
/*
{ Type: 0, Url: '/', IsCompleted: false,  Name: '', Description: '', WizardID: -1 }
 */

var AWizardBaseView = function (title, viewUrl, wizardStep, allSteps) {
    this.WizardStep = wizardStep;
    this.AllSteps = allSteps;
    this.BreadCrumbs = null;

    FormView.Extend(this, title, viewUrl);

}

AWizardBaseView.Extend = function (target, title, viewUrl, wizardStep, allSteps) {
    AWizardBaseView.call(target, title, viewUrl, wizardStep, allSteps);
    ExtendObj(target, AWizardBaseView.prototype);
}

AWizardBaseView.prototype = {

    constructor: AWizardBaseView,

    GetValidator: function () {

        var self = this;

        var validator = new ValidatorHelper(self.OrderSelection);

        validator.OnWizardCreated.Subscribe(this, this._validatorHelperOnCreateEvent)

        return validator;

    },

    _validatorHelperOnCreateEvent: function (e) {

        var self = this;
        var wizard = e.wizardController;
        wizard.Show(self.ViewContainer);
    },

    ShowStatus: function () {
        var self = this;
        var breadCrumbs = new BreadCrumbsUI({
            ContainerElm: this.elements.BreadcrumbContainer,
            Controller: self,
            LinkClick: 'BCGoToStep',
            Crumbs: $.map(self.AllSteps, (wzdStep) => {
                // Crumb Object {Text, Position, Href, State }
                return {
                    Text: wzdStep.Name,
                    Position: wzdStep.Position,
                    Href: '#',
                    State: self.WizardStep.ID == wzdStep.ID ? CrumbState.PROGRESS : (wzdStep.IsCompleted ? CrumbState.COMPLETED : CrumbState.PENDING)
                }
            }),
            AutoRender: true,
            SizeCls: '', // not implemented
            AlignCls: '' // justify-content-center or justify-content-end
        });

    },

    BCGoToStep: function (evt) {

        var self = this;
        var stepBtn = $(evt.target);
        var step = stepBtn.data('position')
        var wzdStepSelected = self.AllSteps.find((val) => val.Position == step)
        var beforeStepSelected = self.AllSteps.find((val) => val.Position == step - 1)

        if (!wzdStepSelected
            || self.WizardStep.ID == wzdStepSelected.ID
            || (wzdStepSelected.IsCompleted == false && beforeStepSelected && beforeStepSelected.IsCompleted == false)) {
            evt.preventDefault();
            return;
        }

        self.GetValidator().GetStep(step);
    },

    Back: function () {
        this.GetValidator().GetStep(this.WizardStep.Position - 1);
    },


    /**
     * return array with the names of the catalogs
     * [Catalog1, Catalog2, ....CatalogN]
     * 
     * REQUIRE TO BE DEFINED IN CHILD CLASS
     */
    GetRequiredCatalogs: function () {
        return [];
    },

    // return array of object with the catalogs names
    _GetRequiredCatalogs: function () {
        return this.GetRequiredCatalogs().map(ct => { return { Name: ct }; });
    },
    /**
    * Customized function to return hashtable with catalogs required for this Custom Wizard
    * hashtable with catalogs data by catalogname: { MadeId: [MadeIn Data], Catalog2:[catalog2 data], ...}
    */
    LoadCatalogs: async function () {

        let self = this;

        let requiredCatalogs = self._GetRequiredCatalogs();

        if (requiredCatalogs.length < 1) return;

        await Promise.all(requiredCatalogs.map(async (catalog) => {

            return await AppContext.Catalogs.GetByName(
                self.OrderSelection[0].ProjectID,
                catalog.Name,
                null,
                null,
                false,
                false
            ).then((response) => {
                let found = requiredCatalogs.find(f => f.Name == catalog.Name)
                found.Definition = response.Data;
            })

        }));

        await Promise.all(requiredCatalogs.map(async (catalog) => {

            return await AppContext.CatalogData.GetByCatalogID(
                catalog.Definition.CatalogID,
                null,
                null,
                false
            ).then(response => {

                let found = requiredCatalogs.find(f => f.Name == catalog.Name)
                found.Data = JSON.parse(response);

            })

        }));

        self.Catalogs = requiredCatalogs;

    },

}
// #endregion


