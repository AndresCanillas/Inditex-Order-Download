/* API MAP Between TableView & GridView
 * 
 * Current					New						Description
 * ------------------------ ----------------------- ------------------------
 *							Headers					*new*, getter, return a full fledged object with its own API representing the header row (column names and properties of each field) of the table.
 *							Rows					*new* getter, returns a full fledged object representing all the rows in the table (excluding header row)
 * columns					Columns					Renamed, it is now a getter and returns a full fledged object with its own API representing the colums of the table (excluding header row).
 *							Cells					*new* getter, returns a full fledged object with its own API representing all cells in the table (excluding header row)
 * data						Data					renamed, also it is now a getter. NOTE: changes done on this array are not automatically reflected on the grid, in general you must refrain from maing changes to this array.
 * containerElement			Container				Renamed, is is now a getter, still returns the jQuery wrapped HTML element that contains the GridView (passed in the constructor)
 * tableElm											private
 * headElm											private
 * bodyElm											private
 * SelectedRow										private, it is not possible to access the <tr> element that was selected anymore
 * SelectedRowIndex			SelectedIndex			Renamed, also changed to getter/setter. Index of the last row that was selected either through mouse or keyboard. Note: Might not be the only row selected.
 * GetSelectedData			Rows.Get(SelectedIndex)	The data of the row needs to be accessed through the Rows object, Get method.
 * SetupFromTemplate								private
 * SetupContextMenu									private, a contextual menu can only be setup through the <template> element
 * ConfigureColumns									private, columns can now only be setup through the <template> element
 * LoadData					LoadData				No changes
 * GetRowCount				Rows.Count				Use the Count property of Rows object
 * GetColCount				Columns.Count			Use the Count property of the Columns object
 * CreateRow										private
 * GetFieldValue									private
 * AddRow					Rows.Add(data)			Same function. Adds a row at the end of the table using the given object as data. The object is also added to the data source.
 * UpdateRow				Rows.Update(idx,data)	Same function. Updates the data of the specified row with the given object. NOTE: new implementation clones the object before adding it to the table to prevent side effects.
 * DeleteRow				Rows.Delete(idx)		Same function. Deletes the specified row from the table, also removes the row from the data source
 * HighlightRow				Rows.Highlight(idx)		Same function.
 * HighlightCell			Cells.Highlight(r,c)	Same function, but updated signature: removed set argument, now accepts color (if color is null uses a default).
 * EnsureCellVisible		Cells.EnsureVisible(r,c) Same function.
 * SetCellHtml				Cells.SetHtml(r,c,html)	Same function.
 * HandleMouseDown									private
 * SelectRow				Rows.Select(idx)		Updated signature: removed evt argument (functionality involving evt argument is private and should be placed elsewere). It selects the specified row. Also impacts SelectedIndex & SelectedRowData
 * BeforeRowEdit			BeforeCellEdit			renamed 
 * AfterRowEdit				AfterCellEdit			renamed 
 * BeforeSelectedChanged	BeforeSelectedChanged	same
 * OnSelectedChanged		AfterSelectedChanged	renamed
 *							RowClick				*new*
 * OnDoubleClick			RowDoubleClick			renamed
 *							CellClick				*new*
 *							CellDoubleClick			*new*
 *							HeaderClick				*new*
 *							HeaderDoubleClick		*new*
 *
 * =========================================================
 * 
 * 
 * 
 * 
 * ==================================================================================
 * GridView object
 * ==================================================================================
 *
 * Methods and Properties
 * 
 * Member						Description
 * ---------------------------- ----------------------------------------------
 * Element						getter, returns the root element representing the grid
 * Headers						getter, returns an object representing the headers of the grid, can use this object to alter the properties of the headers.
 * Rows							getter, returns an object representing the rows of the grid, can use this object to interact with the rows.
 * Clear()						Removes all the data from the grid, causes the DataSource property to go back to an empty array. Also cancells any ongoing editions. IMPORTANT: In case there are errors in the grid, then any editions that have not yet been commited will be lost.
 * ValidState()					Returns a value indicating if the grid data is valid and has been fully commited to the data source. Important: Calling this method will cancel any ongoing editions to ensure all data is commited to the data source (or errors are set if the values dont pass validations).
 *
 * EVENTS
 * 
 * In general all events will pass an object that includes the following as a minimum:
 *	- target			A reference to the GridView object
 *	- preventDefault()	A method that allows to change the way the event is handled by cancelling any action that would have been executed.
 *	- stopPropagation() A method that allows to stop further event handlers from triggering (for instance if from a CellClick event you call this, then RowClick will not execute).
 *	
 *	Any other methods or properties that 'e' might have depend on the particular event.
 * 
 * Event						Description
 * ---------------------------- ------------------------
 * BeforeCellEdit				Raised before a cell enters edit mode (can cancel this by calling e.preventDefault()), 'e' also includes: rowIndex, colIndex.
 *								NOTES: When a cell enters edit mode, the Error state of that cell (if set) is automatically removed. The error state of the cell can
 *								       be set again automatically if the new value is also invalid.
 *								
 * AfterCellEdit				Raised after a cell exits edit mode. 'e' also includes:
 *									- rowIndex		The index of the row
 *									- colIndex		The index of the column
 *									- colName		The name of the field being edited
 *									- originalValue	The current value of the field (notice that at the time this event is triggered, the value in the grid is still not updated)
 *									- newValue		The value entered by the user (this has already been transformed by the grid using the Type property unless valid is false)
 *									- rowData		The entire record from the data source that corresponds to the row (IMPORTANT: Do not assume that rowIndex will match the index of the row being edited in the data source, in case there is sorting applied in the grid, these indices might not match).
 *									- valid			A flag indicating if the data entered by the user is considered valid by the grid, if this is false, then the cell will be put in Error state and the edited value will not be accepted.
 *									- cancelled		A flag indicating if the edition was cancelled by the user (by pressing ESC), in this case the new value will be discarded and the data source will not be updated.
 *								
 *								By the time this event is triggered the cell has already passed all pertinent validations based of the "Validations" and "Type"
 *								properties of the corresponding header. If the new value entered by the user does not meet these restrictions, then the cell
 *								will be put in Error state automatically and the valid flag passed in the event will be false.
 *								
 *								This event handler is the last chance the application has to validate and reject the value entered by the user. All you have to do to
 *								prevent the entered value from being accepted is set the cell in the Error state by calling Cells.SetError.
 *								
 *								You can also cancel the edition in its enterity (as if the user had pressed ESC) by calling e.preventDefault(). However, this
 *								will discard any editions done by the user, so it might not be the best curse of action in most cases.
 *								
 *								GridView.ValidState() method will return false if any cell has ben set to an Error state. This means that there are editions that have not been
 *								commited to the data source due to validations not passing.
 *								
 *								The grid displays cells with errors in a distinctive red color, and hovering over the '!' icon that appears in the cell will display the error message.
 *								
 * BeforeSelectedChanged		Invoked right before SelectedIndex changes value. Includes e.rowIndex, e.rowData, e.preventDefault can prevent the row from becoming selected.
 * AfterSelectedChanged			Invoked after SelectedIndex has been updated (either by the user interacting with the table or programatically by setting the SelectedIndex property)
 * RowClick						Invoked when a Row is clicked. e also includes: rowIndex, e.preventDefault can prevent the clicked row from becoming selected.
 * RowDoubleClick				Invoked when a Row is double clicked. e also includes: rowIndex, e.preventDefault does nothing as there is no default action that triggers off this event.
 * CellClick					Invoked when a Cell is clicked. e also includes: rowIndex, colIndex. Can prevent the RowClick event from being triggered by calling e.stopPropagation(). Can also prevent the cell from entering edit mode by calling e.preventDefault() (asumming the cell is editable)
 * CellDoubleClick				Invoked when a Cell is double clicked. e also includes: rowIndex, colIndex. Can prevent RowDoubleClick event from being triggered by calling e.stopPropagation(). e.preventDefault() does nothing as there is no default action that triggers off this event.
 * HeaderClick					Invoked when a header is clicked. e also includes colIndex. If the header can be sorted, calling e.preventDefault() will prevent the sort from executing.
 * HeaderDoubleClick			Invoked when a header is double clicked. e also includes colIndex. e.preventDefault() does nothing as there is no default action that triggers off this event.
 *	
 *	
 *	
 *	
 *	
 * ==================================================================================
 * GridHeaders object
 * ==================================================================================
 *
 * Represents the headers of a table view. Each header in the collection includes the following properties:
 *
 *	- Index: the index of the header within the collection (0 - N)
 *
 *	- Field: The name of the field as it appears in the data source
 *
 *	- Text: The text displayed in the header of the column
 *
 *	- Width: The Width of the column
 *
 *	- MinWidth: The minimum width of a column, the column cannot be resized below this value. This is 30 by default.
 *
 *	- Visible: Indicates if the column should be visible or if it should be hidden.
 *	
 *	- IsHighlighted: Indicates if the header is currently highlighted or not.
 *
 *	- Type: The data type of the field, it allows to transform the values on the data source to and from their string representations as the values are loaded into the view. It can also help ensure the user enters valid data.
 *	  Supported data types include:
 *		> String (default if a type is not specified), displays a textbox where the user can enter up to maxlen characters. Grid will automatically validate chars and regex validations if specified before accepting the value.
 *		> Bool		- Displays a checkbox
 *		> Int		- Displays a textbox, but user can only type up to 9 numbers. The grid will automatically validate the user input using a regular expression and parse the value to an integer number using parseInt before accepting the new value.
 *		> Long		- Displays a textbox, but user can only type up to 12 numbers. The grid will automatically validate the user input using a regular expression and parse the value to an integer number using parseInt before accepting the new value.
 *		> Decimal	- Displays a textbox, but user can only type up to 12 numers and the '.' and '-' characters. Also grid will automatically validate the user input using a regular expression before accepting the new value.
 *		> Date		- When editing displays a datePicker
 *		> Image		- This data type is tightly integrated with how the system handles images, therefore it is not possible to easily find a use case for it out of the context of this system.
 *					  Cells of this type cannot be edited inline. For this data type, the grid will display an icon from awesome fonts (fa-picture),
 *					  the application MUST handle the cell click event and display the actual image in a dialog, where you will be able to change the image if necesary,
 *					  all this however is outside the scope or knowledge of the grid. In other words, the grid will not handle edition for you, nor try to interpret
 *					  whatever data is stored in this column or how to manipulate it.
 *
 *		Note on Reference & Set data types
 *
 *		The grid is tailored to allow editing the data of a generic catalog as defined in the Print Platform.
 *		You might notice the absence of the Reference and Set data types though...
 *
 *		Edition of Reference types will be handled by providing a Map (explained below), and by handling the cell click event and
 *		displaying a UI to search and select the record that will be asociated, this UI is out of the scope of the grid though and is a
 *		separate view.
 *
 *		In the other hand, it is not possible to edit Sets inline with the grid. It should be possible to load the details of the selected
 *		row in a separate dialog, for instance on double clicking on a row. Then that other view can display fields of type Set as grids of
 *		their own.
 *
 *		- Editable: Indicates if cells under this header can be edited inline
 *
 *		- Map: Stores a set of acceptable values and their associated texts. This causes the grid to display the text associated to the current value of the cell.
 *				Also, when the cell is editable the UI displays a dropdown. Ej: {"1":"Option A", "2":"Option B", "3":"Option C"}
 *
 *				When a map is specified, the grid will automatically validate that the entered value matches one of the supplied options.
 *
 *		- Validations: stores a string with the validations that should be executed automatically on cells under this header. Only applies if Editable is true. Multiple validations can be specified separating them with ';'.
 *		  Supported validations:
 *			> required		User cannot leave this field empty or null (no text)
 *			> numeric		User can only enter numers, this is superfluous if the type has been set to Int, Log or Decimal
 *			> maxlen(N)		User can enter up to N character (N being an integer value)
 *			> range(A,B)	Only taken into account if the type is Int, Long or Decimal. Ensures that the value entered falls within the given range [A, B].
 *			> chars(CHARS)	Only taken into account if the type is string. Ensures that the user enters only characters found in the specified CHARS
 *			> regex(REGEX)	Only taken into account if the type is string. Ensures that the value entered by the user matches the specified regular expression.
 *
 *			Example: "required;numeric;maxlen(5);range(-2000,2000);chars(0123456789);"
 *
 *		- Sortable: Indicates if the column can be sorted by clicking on the header. If true, the header will automatically respond to
 *					click events to sort the data according to the user selections.
 *
 *		- SortPosition: Is an integer value from 0 to N indicating the position of this header in the sorting algorithm.
 *						The lower the value, the more precedence this column will have. No two colums can have the same value, this is enforced
 *						by adjusting the SortPosition of other columns when calling Update.
 *
 *		- SortDirection: Either "", "ASC" or "DESC" depending on if the data is being sorted through this column or not and how.
 *
 *			IMPORTANT: Sorting affects only the VIEW, not the data source. Internally the GridView will keep an index to make sure each
 *						row being displayed can still be correlated back to the data source.
 *
 *	NOTE: All these properties can be setup through the <template> element that can be placed inside the html element that will contain the GridView,
 *		  or through the Update method.
 *
 *
 * Member						Description
 * ---------------------------- ----------------------------------------------
 * Element						Is the root element of the grid header
 * Count						getter, returns the number of headers in the collection
 * Width						getter, return the total width of all the headers
 * IsSorted						getter, return a flag indicating if the data displayed in the grid is currently sorted or not.
 * HighlightColor				getter/setter, gets or sets the color used to highlight headers.
 * Each([thisArg], fn)			Iterates through all the headers in the collection executing the specified function (fn). The function is passed the index of the header and the header data on each iteration. Optionally the call to the function can be bound to the context of the thisArg object (if provided).
 * Get(idx)						Returns the information of the specified header. You can change the properties of the retrieved object however these changes will not be applied until Update(idx, data) is called.
 * Find(fieldName)				Returns the information of the header whose Field property matches the specified field name.
 * Add(data)					Adds a new header at the end of the header collection.
 * Update(data)					Updates the properties of the specified header. This is reflected in the view inmmediatelly, also applies sorting if those properties where changed.
 * Remove(idx)					Removes the specified header from the collection, also impacting the view.
 * SetSorting(sort)				Sets the specified columns as sortable and applies the specified sort. Example: grid.SetSorting({"Name":"ASC", "Code":""}');  This sets the Name and Code headers as sortable, also sorts by Name in ascending order. In this example, The grid is not sorted by Code at the moment SetSorting is called, however the header is updated to be sortable, meaning that the user can click on the column to sort the data by the Code column if they want to.
 * GetSorting()					Gets the currently applied sorting. Each property in the returned object corresponds to the name of a column that is being sorted, the value of the property indicates if the sorting is ascending or descending, in the other hand, the order in which the properties appear in this object themselves indicates the priority of each column (SortPosition). IMPORTANT: Headers that are sortable but are not currently being sorted are not included in the result of this method.
 * ClearSorting()				Removes any currently applied sorting, returning the GridView to the order in which rows appear in the data source. NOTE: This only removes the currently applied sorting, it does not change the Sortable status of the headers. To disable the ability of sorting data altogether, you have to edit each independent header and manually set its Sortable property to false. Example:...
 * ToggleSorting(idx)			Assuming the spcified header is sortable, ToggleSorting changes the SortDirection of the column based of the current value, it can be used to transition between the following states: "No Sorting" -> "Sorting Ascending" -> "Sorting Descending" -> and start over back to "No Sorting"
 * Highlight(idx)				Highlights the specified header (can change the color used to highlight setting the HighlightColor property).
 * IsHighlighted(idx)			Returns a value indicating if the specified header is highlighted.
 * ResetHighlight([idx])		Resets the background color of the specified header to its default, optionally if the idx argument is not specified, then resets the background color of all headers.
 * EnsureVisible(idx)			Ensures the specified header is visible (scrolls the container to the top and left/right if necesary)
 * Added						event, invoked when a header is added to the headers collection. Event inlcudes: e.target (the GridHeaders object), e.colIndex, e.headerData. NOTE: (stopPropagations and preventDefault have no effect).
 * Updated						event, invoked when a header is updated through the Update method (NOTE: This event is not triggered when SortDirection is changed by user interaction). The event includes: e.target, e.colIndex, e.headerData (NOTE: stopPropagations and preventDefault have no effect).
 * Removed						event, invoked when a header is removed through the Remove method. Event includes: e.target, e.colIndex, e.headerData (stopPropagations and preventDefault have no effect).
 * SortingChanged				event, raised when the SortDirection of a sortable header is changed (either programatically or through user interaction).
 * 
 * 
 * 
 * 
 * 
 * ==================================================================================
 * TableRows object
 * ==================================================================================
 * 
 * Represents the rows of the table view.
 *
 * Member						Description
 * ---------------------------- ----------------------------------------------
 * Count						getter, returns the number of rows in the collection
 * MultiSelect					getter/setter, Is a flag indicating if there can be multiple rows selected at once.
 * HighlightColor				getter/setter Allow to change the color used to highlight rows and cells.
 * SelectedIndex				getter. Index of the last row that was selected (either through mouse or programatically). Note: Might not be the only row selected, it is only the index of the last one the user interacted with.
 * DataSource					getter, returns a reference to the array of objects being used as the grid data source. By default the grid has an empty data source to which you can start adding records programatically, or you can load an already populated data source with the LoadData() method.
 * LoadData(data)				Loads data into the grid. This method replaces whatever data was loaded before, also auto accepts any edition that might have been ongoing at the moment.
 * Each([thisArg], fn)			Iterates through all the rows in the collection executing the specified function (fn). The function is passed the index of the row and the row data on each iteration. Optionally the call to the function can be bound to the context of the thisArg object (if provided).
 * Get(idx)						returns the information of the specified row. You can change the properties of the retrieved object, then update the row by calling Update(idx, data). NOTES: If update is not called neither the data source nor the view will be updated.
 * GetSelected()				Returns the data of all selected rows in an array.
 * Add(data)					Adds a row at the end of the table using the given object as data. The object is also added to the data source. Re-applies sorting if the table is being sorted, also performs validations on the supplied data, any cells with invalid data will be set in the error state.
 * Update(idx, data)			Updates the informaction of specified row. This is reflected in the view inmmediatelly, also re-applies sorting if the table is being sorted. Validations will be run against the supplied data, any cells with invalid data will be set in the error state.
 * Remove(idx)					Removes the specified row from the table, also removes the row from the data source.
 * Select(idx, [clearPrev])		Makes the specified row selected. clearPrev is an optional argument that defaults to true, it indicates if the previously selected row should be unselected. Sending clearPrec to false can allow to select multiple rows programatically, this at least assuming that the MultiSelect flag is set to true. Has no effect if the specified row is already selected. Calling this method will also trigger the Before/AfterSelectionChange if addecuate.
 * Unselect(idx)				Changes the selected state of the specified row to false. Calling this method will also trigger the Before/AfterSelectionChange if addecuate.
 * ClearSelection()				Deselects all currently selected rows. Calling this method will also trigger the Before/AfterSelectionChange if addecuate.
 * IsSelected(idx)				Returns a value indicating if the specified row is selected.
 * Highlight(rowIdx, [colIdx])	Highlights the specified row, optionally if a colIndex is specified highlights only that cell.
 * IsHighlighted(rIdx, [colIdx])	Returns a value indicating if the specified row is highlighted. Optionally, if a colIndex is specified checks only the specified cell.
 * ResetHighlight([rowIdx], [colIndex])	Removes the highlighted status from the specified row. Optionally if colIndex is specified resets only that cell. Also if no parameters are given, then resets the entire grid.
 * EnsureVisible(rowIdx, [colIdx]) Scrolls the container up/down/left/right as necesary to ensure that the specified row is visible. Optionally if a colIndex is specified, also ensures that cell is visible.
 * 
 * NOTE: User can perform selection of multiple rows by holding down the ctrl key when clicking on a row. Optionally it is also possible 
 *       to implement multiple selection programatically by handling the cell/row click event, for instance by implementing a cell handler
 *       that check for a click on a specific cell, and process the click by adding the row to the selected rows by calling: Select(rowIdx, false).
 * 
 * 
 * 
 * ---------------------------- TO BE REMOVED ----------------------------
 * 
 * TableColumns object
 * 
 * Represents the columns of the table view.
 *
 * Member						Description
 * ---------------------------- ----------------------------------------------
 * Count						getter, returns the number of columns in the table
 * Each(fn)						Iterates through all the columns executing the specified function, the function is passed the index of the column on each iteration.
 * Highlight(idx, color)			Highlights the specified column using the given color.
 * ResetHighlight([idx])		Resets the background color of the specified column to its default, optionally if the idx argument is not specified, then resets the background color of all columns.
 * EnsureVisible(idx)			Ensures the specified column is visible (scrolls the container left/right if necesary)
 * 
 *
 *
 * TableCells object
 * 
 * Represents the cells of the table view.
 *
 * Member							Description
 * -------------------------------- ----------------------------------------------
 * Each(fn)							Iterates through all the cells executing the specified function, the function is passed the index of the row and column on each iteration.
 * SetError(ridx, cidx, message)	Sets the specified cell in the error state.
 * Highlight(ridx, cidx, color)		Highlights the specified cell using the given color.
 * ResetHighlight([ridx], [cidx])	Resets the background color of the specified cell to its default, optionally if the idx arguments are not specified, then resets the background color of all cells.
 * EnsureVisible(ridx, cidx)		Ensures the specified cell is visible (scrolls the container left/right if necesary)
 * SetHtml(ridx,cidx,html)			Changes the content of the cell to the specified HTML, throws an error if the cell is editable
 * BeginEdit(ridx,cidx)				Initiates edition mode in the specified cell, throws error if the cell is not editable
*/

var GridView = function (element, controller) {
	element = AppContext.GetContainerElement(element)[0];
	element.className = "gvbody";
	var headers = new GridHeaders(element);
	var rows = new GridRows(element, headers);
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

	var template = $(element).find("template");
	if (template.length > 0) {
		setupFromTemplate($(template[0].content), controller);
		template.remove();
	}


	var mouseHeaderElm = null;
	var mouseX = 0;
	$(element).on("mousedown touchstart", (e) => {
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
				if (target.className.indexOf("gvcell") >= 0) {
					if (rowElm.className.indexOf("gvrowselected") < 0) {
						if (editingContext != null)
							editingContext.Accept();
						if (rows.MultiSelect && (shiftKey || ctrlKey))
							rows.Select(rowIndex, false);
						else
							rows.Select(rowIndex, true);
					}
				}
				if (headerData.Editable && e.buttons == 1 &&
					(editingContext == null || editingContext.colIndex != colIndex))
					beginEdit(rowIndex, colIndex);
			}
		}
	});

	$(element).on("mousemove touchmove", (e) => {
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

	$(document).on("mouseup touchend", (e) => {
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

	$(element).on("click", (e) => {
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

	$(element).on("dblclick", (e) => {
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
		$(element).contextmenu(function (e) { ctxMenu.HandleContextMenu(e); });
		$(element).mousemove(function (e) { ctxMenu.HandleMouseMove(e); });
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

		editingContext = new GridEditContext(e, (e) => {
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
		constructor: GridView,

		// Events
		get RowClick() { return rowClick; },
		get RowDoubleClick() { return rowDoubleClick; },
		get CellClick() { return cellClick; },
		get CellDoubleClick() { return cellDoubleClick; },
		get HeaderClick() { return headerClick; },
		get HeaderDoubleClick() { return headerDoubleClick; },
		get BeforeCellEdit() { return beforeCellEdit; },
		get AfterCellEdit() { return afterCellEdit; },

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
// GridHeaders
// ==================================================================
//#region GridHeaders
var GridHeaders = function (container) {
	var headers = [];
	var element = document.createElement("div");
	element.className = "gvheader";
	var added = new AppEvent();
	var updated = new AppEvent();
	var removed = new AppEvent();
	var sortingChanged = new AppEvent();
	var headerWidth = 0;
	var isSorted = false;
	var highlightColor = "#ffd800";

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
				Width: 100, MinWidth: 20, Visible: true,
				IsHighlighted: false, Type: "String",
				Editable: false, Map: null, Validations: {},
				Sortable: false, SortPosition: 0, SortDirection: ""
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
			if ("reference,string,bool,int,long,decimal,date,image".indexOf(dtype) < 0)
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
				var header = headers.First(p => p.Data.Field == prop);
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
		var caption = captions.First(c => c.Language == lang);
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
		constructor: GridHeaders,

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
			var h = headers.First(p => p.Data.Field == fieldName);
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
// GridRows
// ==================================================================
//#region GridRows
var GridRows = function (container, headers) {
	var dataSource = [];	// reference to the user data source (set by LoadData)
	var dataIndex = [];		// sorted index into the data source (used for determining display order)
	var rows = [];			// contains the information of each row being displayed in the grid and references to the html elements
	var selectedIndex = -1;	// The index of the last row that was selected (0-N), or -1 if nothing is selected.
	var multiselect = false;
	var highlightColor = "#ffd800";
	var beforeSelectedChanged = new AppEvent();
	var afterSelectedChanged = new AppEvent();
	var beforeAdded = new AppEvent();
	var afterAdded = new AppEvent();
	var beforeUpdated = new AppEvent();
	var afterUpdated = new AppEvent();
	var beforeRemoved = new AppEvent();
	var afterRemoved = new AppEvent();
	headers.Updated.Subscribe(handleHeaderUpdated);
	headers.SortingChanged.Subscribe(handleSorting);

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
	function addRowElements(q) {
		if (q == 0) return;
		if (q > 0) for (var i = 0; i < q; i++) addRowElement();
		else {
			var elms = rows.splice(rows.length + q, -q);
			for (var i = 0; i < elms.length; i++) deleteRowElement(elms[i]);
		}
	}

	function addRowElement() {
		var elm = document.createElement("div");
		elm.className = "gvrow";
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

	function updateRowInfo(idx) {
		var row = rows[idx];
		var data = dataIndex[idx].Data;
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
				if (rows[idx].Errors[i] != null) {
					content.style.color = "red";
					content.innerText = rows[idx].Errors[i].ProposedValue;
				}
				else {
					content.style.color = "";
					if (header.Type == "bool") {
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
					else {
						value = (value != null) ? value.toString() : "";
						content.innerText = value;
					}
				}
			}
		});
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
			var evt = raiseBeforeSelectedChanged(selectedIndex);
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
				var evt = raiseBeforeSelectedChanged(row.Index);
				if (evt.cancelled) return;
			}
			row.Selected = false;
			row.Element.className = row.Element.className.replace(" gvrowselected", "");
			if (raiseEvt) raiseAfterSelectedChanged();
		}
	}

	function raiseBeforeSelectedChanged(index) {
		var evt = {
			target: self,
			cancelled: false,
			rowIndex: index,
			rowData: self.Get(index),
			preventDefault: () => { evt.cancelled = true; }
		};
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
		var evt = raiseBeforeSelectedChanged(selectedIndex);
		if (evt.cancelled) return;
		if (!multiselect || clearPrevious) clearSelection(false);
		row.Selected = true;
		row.Element.className += " gvrowselected";
		selectedIndex = row.Index;
		raiseAfterSelectedChanged();
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
		constructor: GridRows,

		// Events
		get BeforeSelectedChanged() { return beforeSelectedChanged; },
		get AfterSelectedChanged() { return afterSelectedChanged; },
		get BeforeAdded() { return beforeAdded; },
		get AfterAdded() { return afterAdded; },
		get Updated() { return updated; },
		get BeforeRemoved() { return beforeRemoved; },
		get AfterRemoved() { return afterRemoved; },

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
			rows = [];
			dataSource = [];
			dataIndex = [];
		},

		LoadData: function (data) {
			dataSource = data;
			resetIndex();
			if (data.length != rows.length)
				addRowElements(data.length - rows.length);
			if (headers.IsSorted)
				handleSorting();
			else
				updateRows();
			if (selectedIndex >= data.length)
				clearSelection(true);
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
			var rowIdx = dataIndex.length;
			var cancelled = false;
			var e = {
				taget: self,
				rowIndex: rowIdx,
				rowData: record,
				preventDefault: function () { cancelled = true; }
			};
			beforeAdded.Raise(e);
			if (cancelled) return;
			addRowElement();
			dataSource.push(record);
			dataIndex.push({ Index: rowIdx, Data: record });
			if (headers.IsSorted)
				handleSorting();
			else
				updateRowInfo(rowIdx);
			afterAdded.Raise(e);
		},

		Update: function (idx, record) {
			var cancelled = false;
			var e = {
				taget: self,
				rowIndex: idx,
				rowData: record,
				preventDefault: function () { cancelled = true; }
			};
			beforeUpdated.Raise(e);
			if (cancelled) return;
			Object.assign(dataIndex[idx].Data, record); // This updates both the dataIndex and the dataSource as both reference the same record.
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
			Object.assign(dataIndex[rowidx].Data, currentValues); // This updates both the dataIndex and the dataSource as both reference the same record.
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
			setSelection(row, clearPrevious);
		},

		Unselect: function (idx) {
			clearSelected(rows[idx], true);
		},

		ClearSelection: function () {
			clearSelection(true);
		},

		IsSelected: function (idx, value) {
			if (idx == null) return;
			if (value == null)
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
// GridEditContext
// ==================================================================
//#region GridEditContext
var GridEditContext = function (info, callback) {
	var w = info.cellElement.clientWidth - 2;
	info.cellElement.editing = true;
	var elm = null;
	var map = info.headerData.Map;
	var htype = info.headerData.Type.toLowerCase() || "string";
	var validations = info.headerData.Validations || {};
	var value = info.proposedValue || info.currentValue || "";
	var elm = createElement(info.headerData);
	var datePicker = null;

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
				elm = createTextBox();
				if (validations["maxlen"] != null) {
					elm.attr("maxlength", validations["maxlen"].Value);
				}
			}
		}
		elm[0].userData = header.Index;
		return elm;
	}

	function createTextBox() {
		var elm = $(`<input type="text" value="${value}" class="gveditbox" style="width:${w}px;" />`);
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
				elm.append(`<option value=${op} selected style='color:#000'>${map[op]}</option>`)
			else
				elm.append(`<option value=${op} style='color:#000'>${map[op]}</option>`)
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
					datePicker = new GridDatePicker(elm, value, () => { value = datePicker.Value; self.Accept(); });
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
// GridDatePicker
// =========================================================================
// #region GridDatePicker
var GridDatePicker = function (element, value, onUpdate) {
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
