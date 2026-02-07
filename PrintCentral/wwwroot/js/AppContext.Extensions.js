
var ExtendObj = function (target, base) {
	for (var prop in base) {
		if (base.hasOwnProperty(prop)) {
			target[prop] = base[prop];
		}
	}
};


String.prototype.format = function () {
	var args = arguments;
	return this.replace(/\{\{|\}\}|\{(\d+)\}/g, function (m, n) {
		if (m === "{{") { return "{"; }
		if (m === "}}") { return "}"; }
		return args[n];
	});
};
String.prototype.Format = String.prototype.format;


String.isNullOrEmpty = function (str) {
	if (str != null && str.length != null && str.replace != null)
		return str == null || str.length == 0 || !str.replace(/\s/g, '').length;
	else
		return str == null;
};
String.IsNullOrEmpty = String.isNullOrEmpty;

String.prototype.isNullOrEmpty = function () {
	return String.isNullOrEmpty(this);
};
String.prototype.IsNullOrEmpty = String.prototype.isNullOrEmpty;

String.isString = function (value) {
	var td = typeof value;
	var dd2 = value instanceof String;
	return (typeof value === 'string' || value instanceof String)
}
String.IsString = String.isString;

String.prototype.toDate = function () {
	return new Date(this);
}

Date.prototype.toTicks = function () {
	return (621355968e9 + this.getTime() * 1e4);
};
Date.prototype.ToTicks = Date.prototype.toTicks;

Date.prototype.addDays = function (days) {
	var date = new Date(this.valueOf());
	date.setDate(date.getDate() + days);
	return date;
};
Date.prototype.AddDays = Date.prototype.addDays;

Date.isDate = function (value) {
	var td = typeof value;
	var dd2 = value instanceof Date;
	return (typeof value === 'date' || value instanceof Date);
};
Date.IsDate = Date.isDate;

Date.prototype.toShortString = function () {
	return (this.getMonth() + 1) + "/" + this.getDate() + "/" + this.getFullYear() + " " + this.getHours() + ":" + this.getMinutes() + ":" + this.getSeconds();
};

Array.prototype.where = function (predicate) {
	var result = [];
	for (var i = 0; i < this.length; i++) {
		if (predicate(this[i]) === true)
			result.push(this[i]);
	}
	return result;
};
Array.prototype.Where = Array.prototype.where;

Array.prototype.groupBy = function (predicate) {
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
Array.prototype.GroupBy = Array.prototype.groupBy;

Array.prototype.first = function (predicate) {
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
Array.prototype.First = Array.prototype.first;

Array.prototype.index = function (predicate) {
	if (predicate == null) {
		if (this.length > 0)
			return 0;
		else
			return -1;
	}
	for (var i = 0; i < this.length; i++) {
		if (predicate(this[i]) === true)
			return i;
	}
	return -1;
};
Array.prototype.IndexOf = Array.prototype.index;

Array.prototype.exists = function (predicate) {
	if (predicate == null) return false;
	for (var i = 0; i < this.length; i++) {
		if (predicate(this[i]) === true)
			return true;
	}
	return false;
};
Array.prototype.Exists = Array.prototype.exists;

Array.prototype.removeAll = function (predicate) {
	for (var i = 0; i < this.length; i++) {
		if (predicate(this[i]) === true) {
			this.splice(i, 1);
			i--;
		}
	}
};
Array.prototype.RemoveAll = Array.prototype.removeAll;

Array.prototype.Join = function (predicate, separator) {
	if (predicate == null) return "";
	if (separator == null)
		separator = ",";
	var result = "";
	for (var i = 0; i < this.length; i++) {
		result += predicate(this[i]);
		if (i < this.length - 1)
			result += separator;
	}
	return result;
}

jQuery.fn.findTags = function (tagname, searchChildren) {
	tagname = tagname.toUpperCase();
	var divein = (searchChildren === true);
	var selection = [];
	var set = this;
	for (var i = 0; i < set.length; i++) {
		if (set[i].tagName === tagname)
			selection.push(set[i]);
		if (divein) {
			var subSet = $(set[i]).children().findTags(tagname, divein);
			if (subSet.length > 0)
				for (var j = 0; j < subSet.length; j++)
					selection.push(subSet[j]);
		}
	}
	return $(selection);
};


jQuery.fn.findFirst = function (tagname) {
	tagname = tagname.toUpperCase();
	var set = this;
	for (var i = 0; i < set.length; i++) {
		if (set[i].tagName === tagname) {
			return $([set[i]]);
		}
		var subSet = $(set[i]).children();
		if (subSet.length > 0) {
			var childSearch = subSet.findFirst(tagname);
			if (childSearch.length > 0)
				return $([childSearch[0]]);
		}
	}
	return $([]);
};


function toggleMenu() {
	var leftMenu = $(".left-menu");
	var mainPanel = $(".main-panel");
	leftMenu.toggleClass("left-menu-collapse");
	mainPanel.toggleClass("main-panel-collapse");
}
