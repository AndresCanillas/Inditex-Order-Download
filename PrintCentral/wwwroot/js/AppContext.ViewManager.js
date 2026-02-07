AppContext.ViewManager = (function () {
	var loadedViews = {};
	var historyEntries = [];

	window.history.pushState(historyEntries, null);

	window.onpopstate = function (e) {
		console.log("onpopstate");
		if (historyEntries.length > 1) {
			historyEntries.pop();
			var entry = historyEntries[historyEntries.length - 1];
			var viewData = loadedViews[entry];
			window.history.pushState(historyEntries, viewData.viewInstance.Title);
			loadView(viewData, true);
		}
		else {
			this.document.location = "/";
		}
	}

	function loadView(viewData, ispop) {
		if (!ispop)
			historyEntries.push(viewData.viewUrl);
		window.history.replaceState(historyEntries, viewData.viewInstance.Title);
		viewData.viewInstance.Show(viewData.targetElement);
	}

	return {
		Load: function (targetElement, viewUrl, ...constructorArgs) {
			if (!(targetElement instanceof jQuery))
				throw "targetElement must be a jQuery object";
			return AppContext.LoadJS(viewUrl).then(() => {
				var objName = AppContext.GetCtorFromUrl(viewUrl);
				var viewData = loadedViews[viewUrl];
				if (viewData == null) {
					let viewInstance = new window[objName](constructorArgs);
					viewData = { viewUrl, targetElement, viewInstance }
					loadedViews[viewUrl] = viewData;
				}
				loadView(viewData, false);
				return viewData.viewInstance;
			});
		}
	};
})();