//# sourceURL=https://HomeView.js

//<script>
var HomeView = function () {
	FormView.Extend(this, "Orders", "/FrameworkTest/View3.html");
};


HomeView.prototype = {
	constructor: HomeView,

	OnLoad: async function () {
		console.log("View3 Load")
	},

	OnUnload: function () {
		console.log("View3 Unload");
	}
};
//</script>