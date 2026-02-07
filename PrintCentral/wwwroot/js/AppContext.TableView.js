

var TableView = function (container, controller) {
	var self = this;
	this.data = [];
	this.columns = [];
	this.containerElement = AppContext.GetContainerElement(container);
	if (this.containerElement[0].nodeName.toLowerCase() !== "div")
		throw "TableView container must be a DIV";
	this.containerElement.addClass("grid-body");
	this.tableElm = $("<table class='table table-sm' style='border-bottom:1px solid #e0e0e0; table-layout:fixed;' />");
	this.headElm = $("<thead class='thead-light'></thead>")
	this.bodyElm = $("<tbody></tbody>")
	this.tableElm.append(this.headElm);
	this.tableElm.append(this.bodyElm);
	this.containerElement.append(this.tableElm);
	var template = this.containerElement.find("template");
	if (template.length > 0) {
		var templateElement = $(template[0].content);
		this.SetupFromTemplate(templateElement, controller);
	}
	this.containerElement.mousedown(function (e) {
		self.HandleMouseDown(e);
	});
	this.containerElement.dblclick(function (e) {
		self.HandleDoubleClick(e);
	});
	this.SelectedRow = null;
	this.SelectedRowIndex = -1;
	this.BeforeRowEdit = new AppEvent();
	this.AfterRowEdit = new AppEvent();
	this.OnSelectedChanged = null;
	this.BeforeSelectedChanged = null;
};


TableView.Extend = function (target, container) {
	TableView.call(target, container);
	ExtendObj(target, TableView.prototype);
};


TableView.prototype = {
	constructor: TableView,

	SetupFromTemplate: function (templateElement, controller) {
		var self = this;
		var columnElements = templateElement.find("column");
		self.columns = [];
		$.each(columnElements, function (index, item) {
			var c = {};
			var elm = $(item);
			c.Text = elm.text();
			c.Width = elm.attr("width");
			c.Field = elm.attr("field");
			c.Type = elm.attr("type");
			c.Editable = elm.attr("editable") != null;
			var map = elm.attr("map");
			if (map && map.length && map.length > 0)
				c.Map = JSON.parse(map);
			self.columns.push(c);
		});
		self.ConfigureColumns(self.columns);
		var menuElement = templateElement.find("menu");
		if (menuElement.length > 0) {
			var menuItems = menuElement.find("menuitem");
			self.SetupContextMenu(menuItems, controller);
		}
	},

	SetupContextMenu: function (menuItems, controller) {
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
		this.containerElement.contextmenu(function (e) { ctxMenu.HandleContextMenu(e); });
		this.containerElement.mousemove(function (e) { ctxMenu.HandleMouseMove(e); });
	},

	ConfigureColumns: function (columns) {
		this.columns = columns;
		this.headElm.empty();
		var row = $("<tr></tr>");
		for (var i = 0; i < columns.length; i++) {
			var columnText = columns[i].Text;
			var columnWidth = columns[i].Width ? columns[i].Width : 80;
			var visible = "";
			if (columns[i].hasOwnProperty("Visible"))
				visible = columns[i].Visible ? "" : " display:none;";
			else
				columns[i].Visible = true;
			row.append($(`<th style="width:${columnWidth}px;${visible}">${columnText}</th>`));
		}
		this.headElm.append(row);
	},

	LoadData: function (data) {
		if (!this.columns || !this.columns.length)
			throw "Must configure columns first.";
		if (!Array.isArray(data))
			throw "Data must be an array.";
		this.data = data;
		if (this.SelectedRow)
			this.SelectedRow.removeClass("grid-selected");
		this.SelectedRow = null;
		this.SelectedIndex = -1;
		this.AdjustRows(data.length);
		//this.bodyElm.empty();
		var e = {
			target: this,
			index: -1,
			row: null,
			preventDefault: () => { },
			stopPropagation: () => { }
		}
		if (this.OnSelectedChanged != null)
			this.OnSelectedChanged(e);
		for (var i = 0; i < data.length; i++) {
			var rowData = data[i];
			this.LoadRow(rowData, i);
		}
	},

	AdjustRows: function (rowNum) {
		if (rowNum > this.bodyElm[0].childNodes.length) {
			var rowsToAdd = rowNum - this.bodyElm[0].childNodes.length;
			for (var i = 0; i < rowsToAdd; i++) {
				this.AddEmptyRow();
			}
		}
		else {
			var elm = this.bodyElm[0];
			var idx = elm.childNodes.length;
			while (idx-- > rowNum) {
				elm.childNodes[idx].remove();
			}
		}
	},

	GetRowCount: function () {
		return this.data.length;
	},

	GetColCount: function () {
		return this.columns.length;
	},

	CreateRow: function (rowData, i) {
		var row = $("<tr></tr>");
		for (var c = 0; c < this.columns.length; c++) {
			if (c === 0 && this.columns[0].Text === "#") {
				row.append($(`<td style="border-right:1px solid #c0c0c0;">${(i + 1)}</td>`));
			}
			else {
				var col = this.columns[c];
				var fieldValue = this.GetFieldValue(rowData, col);
				if (fieldValue == null) fieldValue = "";
				var visible = col.Visible ? "" : " display:none;";
				row.append($(`<td style="width:${col.Width}px; overflow:hidden; ${visible}">${fieldValue}</td>`));
			}
		}
		this.bodyElm.append(row);
	},

	LoadRow: function (rowData, i) {
		var rowElm = this.bodyElm[0].childNodes[i];
		for (var c = 0; c < this.columns.length; c++) {
			var col = this.columns[c];
			if (c === 0 && col.Text === "#") {
				rowElm.childNodes[c].innerText = (i + 1);
			}
			else {

				// si la fila tiene definido una funcion de render, utilizar esa

				var fieldValue = this.GetFieldValue(rowData, col);
				if (fieldValue == null) fieldValue = "";
				rowElm.childNodes[c].innerText = fieldValue;

				
			}
		}
	},

	AddEmptyRow: function (rowIndex) {
		var row = $("<tr></tr>");
		for (var c = 0; c < this.columns.length; c++) {
			var col = this.columns[c];
			if (c === 0 && col.Text === "#") {
				row.append($(`<td style="border-right:1px solid #c0c0c0;">${(rowIndex + 1)}</td>`));
			}
			else {
				var visible = col.Visible ? "" : " display:none;";
				row.append($(`<td style="width:${col.Width}px; overflow:hidden; ${visible}"></td>`));
			}
		}
		this.bodyElm.append(row);
	},

	GetFieldValue: function (rowData, col) {
		var fieldValue = "";
		if (rowData.hasOwnProperty(col.Field))
			fieldValue = rowData[col.Field];
		else
			return null;
		if (fieldValue == null)
			fieldValue = "";
		if (col.Map)
			fieldValue = col.Map[fieldValue];
		if (col.Compute)
			fieldValue = col.Compute(fieldValue);
		if (col.Type) {
			switch (col.Type) {
				case "Date":
					if (fieldValue)
						fieldValue = new Date(fieldValue).toLocaleDateString();
					break;
				case "Time":
					if (fieldValue)
						fieldValue = new Date(fieldValue).toLocaleTimeString();
					break;
			}
		}
		return fieldValue;
	},

	UpdateRow: function (rowindex, rowData) {
		if (rowindex < 0 || rowindex >= this.data.length || rowindex == null)
			rowindex = this.SelectedIndex;
		if (rowData == null)
			rowData = this.data[rowindex]
		else
			this.data[rowindex] = rowData;
		for (var c = 0; c < this.columns.length; c++) {
			var fieldValue = this.GetFieldValue(rowData, this.columns[c]);
			if (fieldValue != null)
				this.tableElm[0].rows[rowindex + 1].cells[c].innerText = fieldValue;
		}
	},

	AddRow: function (item) {
		this.data.push(item);
		this.CreateRow(item, this.data.length);
		return this.data.length - 1;
	},

	DeleteRow: function (rowindex) {
		if (rowindex < 0 || rowindex > this.data.length)
			throw "Index out of bounds";
		this.data.splice(rowindex, 1);
		$(this.tableElm[0].rows[rowindex + 1]).remove();
		if (this.SelectedIndex === rowindex) {
			this.SelectedIndex = -1;
			this.SelectedRow = null;
			var e = {
				target: this,
				index: -1,
				row: null,
				preventDefault: () => { },
				stopPropagation: () => { }
			}
			if (this.OnSelectedChanged != null)
				this.OnSelectedChanged(e);
		}
	},

	HighlightCell: function (row, col, set) {
		if (row < 0 || row > this.data.length) return;
		if (col < 0 || col >= this.columns.length) return;
		var cel = this.tableElm[0].rows[row].cells[col];
		if (set)
			$(cel).css("background-color", "#ffc107");
		else
			$(cel).css("background-color", "");
	},

	HighlightRow: function (row, color) {
		if (row < 0 || row > this.data.length) return;
		var rowElm = $(this.tableElm[0].rows[row + 1]);
		if (this.highlightedRow != null)
			this.highlightedRow.css("background-color", "");
		this.highlightedRow = rowElm;
		if (!color)
			color = "#a8cfa0";
		rowElm.css("background-color", color);
	},

	EnsureCellVisible: function (row, col) {
		if (row < 0 || row > this.data.length) return;
		if (col < 0 || col >= this.columns.length) return;
		var cel = this.tableElm[0].rows[row].cells[col];
		var pos = $(cel).position().top;
		var cheight = this.containerElement.innerHeight();
		var spos = this.containerElement.scrollTop();
		if (pos > cheight || pos < 0) {
			var stop = spos + pos - cheight / 2;
			this.containerElement.scrollTop(stop);
		}
	},

	SetCellHtml: function (row, col, html) {
		if (row < 0 || row > this.data.length) return;
		if (col < 0 || col >= this.columns.length) return;
		var cel = this.tableElm[0].rows[row + 1].cells[col];
		cel.innerHTML = html;
	},

	GetSelectedData: function () {
		if (this.SelectedIndex >= 0)
			return this.data[this.SelectedIndex];
		else
			return null;
	},

	HandleMouseDown: function (e) {
		if (e.target.nodeName.toLowerCase() === "td") {
			var row = e.target.parentNode;
			this.SelectRow(row.rowIndex - 1, e);
			var col = e.target.cellIndex;
			if (this.columns[col].Editable)
				this.BeginEdit(this.SelectedIndex, col);
		}
	},

	SelectRow: function (rowIdx, evt) {
		var cancelEvent = false;
		var rowData = (this.SelectedIndex >= 0) ? this.data[this.SelectedIndex] : null;
		var e = {
			target: this,
			index: this.SelectedIndex,
			row: rowData,
			preventDefault: () => { cancelEvent = true; if (evt != null) evt.preventDefault(); },
			stopPropagation: () => { if(evt != null) evt.stopPropagation(); }
		}
		if (this.BeforeSelectedChanged != null) {
			this.BeforeSelectedChanged(e);
			if (cancelEvent)
				return;
		}
		var index = rowIdx;
		if (index < 0 || index >= this.data.length)
			throw "Index out of bounds";
		if (this.SelectedRow)
			this.SelectedRow.removeClass("grid-selected");
		var row = this.tableElm[0].rows[index + 1];
		this.SelectedRow = $(row);
		this.SelectedRow.addClass("grid-selected");
		this.SelectedIndex = index;
		e.index = index;
		e.row = this.data[index];
		if (this.OnSelectedChanged != null) {
			this.OnSelectedChanged(e);
		}
	},

	BeginEdit: function (row, col) {
		var self = this;
		var cancelEvent = false;
		var e = {
			target: this,
			preventDefault: () => { cancelEvent = true; },
			stopPropagation: () => { }
		}
		if (row < 0 || row >= this.data.length || col < 0 || col >= this.columns.length)
			throw `Index out of bounds (${row}, ${col})`;
		if (this.EditingContext != null) {
			this.EditingContext.Accept();
			this.EditingContext = null;
		}
		this.SelectRow(row, e);
		if (cancelEvent)
			return;
		var cell = this.tableElm[0].rows[row + 1].cells[col];
		var rdata = this.data[row];
		var cdata = this.columns[col];
		this.BeforeRowEdit.Raise({ row: row, col: col, data: rdata })
		this.EditingContext = new CellEditContext(this, cell, rdata, cdata, row, col,
			() => {
				self.AfterRowEdit.Raise({ row: row, col: col, data: rdata });
			});
		this.EditingContext.BeginEdit();
	},

	EditNext: function (row, col, forward) {
		var ci = -1, ri = row;
		if (forward) {
			do {
				for (var i = col + 1; i < this.columns.length; i++) {
					if (this.columns[i].Editable) {
						ci = i;
						break;
					}
				}
				if (ci < 0) { ri++; col = -1; }
			} while (ci < 0 && ri < this.data.length);
		}
		else {
			do {
				for (var i = col - 1; i >= 0; i--) {
					if (this.columns[i].Editable) {
						ci = i;
						break;
					}
				}
				if (ci < 0) { ri--; col = this.columns.length; }
			} while (ci < 0 && ri >= 0);
		}
		if (ci < 0 && this.EditingContext != null)
			this.EditingContext.Accept();
		else
			this.BeginEdit(ri, ci);
	},

	HandleDoubleClick: function (e) {
		if (this.SelectedRow != null) {
			if (this.OnDoubleClick != null)
				this.OnDoubleClick(e);
		}
	}
};

var CellEditContext = function (grid, cell, rdata, cdata, row, col, callback) {
	var self = this;
	var f = cdata.Field;
	var w = cell.clientWidth - 18;
	var v = rdata[f] != null ? rdata[f] : "";
	var elm = null;
	if (cdata.Map == null)
		elm = $(`<input type="text" value="${v}" class="celleditbox" style="width:${w}px;" />`);
	else {
		elm = $(`<select class="celleditbox" style="width:${w}px;"></select>`);
		for(var op in cdata.Map) {
			if(v == op)
				elm.append(`<option value=${op} selected style='color:#000'>${cdata.Map[op]}</option>`)
			else
				elm.append(`<option value=${op} style='color:#000'>${cdata.Map[op]}</option>`)
		}
	}
	var disposed = false;

	var Dispose = function () {
		elm.off("keydown");
		elm.off("keyup");
		elm.off("blur");
		elm.remove();
		disposed = true;
	};

	return {
		BeginEdit: function () {
			var self = this;
			cell.innerText = "";
			cell.append(elm[0]);
			setTimeout(function () { elm.focus(); elm.select(); }, 100);
			elm.on("keydown", function (e) {
				if (e.keyCode == 9) {
					e.preventDefault();
					e.stopPropagation();
				}
			});
			elm.on("keyup", function (e) {
				if (disposed) return;
				switch (e.keyCode) {
					case 13: elm.off("blur"); self.Accept(); break;
					case 27: elm.off("blur"); self.Cancel(); break;
					case 9: elm.off("blur"); grid.EditNext(row, col, !e.shiftKey); break;
				}
			});
			elm.on("blur", function (e) {
				if (disposed) return;
				self.Accept();
			});
		},
		Accept: function () {
			if (disposed) return;
			v = elm.val();
			rdata[f] = v;
			Dispose();
			cell.innerText = v;
			if (callback) callback()
		},
		Cancel: function () {
			if (disposed) return;
			Dispose();
			cell.innerText = v;
		}
	};
}
