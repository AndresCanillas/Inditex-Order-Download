var OrderGridView = function (element, controller) {
	element = AppContext.GetContainerElement(element)[0];
	element.className = "gvbody";
	var headers = new OrderGridHeaders(element);
	var rows = new OrderGridRows(element, headers, controller);
	element.appendChild(headers.Element);
	var shiftKey = false; // Indicates if SHIFT key is being pressed
	var ctrlKey = false; // Indicates if CTRL key is being pressed
	var editingContext = null;

	var rowClick = new AppEvent();
	var rowDoubleClick = new AppEvent();
	var cellClick = new AppEvent();
	var cellDoubleClick = new AppEvent();
	var headerClick = new AppEvent();
	var headerDoubleClick = new AppEvent();
	var beforeCellEdit = new AppEvent();
	var afterCellEdit = new AppEvent();
	var beforeShowContextMenu = new AppEvent();

	var template = $(element).find("template");
	if (template.length > 0) {
		setupFromTemplate($(template[0].content), controller);
		template.remove();
	}

	var mouseHeaderElm = null;
	var mouseX = 0;
	$(element).mousedown((e) => {

		if (e.target.className == "gvheader-grip") {
			mouseHeaderElm = $(e.target).prev();
			mouseX = e.pageX;
		}
		else {
			var target = e.target;
			var rowElm = $(target).closest(".gvrow");

			if (rowElm.length > 0) {
				rowElm = rowElm[0];

				var cellElm = $(target).closest(".gvcell")[0];
				var colIndex = cellElm.userData;
				var rowIndex = rowElm.userData;
				var headerData = headers.Get(colIndex);

				// if not selected, selected on middle mouse click and right click
				if (e.which > 1 && rows.IsSelected(rowIndex) == true) {
					return true;
				}




				if (rows.MultiSelect && (shiftKey || ctrlKey))
					rows.Select(rowIndex, false);
				else
					rows.Select(rowIndex, true);

				if (target.className.indexOf("gvcell") >= 0) {
					if (rowElm.className.indexOf("gvrowselected") < 0) {
						if (editingContext != null)
							editingContext.Accept();

					}
				}
				if (headerData.Editable && e.buttons == 1 &&
					(editingContext == null || editingContext.colIndex != colIndex))
					beginEdit(rowIndex, colIndex);
			}
		}
	});

	$(element).mousemove((e) => {
		if (mouseHeaderElm != null) {
			var index = mouseHeaderElm[0].userData;
			var header = headers.Get(index);
			var w = mouseHeaderElm.width() + (e.pageX - mouseX);
			mouseX = e.pageX;
			if (w > header.MinWidth) {
				mouseHeaderElm.width(`${w}px`);
			}
			e.preventDefault();
		}
	});

	$(document).mouseup((e) => {
		if (mouseHeaderElm != null) {
			var index = mouseHeaderElm[0].userData;
			var header = headers.Get(index);
			header.Width = mouseHeaderElm.width();
			headers.Update(header);
			mouseHeaderElm = null;
		}
	});

	$(document).keydown((e) => {
		shiftKey = (e.keyCode == 16);
		ctrlKey = (e.keyCode == 17);
	});

	$(document).keyup((e) => {
		shiftKey = e.keyCode == 16 ? false : shiftKey;
		ctrlKey = e.keyCode == 17 ? false : ctrlKey;
		if (e.keyCode == 9) e.stopPropagation();
	});

	$(element).click((e) => {
		if (e.target.className.indexOf("gvheader-cell") >= 0) {
			var index = e.target.userData;
			var header = headers.Get(index);
			e.colIndex = header.Index;
			e.headerData = headers.Get(header.Index);
			headerClick.Raise(e);
			if (e.isDefaultPrevented()) return;
			if (header.Sortable)
				headers.ToggleSorting(header.Index);
		}
		else {
			var target = e.target;
			var rowElm = $(target).closest(".gvrow");
			if (rowElm.length > 0) {
				rowElm = rowElm[0];
				var rowIndex = rowElm.userData;
				e.colIndex = target.userData;
				e.rowIndex = rowIndex;
				e.rowData = rows.Get(rowIndex);
				cellClick.Raise(e);
				if (e.isDefaultPrevented() === true) return;
				rowClick.Raise(e);
			}
		}
	});

	$(element).dblclick((e) => {
		if (e.target.className.indexOf("gvheader-cell") >= 0) {
			var index = e.target.userData;
			var header = headers.Get(index);
			e.colIndex = header.Index;
			e.headerData = headers.Get(header.Index);
			headerDoubleClick.Raise(e);
		}
		else {
			var closestRow = $(e.target).closest(".gvrow");
			if (closestRow.length > 0) {
				var rowIndex = closestRow[0].userData;
				e.colIndex = e.target.userData;
				e.rowIndex = rowIndex;
				e.rowData = rows.Get(rowIndex);
				cellDoubleClick.Raise(e);
				if (e.isDefaultPrevented() === true) return;
				rowDoubleClick.Raise(e);
			}
		}
	});

	$(element).scroll((e) => {
		if (editingContext != null && editingContext.dataType == "date")
			editingContext.Accept();
	});

	function setupFromTemplate(template, controller) {
		var columnElements = template.find("column");
		$.each(columnElements, function (index, item) {
			var header = {};
			var elm = $(item);
			header.Text = elm.text();
			if (elm.attr("minwidth") != null)
				header.MinWidth = elm.attr("minwidth");
			if (elm.attr("width") != null)
				header.Width = elm.attr("width");
			if (elm.attr("hidden") != null)
				header.Visible = false;
			if (elm.attr("field") != null)
				header.Field = elm.attr("field");
			if (elm.attr("type") != null)
				header.Type = elm.attr("type").toLowerCase();
			if (elm.attr("editable") != null)
				header.Editable = true;
			if (elm.attr("validations") != null)
				header.Validations = elm.attr("validations");
			if (elm.attr("sortable") != null)
				header.Sortable = true;
			if (elm.attr("map") != null) {
				header.Map = JSON.parse(elm.attr("map"));
			}
			if (elm.attr("render") != null && typeof controller[elm.attr("render")] == 'function') {

				header.Render = controller[elm.attr("render")];
			}


			headers.Add(header);
		});
		var menuElement = template.find("menu");
		if (menuElement.length > 0) {
			var menuItems = menuElement.find("menuitem");
			setupContextMenu(menuItems, controller);
		}
	}

	function setupContextMenu(menuItems, controller) {
		var ctxSetup = { MenuWidth: "250px", Options: [] }
		$.each(menuItems, function (index, item) {
			var op = {};
			if (item.innerText === "-") {
				op.IsSeparator = true;
			}
			else {
				op.Text = item.innerText;
				if (item.attributes["action"]) {
					var handler = controller[item.attributes["action"].value];
					if (handler)
						op.OnClick = handler;
				}
			}
			ctxSetup.Options.push(op);
		});
		var ctxMenu = new ContextMenu(ctxSetup);
		ctxMenu.Target = controller;
		$(element).mousemove(function (e) { ctxMenu.HandleMouseMove(e); });
		$(element).contextmenu(function (e) {
			// enable access to contextmenu object
			e.ContextMenu = ctxMenu;
			beforeShowContextMenu.Raise(e);

			// event can cancelled
			// https://www.w3.org/TR/DOM-Level-2-Events/events.html#Events-flow-cancelation
			if (e.isDefaultPrevented() || e.cancelled) return;

			ctxMenu.HandleContextMenu(e);
		});
	}

	function beginEdit(row, col) {
		if (row < 0 || row >= rows.Count || col < 0 || col >= headers.Count) {
			console.log(`Index out of bounds (${row}, ${col}), BeginEdit will be ignored`);
			return;
		}
		var headerData = headers.Get(col);
		if (!headerData.Editable) {
			console.log("Specified cell is not editable...");
			return;
		}
		if (rows.SelectedIndex != row)
			rows.Select(row);
		if (rows.SelectedIndex != row) {
			console.log("Row selection was cancelled, cancelling cell edition as well...");
			return;
		}
		if (editingContext != null) {
			console.log("Another cell is being edited, auto-acepting that edition...")
			editingContext.Accept();
			editingContext = null;
		}

		var rowData = rows.Get(row);
		var colData = rowData[headerData.Field];
		var cancelEvent = false;
		var cell = rows.GetCellElement(row, col);
		var e = {
			target: self,
			rowIndex: row,
			colIndex: col,
			headerData: headerData,
			field: headerData.Field,
			dataType: headerData.Type,
			rowData: rowData,
			currentValue: colData,
			proposedValue: rows.GetProposedValue(row, col),
			cellElement: cell,
			isValid: true,
			preventDefault: () => { cancelEvent = true; }
		}
		beforeCellEdit.Raise(e);
		if (cancelEvent) {
			console.log("BeforeCellEdit event handler cancelled the edition...");
			return;
		}

		editingContext = new OrderGridEditContext(e, (e) => {
			console.log(`Edition ended ${e.field}`)
			if (!e.isValid)
				rows.SetError(e.rowIndex, e.colIndex, e.proposedValue, e.errorMessage);
			else {
				afterCellEdit.Raise(e);
				if (!e.isValid)
					rows.SetError(e.rowIndex, e.colIndex, e.proposedValue, e.errorMessage);
				else {
					rows.ClearError(e.rowIndex, e.colIndex);
					if (e.rowData[e.field] != e.newValue) {
						e.rowData[e.field] = e.newValue;
						rows.IsEdited(e.rowIndex, true);
					}
					rows.UpdateCell(e.rowIndex, e.colIndex, e.proposedValue);
				}
			}
			editingContext = null;
		});
		rows.EnsureVisible(e.rowIndex, e.colIndex);
		editingContext.BeginEdit();
	}

	function editNext(row, col, forward) {
		var nextRow = row;
		var nextCol = headers.GetNextEditable(col, forward);
		if (nextCol < 0) return; // No columns are editable
		if (forward) { if (nextCol <= col) nextRow++; }
		else { if (nextCol >= col) nextRow--; }
		beginEdit(nextRow, nextCol);
	}


	var self = {
		constructor: OrderGridView,

		// Events
		get RowClick() { return rowClick; },
		get RowDoubleClick() { return rowDoubleClick; },
		get CellClick() { return cellClick; },
		get CellDoubleClick() { return cellDoubleClick; },
		get HeaderClick() { return headerClick; },
		get HeaderDoubleClick() { return headerDoubleClick; },
		get BeforeCellEdit() { return beforeCellEdit; },
		get AfterCellEdit() { return afterCellEdit; },
		get BeforeShowContextMenu() { return beforeShowContextMenu; },

		// Methods & Properties
		get Element() { return element; },
		get Headers() { return headers; },
		get Rows() { return rows; },

		Dispose: function () {
			rowClick.Dispose();
			rowDoubleClick.Dispose();
			cellClick.Dispose();
			cellDoubleClick.Dispose();
			headerClick.Dispose();
			headerDoubleClick.Dispose();
			beforeCellEdit.Dispose();
			afterCellEdit.Dispose();
			rows.Dispose();
			headers.Dispose();
			beforeShowContextMenu.Dispose();
		},

		Clear: function () {
			headers.Clear();
			rows.Clear();
		},

		BeginEdit: function (row, col) {
			beginEdit(row, col);
		},

		EditNext: function (row, col, forward) {
			editNext(row, col, forward);
		},

		IsEdited: function () {
			for (var i = 0; i < rows.Count; i++) {
				if (rows.IsEdited(i)) return true;
			}
			return false;
		},

		ValidState: function () {
			return rows.ValidState();
		}
	};
	return self;
};





// ==================================================================
// OrderGridHeaders
// ==================================================================
//#region OrderGridHeaders
var OrderGridHeaders = function (container) {
	var headers = [];
	var element = document.createElement("div");
	element.className = "gvheader";
	var added = new AppEvent();
	var updated = new AppEvent();
	var removed = new AppEvent();
	var sortingChanged = new AppEvent();
	var headerWidth = 0;
	var isSorted = false;
    var highlightColor = "#a8cfa0";

	//#region Private Methods
	function createHeader(data) {
		var index = headers.length;
		var elm = document.createElement("div");
		elm.className = "gvheader-cell";
		elm.innerText = "Column";
		var sortElm = document.createElement("div");
		sortElm.className = "gvheader-sort";
		elm.appendChild(sortElm);
		var grip = document.createElement("div");
		grip.className = "gvheader-grip";
		var header = {
			Element: elm,
			SortElement: sortElm,
			GripElement: grip,
			Data: {
				Index: index, Field: "", Text: "Column",
				Width: 100, MinWidth: 30, Visible: true,
				IsHighlighted: false, Type: "String",
				Editable: false, Map: null, Validations: {},
				Sortable: false, SortPosition: 0, SortDirection: "",
				Render: null
			}
		};
		elm.userData = index;
		headers.push(header);
		updateHeaderInfo(header, data);
		element.appendChild(elm);
		element.appendChild(grip);
		return header;
	}

	function updateHeaderInfo(header, data) {
		var elm = header.Element;
		if (data.hasOwnProperty("Text")) {
			header.Data.Text = data.Text;
			elm.innerText = data.Text;
			elm.appendChild(header.SortElement);
		}
		if (data.hasOwnProperty("MinWidth")) {
			var minWidth = parseInt(data.MinWidth, 10);
			if (minWidth <= 0)
				minWidth = 1;
			header.Data.MinWidth = minWidth;
		}
		if (data.hasOwnProperty("Width")) {
			var width = parseInt(data.Width, 10);
			if (width < header.Data.MinWidth)
				width = header.Data.MinWidth;
			header.Data.Width = width;
			elm.style.width = `${header.Data.Width}px`;
		}
		if (data.hasOwnProperty("Visible")) {
			header.Data.Visible = data.Visible == true;
			if (header.Data.Visible)
				elm.style.display = "";
			else
				elm.style.display = "none";
		}
		if (data.hasOwnProperty("Field"))
			header.Data.Field = data.Field;
		if (data.hasOwnProperty("Type")) {
			var dtype = data.Type.toLowerCase();
			if ("reference,string,bool,int,long,decimal,date,image,password".indexOf(dtype) < 0)
				throw "Invalid data type: " + data.Type;
			header.Data.Type = dtype;
		}
		if (data.hasOwnProperty("Editable"))
			header.Data.Editable = data.Editable == true;
		if (data.hasOwnProperty("Map")) {
			var map = data.Map;
			if (typeof map == "string") {
                if (!String.IsNullOrEmpty(map))
					header.Data.Map = JSON.parse(map);
				else
					header.Data.Map = null;
			}
			else header.Data.Map = data.Map;
		}
		if (data.hasOwnProperty("Validations")) {
			if (typeof data.Validations == "string") {
                if (!String.IsNullOrEmpty(data.Validations))
					header.Data.Validations = parseValidations(data.Validations);
				else
					header.Data.Validations = {};
			}
			else header.Data.Validations = data.Validations;
		}
		if (data.hasOwnProperty("Sortable"))
			header.Data.Sortable = data.Sortable == true;
		if (data.hasOwnProperty("SortPosition"))
			header.Data.SortPosition = data.SortPosition;
		if (data.hasOwnProperty("SortDirection")) {
			if (!isValidSortDirection(data.SortDirection))
				throw "Invalid SortDirection: " + data.SortDirection;
			header.Data.SortDirection = data.SortDirection;
		}

		if (data.hasOwnProperty("Render")) {
			header.Data.Render = data.Render;
		}
		updateSortIcon(header);
	}

	function parseValidations(str) {
		var result = {};
        if (!String.IsNullOrEmpty(str)) {
			var tokens = str.split(";");
			for (var i = 0; i < tokens.length; i++) {
				tokens[i] = tokens[i].trim();
				var validation = tokens[i];
				var validationArguments = null;
				var idx1 = validation.indexOf("(");
				var idx2 = validation.indexOf(")", idx1);
				while (idx2 > idx1 && idx2 < validation.length && validation[idx2 - 1] == "\\") {
					idx2 = validation.indexOf(")", idx2 + 1);
				}
				if (idx1 > 0 && idx2 > idx1) {
					validation = tokens[i].substring(0, idx1);
					validationArguments = tokens[i].substring(idx1 + 1, idx2).replace("\\\\", "\\").replace("\\)", ")");
				}
				addValidation(result, validation, validationArguments);
			}
		}
		return result;
	}

	function addValidation(obj, validation, arguments) {
        if (String.IsNullOrEmpty(validation)) return;
		var vName = validation.toLowerCase();
		switch (vName) {
			case "required":
				obj["required"] = { Value: true };
				break;
			case "numeric":
				obj["numeric"] = { Value: true };
				break;
			case "maxlen":
				obj["maxlen"] = { Value: parseInt(arguments, 10) };
				break;
			case "range":
				var tokens = arguments.split(",");
				if (tokens.length >= 2)
					obj["range"] = { MinValue: parseInt(tokens[0], 10), MaxValue: parseInt(tokens[1], 10) };
				break;
			case "chars":
				obj["chars"] = { Chars: arguments };
				break;
			case "regex":
				obj["regex"] = { Regex: new RegExp(arguments) };
				break;
		}
	}

	function isValidSortDirection(dir) {
		return !(dir.length > 0 && "ASC,DESC".indexOf(dir) < 0);
	}

	function clearSorting() {
		for (var i = 0; i < headers.length; i++) {
			var header = headers[i];
			header.Data.SortPosition = 0;
			header.Data.SortDirection = "";
			header.SortElement.innerHTML = "";
		}
		isSorted = false;
	}

	function setSorting(sortConfig) {
		var i = 1;
		for (var prop in sortConfig) {
			if (isValidSortDirection(sortConfig[prop])) {
				var header = headers.first(p => p.Data.Field == prop);
				if (header != null) {
					header.Data.Sortable = true;
					header.Data.SortPosition = i;
					header.Data.SortDirection = sortConfig[prop];
					if (header.Data.SortDirection != "")
						isSorted = true;
					updateSortIcon(header);
					i++;
				}
			}
		}
	}

	function assignSortPosition() {
		var pos = 1;
		for (var i = 0; i < headers.length; i++) {
			var header = headers[i];
			if (header.Data.Sortable && header.Data.SortDirection != "") pos++;
		}
		isSorted = true;
		return pos;
	}

	function clearHeaderSorting(targetHeader) {
		for (var i = 0; i < headers.length; i++) {
			var header = headers[i];
			if (header.Data.Sortable && header.Data.SortDirection != "" && header.Data.SortPosition > targetHeader.Data.SortPosition) {
				header.Data.SortPosition--;
				updateSortIcon(header);
			}
		}
		targetHeader.Data.SortDirection = "";
		targetHeader.Data.SortPosition = 0;
		updateSortIcon(targetHeader);
		if (assignSortPosition() == 1)
			isSorted = false;
	}

	function updateSortIcon(header) {
		if (header.Data.Sortable && header.Data.SortDirection != "") {
			if (header.Data.SortDirection == "ASC")
				header.SortElement.innerHTML = `<span class='fa fa-arrow-up'></span>${header.Data.SortPosition}`;
			else
				header.SortElement.innerHTML = `<span class='fa fa-arrow-down'></span>${header.Data.SortPosition}`;
		}
		else header.SortElement.innerHTML = "";
	}

	function updateHighlightColor() {
		for (var i = 0; i < headers.length; i++) {
			if (headers[i].Element.style.backgroundColor != "") {
				headers[i].Element.style.backgroundColor = highlightColor;
				if (i > 0) headers[i - 1].GripElement.style.backgroundColor = highlightColor;
			}
		}
	}

	function resetHeaderHighlight(index) {
		headers[index].Element.style.backgroundColor = "";
		if (index > 0) headers[index - 1].GripElement.style.backgroundColor = "";
	}

	function resetAllHeadersHightlight() {
		for (var i = 0; i < headers.length; i++)
			resetHeaderHighlight(i);
	}

	function getCaption(column, lang) {
        if (String.IsNullOrEmpty(column.Captions))
			return column.Name;
		var captions = JSON.parse(column.Captions);
		var caption = captions.first(c => c.Language == lang);
		if (caption == null) return column.Name;
		return caption.Text;
	}

	function getDataType(type) {
		switch (parseInt(type, 10)) {
			case 1: return "reference";
			case 2: return "int";
			case 3: return "long";
			case 4: return "decimal";
			case 5: return "bool";
			case 6: return "date";
			case 7: return "string";
			case 8: return "image";
			case 9: return "set";
			case 10: return "file";
			default: return "string";
		}
	}

	function getColWidth(type) {
		switch (type) {
			case 1: return 120;
			case 2:
			case 3:
			case 4: return 80;
			case 5: return 60;
			case 6: return 100;
			case 7: return 120;
			case 8: return 40;
			case 9: return 100;
			case 10: return 40;
			default: return 80;
		}
	}

	function getEditable(type) {
		if (type >= 2 && type <= 7)
			return true;
		return false;
	}

	function getValidations(field) {
		var validations = "";
		switch (parseInt(field.Type, 10)) {
			case 1: // reference type (1) cannot be edited inline, so no need to validate anything
				break;
			case 2: // int
			case 3: // long
			case 4: // decimal
				if (field.Length != null && field.Length > 0)
					validations += `maxlen(${field.Length});`;
				if (field.CanBeEmpty === false)
					validations += "required;";
				if (field.MinValue != null && field.MaxValue != null)
					validations += `range(${field.MinValue},${field.MaxValue});`;
				break;
			case 5: // bool
				break;
			case 6: // date
				if (field.CanBeEmpty === false)
					validations += "required;";
				//if (field.MinDate != null && field.MaxDate != null)   //TODO: range() validation does not support dates...
				//	validations += `range(${field.MinValue},${field.MaxValue});`;
				break;
			case 7: // string
				if (field.Length != null && field.Length > 0)
					validations += `maxlen(${field.Length});`;
				if (field.CanBeEmpty === false)
					validations += "required;";
				if (field.ValidChars != null && field.ValidChars.length > 0)
					validations += `chars(${field.ValidChars});`;
				if (field.Regex != null && field.Regex.length > 0)
					validations += `regex(${field.Regex});`;
				break;
			case 8: // image (8) & set (9) cannot be edited inline...
			case 9:
				break;
		}
		return validations;
	}

	function getMap(field) {
		return null;
	}
	//#endregion

	var self = {
		constructor: OrderGridHeaders,

		// exposed events
		get Added() { return added; },
		get Updated() { return updated; },
		get Removed() { return removed; },
		get SortingChanged() { return sortingChanged; },

		// exposed properties
		get Element() { return element; },
		get Count() { return headers.length; },
		get Width() { return headerWidth; },
		get IsSorted() { return isSorted; },
		get HighlightColor() { return highlightColor; },
		set HighlightColor(value) {
			highlightColor = value;
			updateHighlightColor();
		},

		Dispose: function () {
			for (var i = 0; i < headers.length; i++) {
				headers[i].Element.remove();
				headers[i].GripElement.remove();
			}
			element.remove();
			element = null;
			headers = [];
			added.Dispose();
			updated.Dispose();
			removed.Dispose();
		},

		// exposed methods
		Each: function (thisArg, fn) {
			for (var i = 0; i < headers.length; i++) {
				var headerCopy = Object.assign({}, headers[i].Data);
				if (thisArg != null && typeof thisArg === "function")
					thisArg(i, headerCopy);
				else
					fn.call(thisArg, i, headerCopy);
			}
		},

		Get: function (index) {
			if (index >= 0 && index < headers.length) {
				var headerCopy = Object.assign({}, headers[index].Data);
				return headerCopy;
			}
			else return null;
		},

		Find: function (fieldName) {
			var h = headers.first(p => p.Data.Field == fieldName);
			var headerCopy = Object.assign({}, h.Data);
			return headerCopy;
		},

		Add: function (data) {
			var header = createHeader(data);
			headerWidth += header.Data.Width + 5;
			added.Raise({ target: self, colIndex: header.Data.Index, headerData: header.Data });
		},

		Update: function (data) {
			var header = headers[data.Index];
			updateHeaderInfo(header, data);
			headerWidth = 0;
			for (var i = 0; i < headers.length; i++)
				headerWidth += headers[i].Data.Width + 5;
			updated.Raise({ target: self, colIndex: header.Data.Index, headerData: header.Data });
		},

		Remove: function (index) {
			var header = headers[index];
			headers.splice(index, 1);
			for (var i = index; i < headers.length; i++)
				headers[i].Data.Index--;
			header.Element.removeChild(header.SortElement)
			element.removeChild(header.GripElement)
			element.removeChild(header.Element);
			header.Element = null;
			header.SortElement = null;
			header.GripElement = null;
			headerWidth -= header.Data.Width - 5;
			removed.Raise({ target: self, colIndex: index, headerData: header.Data });
		},

		Clear: function () {
			while (headers.length > 0)
				this.Remove(0);
		},

		SetSorting: function (sortConfig) {
			clearSorting();
			setSorting(sortConfig);
			sortingChanged.Raise({ target: self })
		},

		ClearSorting: function () {
			clearSorting();
			sortingChanged.Raise({ target: self })
		},

		GetSorting: function () {
			var cfg = {};
			var sortFields = [];
			for (var i = 0; i < headers.length; i++) {
				var header = headers[i];
				if (header.Data.Sortable && header.Data.SortDirection != "")
					sortFields.push({ field: header.Data.Field, pos: header.Data.SortPosition, dir: header.Data.SortDirection });
			}
			sortFields.sort((a, b) => a.pos - b.pos);
			for (var i = 0; i < sortFields.length; i++)
				cfg[sortFields[i].field] = sortFields[i].dir;
			return cfg;
		},

		ToggleSorting: function (index) {
			var header = headers[index];
			if (header.Data.Sortable) {
				if (header.Data.SortDirection == "") {
					header.Data.SortPosition = assignSortPosition();
					header.Data.SortDirection = "ASC";
				}
				else if (header.Data.SortDirection == "ASC")
					header.Data.SortDirection = "DESC";
				else
					clearHeaderSorting(header);
				updateSortIcon(header);
				sortingChanged.Raise({ target: self });
			}
		},

		Highlight: function (index) {
			headers[index].Element.style.backgroundColor = highlightColor;
			if (index > 0) headers[index - 1].GripElement.style.backgroundColor = highlightColor;
		},

		IsHighlighted: function (index) {
			return (headers[index].Element.style.backgroundColor != "");
		},

		ResetHighlight: function (index) {
			if (index != null)
				resetHeaderHighlight(index);
			else
				resetAllHeadersHightlight();
		},

		EnsureVisible: function (index) {
			var header = headers[index];
			var e = $(header.Element);
			var right = e.position().left + e.width();
			if (container.scrollLeft < right)
				container.scrollLeft = right;
			if (container.scrollTop > 0)
				container.scrollTop = 0;
		},

		GetNextEditable: function (startIdx, forward) {
			if (forward) {
				for (var i = startIdx + 1; i < headers.length; i++)
					if (headers[i].Data.Editable) return i;
				for (var i = 0; i <= startIdx; i++)
					if (headers[i].Data.Editable) return i;
				return -1;
			}
			else {
				for (var i = startIdx - 1; i >= 0; i--)
					if (headers[i].Data.Editable) return i;
				for (var i = headers.length - 1; i >= startIdx; i--)
					if (headers[i].Data.Editable) return i;
				return -1;
			}
		},

		SetupFromCatalogDef: function (def, readonly, lang) {
			this.Clear();
			for (var i = 0; i < def.length; i++) {
				var column = def[i];                       //columnType     9 = Set      8 = Image     10 = File
				if (!column.IsHidden && column.Name != "ID" && column.Type != 9 && column.Type != 8 && column.Type != 10) {
					this.Add({
						Field: column.Name,
						Text: getCaption(column, lang),
						Type: getDataType(column.Type),
						Width: getColWidth(column.Type),
						Editable: !readonly && getEditable(column.Type),
						Validations: getValidations(column),
						Map: getMap(column),
						Sortable: true
					});
				}
			}
		}
	};
	return self;
}
//#endregion





// ==================================================================
// OrderGridRows
// ==================================================================
//#region OrderGridRows
var OrderGridRows = function (container, headers, controller) {
	var dataSource = [];	// reference to the user data source (set by LoadData)
	var dataIndex = [];		// sorted index into the data source (used for determining display order)
	var rows = [];			// contains the information of each row being displayed in the grid and references to the html elements
	var selectedIndex = -1;	// The index of the last row that was selected (0-N), or -1 if nothing is selected.
	var multiselect = false;
    var highlightColor = "#a8cfa0";
	var beforeSelectedChanged = new AppEvent();
	var afterSelectedChanged = new AppEvent();
	var beforeAdded = new AppEvent();
	var afterAdded = new AppEvent();
	var beforeUpdated = new AppEvent();
	var afterUpdated = new AppEvent();
	var beforeRemoved = new AppEvent();
	var afterRemoved = new AppEvent();
	var afterLoadRow = new AppEvent();
	var customRowCssClass = new AppEvent();
	var loaded = new AppEvent();
	headers.Updated.Subscribe(handleHeaderUpdated);
	headers.SortingChanged.Subscribe(handleSorting);

	var _defaultRowClass = '';

	var attrRowCssClass = $(container).attr('rowcssclass');

	//#region Private Methods


	// Adjusts the width of each row and the cell associated to the header that was updated.
	function handleHeaderUpdated(e) {
		updateVisibility(e.headerData);
		if (e.headerData.Visible) updateWidth(e.headerData);
	}

	function updateVisibility(header) {
		var value = header.Visible ? "" : "none";
		for (var i = 0; i < rows.length; i++)
			rows[i].Element.children[header.Index].style.display = value;
	}

	function updateWidth(header) {
		var value = `${header.Width}px`;
		for (var i = 0; i < rows.length; i++) {
			rows[i].Element.children[header.Index].style.width = value;
			rows[i].Element.style.width = `${headers.Width}px`;
		}
	}



	// Adds (or removes) 'q' elements from the grid... Pass a negative value to remove elements.
	function _getRowCssClass() {
		return _defaultRowClass;
	}

	function _getControllerRowCssClass(rowIdx) {

		var rowData = dataSource[rowIdx];

		return controller[attrRowCssClass](rowIdx, rowData)
	}

	var GetRowCssClass = _getRowCssClass;



	if (attrRowCssClass != null) {

		// if function
		if (typeof controller[attrRowCssClass] == 'function') {
			GetRowCssClass = _getControllerRowCssClass;
		} else {
			_defaultRowClass = attrRowCssClass;
		}


	}

	function addRowElements(q) {
		if (q == 0) return;
		if (q > 0) {

			for (var i = 0; i < q; i++) {
				var cssRowClass = GetRowCssClass(i);
				addRowElement(cssRowClass);
			}
		}
		else {
			var elms = rows.splice(rows.length + q, -q);
			for (var i = 0; i < elms.length; i++) deleteRowElement(elms[i]);
		}
	}

	function addRowElement(rowCssClass) {

		if (rowCssClass == undefined) {
			rowCssClass = _defaultRowClass;
		}

		var elm = document.createElement("div");
		elm.className = ["gvrow", rowCssClass].join(' ');
		elm.style.width = `${headers.Width}px`;
		var row = {
			Index: rows.length, Element: elm, Selected: false, IsEdited: false, Errors: {}
		};
		elm.userData = row.Index;
		var hcount = headers.Count;
		for (var i = 0; i < hcount; i++) {
			var cell = document.createElement("div");
			cell.userData = i;
			cell.className = "gvcell";
			var content = document.createElement("div");
			content.className = "gvcellcontent";
			content.userData = i;
			cell.appendChild(content);
			elm.appendChild(cell);
		}

		container.appendChild(row.Element);
		rows.push(row);
		return row;
	}

	function deleteRowElement(row) {
		container.removeChild(row.Element);
		row.Element.innerHTML = "";
		row.Element.userData = null;
		row.Element = null;
		rows.splice(row.Index, 1);
		for (var i = row.Index; i < rows.length; i++) {
			var rowItem = rows[i];
			rowItem.Index--;
			rowItem.Element.userData = rowItem.Index;
		}
	}

	function updateRows() {
		for (var i = 0; i < rows.length; i++) {
			updateRowInfo(i);
		}
	}

	function updateRowInfo(j) {
		var row = rows[j];
		var data = dataIndex[j].Data;
		var cells = row.Element.childNodes;

		headers.Each((i, header) => {
			var cell = cells[i];
			var visibility = header.Visible ? "" : "none";
			var width = `${header.Width}px`;
			cell.style.display = visibility;
			cell.style.width = width;
            if (!String.IsNullOrEmpty(header.Field) && data.hasOwnProperty(header.Field)) {
				var value = data[header.Field];
				if (value != null) {
					var propName = value.toString();
					if (header.Map != null && header.Map.hasOwnProperty(propName))
						value = header.Map[propName];
				}
				var content = cell.childNodes[0];
				if (content.editing)
					return;
				if (rows[j].Errors[i] != null) {
					content.style.color = "red";
					content.innerText = rows[j].Errors[i].ProposedValue;
				}
				else {
					content.style.color = "";


					// render option exist? use that, else try to display by type
					if (typeof header.Render == "function") {

						value = header.Render.call(controller, value, j, i, header.Type, data)

						// how to check if response is text or html, use like html for flexibility
						content.innerHTML = value;
					}
					else if (header.Type == "bool") {
						if (value)
							content.innerHTML = "<span class='fa fa-check-square-o'></span>";
						else
							content.innerHTML = "<span class='fa fa-square-o'></span>";
					}
					else if (header.Type == "date") {
						if (value != null && !(value instanceof Date)) {
							value = new Date(value);
							data[header.Field] = value;
						}

						value = (value != null) ? value.toLocaleDateString() : "";
						content.innerText = value;
					}
					else if (header.Type == "reference") {
						value = data["_" + header.Field + "_DISP"];
						content.innerText = value;
					}
					//else if (header.Type == "password") {
					//    value = (value != null) ? value.toString() : "";
					//    content.innerHTML = `<input type='Password' value='${value}' class="gveditbox" style="color:#000" />`;
					//}
					else {
						if (header.Type == "password") {
							for (var i = 1; i <= value.length; i++) {
								content.innerText += "*";
							}
						} else {
							value = (value != null) ? value.toString() : "";
							content.innerText = value;
						}
					}
				}
            } else if (!String.IsNullOrEmpty(header.Field) && !data.hasOwnProperty(header.Field) && typeof header.Render == 'function') {
				var content = cell.childNodes[0];

				value = header.Render.call(controller, value, j, i, header.Type, data)

				// how to check if response is text or html, use like html for flexibility
				content.innerHTML = value;
			}
		});


		AppContext.BindActions($(row.Element), controller);
	}

	function handleSorting(e) {
		resetIndex();
		if (headers.IsSorted) {
			var sortConfig = headers.GetSorting();
			dataIndex.sort((a, b) => {
				for (var prop in sortConfig) {
					var dir = sortConfig[prop];
					if (dir != "") {
						var avalue = a.Data[prop];
						var bvalue = b.Data[prop];
						if (avalue != null && bvalue != null) {
							if (avalue < bvalue)
								return dir == "ASC" ? -1 : 1;
							else if (avalue > bvalue)
								return dir == "ASC" ? 1 : -1;
						}
					}
				}
				return 0;
			});
		}
		updateRows();
	}

	function resetIndex() {
		dataIndex = [];
		for (var i = 0; i < dataSource.length; i++)
			dataIndex.push({ Index: i, Data: dataSource[i] });
	}

	function clearSelection(raiseEvt) {
		if (raiseEvt) {
			var evt = raiseBeforeSelectedChanged();
			if (evt.cancelled) return;
		}
		for (var i = 0; i < rows.length; i++)
			clearSelected(rows[i], false);
		selectedIndex = -1;
		if (raiseEvt) raiseAfterSelectedChanged();
	}

	function clearSelected(row, raiseEvt) {
		if (row.Selected) {
			if (raiseEvt) {
				var evt = raiseBeforeSelectedChanged(row);
				if (evt.cancelled) return;
			}
			row.Selected = false;
			row.Element.className = row.Element.className.replace(" gvrowselected", "");
			if (raiseEvt) raiseAfterSelectedChanged();
		}
	}

	function raiseBeforeSelectedChanged(row, addSelection) {

		if (addSelection == undefined) {
			addSelection = false;
		}

		var evt = {}

		if (row == undefined) {

			var evt = {
				target: self,
				cancelled: false,
				rowIndex: selectedIndex,
				rowData: {},
				addSelection: addSelection,
				preventDefault: () => { evt.cancelled = true; }
			};

		} else {

			var evt = {
				target: self,
				cancelled: false,
				rowIndex: row.Index,
				rowData: self.Get(row.Index),
				addSelection: addSelection,
				preventDefault: () => { evt.cancelled = true; }
			};

		}

		beforeSelectedChanged.Raise(evt);

		return evt;
	}

	function raiseAfterSelectedChanged() {
		var evt = {
			target: self,
			rowIndex: selectedIndex,
			rowData: self.Get(selectedIndex),
			preventDefault: () => { }
		};
		afterSelectedChanged.Raise(evt);
	}

	function setSelection(row, clearPrevious) {
		var evt = raiseBeforeSelectedChanged(row, !clearPrevious);
		if (evt.cancelled) return;
		if (!multiselect || clearPrevious) clearSelection(false);

		if (row.Selected == true) {
			clearSelected(row, true);
		} else {
			row.Selected = true;
			row.Element.className += " gvrowselected";
			selectedIndex = row.Index;
			raiseAfterSelectedChanged();
		}


	}

	function updateHighlightColor() {
		for (var i = 0; i < rows.length; i++) {
			var e = rows[i].Element;
			if (e.style.backgroundColor != "")
				e.style.backgroundColor = highlightColor;
			for (var j = 0; j < e.childNodes.length; j++) {
				var c = e.childNodes[j];
				if (c.style.backgroundColor != "") c.style.backgroundColor = highlightColor;
			}
		}
	}

	function resetRowHighlight(rowIdx) {
		var elm = rows[rowIdx].Element;
		elm.style.backgroundColor = "";
		for (var j = 0; j < elm.childNodes.length; j++)
			elm.childNodes[j].style.backgroundColor = "";
	}

	function resetGridHighlight() {
		for (var i = 0; i < rows.length; i++) {
			resetRowHighlight(i);
		}
	}

	function ensureCellVisible(rowIdx, colIdx) {
		var cell = $(rows[rowIdx].Element.childNodes[colIdx]);
		var right = 0;
		headers.Each((index, header) => { if (index < colIdx) right += header.Width + 5; });
		var bottom = cell.height() * rowIdx;
		var visibleRight = container.scrollLeft + container.clientWidth;
		var visibleBottom = container.scrollTop + container.clientHeight + 25;
		if (right > visibleRight || right < container.scrollLeft)
			container.scrollLeft = right;
		if (bottom > visibleBottom || bottom < container.scrollTop)
			container.scrollTop = bottom;
	}

	function ensureRowVisible(rowIdx) {
		var row = $(rows[rowIdx].Element);
		var bottom = row.height() * rowIdx;
		container.scrollLeft = 0;
		container.scrollTop = bottom;
	}
	//#endregion 


	var self = {
		constructor: OrderGridRows,

		// Events
		get BeforeSelectedChanged() { return beforeSelectedChanged; },
		get AfterSelectedChanged() { return afterSelectedChanged; },
		get BeforeAdded() { return beforeAdded; },
		get AfterAdded() { return afterAdded; },
		get Updated() { return updated; },
		get BeforeRemoved() { return beforeRemoved; },
		get AfterRemoved() { return afterRemoved; },
		get AfterLoadRow() { return afterLoadRow; },

		// Methods and Properties
		get Count() { return rows.length; },
		get MultiSelect() { return multiselect; },
		set MultiSelect(value) { multiselect = (value == true); },
		get HighlightColor() { return highlightColor; },
		set HighlightColor(value) {
			highlightColor = value;
			updateHighlightColor();
		},
		get SelectedIndex() { return selectedIndex; },
		get DataSource() { return dataSource; },

		get DataLoaded() { return loaded; },

		Dispose: function () {
			for (var i = 0; i < rows.length; i++)
				rows[i].Element.remove();
			beforeSelectedChanged.Dispose();
			afterSelectedChanged.Dispose();
			beforeAdded.Dispose();
			afterAdded.Dispose();
			beforeUpdated.Dispose();
			afterAdded.Dispose();
			beforeRemoved.Dispose();
			afterRemoved.Dispose();
			loaded.Dispose();
			rows = [];
			dataSource = [];
			dataIndex = [];
		},

		LoadData: function (data, redraw) {
			selectedIndex = -1;
			dataSource = data;

			if (redraw == undefined) {
				redraw = false;
			}

			resetIndex();

			if (redraw == true) {
				var totalRows = rows.length

				while (totalRows--) {
					deleteRowElement(rows[totalRows])
				}

				rows = [];
			}

			if (data.length != rows.length)
				addRowElements(data.length - rows.length);
			if (headers.IsSorted)
				handleSorting();
			else {

				//updateRows();

				for (var i = 0; i < rows.length; i++) {

					var e = {
						taget: self,
						rowIndex: i,
						rowData: $.extend({}, dataSource[i]), // readonly copy
						preventDefault: function () { cancelled = true; }
					};

					updateRowInfo(i);

					afterLoadRow.Raise(e);

				}

				var evt = {
					target: self
				};

				this.DataLoaded.Raise(evt);
			}
		},

		Each: function (thisArg, fn) {
			for (var i = 0; i < rows.length; i++) {
				var rowCopy = Object.assign({}, dataIndex[i].Data);
				if (thisArg != null && typeof thisArg === "function")
					thisArg(i, rowCopy);
				else
					fn.call(thisArg, i, rowCopy);
			}
		},

		Get: function (idx) {
			if (idx >= 0 && idx < dataIndex.length) {
				var rowCopy = Object.assign({}, dataIndex[idx].Data);
				return rowCopy;
			}
			else return null;
		},

		GetSelected: function () {
			var row, result = [];
			for (var i = 0; i < rows.length; i++) {
				row = rows[i];
				if (row.Selected) {
					var rowCopy = Object.assign({}, dataIndex[i].Data);
					result.push(rowCopy);
				}
			}
			return result;
		},

		Add: function (record) {
			var rowData = Object.assign({}, record);
			var rowIdx = dataIndex.length;
			var cancelled = false;
			var e = {
				taget: self,
				rowIndex: rowIdx,
				rowData: rowData,
				preventDefault: function () { cancelled = true; }
			};
			beforeAdded.Raise(e);
			if (cancelled) return;
			var rowCssClass = GetRowCssClass(rowIdx);;
			addRowElement(rowCssClass);
			dataSource.push(rowData);
			dataIndex.push({ Index: rowIdx, Data: rowData });
			if (headers.IsSorted)
				handleSorting();
			else
				updateRowInfo(rowIdx);
			afterAdded.Raise(e);
		},

		Update: function (idx, record) {
			var rowData = Object.assign({}, record);
			var cancelled = false;
			var e = {
				taget: self,
				rowIndex: idx,
				rowData: rowData,
				preventDefault: function () { cancelled = true; }
			};
			beforeUpdated.Raise(e);
			if (cancelled) return;
			Object.assign(dataIndex[idx].Data, rowData); // This updates both the dataIndex and the dataSource as both reference the same record.
			updateRowInfo(idx);
			afterUpdated.Raise(e);
		},

		UpdateCell: function (rowidx, colidx, value) {
			var currentValues = self.Get(rowidx);
			var field = headers.Get(colidx);
			currentValues[field.Field] = value;
			var cancelled = false;
			var e = {
				taget: self,
				rowIndex: rowidx,
				rowData: currentValues,
				preventDefault: function () { cancelled = true; }
			};
			beforeUpdated.Raise(e);
			if (cancelled) return;
			dataIndex[rowidx].Data[field.Field] = value; // This updates both the dataIndex and the dataSource as both reference the same record.
			updateRowInfo(rowidx);
			afterUpdated.Raise(e);
		},

		Remove: function (idx) {
			var row = rows[idx];
			var rowData = dataIndex[idx];
			var cancelled = false;
			var e = {
				target: self,
				rowIndex: idx,
				rowData: rowData,
				preventDefault: function () { cancelled = true; }
			};
			beforeRemoved.Raise(e)
			if (cancelled) return;
			dataSource.splice(idx, 1);
			dataIndex.splice(idx, 1);
			deleteRowElement(row);
			afterRemoved.Raise(e);
		},

		Clear: function () {
			this.LoadData([]);
		},

		Select: function (idx, clearPrev) {
			var clearPrevious = clearPrev == null ? true : clearPrev;
			var row = rows[idx];
			//if (row.Element.className.indexOf("gvrowselected") < 0)
			// could be better ask for row.Selected property -> IsSelected method
			// disable condition to unselect selected row
			setSelection(row, clearPrevious);
		},

		Unselect: function (idx) {
			clearSelected(rows[idx], true);
		},

		ClearSelection: function () {
			clearSelection(true);
		},

		IsSelected: function (idx, value) {
			if (idx == null || idx == undefined) return;
			if (value == null || value == undefined)
				return rows[idx].Selected;
			else {
				if (value == true)
					self.Select(idx, false);
				else
					self.Unselect(idx);
			}
		},

		IsEdited: function (idx, value) {
			if (idx == null) return;
			if (value == null)
				return rows[idx].IsEdited;
			else
				rows[idx].IsEdited = (value != false);
		},

		Highlight: function (rowIdx, colIdx) {
			if (colIdx != null)
				rows[rowIdx].Element.childNodes[colIdx].style.backgroundColor = highlightColor;
			else
				rows[rowIdx].Element.style.backgroundColor = highlightColor;
		},

		IsHighlighted: function (rowIdx, colIdx) {
			var rowColor = rows[rowIdx].Element.style.backgroundColor;
			if (colIdx != null)
				return rowColor != "" || rows[rowIdx].Element.childNodes[colIdx].style.backgroundColor != "";
			else
				return rowColor != "";
		},

		ResetHighlight: function (rowIdx, colIdx) {
			if (colIdx != null)
				rows[rowIdx].Element.childNodes[colIdx].style.backgroundColor = "";
			else if (rowIdx != null)
				resetRowHighlight(rowIdx);
			else
				resetGridHighlight();
		},

		EnsureVisible: function (rowIdx, colIdx) {
			if (colIdx != null)
				ensureCellVisible(rowIdx, colIdx);
			else
				ensureRowVisible(rowIdx);
		},

		GetCellElement: function (rowIdx, colIdx) {
			return rows[rowIdx].Element.childNodes[colIdx].childNodes[0];
		},

		SetError: function (rowIdx, colIdx, value, message) {
			rows[rowIdx].Errors[colIdx] = { ProposedValue: value, Message: message };
			updateRowInfo(rowIdx);
		},

		GetError: function (rowIdx, colIdx) {
			return rows[rowIdx].Errors[colIdx];
		},

		ClearErrors: function (rowIdx) {
			for (var i = 0; i < rows.length; i++)
				this.ClearError(rowIdx, i);
		},

		ClearError: function (rowIdx, colIdx) {
			if (rowIdx == null || colIdx == null) return;
			delete rows[rowIdx].Errors[colIdx];
		},

		IsError: function (rowIdx, colIdx) {
			return rows[rowIdx].Errors[colIdx] != null;
		},

		GetProposedValue: function (rowIdx, colIdx) {
			if (rows[rowIdx].Errors[colIdx] != null)
				return rows[rowIdx].Errors[colIdx].ProposedValue;
			else
				return null;
		},

		ValidState: function () {
			for (var i = 0; i < rows.length; i++) {
				for (var p in rows[i].Errors)
					if (rows[i].Errors[p] != null)
						return false;
			}
			return true;
		}
	};
	return self;
}
//#endregion



// ==================================================================
// OrderGridEditContext
// ==================================================================
//#region OrderGridEditContext
var OrderGridEditContext = function (info, callback) {
	var w = info.cellElement.clientWidth - 2;
	info.cellElement.editing = true;
	var elm = null;
	var datePicker = null;
	var map = info.headerData.Map;
	var htype = info.headerData.Type.toLowerCase() || "string";
	var validations = info.headerData.Validations || {};
	var value = info.proposedValue || info.currentValue || "";
	var elm = createElement(info.headerData);

	var disposed = false;
	function dispose() {
		if (disposed) return;
		elm.off("keydown");
		elm.off("keyup");
		elm.off("blur");
		if (htype != "date")
			elm.remove();
		else
			elm.hide();
		elm = null;
		self = null;
		if (datePicker != null) {
			datePicker.Dispose();
			datePicker = null;
		}
		disposed = true;
	};

	function createElement(header) {
		var elm;
		if (htype != "bool" && header.Map != null)
			elm = createSelect(header.Map);
		else {
			if (htype == "bool") {
				elm = createCheckbox();
			}
			else if (htype == "date") {
				elm = createDatePicker();
			}
			else {
				if (htype == 'string')
					htype = 'text';
				elm = createTextBox(htype);
				if (validations["maxlen"] != null) {
					elm.attr("maxlength", validations["maxlen"].Value);
				}
			}
		}
		elm[0].userData = header.Index;
		return elm;
	}

	function createTextBox(htype) {
		var elm = $(`<input type="${htype}" value="${value}" class="gveditbox" style="width:${w}px;" />`);
		return elm;
	}

	function createCheckbox() {
		var elm = $(`<input type="checkbox" class="gveditbox" ${value == true ? "checked" : ""} />`);
		return elm;
	}

	function createDatePicker() {
		var elm = $(`<div class="gvdatebox"></div>`);
		return elm;
	}

	function createSelect(map) {
		var elm = $(`<select  class="gvselectbox" style="width:${w}px;"></select>`);
		for (var op in map) {
			if (value == op)
				elm.append(`<option value="${op}" selected style='color:#000'>${map[op]}</option>`)
			else
				elm.append(`<option value="${op}" style='color:#000'>${map[op]}</option>`)
		}
		return elm;
	}

	function isValidNumber(value) {
		var numericValue;
		switch (htype) {
			case "int":
			case "long":
				numericValue = parseInt(value, 10);
				break;
			case "decimal":
				numericValue = parseFloat(value);
				break;
		}
		return !(numericValue === NaN);
	}

	function isInsideRange(value, range) {
		var numericValue = 0;
		switch (htype) {
			case "int":
			case "long":
				numericValue = parseInt(value, 10);
				break;
			case "decimal":
				numericValue = parseFloat(value);
				break;
			default:
				return false;
		}
		if (numericValue === NaN)
			return false;
		return (numericValue >= range.MinValue && numericValue <= range.MaxValue);
	}


	function isValidChars(value, validation) {
        if (String.IsNullOrEmpty(value)) return true;
		for (var i = 0; i < value.length; i++) {
			if (validation.Chars.indexOf(value[i]) < 0)
				return false;
		}
		return true;
	}


	function checkValidations(userValue) {
        if (validations["required"] != null && String.IsNullOrEmpty(userValue)) {
			info.isValid = false;
			info.errorMessage = "Value is required";
		}
		if (validations["numeric"] != null && !isValidNumber(userValue)) {
			info.isValid = false;
			info.errorMessage = "Value is not a valid number";
		}
		if (validations["range"] != null && !isInsideRange(userValue, validations["range"])) {
			info.isValid = false;
			info.errorMessage = "Value is outside the expected range";
		}
		if (validations["chars"] != null && !isValidChars(userValue, validations["chars"])) {
			info.isValid = false;
			info.errorMessage = "Value contains invalid characters";
		}
		if (validations["regex"] != null && validations["regex"].Regex.match(userValue) == null) {
			info.isValid = false;
			info.errorMessage = "Value does not have the expected format";
		}
		info.isValue = true;
		info.errorMessage = "";
	}


	function applyDataType(value) {
		switch (htype) {
			case "int":
			case "long":
				return parseInt(value, 10);
			case "decimal":
				return parseFloat(value);
			case "bool":
				return elm[0].checked;
			case "date":
				return value;
			default:
				return value;
		}
	}

	var self = {
		get rowIndex() { return info.rowIndex; },
		get colIndex() { return info.colIndex; },
		get dataType() { return info.dataType; },

		BeginEdit: function () {
			info.cellElement.innerText = "";
			info.cellElement.append(elm[0]);
			setTimeout(function () {
				if (disposed) return;
				elm.focus();
				if (htype == "bool")
					elm.prop("checked", !value);
				else if (htype == "date")
					datePicker = new OrderGridDatePicker(elm, value, () => { value = datePicker.Value; self.Accept(); });
				else
					elm.select();
			}, 100);
			elm.on("keydown", function (e) {
				if (e.keyCode == 9) {
					e.preventDefault();
					e.stopPropagation();
				}
				else if ("int,long,decimal".indexOf(htype) >= 0 || validations["numeric"] != null) {
					var key = e.keyCode ? e.keyCode : e.which;
					if (!([8, 9, 13, 27, 46, 110, 190, 189, 109].indexOf(key) !== -1 ||
						(key === 65 && (e.ctrlKey || e.metaKey)) ||
						(key === 67 && (e.ctrlKey || e.metaKey)) ||
						(key === 86 && (e.ctrlKey || e.metaKey)) ||
						(key >= 35 && key <= 40) ||
						(key >= 48 && key <= 57 && !(e.shiftKey || e.altKey)) ||
						(key >= 96 && key <= 105)
					)) e.preventDefault();
				}

			});
			elm.on("keyup", function (e) {
				if (disposed) return;
				switch (e.keyCode) {
					case 13: elm.off("blur"); self.Accept(); break;
					case 27: elm.off("blur"); self.Cancel(); break;
					case 9: elm.off("blur"); info.target.EditNext(info.rowIndex, info.colIndex, !e.shiftKey); e.preventDefault(); e.stopPropagation(); break;
				}
			});
			elm.on("blur", function (e) {
				if (disposed) return;
				self.Accept();
			});
		},
		Accept: function () {
			if (disposed) return;
			info.cellElement.editing = false;
			info.cancelled = false;
			var userValue = null;
			if (htype == "date")
				userValue = datePicker.Value;
			else if (htype == "bool")
				userValue = elm[0].checked;
			else
				userValue = elm.val();
			checkValidations(userValue);
			info.proposedValue = userValue;
			info.newValue = applyDataType(userValue);
			dispose();
			if (callback) callback(info)
		},
		Cancel: function () {
			if (disposed) return;
			info.cellElement.editing = false;
			info.cancelled = true;
			info.isValid = false;
			dispose();
			if (callback) callback(info)
		}
	};
	return self;
}
//#endregion



// =========================================================================
// OrderGridDatePicker
// =========================================================================
// #region OrderGridDatePicker
var OrderGridDatePicker = function (element, value, onUpdate) {
	if (value == null || !(value instanceof Date))
		value = new Date();
	var pos = $(element).offset();
	var w = $(element).width();
	var h = $(element).height() - 1;
	var elm = document.createElement("div");
	var keyDownHandler = (e) => {
		var close = false;
		if (e.keyCode == 13) {
			value = new Date();
			close = true;
		}
		if (e.keyCode == 27) {
			value = null;
			close = true;
		}
		if (close) {
			if (value != null)
				valueElm.innerText = value.toLocaleDateString();
			else
				valueElm.innerText = "";
			if (onUpdate != null)
				onUpdate(value);
			$("BODY").off("keydown", keyDownHandler);
		}
	};
	$("BODY").on("keydown", keyDownHandler);
	elm.className = "gvdtpicker";
	elm.style.left = (pos.left - 2) + "px";
	elm.style.top = (pos.top - 3) + "px";
	elm.style.width = w + "px";
	elm.style.height = h + "px";
	var valueElm = document.createElement("div");
	valueElm.className = "gvdtpicker-value";
	valueElm.innerText = value.toLocaleDateString();
	elm.appendChild(valueElm);
	elm.appendChild($(`<div class="fa fa-calendar gvdtpicker-icon"></div>`)[0]);
	$("BODY")[0].appendChild(elm);
	$(elm).datepicker({
		changeMonth: true,
		changeYear: true,
		onSelect: () => {
			value = $(elm).datepicker("getDate");
			if (value == null)
				value = new Date();
			valueElm.innerText = value.toLocaleDateString();
			if (onUpdate != null)
				onUpdate(value);
			$("BODY").off("keydown", keyDownHandler);
		}
	});


	return {
		get Value() { return value; },
		set Value(val) {
			value = val;
			valueElm.innerText = value.toLocaleDateString();
		},

		Dispose: function () {
			elm.remove();
			$(elm).datepicker("destroy");
		}
	};
};
// #endregion
