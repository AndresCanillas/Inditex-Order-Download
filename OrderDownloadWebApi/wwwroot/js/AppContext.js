
var ExtendObj = function (target, base) {
	for (var prop in base) {
		if (base.hasOwnProperty(prop)) {
			target[prop] = base[prop];
		}
	}
};


String.prototype.Format = function () {
	var args = arguments;
	return this.replace(/\{\{|\}\}|\{(\d+)\}/g, function (m, n) {
		if (m === "{{") { return "{"; }
		if (m === "}}") { return "}"; }
		return args[n];
	});
};

String.IsNullOrEmpty = function (str) {
	if (str != null && str.length != null && str.replace != null)
		return str == null || str.length == 0 || !str.replace(/\s/g, '').length;
	else
		return str == null;
};

String.prototype.IsNullOrEmpty = function () {
	return String.IsNullOrEmpty(this);
};

String.IsString = function (value) {
	if (value == null) return false;
	return (typeof value === 'string' || value instanceof String)
}


Date.prototype.ToTicks = function () {
	return (621355968e9 + this.getTime() * 1e4);
};

Date.prototype.AddDays = function (days) {
	var date = new Date(this.valueOf());
	date.setDate(date.getDate() + days);
	return date;
};

Date.IsDate = function (value) {
	if (value == null) return false;
	return (typeof value === 'date' || value instanceof Date);
}

Date.prototype.ToShortString = function () {
	return `${this.getMonth() + 1}/${this.getDate()}/${this.getFullYear()}`;
}

Array.prototype.Where = function (predicate) {
	var result = [];
	for (var i = 0; i < this.length; i++) {
		if (predicate(this[i]) === true)
			result.push(this[i]);
	}
	return result;
};

Array.prototype.GroupBy = function (predicate) {
	var groups = [];
	var group = null;
	var groupIndex = {};
	for (var i = 0; i < this.length; i++) {
		var gvalue = predicate(this[i]);
		if (groupIndex[gvalue] == null) {
			group = [];
			groupIndex[gvalue] = groups.length;
			groups.push(group);
		}
		else {
			group = groups[groupIndex[gvalue]];
		}
		group.push(this[i]);
	}
	return groups;
};

Array.prototype.First = function (predicate) {
	if (predicate == null) {
		if (this.length > 0)
			return this[0];
		else
			return null;
	}
	for (var i = 0; i < this.length; i++) {
		if (predicate(this[i]) === true)
			return this[i];
	}
	return null;
};

Array.prototype.Exists = function (predicate) {
	if (predicate == null) return false;
	for (var i = 0; i < this.length; i++) {
		if (predicate(this[i]) === true)
			return true;
	}
	return false;
};

Array.prototype.RemoveAll = function (predicate) {
	for (var i = 0; i < this.length; i++) {
		if (predicate(this[i]) === true) {
			this.splice(i, 1);
			i--;
		}
	}
};


jQuery.fn.FindTags = function (tagname, searchChildren) {
	tagname = tagname.toUpperCase();
	var divein = (searchChildren === true);
	var selection = [];
	var set = this;
	for (var i = 0; i < set.length; i++) {
		if (set[i].tagName === tagname)
			selection.push(set[i]);
		if (divein) {
			var subSet = $(set[i]).children().FindTags(tagname, divein);
			if (subSet.length > 0)
				for (var j = 0; j < subSet.length; j++)
					selection.push(subSet[j]);
		}
	}
	return $(selection);
};


jQuery.fn.FindFirst = function (tagname) {
	tagname = tagname.toUpperCase();
	var set = this;
	for (var i = 0; i < set.length; i++) {
		if (set[i].tagName === tagname) {
			return $([set[i]]);
		}
		var subSet = $(set[i]).children();
		if (subSet.length > 0) {
			var childSearch = subSet.FindFirst(tagname);
			if (childSearch.length > 0)
				return $([childSearch[0]]);
		}
	}
	return $([]);
};


function ToggleMenu() {
	var leftMenu = $(".left-menu");
	var mainPanel = $(".main-panel");
	leftMenu.toggleClass("left-menu-collapse");
	mainPanel.toggleClass("main-panel-collapse");
}



// =========================================================================
// AppContext
// =========================================================================
// #region AppContext
var AppContext = (function () {
	var basePort = location.port ? `:${location.port}` : "";
	var baseUrl = `${location.protocol}//${location.hostname}${basePort}`;

    var settings = {
        enableCache: false,
        timeMark: null,
    };

	parseController = function (url) {
		var idx1 = url.lastIndexOf("/") + 1;
		var idx2 = url.lastIndexOf(".js");
		var controllerType = url.substring(idx1, idx2);
		return controllerType;
	};

	function httpGet(url) {
		return new Promise((resolve, reject) => {
			var rq = new XMLHttpRequest();
			rq.open("GET", url, true);
			rq.onload = (e) => {
                if (rq.readyState === 4) {
					if (rq.responseURL != `${baseUrl}${url}`)
						document.location.href = rq.responseURL;
					else if (rq.status === 200)
						resolve(rq.responseText);
					else
						reject(rq.statusText);
				}
			};
			rq.onerror = (e) => {
				AppContext.ShowError(rq.statusText);
				reject(rq.statusText);
			};
			rq.send();
		});
	}

	function httpPost(url, content, contentType) {
		return new Promise((resolve, reject) => {
			var rq = new XMLHttpRequest();
			rq.open("POST", url, true);
			if (String.IsString(contentType))
				rq.setRequestHeader("Content-Type", contentType);
			rq.onload = (e) => {
				if (rq.readyState === 4) {
					if (rq.responseURL != `${baseUrl}${url}`)
						document.location.href = rq.responseURL;
					else if (rq.status === 200)
						resolve(rq.responseText);
					else
						reject(rq.statusText);
				}
			};
			rq.onerror = (e) => {
				AppContext.ShowError(rq.statusText);
				reject(rq.statusText);
			};
			rq.send(content);
		});
	}

    async function loadScript(url) {
        
        //var scriptUrl = await AppContext.AjaxScriptSettings(url);

        var result = await httpGet(url);

		let elm = document.createElement("script");
		elm.textContent = result;
		document.head.append(elm);
	}

	var allScripts = {};  //Tracks all loaded scripts to prevent loading the same JS multiple times.

    return {

        SetSettings: function (newSettings) {

            settings = $.extend(settings, newSettings);

        },

        AjaxScriptSettings: async function (url) {
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

		LoadJS: async function () {
			if (arguments.length < 1)
				throw "Was expecting at least one URL and a callback!";
			var downloads = [];
			var filesToLoad = arguments;
			if (filesToLoad[0] instanceof Array)
				filesToLoad = filesToLoad[0];
            for (var scriptUrl of filesToLoad) {

				if (!String.IsString(scriptUrl))
					throw "Invalid URL, was expecting a string!";
				if (!allScripts[scriptUrl])
					downloads.push(loadScript(scriptUrl))
			}
			await Promise.all(downloads);
			for (var scriptUrl of filesToLoad)
				allScripts[scriptUrl] = true;
		},

		HttpGet: async function (url, overlayMessage, successMessage) {
			var overlay = null;
			if (String.IsString(overlayMessage)) {
				overlay = $(`<div><div class="overlay"></div><div class="overlaymessage">${overlayMessage}</div></div>`);
				overlay.appendTo(document.body)
			}
			try {
				var response = await httpGet(url);
				if (String.IsString(successMessage) && !String.IsNullOrEmpty(successMessage))
					AppContext.ShowSuccess(successMessage);
				var data = JSON.parse(response);
				return data;
			}
			finally {
				if (overlay != null)
					overlay.remove();
			}
		},

        HttpPost: async function (url, data, overlayMessage, successMessage, overlayTimeout) {
            var overlay = null;
            if (overlayTimeout) {
                var intervalId = setInterval(incrementSeconds, 1000);
                var seconds = 0;
            }
            if (String.IsString(overlayMessage)) {
                overlay = $(`<div><div class="overlay"></div><div class="overlaymessage">${overlayMessage}</div></div>`);
                overlay.appendTo(document.body);
            }
            try {

                var response = await httpPost(url, JSON.stringify(data), "application/json; charset=UTF-8");
                var responseData = JSON.parse(response);
                var msg = responseData.Message ? responseData.Message : "";
                if (String.IsString(msg) && !String.IsNullOrEmpty(msg)) {
                    if (!responseData.Success)
                        AppContext.ShowError(msg);
                    else
                        AppContext.ShowSuccess(msg);
                }

                return responseData;
            }
            finally {
                if (overlay != null)
                    overlay.remove();
            }

            function incrementSeconds() {
                seconds++;
                if (seconds > overlayTimeout) {
                    overlay.remove();
                    clearInterval(intervalId);
                }
            }
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
				.delay(timeout ? timeout : 4000)
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

		LoadComboBox: function (element, data, valueField, textField) {
			var opValue, opText;
            var combo = AppContext.GetContainerElement(element);
			combo.empty();
			$.each(data, function (i, item) {
				opValue = valueField ? item[valueField] : item.ID ? item.ID : item.Value ? item.Value : item;
				opText = textField ? item[textField] : item.Name ? item.Name : item.Text ? item.Text : item;
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
					AppContext.ShowError(err);
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

		BindElements: async function (container, controller) {
			var elements = { };
			var viewContainer = AppContext.GetContainerElement(container);
			var componentsWithController = viewContainer.find("[component][controller]");
			if (componentsWithController.length > 0) {
				var filesToLoad = [];
				$.each(componentsWithController, (index, item) => {
					var componentController = item.attributes["controller"].value;
					filesToLoad.push(componentController);
				});
				await AppContext.LoadJS(filesToLoad);
				await LoadComponents();
				return elements;
			}
			else {
				await LoadComponents();
				return elements;
			}

			async function LoadComponents() {
				var components = viewContainer.find("[component], [name]");
				if (viewContainer.attr("name") != null) {
					components.push(viewContainer[0]);
				}
				var index = 0;
				for (var item of components) {
					if (item.attributes["component"] != null) {
						var ctype = item.attributes["component"].value;
						var cname = "C" + index;
						if (item.attributes["name"] != null)
							cname = item.attributes["name"].value
						elements[cname] = await CreateComponent(item, ctype);
					}
					else if (item.attributes["name"] != null) {
						var elementName = item.attributes["name"].value;
						elements[elementName] = $(item);
					}
					index++;
				}
			}

			async function CreateComponent(item, ctype) {
				switch (ctype) {
					case "jQuery":
						return $(item);
					case "FormView":
						var componentController = item.attributes["controller"].value;
						var objectName = AppContext.GetCtorFromUrl(componentController);
						var compArgs = null;
						if (item.attributes["args"] != null)
							compArgs = JSON.parse(item.attributes["args"].value);
						var component = new window[objectName](compArgs);
						await component.Load();
						component.Show($(item));
						return component;
					default:
						var component = new window[ctype]($(item), controller);
						if (component.Load != null && component.Load instanceof Function) {
							var promise = component.Load();
							if (promise instanceof Promise) {
								await promise;
							}
						}
						if (component.Show != null && component.Show instanceof Function)
							component.Show($(item));
						return component;
				}
			}
		},

		BindView: async function (container, controller) {
			controller.model = AppContext.BindModel(container, controller);
			AppContext.BindActions(container, controller);
			var elements = await AppContext.BindElements(container, controller);
			controller.elements = elements;
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
		}
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
				if (String.IsString(value) && value.length > 0) {
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
				else if (!Date.IsDate(value)) {
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
					var dt = new Date().ToTicks();
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
				if (Date.IsDate(value))
					element.innerText = value.toLocaleDateString();
				else
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
			var errorIcon = $('<div class="form-error"><span class="fa fa-exclamation-triangle"></span></div>');
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
			if (jq.is(":hidden") || jq.prop("disabled"))
				return;
			switch (validation) {
				case "required":
                    if (value == null || (value.toString().length == 0) || value == -1) {
						this.SetElementError(jq, "Field cannot be empty.");
						result = false;
					}
					break;
				case "email":
					if (value && value.length > 0 && !(/^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$/.test(value))) {
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
					var sepIdx = validationArguments.indexOf(",");
					var min = parseInt(validationArguments.substring(0, sepIdx), 10);
					var max = parseInt(validationArguments.substring(sepIdx + 1), 10);
					if (value < min || value > max) {
						this.SetElementError(jq, "Value is out of range.");
						result = false;
					}
                    break;
                case "minlen":
                    var min = parseInt(validationArguments, 10);
                    if (value.length < min ) {
                        this.SetElementError(jq, `The minimum length is ${min}`);
                        result = false;
                    }
                    break;
                case "validdate":
                    var date = new Date(Date.parse(validationArguments));
                    if (new Date(Date.parse(value)) < date.addDays(1)) {
                        this.SetElementError(jq, `Invalid Date`);
                        result = false;
                    }
                    break;
                case "regex":
                    var rgx = new RegExp(validationArguments);
                    if (rgx.test(value) === false) {
                        this.SetElementError(jq, "Value does not match the expected format");
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
	this._loaded = false;
	this._visible = false;
	this.OnLoaded = new AppEvent(true);			// event raised when the view is firt loaded and initialized (automatically raised as the view is shown for the first time, and should happen only once during the life time of the view).
	this.OnUnloaded = new AppEvent(true);		// event raised when the view is unloaded from the document (automatically raised when Unload is called, and should happen only once during the life time of the view).
	this.OnShowed = new AppEvent();				// event raised whenever the view is shown on its container (automatically raised when Show is called).
	this.OnHidden = new AppEvent();				// event raised whenever the view is hidden (automatically raised when Hide is called).
	this.OnSaved = new AppEvent();				// views can raise this event to inform subscribers when the record loaded in the view has been modified.
	this.OnBeforeGetData = new AppEvent();		// event raised before data is retrieved from the form by calling view.GetData() method, this event is meant as a last chance for the view to apply any model changes before its data is returned from GetData().
	this.OnAfterLoadData = new AppEvent();		// event raised right after LoadData(data) method is called. Can be used by the view to respond to a programatic upload.
	if (FormView.hiddenElements == null) {
		FormView.hiddenElements = $("<div style='display:none; position:absolute; left:0px; top:0px; width:0px; height:0px;'>");
		$("BODY").append(FormView.hiddenElements);
	}
};


FormView.Extend = function (target, title, viewUrl) {
	if (title == null || title.length === 0)
		title = "Title";
	FormView.call(target, title, viewUrl);
	ExtendObj(target, FormView.prototype);
};

FormView.prototype = {
	constructor: FormView,

	Load: async function () {
		if (this._loaded) return;
		if (this.ViewUrl != null) {
			try {
				this.Content = await $.ajax({ type: "GET", url: this.ViewUrl });
			}
			catch (err) {
				AppContext.ShowError("Error Loading View");
			}
		}

		var jqe = this.Content;
		if (String.IsString(this.Content))
			jqe = $(this.Content);

		if (jqe.length > 1) {
			this.TopElement = $("<div>");
			this.TopElement.append(jqe);
		}
		else this.TopElement = jqe;

		FormView.hiddenElements.append(this.TopElement);
		this.TransformView(this.TopElement);
		await AppContext.BindView(this.TopElement, this);
		this._loaded = true;
		if (this.OnLoad != null)
			this.OnLoad();
		this.OnLoaded.Raise(this);
	},

	Show: function (viewContainer) {
		if (!this._loaded)
			throw "View is in an invalid state";
		if (viewContainer == null)
			throw "Invalid viewContainer";
		this.ViewContainer = AppContext.GetContainerElement(viewContainer);
		if (this.ViewContainer[0].CurrentView != null && this.ViewContainer[0].CurrentView !== this) {
			this.ViewContainer[0].CurrentView.Hide();
			this.ViewContainer[0].CurrentView = null;
		}
		this.ViewContainer.append(this.TopElement);
		this._visible = true;
		this.ViewContainer[0].CurrentView = this;
		if (this.OnShow != null)
			this.OnShow();
		this.OnShowed.Raise(this);
		this.isDialog = false;
		var self = this;
		return new Promise((resolve) => {
			self.resolveMethod = resolve;
		});
	},

	ShowDialog: function () {
		var self = this;
		if (!this._loaded || this._visible)
			throw "View is in an invalid state";
		var dlg = $('<div class="modal-form">');
		var dialogDoc = $('<div class="modal-form-content container-fluid" action="keyup:HandleKeyUp">');
		dlg.append(dialogDoc);
		var dialogCard = $("<div class='card'></div>");
		dialogDoc.append(dialogCard);
		this.closeBtn = $('<span class="modal-form-close" action="Cancel">&times;</span>');
		this.closeBtn.on("click", ()=>self.Cancel());
		dialogCard.append(this.closeBtn);
		var dialogTitle = $(`<div class="modal-form-title">${this.Title}</div>`);
		dialogCard.append(dialogTitle);
		var cardBody = $("<div class='card-body' style='max-height:75vh; overflow:auto;'></div>");
		cardBody.append(self.TopElement);
		dialogCard.append(cardBody);
		this.isDialog = true;
		this.dialogElement = dlg;
		this.ViewContainer = $("BODY");
		this.ViewContainer.append(dlg);
		this._visible = true;
		if (this.OnShow != null)
			this.OnShow();
		setTimeout(() => {
			self.Focus();
		}, 100);
		return new Promise((resolve) => {
			self.resolveMethod = resolve;
		});
	},

	Focus: function () {
		if (!this._loaded || !this._visible)
			return;
		var selection = null;
		if (this.isDialog)
			selection = this.dialogElement.find("input, select, textarea");
		else
			selection = this.topElement.find("input, select, textarea");
		if (selection != null && selection.length > 0)
			selection[0].focus();
	},

	Ok: function () {
		if (!this._loaded || !this._visible)
			throw "View is in an invalid state";
		if (!this.model.ValidState()) return;
		var data = this.GetData();
		this.OnSaved.Raise(data);
		this.Close(data);
	},

	Cancel: function () {
		if (!this._loaded || !this._visible)
			throw "View is in an invalid state";
		this.Close(null);
	},

	Close: function (data) {
		if (!this._loaded || !this._visible)
			throw "View is in an invalid state";
		this.Hide(data);
	},

	Hide: function (data) {
		if (!this._loaded || !this._visible)
			throw "View is in an invalid state";
		FormView.hiddenElements.append(this.TopElement);
		this._visible = false;
		if (this.ViewContainer[0].CurrentView == this)
			this.ViewContainer[0].CurrentView = null;
		if (this.isDialog) {
			this.closeBtn.off();
			this.closeBtn = null;
			this.dialogElement.empty();
			this.dialogElement.remove();
			this.dialogElement = null;
		}
		if (this.OnHide != null)
			this.OnHide();
		this.OnHidden.Raise(this);
		if (this.resolveMethod != null)
			this.resolveMethod(data);
	},

	LoadData: function (data) {
		for (var p in data) {
			if (p in this.model)
				this.model[p] = data[p];
		}
		this.OnAfterLoadData.Raise();
	},

	GetData: function () {
		this.OnBeforeGetData.Raise();
		return JSON.parse(JSON.stringify(this.model.data));
	},

	Unload: function () {
		if (!this._loaded) return;
		this.Hide();
		if (this.OnUnload != null)
			this.OnUnload();
		this.OnUnloaded.Raise();
		this._loaded = false;
		if (this.ViewContainer[0].CurrentView == this)
			this.ViewContainer[0].CurrentView = null;
		this.OnLoaded.Dispose();
		this.OnUnloaded.Dispose();
		this.OnShowed.Dispose();
		this.OnHidden.Dispose();
		this.OnSaved.Dispose();
		this.OnBeforeGetData.Dispose();
		this.OnAfterLoadData.Dispose();
		this.Content = null;
		this.model = null;
		this.elements = null;
		this.ViewContainer = null;
		this.TopElement.remove();
		this.TopElement.empty();
		this.TopElement = null;
		this.resolveMethod = null;
	},

	TransformView: function (container) {
		this.TransformElement(container, "viewcard", this.TransformViewCardElement);
		this.TransformElement(container, "viewrow", this.TransformViewRowElement);
		this.TransformElement(container, "viewtitle", this.TransformViewTitleElement);
		this.TransformElement(container, "viewform", this.TransformViewFormElement);
		this.TransformElement(container, "inlineform", this.TransformInlineFormElement);
		this.TransformElement(container, "viewfooter", this.TransformViewFooterElement);
	},

	TransformElement: function (container, tagName, fn) {
		var element;
		do {
			element = container.FindFirst(tagName);
			if (element[0] === container[0]) {
				var transformedElm = fn.call(this, element);
				break;
			}
			else if (element.length != 0) {
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
		var rowColumns = rowdiv.children().FindTags("viewcolumn");
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
				if (!coldiv.hasClass(colClass))
					coldiv.addClass(colClass);
				coldiv.append(col.html());
				rowdiv.append(coldiv);
			});
		}
		return rowdiv;
	},

	TransformViewTitleElement: function (item) {
		var div = $("<div>");
		var title = $("<div>");
		this.CopyAttributes(div, item);
		div.css("font-size", "17px");
		title.append(item.html());
		div.append(title);
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
			else if (fi.isDiv || fi.isSpan || fi.isTable) {
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
		var footerRow = $("<div class='row' style='border-top:1px solid #bebebe; margin-top:10px; padding-top:20px;'></div>");
		var footer = $("<div class='col-12'></div>");
		footer.append(item.children());
		footerRow.append(footer);
		return footerRow;
	},

	GetFieldInfo: function (item, jqe) {
		var data = {};
		data.isNoTransform = item.attributes["no-transform"] != null;
		data.isDiv = item.tagName.toLowerCase() === "div";
		data.isSpan = item.tagName.toLowerCase() === "span";
		data.isTable = "table,thead,tbody,th,tr,td".indexOf(item.tagName.toLowerCase()) >= 0;
		data.isInput = ",input,textarea,select,".indexOf("," + item.tagName.toLowerCase() + ",") >= 0;
		data.isFormField = item.attributes["model"] != null || data.isInput;
		data.isHidden = false;
		data.isCheckbox = false;
		data.isDate = false;
		var t = item.attributes["type"];
		if (t != null) {
			var itype = t.value.toLowerCase();
			data.isHidden = itype == "hidden";
			data.isCheckbox = itype == "checkbox";
			data.isDate = itype == "date";
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
	}
};
// #endregion



// =========================================================================
// RegularFormElements
// =========================================================================
// #region RegularFormElements
var RegularFormElements = (function () {
	function getTopContainer(compact) {
		var margin = "";
		if (compact)
			margin = " style='margin-bottom:5px;'";
		else
			margin = " style='margin-bottom:10px;'";
		return $(`<div class='form-group row'${margin}></div>`);
	}

	return {
		CreateCheckbox: function (fi, jqe, compact) {
			var topContainer = getTopContainer(compact);
			var leftSpace = $(`<div class='col-4'></div>`)
			var fieldContainer = $(`<div class="col-8"></div>`);
			topContainer.append(leftSpace);
			topContainer.append(fieldContainer);
			if (fi.hasLabel) {
				var label = $(`<label>${fi.labelText}</label>`);
				jqe.css("margin-right", "10px");
				label.prepend(jqe);
				fieldContainer.append(label);
			}
			else {
				fieldContainer.append(jqe);
			}
			return topContainer;
		},

		CreateDivElement: function (fi, jqe, compact) {
			var topContainer = getTopContainer(compact);
			var fieldContainer = $(`<div class="col-8"></div>`);
			jqe.addClass("FormDisplay");
			if (fi.hasLabel) {
				var label = $(`<label class="col-4">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			else {
				var leftSpace = $(`<div class='col-4'></div>`)
				topContainer.append(leftSpace);
			}
			fieldContainer.append(jqe);
			topContainer.append(fieldContainer);
			return topContainer;
		},

		CreateDateElement: function (fi, jqe, compact) {
			var topContainer = getTopContainer(compact);
			var fieldContainer = $(`<div class="col-8"></div>`);
			if (fi.hasLabel) {
				var label = $(`<label class="col-4">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			else {
				var leftSpace = $(`<div class='col-4'></div>`)
				topContainer.append(leftSpace);
			}
			fieldContainer.append(jqe);
			topContainer.append(fieldContainer);
			if (fi.isDate && jqe.prop("tagName") === "INPUT")
				jqe.kendoDatePicker();
			return topContainer;
		},

		CreateFileElement: function (fi, jqe, compact, controller) {
			var topContainer = getTopContainer(compact);
			var fieldContainer = $(`<div class="col-8"></div>`);
			if (fi.hasLabel) {
				var label = $(`<label class="col-4">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			else {
				var leftSpace = $(`<div class='col-4'></div>`)
				topContainer.append(leftSpace);
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
			var topContainer = getTopContainer(compact);
			var fieldContainer = $(`<div class="col-8"></div>`);
			if (fi.hasLabel) {
				var label = $(`<label class="col-4">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			else {
				var leftSpace = $(`<div class='col-4'></div>`)
				topContainer.append(leftSpace);
			}
			if (jqe.attr("type").toLowerCase() == "file")
				jqe.attr("type", "hidden");
			topContainer.append(jqe);
			var fileTypes = jqe.attr("accept");
			fieldContainer.append(`<img name="_${fi.fieldName}_Img" src="/images/no_logo.png" class="ThumbImage2" />`)
			var widgetContainer = $("<div style='float:left;'></div>");
			var fileElm = $(`<input name="_${fi.fieldName}_File" type="file" accept="${fileTypes}" />`);
			widgetContainer.append(fileElm);
			fieldContainer.append(widgetContainer);
			topContainer.append(fieldContainer);
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
						var dt = new Date().ToTicks();
						var formGroup = e.sender.element.closest(".form-group");
						var img = formGroup.find(".ThumbImage2");
						var elm = formGroup.find(`[name='${fi.fieldName}']`);
						elm.val(e.response.FileID).trigger('change');
						img.attr("src", `${controller.GetImageDownloadUrl(fi.fieldName, e.response.FileID)}?${dt}`);
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
			var topContainer = getTopContainer(compact);
			var fieldContainer = $(`<div class="col-8"></div>`);
			jqe.addClass("form-control form-control-sm");
			if (fi.hasLabel) {
				var label = $(`<label class="col-4">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			else {
				var leftSpace = $(`<div class='col-4'></div>`)
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
	return {
		CreateCheckbox: function (fi, jqe, compact) {
			jqe.css("margin-right", "10px");
			var topContainer = $("<div class='form-group' style='margin-right:10px;'></div>");
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

		CreateDivElement: function (fi, jqe, compact) {
			var topContainer = $("<div class='form-group' style='margin-right:10px;'></div>");
			if (fi.hasLabel) {
				var label = $(`<label style="margin-right:10px;">${fi.labelText}</label>`);
				topContainer.append(label);
				label.append(jqe);
			}
			else {
				topContainer.append(jqe);
			}
			return topContainer;
		},

		CreateDateElement: function (fi, jqe, compact) {
			var topContainer = $("<div class='form-group' style='margin-right:10px;'></div>");
			if (fi.hasLabel) {
				var label = $(`<label style="margin-right:10px;">${fi.labelText}</label>`);
				topContainer.append(label);
			}
			jqe.addClass("form-control form-control-sm");
			topContainer.append(jqe);
			if (fi.isDate && jqe.prop("tagName") === "INPUT")
				jqe.kendoDatePicker();
			return topContainer;
		},

		CreateFormElement: function (fi, jqe, compact) {
			var topContainer = $("<div class='form-group' style='margin-right:10px;'></div>");
			if (fi.hasLabel) {
				var label = $(`<label style="margin-right:10px;">${fi.labelText}</label>`);
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
