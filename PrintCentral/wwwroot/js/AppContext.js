// =========================================================================
// AppContext
// =========================================================================
// #region AppContext

var AppContext = (function () {
	var windowUrl = window.location.href;
	var tokens = windowUrl.split("/");
	var baseUrl = tokens[0] + "//" + tokens[2];

	var settings = {
		enableCache : false,
		timeMark : null,
	};

	parseController = function (url) {
		var idx1 = url.lastIndexOf("/") + 1;
		var idx2 = url.lastIndexOf(".js");
		var controllerType = url.substring(idx1, idx2);
		return controllerType;
	};

	var allScripts = {};

	return {

		SetSettings: function (newSettings) {

			settings = $.extend(settings, newSettings);

        },

        GetSettings: function () {
            return $({}, settings);// clone settings
        },

		AjaxScriptSettings: function (url) {
			var getScriptSettings = {
				cache: false,
				url: url,
				dataType: "script"
			};

			// fixed cache for scripts
			if (settings.enableCache && settings.timeMark) {

				var newUrl = '';

				try {

					if (url[0] != '/') {
						url = '/' + url;
					}

					var parsedUrl = new URL(baseUrl + url);

					parsedUrl.searchParams.delete('_');
					parsedUrl.searchParams.append('_', settings.timeMark);
					newUrl = parsedUrl.toString();

				} catch {
					// bad url
				}

				getScriptSettings = $.extend(getScriptSettings, {
					cache: true,
					url: newUrl
				});

			}

			return getScriptSettings;
		},

		LoadJS: function () {
			if (arguments.length < 1)
				throw "Was expecting at least one URL and a callback!";
			var downloads = [];
			var filesToLoad = arguments;
			if (filesToLoad[0] instanceof Array)
				filesToLoad = filesToLoad[0];
			var totalScripts = filesToLoad.length;
			return new Promise((resolve, reject) => {
				for (var i = 0; i < totalScripts; i++) {
					if (typeof filesToLoad[i] !== "string")
						throw "Invalid URL, was expecting a string!";
					var url = filesToLoad[i];
					if (!allScripts[url]) {

						var getScriptSettings = AppContext.AjaxScriptSettings(url);
						
						$.ajax(getScriptSettings).done(function () {
							allScripts[url] = true;
							downloads.push(url);
							if (downloads.length === totalScripts)
								resolve(null);
						}).fail((rq) => {
							AppContext.ShowError("Error loading content from server, try again later.");
							reject(rq);
							return false;
						});
					}
					else {
						downloads.push(url);
					}
				}
				if (downloads.length === totalScripts) resolve(null);
			});
		},

		GetToastContainer: function () {
			var elm = $(".ToastContainer");
			if (elm.length > 0)
				return elm;
			else {
				elm = $("<div class='ToastContainer'></div>");
				$("BODY").append(elm);
				return elm;
			}
		},

		ShowToast: function (html, timeout) {
			var elm = AppContext.GetToastContainer();
			elm.append(html);
			html.fadeIn("fast")
				.delay(timeout ? timeout : 5000)
				.fadeOut({
					duration: 500,
					complete: function () {
						html.remove();
					}
				});
		},

		ShowError: function (message, timeout) {
			var html = $(`<div class="Toast ToastError">
				<div class="ToastIcon"><span class="k-icon k-i-error" style="font: 30px/1 WebComponentsIcons;"></span></div>
				<div class="ToastMessage">${message}</div></div>`);
			AppContext.ShowToast(html, timeout);
		},

		ShowSuccess: function (message, timeout) {
			var html = $(`<div class="Toast ToastSuccess">
				<div class="ToastIcon"><span class="k-icon k-i-success" style="font: 30px/1 WebComponentsIcons;"></span></div>
				<div class="ToastMessage">${message}</div></div>`);
			AppContext.ShowToast(html, timeout);
		},

		ShowInfo: function (message, timeout) {
			var html = $(`<div class="Toast ToastInfo">
				<div class="ToastIcon"><span class="k-icon k-i-info" style="font: 30px/1 WebComponentsIcons;"></span></div>
				<div class="ToastMessage">${message}</div></div>`);
			AppContext.ShowToast(html, timeout);
        },

        ShowWarning: function (message, timeout) {
            var html = $(`<div class="Toast ToastWarning">
				<div class="ToastIcon"><span class="k-icon k-i-warning" style="font: 30px/1 WebComponentsIcons;"></span></div>
				<div class="ToastMessage">${message}</div></div>`);
            AppContext.ShowToast(html, timeout);
		},

		ShowProcessing: function () {
			var overlay = null;
			overlay = $('<div id="overlayWrapper"><div id="overlay"></div><div id="overlaymessage">Processing...</div></div>');
			overlay.appendTo(document.body)
		},

		HideProcessing: function () {
			$('#overlayWrapper').remove();
		},

		LoadComboBox: function (element, data, valueField, textField, anyOption) {
			var opValue, opText;
			var combo = AppContext.GetContainerElement(element);
			combo.empty();
			if (anyOption != null)
				combo.append($('<option>', { value: "0", text: anyOption }));
			$.each(data, function (i, item) {
				opValue = valueField ? item[valueField] : item.ID ? item.ID : item.Value ? item.Value : 0;
				opText = textField ? item[textField] : item.Name ? item.Name : item.Text ? item.Text : "";
				combo.append($('<option>', {
					value: opValue,
					text: opText
				}));
				if (i === 0) {
					combo.val(opValue);
					combo.change();
				}
			});
		},

		Authenticate: function (user, password) {
			return new Promise(function (resolve, reject) {
				try {
					var rq = new XMLHttpRequest();
					rq.onload = (r) => {
						if (rq.status == 200)
							resolve();
						else
							reject();
					};
					rq.onerror = reject;
					rq.open("POST", "/auth");
					rq.setRequestHeader('Authorization', `Basic ${btoa(user + ":" + password)}`);
					rq.send();
				} catch (err) {
					alert(err);
				}
			});
		},

		// return promise
		HttpGet: function (url, callback, errorhandler, showOverlay)  {
			var overlay = null;
			if (showOverlay) {
				overlay = $('<div><div id="overlay"></div><div id="overlaymessage">Processing...</div></div>');
				overlay.appendTo(document.body)
			}
			return $.ajax({
				type: "GET",
				url: url,
				success: function (data) {
					if (overlay != null) overlay.remove();
					if (callback != null) callback(data);
				},
				error: function (data) {
					if (overlay != null) overlay.remove();
					if (errorhandler != null) errorhandler(data);
					else AppContext.ShowError(data.statusText);
				}
			});
		},
        // return promise
		HttpPost: function (url, data, callback, errorhandler, showOverlay, showSuccessMessage) {
			var overlay = null;
			if (showOverlay) {
				overlay = $('<div><div id="overlay"></div><div id="overlaymessage">Processing...</div></div>');
				overlay.appendTo(document.body)
			}
			if (showSuccessMessage == null) showSuccessMessage = true;
			return $.ajax({
				type: "POST",
				url: url,
				contentType: "application/json; charset=utf-8",
				dataType: "json",
				data: data ? JSON.stringify(data) : null,
				success: function (data, status, rq) {
					if (overlay != null)
						overlay.remove();
					if (data)
					{
						var msg = data.Message ? data.Message : "";
						if (msg != "") {
							if (!data.Success)
								AppContext.ShowError(msg);
							else if (showSuccessMessage)
								AppContext.ShowSuccess(msg);
						}
						if (callback) callback(data);
					}
					else
					{
						if (callback) callback();
					}
				},
				error: errorhandler ? errorhandler : function (rq, status, error) {
					if (overlay != null)
						overlay.remove();
					var msg = rq && rq.Message ? rq.Message : rq.statusText;
					AppContext.ShowError(msg);
					if (errorhandler) errorhandler(rq, status, error);
				},
				complete: function (rq, st) {
					if (overlay != null)
						overlay.remove();
				}
			});
		},

		BindModel: function (container, controller) {
			var viewContainer = AppContext.GetContainerElement(container);
			var model = new ViewDataModel();
			model.Bind(viewContainer, controller);
			return model.GetProxy();
		},

		BindActions: function (container, controller) {
			var viewContainer = AppContext.GetContainerElement(container);
			if (viewContainer.attr("action") != null) {
				item = viewContainer[0];
				if (this.SupportedNodeType(item.nodeName)) {
					var actions = item.attributes["action"].value;
					AppContext.SetElementActions(viewContainer, actions, controller);
				}
			}
			$.each(viewContainer.find("[action]"), function (index, item) {
				if (item.attributes["action"] != null) {
					if (AppContext.SupportedNodeType(item.nodeName)) {
						var actions = item.attributes["action"].value;
						AppContext.SetElementActions($(item), actions, controller);
					}
				}
			});
		},

		BindElements: function (container, controller) {
			var elements = { };
			var viewContainer = AppContext.GetContainerElement(container);
			var pendingFormViewComponents = 0;

			return new Promise((resolve, reject) => {
				var componentsWithController = viewContainer.find("[component][controller]");
				if (componentsWithController.length > 0) {
					var filesToLoad = [];
					$.each(componentsWithController, (index, item) => {
						var componentController = item.attributes["controller"].value;
						filesToLoad.push(componentController);
					});
					AppContext.LoadJS(filesToLoad).then(() => {
						LoadComponents(resolve);
						if (pendingFormViewComponents == 0)
							resolve(elements);
					}).catch(()=>reject());
				}
				else {
					LoadComponents(resolve);
					if (pendingFormViewComponents == 0)
						resolve(elements);
				}
			});

			function LoadComponents(resolve) {
				var components = viewContainer.find("[component], [name]");
				$.each(components, function (index, item) {
					if (item.attributes["component"] != null) {
						var ctype = item.attributes["component"].value;
						var cname = "C" + index;
						if (item.attributes["name"] != null)
							cname = item.attributes["name"].value
						CreateComponent(item, ctype, cname, resolve);
					}
					else if (item.attributes["name"] != null) {
						var elementName = item.attributes["name"].value;
						elements[elementName] = $(item);
					}
				});
			}

			function CreateComponent(item, ctype, cname, resolve) {
				switch (ctype) {
					case "jQuery":
						var component = $(item);
						elements[cname] = component;
						break;
					case "FormView":
						var componentController = item.attributes["controller"].value;
						var objectName = AppContext.GetCtorFromUrl(componentController);
						var compArgs = null;
						if (item.attributes["args"] != null)
							compArgs = JSON.parse(item.attributes["args"].value);
						var component = new window[objectName](compArgs);
						component.IsEmbedded = true;
						elements[cname] = component;
						ShowFormViewComponent(component, item, resolve);
						break;
					default:
						var component = new window[ctype]($(item), controller);
						elements[cname] = component;
						if (component.constructor.name == "FormView")
							ShowFormViewComponent(component, item, resolve)
						break;
				}
			}

			function ShowFormViewComponent(component, item, resolve) {
				pendingFormViewComponents++;
				component.OnLoaded.Subscribe(() => {
					pendingFormViewComponents--;
					if (pendingFormViewComponents == 0)
						resolve(elements);
				});
				component.Show($(item));
			}
		},

		BindView: function (container, controller) {
			return new Promise((resolve) => {
				controller.model = AppContext.BindModel(container, controller);
				AppContext.BindActions(container, controller);
				AppContext.BindElements(container, controller).then((elements) => {
					controller.elements = elements;
					resolve();
				});
			});
		},

		SupportedNodeType: function (nodeType) {
			var str = "," + nodeType.toLowerCase() + ",";
			if (",menu,menuitem,".indexOf(str) >= 0)
				return false;
			return true;
		},

		IsDataInput: function (nodeType) {
			if ("input,textarea,select".indexOf(nodeType.toLowerCase()) >= 0)
				return true;
			return false;
		},

		SetElementActions: function (element, actions, controller) {
			var tokens = actions.split(";");
			for (var i = 0; i < tokens.length; i++) {
				var methodName = tokens[i].trim();
				var eventName = "click";
				var idx = methodName.indexOf(":");
				if (idx > 0) {
					var str = methodName;
					eventName = str.substring(0, idx);
					methodName = str.substring(idx + 1, str.length);
				}
				AppContext.SetElementAction(element, eventName, methodName, controller);
			}
		},

		SetElementAction: function (element, eventName, methodName, controller) {
			element.on(eventName, function (e) {
				var disabled = element.prop("disabled") || (element.attr("data-disabled") != null && element.attr("data-disabled") == "true");
				if (disabled) {
					e.preventDefault();
				}
				else {
					e.stopPropagation();
					controller[methodName](e);
				}
			});
		},

		GetContainerElement: function (container) {
			if (!container)
				throw "Need to provide a valid container";
			if (typeof container === "string")
				return $("#" + container);
			else if (container instanceof jQuery)
				return container;
			else
				throw "Argument container is invalid";
		},

		GetCtorFromUrl: function (controllerUrl) {
			var extidx = controllerUrl.indexOf(".js");
			if (extidx < 0)
				throw "The controller URL must be a .js file";
			var pathidx = controllerUrl.lastIndexOf("/");
			if (pathidx >= 0)
				return controllerUrl.substring(pathidx + 1, extidx);
			else
				return controllerUrl.substring(0, extidx);
        },

        EmptyFn: function () { }
	};
})();
// #endregion



// =========================================================================
// ViewDataModel
// =========================================================================
// #region ViewDataModel
var ViewDataModel = function () {
	this.data = {};
	this.CustomValidation = new AppEvent();
};

ViewDataModel.Extend = function (target, viewContainer, controller) {
	ViewDataModel.call(target, viewContainer, controller);
	ExtendObj(target, ViewDataModel.prototype);
};

ViewDataModel.prototype = {
	constructor: ViewDataModel,

	Bind: function (viewContainer, controller) {
		this.ViewContainer = viewContainer;
		this.Controller = controller;
		this.ModelElements = [];
		var self = this;
		$.each(viewContainer.find("[options]"), function (index, item) {
			var options = JSON.parse(item.attributes["options"].value);
			self.SetElementOptions(item, options);
		});
		$.each(viewContainer.find("[model]"), function (index, item) {
			if (item.attributes["name"] == null)
				throw "Elements marked with the model attribute must have a name";
			self.ModelElements.push(item);
			var fieldName = item.attributes["name"].value;
			self.data[fieldName] = self.GetFieldValue(item);
			self.SetElementEvents(item, fieldName);
		});
		this.ForEachValidation(self.SetElementRestrictions);
	},

	GetData: function () {
		var result = {};
		for (var elm of this.ModelElements) {
			var fieldName = elm.attributes["name"].value;
			result[fieldName] = this.GetFieldValue(elm);
		}
		return result;
	},

    SetElementEvents: function (element, field) {
        var self = this;
        var eType = element.nodeName.toLowerCase();
        switch (eType) {
            case "input":
            case "textarea":
                if (element.type === "checkbox") {
                    $(element).change(function () {
                        self.data[field] = element.checked;
                    });
				}
				else if (this._IsDateField(element)) {
					var elmType = element.attributes["type"].value.toLowerCase();
					if (elmType != "hidden") {
						$(element).change(function () {
							self.data[field] = $(element).data("kendoDatePicker").value();
						});
					}
				}
				else if (element.type == "hidden") {
					$(element).change(function () {
						self.data[field] = self.GetFieldValue(element);
					});
				}
                else {
                    $(element).keyup(function () {
						self.data[field] = self.GetFieldValue(element);
                    });
                }
                break;
            case "select":
                $(element).change(function () {
                    self.data[field] = element.value;
                });
                break;
        }
    },

	SetElementOptions: function (element, options) {
		var optionElm, opt, selectedOp;
		var selectElm = $(element);
		if (options.length > 0) {
			for (var i = 0; i < options.length; i++) {
				opt = options[i];
				optionElm = new Option(opt.Name, opt.Value);
				optionElm.innerHTML = opt.Name;
				selectElm.append(optionElm);
				if (opt.Selected)
					selectedOp = opt.Value;
			}
		}
		if (selectedOp)
			selectElm.val(selectedOp);
	},

	ForEachValidation: function (callback) {
		var self = this;
		$.each(this.ModelElements, function (index, item) {
			if (item.attributes["validation"] != null) {
				var validations = item.attributes["validation"].value;
				if (validations && validations.length > 0) {
					var tokens = validations.split(";");
					for (var i = 0; i < tokens.length; i++) {
						tokens[i] = tokens[i].trim();
						var validation = tokens[i];
						var validationArguments = null;
                        var idx1 = validation.indexOf("(");
                        var idx2 = validation.lastIndexOf(")");// search backward  for faster operation
						while (idx2 > idx1 && idx2 < validation.length && validation[idx2 - 1] == "\\") {
							idx2 = validation.indexOf(")", idx2 + 1);
						}
						if (idx1 > 0 && idx2 > idx1) {
							validation = tokens[i].substring(0, idx1);
							validationArguments = tokens[i].substring(idx1 + 1, idx2).replace("\\\\", "\\").replace("\\)", ")");
						}
						callback.call(self, item, validation, validationArguments);
					}
				}
			}
		});
	},

    SetElementRestrictions: function (element, validation, validationArguments) {
        // ???: can combine validators ?
		var jq = $(element);
		switch (validation) {
			case "numeric":
				jq.keydown(function (e) {
					var key = e.keyCode ? e.keyCode : e.which;
					if (!([8, 9, 13, 27, 46, 110, 190, 189, 109].indexOf(key) !== -1 ||
						(key === 65 && (e.ctrlKey || e.metaKey)) ||
						(key === 67 && (e.ctrlKey || e.metaKey)) ||
						(key === 86 && (e.ctrlKey || e.metaKey)) ||
						(key >= 35 && key <= 40) ||
						(key >= 48 && key <= 57 && !(e.shiftKey || e.altKey)) ||
						(key >= 96 && key <= 105)
                    )) e.preventDefault();

                    // TODO: check to try change for regex expression to avoid check all KEYTYPE
                    // and separete into integer validator and float validator
                    // int: \d*, float: \d*(\,\d{1,6}) -> assume "," is decimal separator with 6 positions for precision
                    // try to find a solution to format floats
				});
				break;
			case "maxlen":
				jq.attr("maxlength", validationArguments);
				break;
			case "chars":
				jq.keypress(function (e) {
					var key = e.keyCode ? e.keyCode : e.which;
					var char = String.fromCharCode(key)
					if (validationArguments.indexOf(char) < 0)
						e.preventDefault();
				});
                break;

    //        case "regex":
    //            jq.keypress(function (e) {
				//	// XXX: bug if text is selected, selected text could be replaced
    //                var key = e.keyCode ? e.keyCode : e.which;

    //                var char = String.fromCharCode(key);

				//	var currentVal = jq.val();

				//	if ()

				//	var valAdden = char;

    //                var rgx = new RegExp(validationArguments);

    //                if (rgx.test(currentVal) == false) {
    //                    e.preventDefault();
    //                }

    //            });
				//break;

			//case "range":

			//	jq.keypress(function (e) {

			//		var params = validationArguments.split(',');

			//		var key = e.keyCode ? e.keyCode : e.which;

			//		var char = String.fromCharCode(key);

			//		var min = parseFloat(params[0])
			//		var max = parseFloat(params[1])
			//		var inclusive = true;
			//		if (params[2] != undefined) {
			//			inclusive = params[2].toLowerCase() == 'true' || params[2] == '1';
			//		}

			//		var currentVal = jq.val() + char;

			//		if (/-?\d+/.test(currentVal) == false) {
			//			return;
			//		}

			//		var numericVal = parseFloat(currentVal);


			//		if (inclusive) {
			//			if (numericVal < min || numericVal > max) {
			//				e.preventDefault();
			//			}
			//		} else {
			//			if (numericVal <= min || numericVal >= max) {
			//				e.preventDefault();
			//			}
			//		}

			//	});

			//	break;
		}
	},

	GetProxy: function () {
		return new Proxy(this, this.ProxyHandler);
	},

	ProxyHandler: {
		get: function (target, prop, receiver) {
			var value = target.data[prop];
			if (value == null)
				return target[prop];
			return value;
		},
		set: function (target, prop, value) {
			target.data[prop] = value;
			$.each(target.ViewContainer.find("[model][name='" + prop + "']"), function (index, item) {
				target.SetFieldValue(item, value);
			});
		},
		has: function (target, key) {
			return target.data.hasOwnProperty(key);
		}
	},

	GetFieldValue: function (element) {
		if (typeof element === "string") {
			var elm = this.ViewContainer.find(`[model][name='${element}']`);
			if (elm.length > 0)
				element = elm[0];
			else
				throw `Could not find element ${element}`;
		}
		var eType = element.nodeName.toLowerCase();
		var value = "";
		switch (eType) {
			case "input":
				if (element.type === "checkbox")
					value = element.checked;
				else
					value = element.value;
				break;
			case "textarea":
			case "select":
				value = element.value;
				break;
			case "div":
			case "span":
				value = element.innerText;
				break;
			case "pre":
				value = element.innerHTML;
				break;
		}
		if (value != null)
		{
			if (element.attributes["data-type"])
				value = this.ApplyDataTypeAttribute(value, element);
			if (element.attributes["data-map"])
				value = this.ApplyDataMapAttribute(value, element);
		}
		return value;
	},

	ApplyDataTypeAttribute: function (value, element) {
        if ((typeof element !== "object") || (typeof element === "object" && element.hasOwnProperty("attributes") && !element.attributes["data-type"]))
			return value;
		var type = element.attributes["data-type"].value.toLowerCase();
		switch (type) {
			case "date":
				if (!value)
					value = new Date();
				if (String.isString(value) && value.length > 0) {
					var jqe = $(element);
					if (jqe.data && jqe.data("kendoDatePicker")) {
						var kdpVal = jqe.data("kendoDatePicker").value();
						if (kdpVal)
							value = kdpVal;
						else
							value = new Date(value);
					}
					else value = new Date(value);
				}
				else if (!Date.isDate(value)) {
					value = new Date();
				}
				break;
			case "time":
				if (value)
					value = new Date(value).getTime();
				break;
			case "int":
			case "image":
			case "file":
				value = parseInt(value, 10);
				if (isNaN(value)) value = 0;
				break;
			case "decimal":
				value = parseFloat(value);
				if (isNaN(value)) value = 0;
				break;
			case "bool":
				value = (value != null && (value === true || value === 1 || value === "1" || value === "true" || value === "True"));
				break;
		}
		return value;
	},

	ApplyDataMapAttribute: function (value, element) {
        if ((typeof element !== "object") || (typeof element === "object" && element.hasOwnProperty("attributes") && !element.attributes["data-map"]))
			return value;
		var map = element.attributes["data-map"].value;
		var mapping = JSON.parse(map);
		if (mapping.hasOwnProperty(value))
			return mapping[value];
		else
			return value;
	},

	SetFieldValue: function(element, value) {
		var eType = element.nodeName.toLowerCase();
		if (value) {
			if (element.attributes["data-type"])
				value = this.ApplyDataTypeAttribute(value, element);
			if (element.attributes["data-map"])
				value = this.ApplyDataMapAttribute(value, element);
		}
		switch (eType) {
			case "input":
				if (element.type === "checkbox")
					element.checked = value;
				else if (this._IsDateField(element)) {
					if (!(value instanceof Date))
						value = new Date(value);
					var elmType = element.attributes["type"].value.toLowerCase();
					if (elmType == "hidden")
						element.value = value;
					else
						$(element).data("kendoDatePicker").value(value);
				}
				else if (element.attributes["data-type"] != null && element.attributes["data-type"].value.toLowerCase() == "image") {
					var dt = new Date().toTicks();
					var imgElm = $(element.parentNode).find(".ThumbImage2");
					if (this.Controller.GetImageDownloadUrl != null) {
						var imageDownloadUrl = this.Controller.GetImageDownloadUrl(element.attributes["Name"].value, value)
						imgElm.attr("src", `${imageDownloadUrl}?${dt}`);
					}
					element.value = value;
				}
				else if (element.attributes["data-type"] != null && element.attributes["data-type"].value.toLowerCase() == "file") {
					var fieldName = element.attributes["Name"].value;
					var downloadLink = $(element.parentNode).find(`[name='_${fieldName}_DownloadLink']`);
					if (this.Controller.GetFileDownloadUrl != null) {
						var fileDownloadUrl = this.Controller.GetFileDownloadUrl(fieldName, value)
						downloadLink.prop("href", fileDownloadUrl);
					}
					element.value = value;
				}
				else element.value = value;
				break;
			case "textarea":
			case "select":
				element.value = value;
				break;
			case "div":
			case "span":
				element.innerText = value;
				break;
			case "pre":
				element.innerHTML = value;
				break;
		}
		$(element).change();
	},

	_IsDateField: function (element) {
		return (element.attributes["data-type"] && element.attributes["data-type"].value.toLowerCase() === "date") ||
			(element.attributes["type"] && element.attributes["type"].value === "date");
	},

	SetError: function (field, error) {
		var elm = this.ViewContainer.find(`[model][name='${field}']`);
		this.SetElementError(elm, error);
	},

	SetElementError: function (elm, error) {
		if (elm[0].ErrorElm)
			elm[0].ErrorElm.innerText = error;
		else {
			var errorIcon = $('<div class="form-error" title="${error}"><span class="fa fa-exclamation-triangle"></span></div>');
			var errorElm = $(`<span class="form-error-text">${error}</span>`);
			errorIcon.append(errorElm);
			elm[0].ErrorIcon = errorIcon;
			elm[0].ErrorElm = errorElm;
			elm.parent().append(errorIcon);
		}
	},

	ClearError: function (field) {
		var elm = this.ViewContainer.find(`[model][name='${field}']`);
		this.ClearElementError(elm);
	},

	ClearElementError: function (elm) {
		if (elm[0].ErrorElm) {
			elm[0].ErrorElm.remove();
			elm[0].ErrorIcon.remove();
			elm[0].ErrorElm = null;
			elm[0].ErrorIcon = null;
		}
	},

	ClearErrors: function () {
		var elements = this.ViewContainer.find("[model]");
		for (var i = 0; i < elements.length; i++) {
			var elm = elements[i];
			if (elm.ErrorElm) {
				elm.ErrorElm.remove();
				elm.ErrorIcon.remove();
				elm.ErrorElm = null;
				elm.ErrorIcon = null;
			}
		}
	},

	ValidState: function () {
		var result = true;
		this.ClearErrors();
		this.ForEachValidation(function (element, validation, validationArguments) {
			var value = this.GetFieldValue(element);
            var jq = $(element);
            var errorMessages = jq.attr("error-messages")

            if (errorMessages)
                errorMessages = JSON.parse(errorMessages);// valiator names is a key for a messages
            else
                errorMessages = {};

			if (jq.is(":hidden") || jq.prop("disabled"))
				return;
			switch (validation) {
				case "required":
					if (value == null || (value.toString().length == 0)) {
						this.SetElementError(jq, "Field cannot be empty.");
						result = false;
					}
					break;
				case "email":
					if (value && value.length > 0 && !(/^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,})+$/.test(value))) {
						this.SetElementError(jq, "Is not a valid email.");
						result = false;
					}
					break;
				case "chars":
					if (value && value.length > 0) {
						for (var i = 0; i < value.length; i++) {
							if (validationArguments.indexOf(value[i]) < 0) {
								this.SetElementError(jq, "Field contains invalid characters.");
								result = false;
								break;
							}
						}
					}
					break;
				case "range":
					var params = validationArguments.split(',');
					var min = parseFloat(params[0])
					var max = parseFloat(params[1])
					var inclusive = true;
					var errMsg = undefined;

					if (params[2] != undefined) {
						inclusive = params[2].toLowerCase() == 'true' || params[2] == '1';
					}

					if (params[3] != undefined) {
						errMsg = params[3];
					}

					if (/-?\d+/.test(value) == false) {
						this.SetElementError(jq, "Invalid Value.");
						result = false;
					}

					var numericVal = parseFloat(value)

                    var defaultRangeErrorMessage = `Value is out of range. Min: ${min} - Max: ${max}`;
                    var _msg = errorMessages.range ? errorMessages.range : defaultRangeErrorMessage;

					if (inclusive) {
						if (numericVal < min || numericVal > max) {
							
							if (errMsg != undefined) _msg = errMsg;

							this.SetElementError(jq, _msg);
							result = false;
						}
					} else {
						if (numericVal <= min || numericVal >= max) {
							if (errMsg != undefined) _msg = errMsg;
							this.SetElementError(jq, errMsg);
							result = false;
						}
					}
				
					break;

				case "regex":

                    var rgx = new RegExp(validationArguments);
                    var defaultRegexErrorMessage = "Value does not match the expected format";

                    if (rgx.test(value) === false) {
                        this.SetElementError(jq, errorMessages.regex ? errorMessages.regex : defaultRegexErrorMessage);
						result = false
					}

					break;

			}
		});
		if (result)
			this.ClearErrors();
		var cancelled = false;
		var e = {
			target: this,
			preventDefault: function() { cancelled = true; },
			stopPropagation: function() { }
		};
		this.CustomValidation.Raise(e);
		if (cancelled)
			return false;
		else
			return result;
	}
};
// #endregion




// =========================================================================
// FormView
// =========================================================================
// #region FormView
var FormView = function (title, viewUrl) {
	this.Title = title;
	this.ViewUrl = viewUrl;
	this.Loaded = false;
	this.BeforeGetData = new AppEvent();
	this.AfterLoadData = new AppEvent();
	this.OnSaved = new AppEvent();
	this.OnLoaded = new AppEvent();
	this.OnUnloaded = new AppEvent();
};


FormView.Extend = function (target, title, viewUrl) {
	if (title == null || title.length === 0)
		title = "Title";
	FormView.call(target, title, viewUrl);
	ExtendObj(target, FormView.prototype);
};

FormView.UpdateMainTitle = function (title) {
	$(".main-panel-header").html(title);
};


FormView.prototype = {
	constructor: FormView,

	Show: function (viewContainer) {
		if (viewContainer == null)
			throw "Invalid viewContainer";
		this.ViewContainer = AppContext.GetContainerElement(viewContainer);
		if (this.ViewContainer[0].CurrentView != null && this.ViewContainer[0].CurrentView !== this) {
			this.ViewContainer[0].CurrentView.Unload();
			this.ViewContainer[0].CurrentView = null;
		}
		var self = this;
		if (this.ViewUrl != null) {
			$.ajax({
				type: "GET",
				url: this.ViewUrl,
				success: function (html) {
					self.Content = html;
					loadContent();
				},
				error: function (data) {
					AppContext.ShowError("Error Loading View");
				},
				fail: function (data) {
					AppContext.ShowError("Error Loading View");
				}
			});
		}
		else {
			loadContent(this.Content);
		}

		function loadContent() {
			self.ViewContainer.empty();
			self.ViewContainer.append(self.Content);
			self.TransformView(self.ViewContainer);
			AppContext.BindView(self.ViewContainer, self).then(() => {
				if (!self.IsEmbedded) {
					if (self.Title != "Title")
						$(".main-panel-header").html(self.Title);
					self.ViewContainer.find("input, select, textarea").first().focus();
				}
				self.ViewContainer[0].CurrentView = self;
				self.Loaded = true;
				if (self.OnLoad != null) self.OnLoad();
				self.OnLoaded.Raise(self);
			});
		}
	},

	ShowDialog: function (callback, width) {
		if (callback != null && typeof callback != "function")
			throw "ShowDialog was supplied an invalid callback!!";
		this.dlgCallback = callback;
		this.width = width;
		this.dialog = true;
		var self = this;
		if (this.ViewUrl != null) {
			$.ajax({
				type: "GET",
				url: this.ViewUrl,
				success: function (html) {
					self.Content = html;
					loadContent();
				},
				error: function (data) {
					AppContext.ShowError("Error Loading View");
				}
			});
		}
		else {
			loadContent(this.Content);
		}

		function loadContent() {
			self.dialogElm = self.CreateDialog(self.Content);
			self.ViewContainer = self.dialogElm;
			$("BODY").append(self.dialogElm);
			self.TransformView(self.dialogElm);
			AppContext.BindView(self.ViewContainer, self).then(() => {
				self.ViewContainer.find("input, select, textarea, datalist").first().focus();
				self.Loaded = true;
				if (self.OnLoad) self.OnLoad();
				self.OnLoaded.Raise(self);
			});
		}
	},

	Unload: function () {
		if (this.OnUnload != null)
			this.OnUnload();
		this.OnUnloaded.Raise();
		this.Loaded = false;
		this.ViewContainer.empty();
		this.ViewContainer[0].CurrentView = null;
		this.OnSaved.Clear();
		this.OnUnloaded.Clear();
		this.Content = null;
		this.model = null;
		this.elements = null;
		this.ViewContainer = null;
		if (!this.IsEmbedded && !this.dialog)
			$(".main-panel-header").html("&nbsp;");
	},

	CreateDialog: function (content) {
		var dlg = $('<div class="modal-form">');
		var dialogDoc = $('<div class="modal-form-content container-fluid" style="max-height:80%;" action="keyup:HandleKeyUp">');
		if (this.width)
			dialogDoc.css("width", this.width);
		dlg.append(dialogDoc);
		var dialogCard = $("<div class='card'></div>");
		dialogDoc.append(dialogCard);
		var closeBtn = $('<span class="modal-form-close" action="Cancel">&times;</span>');
		dialogCard.append(closeBtn);
		var dialogTitle = $(`<div class="modal-form-title">${this.Title}</div>`);
		dialogCard.append(dialogTitle);
		var cardBody = $("<div class='card-body' style='max-height:75vh; overflow:auto;'></div>");
		cardBody.append(content);
		dialogCard.append(cardBody);
		return dlg;
	},

	TransformView: function (container) {
		if (this.dialog)
			this.RemoveTopViewCard(container);
		else
			this.TransformElement(container, "viewcard", this.TransformViewCardElement);
		this.TransformElement(container, "viewrow", this.TransformViewRowElement);
		this.TransformElement(container, "viewtitle", this.TransformViewTitleElement);
		this.TransformElement(container, "viewform", this.TransformViewFormElement);
		this.TransformElement(container, "inlineform", this.TransformInlineFormElement);
		this.TransformElement(container, "viewfooter", this.TransformViewFooterElement);
	},

	RemoveTopViewCard(container) {
		var card = container.findFirst("viewcard");
		if (card.length > 0) {
			var cardContainer = card.parent();
			var sibling = card.next();
			card.remove();
			var html = $($(card[0]).html());
			if (sibling.length != 0) { sibling.before(html); }
			else if (cardContainer.length != 0) { cardContainer.append(html); }
			else container.append(html);
		}
	},

	TransformElement: function (container, tagName, fn) {
		var element;
		do {
			element = container.findFirst(tagName);
			if (element.length != 0) {
				var topContainer = element.parent();
				var sibling = element.next();
				element.remove();

				var transformedElm = fn.call(this, element);

				if (sibling.length != 0) { sibling.before(transformedElm); }
				else if (topContainer.length != 0) { topContainer.append(transformedElm); }
			}
		} while (element.length != 0);
	},

	TransformViewCardElement: function (item) {
		var card = $("<div class='card'></div>");
		var cardbody = $("<div class='card-body container-fluid'></div>");
		card.append(cardbody);
		this.CopyAttributes(cardbody, item);
		cardbody.append(item.html());
		return card;
	},

	TransformViewRowElement: function (row) {
		var self = this;
		var rowdiv = $("<div class='row'></div>");
		this.CopyAttributes(rowdiv, row);
		rowdiv.append(row.html());
		var rowColumns = rowdiv.children().findTags("viewcolumn");
		var colcount = rowColumns.length;
		if (colcount > 0) {
			var colSize = parseInt(12 / colcount, 10);
			if (colSize < 1) colSize = 1;
			var colClass = "col-sm-" + colSize;
			$.each(rowColumns, (index, col) => {
				col = $(col);
				col.remove();
				var coldiv = $(`<div></div>`);
				self.CopyAttributes(coldiv, col);
				coldiv.css("margin-bottom", "10px");
				if (!coldiv.hasClass(colClass))
					coldiv.addClass(colClass);
				coldiv.append(col.html());
				rowdiv.append(coldiv);
			});
		}
		return rowdiv;
	},

	TransformViewTitleElement: function (item) {
		var div = $("<div />");
		this.CopyAttributes(div, item);
		div.css("font-size", "17px");
		div.append(item.html());
		div.append($("<hr class='subtitle' />"));
		return div;
	},

	TransformViewFormElement: function (item) {
		var div = $("<div style='font-size:13px'></div>");
		this.CopyAttributes(div, item);
		div.append(item.html());
		var compact = (item.attr("compact") != null);
		this.TransformFormElements(div.children(), RegularFormElements, compact);
		return div;
	},

	TransformInlineFormElement: function (item) {
		var div = $("<div class='form-inline' style='font-size:13px; padding:5px;'></div>");
		this.CopyAttributes(div, item);
		div.append(item.html());
		this.TransformFormElements(div.children(), InlineFormElements, true);
		return div;
	},

	TransformFormElements: function (elements, elementCreator, compact) {
		var self = this;
		$.each(elements, function (index, item) {
			var jqe = $(item);
			var itemContainer = jqe.parent();
			jqe.remove();
			var fi = self.GetFieldInfo(item, jqe);
			if (fi.isNoTransform) {
				itemContainer.append(jqe);
			}
			else if (fi.isDiv || fi.isSpan) {
				if (fi.isFormField) {
					var elem = elementCreator.CreateDivElement(fi, jqe, compact)
					itemContainer.append(elem);
				}
				else {
					itemContainer.append(jqe);
					if (!fi.isNoTransform) {
						var children = jqe.children();
						if (children.length > 0)
							self.TransformFormElements(children, elementCreator, compact);
					}
				}
			}
			else {
				if (fi.isFormField) {
					if (fi.isFile) {
						var fileElm;
						if (fi.fileType == "image")
							fileElm = elementCreator.CreateImageElement(fi, jqe, compact, self);
						else
							fileElm = elementCreator.CreateFileElement(fi, jqe, compact, self);
						itemContainer.append(fileElm);
					}
					else if (fi.isCheckbox && !fi.isHidden) {
						var chk = elementCreator.CreateCheckbox(fi, jqe, compact);
						itemContainer.append(chk);
					}
					else if(fi.isRadioButton && !fi.isHidden) {
						var chk = elementCreator.CreateRadioButton(fi, jqe, compact);
						itemContainer.append(chk);
					}
					else if (fi.isDate && !fi.isHidden) {
						var dateElm = elementCreator.CreateDateElement(fi, jqe, compact);
						itemContainer.append(dateElm);
					}
					else if (!fi.isHidden) {
						var frmElm = elementCreator.CreateFormElement(fi, jqe, compact);
						itemContainer.append(frmElm);
					}
					else itemContainer.append(jqe);
				}
				else itemContainer.append(jqe);
			}
		});
	},

	TransformViewFooterElement: function (item) {
		var footerRow = $("<div class='row'></div>");
		var footer = $("<div class='col-12'></div>");
		footer.append($("<hr />"));
		footer.append(item.children());
		footerRow.append(footer);
		return footerRow;
	},

	GetFieldInfo: function (item, jqe) {
		var data = {};
		data.isNoTransform = item.attributes["no-transform"] != null;
		data.isDiv = item.tagName.toLowerCase() === "div";
		data.isSpan = item.tagName.toLowerCase() === "span";
		data.isInput = ",input,textarea,select,".indexOf("," + item.tagName.toLowerCase() + ",") >= 0;
		data.isFormField = item.attributes["model"] != null || data.isInput;
		data.isHidden = false;
		data.isCheckbox = false;
		data.isDate = false;
		data.isRadioButton = false;
		var t = item.attributes["type"];
		if (t != null) {
			var itype = t.value.toLowerCase();
			data.isHidden = itype == "hidden";
			data.isCheckbox = itype == "checkbox";
			data.isDate = itype == "date";
			data.isRadioButton = itype == "radio";
		}
		if (item.attributes["data-type"] != null) {
			var dataType = item.attributes["data-type"].value.toLowerCase();
			if (dataType === "image") {
				data.isFile = true;
				data.fileType = "image";
			}
			else if (dataType === "file") {
				data.isFile = true;
				data.fileType = "file";
			}
			else if (dataType === "date") {
				data.isDate = true;
			}
		}
		data.fieldName = jqe.attr("name");
		data.hasLabel = item.attributes["label"] != null;
		if (data.hasLabel) {
			var caption = item.attributes["label"].value;
			if (caption != null && caption.length > 0)
				data.labelText = caption;
			else if (data.fieldName != null)
				data.labelText = data.fieldName + ":";
			else
				data.labelText = "";
		}
		return data;
	},

	CopyAttributes: function (dst, src) {
		if(src.attr("style") != null)
			dst.attr("style", src.attr("style"));
		if (src.attr("class") != null)
			dst.attr("class", src.attr("class"));
		if (src.attr("name") != null)
			dst.attr("name", src.attr("name"));
	},

	HandleKeyUp: function (e) {
		if (e.keyCode === 27)
			this.Cancel();
	},

	Ok: function () {
		if (!this.model.ValidState()) return;
		var data = this.GetData();
		this.OnSaved.Raise(data);
		this.Close(data);
	},

	Cancel: function () {
		this.Close(null);
	},

	Close: function (data) {
		if (this.dialog == true) {
			this.Unload();
			if (this.dialogElm != null) {
				this.dialogElm.empty();
				this.dialogElm.remove();
				this.dialogElm = null;
			}
			if (this.dlgCallback != null)
				this.dlgCallback(data);
		}
		else this.Unload();
	},

	LoadData: function (data) {
		for (var p in data) {
			if (p in this.model)
				this.model[p] = data[p];
		}
		this.AfterLoadData.Raise();
	},

	GetData: function () {
		this.BeforeGetData.Raise();
		return JSON.parse(JSON.stringify(this.model.data));
	},

	AddElements: function (target, html) {
		AppContext.BindActions(html, this);
		var namedElements = {};
		$.each(html, function (index, item) {
			if (item.attributes["name"] != null) {
				var elementName = item.attributes["name"].value;
				namedElements[elementName] = $(item);
			}
		});
		for (var elm in namedElements)
			this.elements[elm] = namedElements[elm];
		target.append(html);
	}
};
// #endregion



// =========================================================================
// RegularFormElements
// =========================================================================
// #region RegularFormElements
var RegularFormElements = (function () {
	return {
		CreateCheckbox: function (fi, jqe, compact) {
			var topContainer = $("<div class='form-group row' style='margin-bottom:5px;'></div>");
			var leftSpaceClass = compact ? "col-form-label-sm" : "col-form-label";
			var leftSpace = $(`<div class='col-sm chk-label ${leftSpaceClass}'></div>`)
			var fieldContainer = $(`<div class="col-sm"></div>`);
			topContainer.append(leftSpace);
			topContainer.append(fieldContainer);
			if (fi.hasLabel) {

				var id = "_clickable_label_" + jqe.attr("name");

				var label = $(`<label for="${id}">${fi.labelText}</label>`);

				jqe.attr("id", id);

				jqe.css("margin-right", "10px");

				label.prepend(jqe);

				fieldContainer.append(label);

			}
			else {
				fieldContainer.append(jqe);
			}
			return topContainer;
		},

		CreateRadioButton: function (fi, jqe, compact) {
			return this.CreateCheckbox(fi, jqe, compact);
		},

		CreateDivElement: function (fi, jqe, compact) {
			var topContainer = $("<div class='form-group row' style='margin-bottom:5px;'></div>");
			var fieldContainer = $(`<div class="col-sm"></div>`);
			var leftSpaceClass = compact ? "col-form-label-sm" : "col-form-label";
			jqe.addClass("FormDisplay");
			if (fi.hasLabel) {
				var label = $(`<label class="col-sm ${leftSpaceClass}">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			else {
				var leftSpace = $(`<div class='col-sm ${leftSpaceClass}'></div>`)
				topContainer.append(leftSpace);
			}
			fieldContainer.append(jqe);
			topContainer.append(fieldContainer);
			return topContainer;
		},

		CreateDateElement: function (fi, jqe, compact) {
			var topContainer = $("<div class='form-group row' style='margin-bottom:5px;'></div>");
			var fieldContainer = $(`<div class="col-sm"></div>`);
			var leftSpaceClass = compact ? "col-form-label-sm" : "col-form-label";
			if (fi.hasLabel) {
				var label = $(`<label class="col-sm ${leftSpaceClass}">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			else {
				var leftSpace = $(`<div class='col-sm ${leftSpaceClass}'></div>`)
				topContainer.append(leftSpace);
			}
			fieldContainer.append(jqe);
			topContainer.append(fieldContainer);
			if (fi.isDate && jqe.prop("tagName") === "INPUT")
				jqe.kendoDatePicker();
			return topContainer;
		},

		CreateFileElement: function (fi, jqe, compact, controller) {
			var topContainer = $("<div class='form-group row' style='margin-bottom:5px;'></div>");
			var fieldContainer = $(`<div class="col-sm"></div>`);
			var leftSpaceClass = compact ? "col-form-label-sm" : "col-form-label";
			if (fi.hasLabel) {
				var label = $(`<label class="col-sm ${leftSpaceClass}">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			if (jqe.attr("type").toLowerCase() == "file")
				jqe.attr("type", "hidden");
			topContainer.append(jqe);
			var fileTypes = jqe.attr("accept");
			var widgetContainer = $("<div style='float:left;'></div>");
			var fileElm = $(`<input name="_${fi.fieldName}_File" type="file" accept="${fileTypes}" />`);
			widgetContainer.append(fileElm);
			fieldContainer.append(widgetContainer);
			if (controller.GetFileDownloadUrl != null)
				fieldContainer.append(`<a name="_${fi.fieldName}_DownloadLink" href="${controller.GetFileDownloadUrl(fi.fieldName, 0)}" style="display: inline-block; margin-top: 30px; margin-left: 10px;">Download File</a>`);
			topContainer.append(fieldContainer);
			fileElm.kendoUpload({
				async: {
					saveUrl: controller.GetFileUploadUrl(fi.fieldName, 0),
					autoUpload: true
				},
				multiple: false,
				select: function (e) {
					var formGroup = e.sender.element.closest(".form-group");
					var fileVal = formGroup.find(`input[name='${fi.fieldName}']`);
					var fileid = fileVal.val() || 0;
					e.sender.options.async.saveUrl = controller.GetFileUploadUrl(fi.fieldName, fileid);
				},
				success: function (e) {
					if (!e.response.success) {
						e.preventDefault();
						AppContext.ShowError(e.response.message);
					}
					else {
						var formGroup = e.sender.element.closest(".form-group");
						var elm = formGroup.find(`[name='${fi.fieldName}']`);
						elm.val(e.response.FileID).trigger('change');

						if (typeof (controller.KendoUpload__afterUpload) == 'function') {

							controller.KendoUpload__afterUpload(fi.fieldName, e.response.FileID, e);
						}

					}

					

				},
				error: function (e) {
					AppContext.ShowError("Could not upload file.");
				}
			});
			return topContainer;
		},

		CreateImageElement: function (fi, jqe, compact, controller) {
			
			var topContainer, fileElm;

			if (controller.KendoUploadImage_UseCustomImageElement === true
				&& typeof(controller.KendoUploadImage_GetFileElementTemplate) === 'function'
				&& typeof(controller.KendoUploadImage_GetFileElementWiget) == 'function'
			) {

				topContainer = controller.KendoUploadImage_GetFileElementTemplate(fi, jqe);
				fileElm = controller.KendoUploadImage_GetFileElementWiget(fi, jqe);

			} else {


				topContainer = $("<div class='form-group row ke-topContainer' style='margin-bottom:5px;'></div>");
				var fieldContainer = $(`<div class="col-sm ke-fieldContainer"></div>`);
				var leftSpaceClass = compact ? "col-form-label-sm space-cls" : "col-form-label space-cls";
				if (fi.hasLabel) {
					var label = $(`<label class="col-sm ${leftSpaceClass} ke-label-class">${fi.labelText}</label>`);
					topContainer.append(label);
				}
				if (jqe.attr("type").toLowerCase() == "file")
					jqe.attr("type", "hidden");

				jqe.addClass('jqe-Container');

				topContainer.append(jqe);
				var fileTypes = jqe.attr("accept") != undefined ? jqe.attr("accept") : '';
				fieldContainer.append(`<img name="_${fi.fieldName}_Img" src="/images/no_logo.png" class="ThumbImage2 ke-img-element" />`)
				var widgetContainer = $('<div style="float:left;" class="ke-widgetContainer"></div>');

				fileElm = $(`<input name="_${fi.fieldName}_File" type="file" accept="${fileTypes}" class="ke-file-element" />`);

				widgetContainer.append(fileElm);
				fieldContainer.append(widgetContainer);
				topContainer.append(fieldContainer);

			}


			fileElm.kendoUpload({
				async: {
					saveUrl: controller.GetImageUploadUrl(fi.fieldName, 0),
					autoUpload: true
				},
				multiple: false,
				select: function (e) {
					var formGroup = e.sender.element.closest(".form-group");
					var imgVal = formGroup.find(`input[name='${fi.fieldName}']`);
					var fileid = imgVal.val() || 0;
					e.sender.options.async.saveUrl = controller.GetImageUploadUrl(fi.fieldName, fileid);
				},
				success: function (e) {
					if (!e.response.success) {
						e.preventDefault();
						AppContext.ShowError(e.response.message);
					}
					else {
						var dt = new Date().toTicks();
						var formGroup = e.sender.element.closest(".form-group");
						var img = formGroup.find(".ThumbImage2");
						var elm = formGroup.find(`[name='${fi.fieldName}']`);
						elm.val(e.response.FileID).trigger('change');

						var imageUrl = controller.GetImageDownloadUrl(fi.fieldName, e.response.FileID);
						
						img.attr("src", `${imageUrl}?${dt}`);

						if (typeof(controller.KendoUploadImage__afterUpload) == 'function') {

							controller.KendoUploadImage__afterUpload(fi.fieldName, e.response.FileID);
						}
					}
				},
				error: function (e) {
					AppContext.ShowError("Could not upload image.");
				}
			});
			jqe[0].SetImage = function (val) { controller.model.SetFieldValue(jqe[0], val); }
			return topContainer;
		},

		CreateFormElement: function (fi, jqe, compact) {
			var topContainer = $("<div class='form-group row' style='margin-bottom:5px;'></div>");
			var fieldContainer = $(`<div class="col-sm"></div>`);
			jqe.addClass("form-control form-control-sm");
			var leftSpaceClass = compact ? "col-form-label-sm" : "col-form-label";
			if (fi.hasLabel) {
				var label = $(`<label class="col-sm ${leftSpaceClass}">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			else {
				var leftSpace = $(`<div class='col-sm ${leftSpaceClass}'></div>`)
				topContainer.append(leftSpace);
			}
			fieldContainer.append(jqe);
			topContainer.append(fieldContainer);
			return topContainer;
		}
	};
})();
// #endregion



// =========================================================================
// InlineFormElements
// =========================================================================
// #region InlineFormElements
var InlineFormElements = (function () {
    let marginPosition = 'margin-right';
    let marginValue = '10px';
    let divFromGroup = `<div class="form-group" style="${marginPosition}:${marginValue}"></div>`;

	return {
		CreateCheckbox: function (fi, jqe, compact) {
            jqe.css(marginPosition, marginValue);
            var topContainer = $(divFromGroup);
			if (fi.hasLabel) {
				var label = $(`<label>${fi.labelText}</label>`);
				label.prepend(jqe);
				topContainer.append(label);
			}
			else {
				topContainer.append(jqe);
			}
			return topContainer;
		},

		CreateRadioButton: function (fi, jqe, compact) {
			return this.CreateCheckbox(fi, jqe, compact);
		},

		CreateDivElement: function (fi, jqe, compact) {
            var topContainer = $(divFromGroup);
			if (fi.hasLabel) {
                var label = $(`<label style="${marginPosition}:${marginValue}">${fi.labelText}</label>`);
				topContainer.append(label);
				label.append(jqe);
			}
			else {
				topContainer.append(jqe);
			}
			return topContainer;
		},

		CreateDateElement: function (fi, jqe, compact) {
            var topContainer = $(divFromGroup);
			if (fi.hasLabel) {
                var label = $(`<label style="${marginPosition}:${marginValue}">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			jqe.addClass("form-control form-control-sm");
			topContainer.append(jqe);
			if (fi.isDate && jqe.prop("tagName") === "INPUT")
				jqe.kendoDatePicker();
			return topContainer;
		},

		CreateFormElement: function (fi, jqe, compact) {
            var topContainer = $(divFromGroup);
			if (fi.hasLabel) {
                var label = $(`<label style="${marginPosition}:${marginValue}">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			if (jqe.prop("tagName") === "SELECT")
				jqe.addClass("custom-select custom-select-sm");
			else {
				jqe.addClass("form-control form-control-sm");
			}
			topContainer.append(jqe);
			return topContainer;
		}
	};
})();
// #endregion


// =========================================================================
// Javascript Utils
// =========================================================================

// #region ArrayHelper
/**
 *  Helper Functions to solve array common tasks
 *  
 * 
 * static object ArrayHelper
 */
let ArrayHelper = (function () {

    let prototype = {

        /**
         * https://www.geeksforgeeks.org/counting-frequencies-of-array-elements/
         * Input :  arr[] = {10, 20, 20, 10, 10, 20, 5, 20}
         * Output : 
         * 10 3
         * 20 4
         * 5  1
         * 
         * @param Array arr array of number elements
         * @param int n number of element inner array
         */
        Frequency: function (arr, n) {
            let visited = Array.from({ length: n }, (_, i) => false);

            // Traverse through array elements and
            // count frequencies
            for (let i = 0; i < n; i++) {

                // Skip this element if already processed
                if (visited[i] == true)
                    continue;

                // Count frequency
                let count = 1;
                for (let j = i + 1; j < n; j++) {
                    if (arr[i] == arr[j]) {
                        visited[j] = true;
                        count++;
                    }
                }
                document.write(arr[i] + " " + count + "<br/>");
            }
        },

        FastFrequency: function () {
            // usign a map objet is a good option

            throw "not implemented";
        }

    }; // End of prototype definition

    return prototype;
    })();

// #endregion ArrayHelper