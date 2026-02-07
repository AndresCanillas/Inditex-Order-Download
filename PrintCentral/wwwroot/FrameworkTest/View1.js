var View1 = function () {
	FormView.Extend(this, "Employee Details", "/View1.html");
};


View1.prototype = {
	constructor: View1,

	OnLoad: function () {
		AppContext.ShowInfo("View1 OnLoad");
	},

	Save: function () {
		AppContext.ShowSuccess("Saved successfully!");
	}
};
