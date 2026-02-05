AppContext.ViewManager = (function () {
	var ispop;
	var loadedViews = {};
	var historyEntries = [];

	window.history.pushState(historyEntries, null);

	window.onpopstate = function (e) {
		console.log("onpopstate");
		ispop = true;
		if (historyEntries.length > 0) {
			var entry = historyEntries.pop();
			var viewData = loadedViews[entry];
			window.history.pushState(historyEntries, viewData.viewInstance.Title);
			if (viewData.viewInstance.isDialog) {
				viewData.viewInstance.Hide();
			}
			else {
				var currentView = viewData.viewInstance.ViewContainer[0].CurrentView;
				if (currentView != null)
					currentView.Hide();
				viewData.viewInstance.Show(viewData.viewInstance.ViewContainer);
			}
		}
		else {
			this.document.location = "/";
		}
		ispop = false;
	}

	function addHistoryTrace(viewData) {
		if (ispop || viewData.viewInstance.isDialog)
			return;
		if (historyEntries.length > 0 && historyEntries[historyEntries.length - 1] == viewData.viewUrl)
			return;
		if (historyEntries.length > 9)
			historyEntries.pop();
		historyEntries.push(viewData.viewUrl);
		window.history.replaceState(historyEntries, viewData.viewInstance.Title);
	}

	return {
		Load: async function (viewUrl, ...constructorArgs) {
			await AppContext.LoadJS(viewUrl);
			var objName = AppContext.GetCtorFromUrl(viewUrl);
			var viewData = loadedViews[viewUrl];
			if (viewData == null) {
				let viewInstance = new window[objName](constructorArgs);
				viewData = { viewUrl, viewInstance }
				loadedViews[viewUrl] = viewData;
			}
			viewData.viewInstance.OnHidden.Subscribe(() => { addHistoryTrace(viewData); })
			await viewData.viewInstance.Load();
			return viewData.viewInstance;
		}
	};
})();