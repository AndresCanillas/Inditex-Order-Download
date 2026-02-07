var View2 = function () {
	FormView.Extend(this, "Employee Details", "/View1.html");
};


View2.prototype = {
	constructor: View2,

	OnLoad: function () {
		AppContext.ShowInfo("View2 OnLoad");
	},

	Save: function () {
		AppContext.ShowSuccess("Saved successfully!");
	}
};
