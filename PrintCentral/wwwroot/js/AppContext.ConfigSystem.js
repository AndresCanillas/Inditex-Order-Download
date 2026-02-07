

// =========================================================================
// AppContext.Metadata
// =========================================================================
// #region AppContext.Metadata

AppContext.Metadata = (function () {
	var componentCache = { };

	return {
		GetSystemMetadata: function (systemName) {
			return new Promise((resolve, reject) => {
				$.ajax({
					type: "GET",
					url: `/meta/${systemName}`,
					success: (sysmeta) => {
						resolve(sysmeta);
					},
					error: (rq, status, error) => {
						reject({ "rq": rq, "status": status, "error": error });
					}
				});
			});
		},

		GetSystemComponents: function (systemName) {
			if (!componentCache[systemName]) {
				return new Promise((resolve, reject) => {
					$.getJSON(`/meta/${systemName}/components`, null, (result) => {
						componentCache[systemName] = result;
						resolve(componentCache[systemName]);
					});
				});
			}
			else {
				return new Promise((resolve, reject) => {
					resolve(componentCache[systemName]);
				});
			}
		},

		GetDefaultSystemConfig: function (systemName) {
			return new Promise((resolve, reject) => {
				$.ajax({
					type: "GET",
					url: `/meta/${systemName}/default`,
					success: (config) => {
						resolve(config);
					},
					error: (rq, status, error) => {
						reject({ "rq": rq, "status": status, "error": error });
					}
				});
			});
		}
	};
})();

// #endregion



// =========================================================================
// DynamicConfigForm
// =========================================================================
// #region DynamicConfigForm

AppContext.DynamicConfigForm = function (systemName, savedConfig) {
	this.systemName = systemName;
	this.savedConfig = savedConfig;
	this.element = $("<form />");
	this.model = {};
	var self = this;
	AppContext.Metadata.GetSystemMetadata(systemName).then((meta) => {
		self.meta = meta;
		if (savedConfig === null || savedConfig.length === 0) {
			AppContext.Metadata.GetDefaultSystemConfig(systemName).then((cfg) => {
				self.model = cfg;
				self._createFields(self.meta.Fields, self.element, self.model);
			});
		}
		else {
			self.model = JSON.parse(savedConfig);
			self._createFields(self.meta.Fields, self.element, self.model);
		}
	});
};

AppContext.DynamicConfigForm.prototype = {
	constructor: AppContext.DynamicConfigForm,

	Show: function (viewContainer) {
		if (viewContainer == null)
			throw "Invalid viewContainer";
		this.ViewContainer = AppContext.GetContainerElement(viewContainer);
		if (this.ViewContainer[0].CurrentView != null) {
			this.ViewContainer[0].CurrentView.Unload();
			this.ViewContainer[0].CurrentView = null;
		}
		this.ViewContainer.empty();
		this.ViewContainer.append(this.element);
		this.ViewContainer.find("input, select, textarea").first().focus();
		this.ViewContainer[0].CurrentView = self;
		this.DisableSaveButton(); 
	},
	DisableSaveButton: function () {
		let btn = document.querySelector("#save"); 
		if (document.querySelectorAll(".invalid").length > 0) {
			btn.classList.add("save-disabled");
		} else 
		{
            btn.classList.remove("save-disabled");
        }	
	},

	Save: function () {

	//	if (this.model.ValidState())
			return JSON.stringify(this.model);
		//return ""; 
	//	else return ""; 
	},

	Unload: function () {
		this.ViewContainer.empty();
		this.ViewContainer[0].CurrentView = null;
		this.element = null;
		this.model = null;
		this.ViewContainer = null;
	},

	_createFields: function (metadata, container, model) {
		for (var f of metadata) {
			if (this._hasFixedOptions(f)) {
				this._createSelect(f, container, model);
			}
			else {
				switch (f.Type) {
					case "Boolean":
						this._createBool(f, container, model);
						break;
					case "Byte":
						this._createByte(f, container, model);
						break;
					case "Char":
						this._createChar(f, container, model);
						break;
					case "Int32":
					case "Int64":
						this._createNumber(f, container, model);
						break;
					case "Single":
					case "Double":
					case "Decimal":
						this._createFloat(f, container, model);
						break;
					case "String":
						this._createString(f, container, model);
						break;
					case "DateTime":
						this._createDate(f, container, model);
						break;
					case "TimeSpan":
						this._createTime(f, container, model);
						break;
					default:
						if (f.Type.indexOf("List<") >= 0) {
							this._createList(f, container, model);
						}
						else if (f.SubFields !== null) {
							this._createComplex(f, container, model);
						}
						else {
							this._createComponent(f, container, model);
						}
						break;
				}
			}
		}

		this.DisableSaveButton(); 
	},


	_createBool: function (meta, container, model) {
		var div = $(`<div class="form-group row" />`);
		var ctrl = $(`<input type="checkbox" style="margin-right:5px;" ${model[meta.Name] ? "checked" : ""} />`);
		var lbl = $(`<label class="col-sm form-check-label">${meta.Caption}?</label>`);
		ctrl.change((e) => model[meta.Name] = e.target.checked);
		lbl.appendTo(div);
		ctrl.prependTo(lbl);
		div.appendTo(container);
		this._setupConstraints(ctrl, meta);
	},


	_createTextbox: function (meta, container, model) {
		var div = $("<div class='form-group' style='margin-bottom:10px;' />");
		var lbl = $(`<label for='${meta.Name}' class="col-form-label">${meta.Caption}:</label>`);
		let validable = ""; 
		let invalid = ""; 
		let constraints = this._createTextBoxConstraints(meta.Constraints.Items); 
		if (constraints) { 
			//validable = "validable"; 
			constraints = constraints + " action='onchange:_validateTextBox'"
			if (constraints.includes("Required") && (!model[meta.Name] || model[meta.Name].length == 0))
				invalid = "invalid"
		} 

		var ctrl = $(`<input ${constraints} type='text' name='${meta.Name}' class='form-control form-control-sm  ${invalid}' />`).val(model[meta.Name]);
		lbl.appendTo(div);
		ctrl.appendTo(div);
		if (constraints) {
			//ctrl.addEventListener("blur", this._validateTextBox);
			let span = $("<span class='error-message'></span>");
			span.appendTo(div);
		} 

		ctrl.change((e) => {
			model[meta.Name] = e.target.value
			if (constraints) {
                this._validateTextBox(e)
            }
		});
		div.appendTo(container);

		//this._setupConstraints(ctrl, meta);
		return ctrl;
	},

	_createTextBoxConstraints: function (constraints) {
		if (!constraints || constraints.length === 0) return;
		let stringConstraints = ""
		for (var c of constraints) {
			if (c.Data === null) {
				stringConstraints += " " + c.Type;

			} else {

				const jsonString = `{${c.Data.replace(/'/g, '"')}}`;
				let valor = JSON.parse(jsonString);
				stringConstraints += " " + c.Type + "='" + valor.value + "'";
			}
		}
		return stringConstraints;
	},

	_validateTextBox: function (event) 
	{
		const input = event.target;
		const value = input.value.trim();
		let isValid = true;
		let errorMessage = '';

		if (input.hasAttribute('required') && value === '') {
			isValid = false;
			errorMessage = 'This field is mandatory.';
		}
		if (input.hasAttribute('maxlength')) {
			const maxLength = parseInt(input.getAttribute('maxlength'), 10);
			if (value.length > maxLength) {
				isValid = false;
				errorMessage = `Max lenght  ${maxLength}.`;
			}
		}

		const errorElement = input.nextElementSibling;
		if (isValid) {
			input.classList.remove('invalid');
			input.classList.add('valid');
			errorElement.textContent = '';
		} else {
			input.classList.remove('valid');
			input.classList.add('invalid');
			errorElement.textContent = errorMessage;
		}
		this.DisableSaveButton();

	}, 
	_createByte: function (meta, container, model) {
		var ctrl = this._createTextbox(meta, container, model);
		ctrl[0].maxLength = 3;
		ctrl.addClass("col-4");
		ctrl.keyup((e) => {
			var intValue = parseInt(e.target.value, 10);
			if (isNaN(intValue) || intValue > 255 || intValue < 0) {
				ctrl.addClass("is-invalid");
			}
			else {
				model[meta.Name] = intValue;
				ctrl.removeClass("is-invalid");
			}
		});
	},


	_createNumber: function (meta, container, model) {
		var ctrl = this._createTextbox(meta, container, model);
		ctrl[0].maxLength = (meta.Type === "Int32") ? 8 : 14;
		ctrl.addClass("col-4");
		ctrl.keyup((e) => {
			var intValue = parseInt(e.target.value, 10);
			if (isNaN(intValue)) {
				ctrl.addClass("is-invalid");
			}
			else {
				model[meta.Name] = intValue;
				ctrl.removeClass("is-invalid");
			}
		});
	},


	_createFloat: function (meta, container, model) {
		var ctrl = this._createTextbox(meta, container, model);
		ctrl[0].maxLength = (meta.Type === "Single") ? 8 : 14;
		ctrl.addClass("col-4");
		ctrl.keyup((e) => {
			var floatValue = parseFloat(e.target.value);
			if (isNaN(floatValue)) {
				ctrl.addClass("is-invalid");
			}
			else {
				model[meta.Name] = floatValue;
				ctrl.removeClass("is-invalid");
			}
		});
	},


	_createChar: function (meta, container, model) {
		var ctrl = this._createTextbox(meta, container, model);
		ctrl.change((e) => model[meta.Name] = e.target.value);
		ctrl.addClass("col-4");
		ctrl[0].maxLength = 1;
	},


	_createString: function (meta, container, model) {
		var ctrl = this._createTextbox(meta, container, model);
		//ctrl.change((e) => model[meta.Name] = e.target.value);
	},


	_createDate: function (meta, container, model) {
		var ctrl = this._createTextbox(meta, container, model);
		ctrl.removeClass("form-control");
		ctrl.removeClass("form-control-sm");
		ctrl.change((e) => model[meta.Name] = e.target.value);
		var jqe = container.find(`[name="${meta.Name}"]`);
		jqe.kendoDatePicker();
	},


	_createTime: function (meta, container, model) {
		var ctrl = this._createTextbox(meta, container, model);
		ctrl[0].type = "time";
		ctrl.addClass("col-6");
		ctrl.change((e) => model[meta.Name] = e.target.value);
	},


	_createSelect: function (meta, container, model) {
		var topDiv = $("<div style='margin-bottom:10px'></div>");
		var lbl = $(`<label for='${meta.Name}'>${meta.Caption}:</label>`);
		lbl.appendTo(topDiv);
		var cbo = $("<select class='form-control form-control-sm col-6' />");
		cbo.appendTo(topDiv);
		var fixedOps = this._getFixedOptions(meta);
		for (var op of fixedOps) {
			var option = $("<option />").val(op.Value).text(op.Text);
			if (op.Value == model[meta.Name])
				option.attr("selected", "selected");
			cbo.append(option);
		}
		topDiv.appendTo(container);
		cbo.change((e) => {
			var selectedValue = e.target.value;
			model[meta.Name] = this._parseSelectValue(meta, selectedValue);
		});
	},


	_createComplex: function (meta, container, model) {
		var div = $("<div />");
		var hd = $("<h2 />").text(meta.Caption + ":");
		hd.appendTo(div);
		$("<hr/>").appendTo(div);
		div.appendTo(container);
		if (!model[meta.Name])
			model[meta.Name] = {};
		this._createFields(meta.SubFields, div, model[meta.Name]);
	},


	_createComponent: function (meta, container, model) {
		var topDiv = $("<div />");
		$("<label />", { "for": meta.Name, text: meta.Caption + ":", "class": "modal-form-title" }).appendTo(topDiv);
		var cbo = $("<select class='form-control form-control-sm' />");
		cbo.appendTo(topDiv);
		var subDiv = $("<div />", { "style": "margin-top:10px; margin-bottom:20px; margin-left:40px;" });
		subDiv.appendTo(topDiv);
		topDiv.appendTo(container);
		if (!model[meta.Name])
			model[meta.Name] = {};
		AppContext.Metadata.GetSystemComponents(this.systemName).then((components) => {
			var component = components.find((c) => c.Contract === meta.Type);
			if (component) {
				if (meta.Nullable) {
					const option = cbo.append("<option />").val(null);
					if (!model[meta.Name]._impl)
						option.attr("selected", "selected");
				}
				for (var impl of component.Implementations) {
					cbo.append($("<option />").val(impl.Implementation).text(impl.DisplayName));
				}
				if (model[meta.Name]._impl) {
					var selectedImpl = component.Implementations.find(p => p.Implementation == model[meta.Name]._impl);
					var option = cbo.find(`option[value="${selectedImpl.Implementation}"]`);
					option.attr("selected", "selected");
					this._createFields(selectedImpl.Config, subDiv, model[meta.Name]._data);
				}
				cbo.change((e) => {
					subDiv.empty();
					var selectedImpl = component.Implementations.find((c) => c.Implementation === e.target.value);
					if (selectedImpl) {
						model[meta.Name]._impl = selectedImpl.Implementation;
						if (!model[meta.Name]._data) model[meta.Name]._data = {};
						this._createFields(selectedImpl.Config, subDiv, model[meta.Name]._data);
					}
					else {
						model[meta.Name]._impl = null;
						model[meta.Name]._data = null;
					}
				});
			}
		});
	},


	_createList: function (meta, container, data) {
		if (!data[meta.Name])
			data[meta.Name] = [];
		var fields = this._createFieldDefinitions(meta);
		var model = data[meta.Name];
		var modelView = this._createModelView(meta, model);
		var isScalar = this._isSingleColumnType(meta);
		var gridContainer = $(`<div><label>${meta.Caption}:</label></div>`);
		var gridElement = $("<div>");
		gridContainer.append(gridElement);
		container.append(gridContainer);
		gridElement.jsGrid({
			confirmDeleting: false,
			rowClass: "small-grid-row",
			width: "99%",
			height: "200px",
			inserting: true,
			editing: true,
			sorting: true,
			paging: false,
			data: modelView,
			fields: fields,
			controller: {
				loadData: $.noop,
				insertItem: (e) => {
					e._rowIndex = model.length;
					if (isScalar)
						model.push(e.value);
					else
						model.push(e);
					return e;
				},
				updateItem: (e) => {
					if (isScalar)
						model[e._rowIndex] = e.value;
					else
						model[e._rowIndex] = e;
					return e;
				},
				deleteItem: (e) => {
					model.splice(e._rowIndex, 1);
					for (var i = e._rowIndex + 1; i < modelView.length; i++)
						modelView[i]._rowIndex--;
				}
			}
		});
	},


	_createFieldDefinitions: function (meta) {
		var fields = [];
		if (this._isSingleColumnType(meta)) {
			fields.push({
				name: "value",
				type: this._getGridType(meta),
				title: "Value",
				width: this._getColumnWidth(meta),
				validate: "required"
			});
		}
		else {
			if (meta.SubFields === null || meta.SubFields.length == 0) {
				fields.push({
					name: "value",
					type: "select",
					selectedIndex: -1,
					items: this._getFixedOptions(meta),
					title: "Value",
					valueField: "Value",
					textField: "Text",
					width: 180,
					validate: "required"
				});
			}
			else {
				for (var prop of meta.SubFields) {
					fields.push({
						name: prop.Name,
						type: this._getGridType(prop),
						items: this._getFixedOptions(prop),
						valueField: "Value",
						textField: "Text",
						title: prop.Caption,
						width: this._getColumnWidth(prop),
						validate: this._hasRequiredConstraint(prop) ? "required" : null
					});
				}
			}
		}
		fields.push({ type: "control" });
		return fields;
	},


	_createModelView: function (meta, model) {
		var modelView = [];
		var i = 0;
		for (var record of model) {
			var row = {};
			row._rowIndex = i;
			if (this._isSingleColumnType(meta)) {
				row.value = model[i]
			}
			else {
				for (var field of meta.SubFields) {
					if (model[i] && model[i][field.Name])
						row[field.Name] = model[i][field.Name];
				}
			}
			modelView.push(row);
			i++;
		}
		return modelView;
	},


	_isSingleColumnType: function (meta) {
		var fieldType = meta.Type;
		if (fieldType.indexOf("List<") === 0)
			fieldType = meta.Type.substring(5, meta.Type.length - 1);
		if ("Boolean,Byte,Char,Int32,Int64,Single,Double,Decimal,String,DateTime,TimeSpan".indexOf(fieldType) >= 0)
			return true;
		fieldType = meta.Type;
		if (fieldType.indexOf("List<") && (meta.SubFields === null || meta.SubFields.length == 0))
			return true;
		return false;
	},


	_getGridType: function (meta) {
		var fieldType = meta.Type;
		if (fieldType.indexOf("List<") === 0)
			fieldType = meta.Type.substring(5, meta.Type.length - 1);
		if (this._hasFixedOptions(meta))
			return "select";
		if ("Byte,Int32,Int64,Single,Double,Decimal".indexOf(fieldType) >= 0)
			return "number";
		if ("Char,String".indexOf(fieldType) >= 0)
			return "text";
		if (fieldType === "Boolean")
			return "checkbox";
		if (fieldType === "DateTime")
			return "datetime";
		if (fieldType === "TimeSpan")
			return "time";
		throw "Unsupported data type: " + fieldType;
	},


	_getColumnWidth: function (meta) {
		switch (meta.Type) {
			case "Boolean":
			case "Byte":
			case "Char":
				return 80;
			case "Int32":
			case "Int64":
			case "Single":
			case "Double":
			case "Decimal":
				return 100;
			case "DateTime":
			case "TimeSpan":
				return 120;
			case "String":
				return 180;
			default:
				return 80;
		}
	},


	_hasFixedOptions: function (meta) {
		if (!meta.Constraints || !meta.Constraints.Items)
			return false;
		if (meta.Constraints.Items.find(p => p.Type === "FixedOptions")) {
			var validTypes = ["Byte", "Char", "Int32", "Int64", "Single", "Double", "Decimal", "String"];
			if (validTypes.find(p => p == meta.Type))
				return true;
			else
				return false;
		}
		else return false;
	},


	_getFixedOptions: function (meta) {
		if (!meta.Constraints || !meta.Constraints.Items)
			return null;
		var item = meta.Constraints.Items.find(p => p.Type === "FixedOptions");
		if (item && item.Data && item.Data.length > 0) {
			return item.Data;
		}
		return null;
	},


	_hasRequiredConstraint: function (meta) {
		if (!meta.Constraints || !meta.Constraints.Items)
			return false;
		return meta.Constraints.Items.find(p => p.Type === "Required");
	},


	_parseSelectValue: function (meta, value) {
		switch (meta.Type) {
			case "Byte":
			case "Int32":
			case "Int64":
				return parseInt(value, 10);
			case "Single":
			case "Double":
			case "Decimal":
				return parseFloat(value);
			case "Char":
			case "String":
			default:
				return value;
		}
	},


	_setupConstraints: function (ctrl, meta) {
		if (!meta.Constraints || !meta.Constraints.Items) return;
		for (var c of meta.Constraints.Items) {
			switch (c.Type) {
				case "Readonly":
					ctrl.prop("readonly", "readonly");
					break;
				case "Hidden":
					ctrl.hide();
					break;
				case "Required": // todo
					break;
				case "FixedOptions": // todo
					break;
				case "MaxLength":
					ctrl.attr("maxlength", c.Value);
					break;
				case "Range": // todo
					break;
				case "ValidChars": // todo
					break;
				case "RegEx": // todo
					break;
				case "Password":
					ctrl.prop("type", "password");
					break;
			}
		}
	}
}
// #endregion
