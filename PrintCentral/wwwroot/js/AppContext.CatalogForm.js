// =========================================================================
// CatalogForm
// =========================================================================

AppContext.CatalogForm = function (catalogdef, lang) {
	var formContent = `
	<viewrow>
		<viewcolumn>
			<viewform>`;

	for (var f of catalogdef) {
		if (f.IsHidden)
			createHidden(f);
		else if (hasFixedOptions(f))
			createSelect(f);	// Any Int, Long, Decimal or String with FixedOptions will be represented as combobox instead of textbox
		else {
			switch (parseInt(f.Type, 10)) {
				case 1: // Reference
					createReference(f);
					break;
				case 2: // Int
					createNumber(f, 10);
					break;
				case 3: // Long
					createNumber(f, 14);
					break;
				case 4: // Decimal
					createFloat(f, 16);
					break;
				case 5: // Bool
					createBool(f);
					break;
				case 6: // Date
					createDate(f);
					break;
				case 7: // String
					createString(f);
					break;
				case 8: // Image
					createImage(f);
					break;
				case 9: // Set
					createSet(f);
					break;
				case 10: // File
					createFile(f);
					break;
			}
		}
	}

	formContent += `
			</viewform>
		</viewcolumn>
	</viewrow>`;

	return formContent;


	function isReference(f) {
		return parseInt(f.Type, 10) == 1;
	}

	function isNumeric(f) {
		var nT = parseInt(f.Type, 10);
		return (nT >= 2 && nT <= 4);
	}

	function isBool(f) {
		return parseInt(f.Type, 10) == 5;
	}

	function isDate(f) {
		return parseInt(f.Type, 10) == 6;
	}

	function isString(f) {
		return parseInt(f.Type, 10) == 7;
	}

	function isImage(f) {
		return parseInt(f.Type, 10) == 8;
	}

	function isSet(f) {
		return parseInt(f.Type, 10) == 9;
	}

	function hasFixedOptions(f) {
		if (f.Functions != null && f.Functions["FixedOptions"] != null)
			return ([2, 3, 4, 7].find(p => p == f.Type) != null);
		return false;
	}

	function getFixedOptions(f) {
		if (f.Functions != null && f.Functions["FixedOptions"] != null)
			return `options="${f.Functions["FixedOptions"].Data}"`;  // Data must hold a string with a JSON array such as: '[{"Value":X, "Name":"The text"}, {},...]'
		else
			return "";
	}

	function getCaption(f) {
		var caption = f.Name;
		if (!String.isNullOrEmpty(f.Captions)) {
			var captions = JSON.parse(f.Captions);
			var found = captions.first(c => c.Language == lang);
			if (found != null) caption = found.Text;
		}
		if (!f.CanBeEmpty)
			return caption + ":*";
		return caption + ":";
	}

	function getValidations(f, maxlen) {
		// Example of a typical return value: "required;numeric;maxlen(6);"
		var result = "";
		if (!f.CanBeEmpty) result += "required;"
		if (maxlen != null) result += `maxlen(${maxlen});`;
		if (isNumeric(f)) {
			result += "numeric;";
			if (f.MinValue != null && f.MaxValue != null)
				result += `range(${f.MinValue},${f.MaxValue});`;
		}
		if (isString(f)) {
			if (f.ValidChars != null && !String.isNullOrEmpty(f.ValidChars)) result += `chars(${f.ValidChars});`;
			if (f.Regex != null && !String.isNullOrEmpty(f.Regex)) result += `regex(${f.Regex});`;
		}
		if (isImage(f)) {
			if (f.MaxWidth != null && f.MaxHeight != null)
				result += `maxsize(${f.MaxWidth},${f.MaxHeight});`;
		}
		return result;
	}

	function getDataTypeAttr(f) {
		switch (parseInt(f.Type, 10)) {
			case 1:
			case 2:
			case 3: return 'data-type="int"'
			case 4: return 'data-type="decimal"';
			case 5: return 'data-type="bool"';
			case 6: return 'data-type="date"';
		}
		return "";
	}

	function createHidden(f) {
		var ctn = `<input model name="${f.Name}" type="hidden" ${getDataTypeAttr(f)} />`;
		formContent += ctn;
	}

	function createNumber(f, maxlen) {
		maxlen = f.Length || maxlen;
		var ctn = `<input model name="${f.Name}" label="${getCaption(f)}" validation="${getValidations(f, maxlen)}" type="text" data-type="int" maxlength="${maxlen}" />`;
		formContent += ctn;
	}

	function createFloat(f, maxlen) {
		maxlen = f.Length || maxlen;
		var ctn = `<input model name="${f.Name}" label="${getCaption(f)}" validation="${getValidations(f, maxlen)}" type="text" data-type="decimal" maxlength="${maxlen}" />`;
		formContent += ctn;
	}

	function createBool(f) {
		var ctn = `<input model name="${f.Name}" label="${getCaption(f)}" type="checkbox" style="margin-right: 10px;" data-type="bool" />`;
		formContent += ctn;
	}

	function createDate(f) {
		var maxlen = f.Length;
		var ctn = `<input model name="${f.Name}" label="${getCaption(f)}" validation="${getValidations(f, maxlen)}" type="date" data-type="date" />`;
		formContent += ctn;
	}

	function createString(f) {
		var maxlen = f.Length || 1000;
		var ctn = `<input model name="${f.Name}" label="${getCaption(f)}" validation="${getValidations(f, maxlen)}" type="text" maxlength="${maxlen}" />`;
		formContent += ctn;
	}

	function getSelectDataType(f) {
		switch (parseInt(f.Type, 10)) {
			case 2:
			case 3:	return 'data-type="int"';
			case 4: return 'data-type="decimal"';
			default: return "";
		}
	}

	function createSelect(f) {
		var ctn = `<select model name="${f.Name}" label="${getCaption(f)}" validation="${getValidations(f)}" ${getSelectDataType(f)} ${getFixedOptions()} />`;
		formContent += ctn;
	}

	function createReference(f) {
		var ctn = `
		<div class="form-group row" style="margin-bottom:5px;">
			<input model name="${f.Name}" type="hidden" />
			<label class="col-sm col-form-label">${getCaption(f)}</label>
			<div class="col-sm cat-ref">
				<div action="OpenRef" fieldname="${f.Name}" class="fa fa-external-link cat-refbtn"></div>
				<div action="AssignRef" fieldname="${f.Name}" class="fa fa-search cat-refbtn"></div>
				<div model name="_${f.Name}_DISP" no-transform class="cat-reftext">Loading...</div>
			</div>
		</div>`;
		formContent += ctn;
	}

	function createImage(f) {
		var ctn = `<input model name="${f.Name}" label="${getCaption(f)}" type="hidden" data-type="image" validation="${getValidations(f)}" accept=".png, .jpg, .jpeg, .gif">`;
		formContent += ctn;
	}

	function createFile(f) {
		var ctn = `<input model name="${f.Name}" label="${getCaption(f)}" type="hidden" data-type="file" validation="${getValidations(f)}" accept=".xls, .xlsx, .doc, .docx, .rtf, .pdf, .txt, .zip">`;
		formContent += ctn;
	}

	function createSet(f) {
		var ctn = `
		<div class="form-group row" style="margin-bottom:5px; padding:10px; padding-bottom:60px;">
			<viewtitle style="width:100%">${getCaption(f)}</viewtitle>
			<input model name="${f.Name}" type="hidden" />
			<div name="_${f.Name}_Grid" component="FormView" controller="/Common/Components/CatalogGridView.js" style="width:100%; height:25vh;" />
		</div>`;
		formContent += ctn;
	}
};

