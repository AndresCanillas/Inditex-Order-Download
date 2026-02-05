


// =========================================================================
// ContextMenu
// =========================================================================
// #region ContextMenu

var ContextMenu = function (setup) {
	this.MenuWidth = setup.Width ? setup.Width : "250px";
	this.Options = setup.Options ? setup.Options : [];
};

ContextMenu.prototype = {
	Show: function (x, y) {
		var op, opElm;
		var self = this;
		if (ContextMenu.CurrentMenu) {
			ContextMenu.CurrentMenu.Close();
		}
		this.element = $(`<ul class="context-menu-list" style="width: ${this.MenuWidth}; left: 0px; top: 0px;">`);
		this.element.click(function (e) { self.handleClick(e); });
		this.element.css("left", x + "px");
		this.element.css("top", y + "px");
		for (var index = 0; index < this.Options.length; index++) {
			op = this.Options[index];
			if (op.IsSeparator === true)
				opElm = $(`<li class="context-menu-separator"></li>`);
			else {
				if (op.Disabled === true)
					opElm = $(`<li class="context-menu-item context-menu-disabled">${op.Text}</li>`);
				else
					opElm = $(`<li class="context-menu-item">${op.Text}</li>`);
			}
			this.element.append(opElm);
		}
		if (!ContextMenu.RegisteredGlobalHandlers) {
			ContextMenu.RegisteredGlobalHandlers = true;
			$("BODY").click(ContextMenu.handleGlobalClick);
			$("BODY").contextmenu(ContextMenu.handleGlobalContextMenu);
		}
		ContextMenu.CurrentMenu = this;
		$("BODY").append(this.element);
	},

	handleClick: function (e) {
		e.stopPropagation();
		var text = e.target.innerText;
		for (var index = 0; index < this.Options.length; index++) {
			var op = this.Options[index];
			if (op.Text === text && !op.Disabled) {
				this.Close();
				if (op.OnClick)
					op.OnClick.call(this.Target);
				break;
			}
		}
	},

	Close: function () {
		this.element.empty();
		this.element.remove();
		this.element = null;
		ContextMenu.CurrentMenu = null;
	},

	HandleMouseMove: function (e) {
		this.mouseX = e.pageX;
		this.mouseY = e.pageY;
	},

	HandleContextMenu: function (e) {
		e.preventDefault();
		e.stopPropagation();
		this.Show(this.mouseX, this.mouseY);
	}
};


ContextMenu.handleGlobalClick = function (e) {
	if (ContextMenu.CurrentMenu) {
		ContextMenu.CurrentMenu.Close();
	}
};

ContextMenu.handleGlobalContextMenu = function (e) {
	if (ContextMenu.CurrentMenu) {
		ContextMenu.CurrentMenu.Close();
	}
};

// #endregion



// =========================================================================
// TreeNode
// =========================================================================
// #region TreeNode

var TreeNode = function (nodeType, id, text, iconClass) {
	var self = this;
	this.NodeType = nodeType;
	this.EntityID = id;
	this.NodeID = nodeType + "_" + id;
	this.nodeText = text;
	this.Expanded = false;
	this.Nodes = [];
	this.IsLoaded = false;
	this.CanEdit = false;
	this.element = $(`<div class="treeNode"></div>`);
	this.bulletElm = $(`<div class="collapsedNode"></div>`);
	if (iconClass != null)
		this.iconElm = $(`<div class="treeNodeIcon"><span class="${iconClass}"></span></div>`);
	else
		this.iconElm = null;
	this.textElm = $(`<div class="treeNodeText">${text}</div>`);
	this.childenElm = $(`<div style="display:none"></div>`);
	this.element[0]._node = this;
	this.element.append(this.bulletElm);
	if (this.iconElm != null)
		this.element.append(this.iconElm);
	this.element.append(this.textElm);
	this.element.click(function (e) { self.handleClick(e); });
	this.element.dblclick(function (e) { self.handleDblClick(e); });
	this.bulletElm.click(function (e) { self.Toggle(); });
};

TreeNode.Extend = function (target, nodeType, entityid, nodeText) {
	TreeNode.call(target, nodeType, entityid, nodeText);
	ExtendObj(target, TreeNode.prototype);
};

TreeNode.prototype = {
	getText: function () {
		return this.nodeText;
	},

	setText: function (text) {
		var originalText = this.nodeText;
		this.nodeText = text;
		this.textElm.text(text);
		if (this.OnTextChanged)
			this.OnTextChanged(originalText);
	},

	getLevel: function () {
		return this.nodeLevel;
	},

	setLevel: function (level) {
		this.nodeLevel = level;
		var padding = "{0}px".Format(level * 20);
		this.element.css("padding-left", padding);
		for (var index = 0; index < this.Nodes.length; index++) {
			this.Nodes[index].setLevel(level + 1);
		}
	},

	getIsSelected: function () {
		return this.isSelected;
	},

	setIsSelected: function (value) {
		this.isSelected = value;
		this.element.toggleClass("selectedNode", value === true);
		if (!value && this.Editing)
			this.handleEditKeyPress({ keyCode: 13, stopPropagation: function () { } });
	},

	getTreeView: function () {
		var parent = this.Parent;
		while (parent && parent.Parent) {
			parent = parent.Parent;
		}
		return parent;
	},

	getViewContainer: function () {
		var tree = this.getTreeView();
		return tree.TargetViewContainer;
	},

	AddNode: function (node, prepend) {
		if (prepend) {
			this.Nodes.splice(0, 0, node);
			this.childenElm.prepend(node.childenElm);
			this.childenElm.prepend(node.element);
		}
		else {
			this.Nodes.push(node);
			this.childenElm.append(node.element);
			this.childenElm.append(node.childenElm);
		}
		node.Parent = this;
		node.setLevel(this.nodeLevel + 1);
		this.bulletElm.attr("class", this.Expanded ? "expandedNode" : "collapsedNode");
		if (this.OnNodeAdded)
			this.OnNodeAdded();
		var tree = this.getTreeView();
		if (tree)
			tree.notifyNodesChanged();
	},

	RemoveNode: function (node) {
		var index = this.Nodes.indexOf(node);
		if (index >= 0) {
			if (node.OnRemoved)
				node.OnRemoved();
			var container = this.getViewContainer();
			if (container.length > 0 && container[0].CurrentView && container[0].CurrentView.NodeID == node.NodeID)
				container[0].CurrentView.Unload();
			this.Nodes.splice(index, 1);
			node.element.remove();
			node.childenElm.remove();
			node.Parent = null;
			if (this.Nodes.length === 0)
				this.bulletElm.attr("class", "emptyNode");
			if (this.OnNodeRemoved)
				this.OnNodeRemoved();
			var tree = this.getTreeView();
			if (tree)
				tree.notifyNodesChanged();
		}
	},

	Toggle: function () {
		if (this.Expanded)
			this.Collapse();
		else
			this.Expand();
	},

	Expand: function () {
		if (this.Expanded) return;
		this.Expanded = true;
		if (this.OnExpanded)
			this.OnExpanded();
		this.childenElm.css("display", "block");
		if (this.Nodes.length > 0)
			this.bulletElm.attr("class", "expandedNode");
		else
			this.bulletElm.attr("class", "emptyNode");
	},

	Collapse: function () {
		if (!this.Expanded) return;
		this.Expanded = false;
		if (this.OnCollapsed)
			this.OnCollapsed();
		this.childenElm.css("display", "none");
		if (this.Nodes.length > 0)
			this.bulletElm.attr("class", "collapsedNode");
		else
			this.bulletElm.attr("class", "emptyNode");
	},

	FindNode: function (nodeType, id, searchSubNodes) {
		for (var index = 0; index < this.Nodes.length; index++) {
			var node = this.Nodes[index];
			if (node.NodeType == nodeType && node.EntityID == id) {
				return node;
			} else if (searchSubNodes && node.Nodes.length > 0) {
				var found = node.FindNode(nodeType, id, searchSubNodes);
				if (found) return found;
			}
		}
		return null;
	},

	FindParent: function (nodeType) {
		var parent = this.Parent;
		while (parent != null && parent.Parent != null) {
			parent = parent.Parent;
			if (parent.NodeType === nodeType)
				return parent;
		}
		return null;
	},

	BeginEdit: function () {
		if (this.Editing)
			return;
		var startEdit = true;
		var self = this;
		this.originalText = this.nodeText;
		if (this.OnEditStarting)
			startEdit = this.OnEditStarting();
		if (startEdit) {
			this.textElm.text("");
			this.editElm = $(`<input type="text" value="${this.nodeText}" />`);
			this.textElm.append(this.editElm);
			this.editElm.keypress(function (e) { self.handleEditKeyPress(e); });
			this.Editing = true;
			setTimeout(function () { self.editElm.focus(); self.editElm.select(); }, 100);
		}
	},

	Remove: function () {
		this.Parent.RemoveNode(this);
	},

	handleClick: function (e) {
		var tree = this.getTreeView();
		var csn = tree.SelectedNode;
		if (csn !== this)
			tree.SelectedNode = this;
		if (this.OnClick)
			this.OnClick();
	},

	handleDblClick: function (e) {
		this.Toggle();
		if (this.OnDoubleClick) {
			this.OnDoubleClick();
			if ($(window).outerWidth() < 991 && window.toggleMenu)
				window.ToggleMenu();
		}
	},

	handleEditKeyPress: function (e) {
		e.stopPropagation();
		var self = this;
		var endResult = true;
		var keycode = e.keyCode ? e.keyCode : e.which;
		if (keycode === 13) {
			var newText = this.editElm.val();
			if (this.OnEditEnding)
				endResult = this.OnEditEnding(newText);
			this.editElm.remove();
			if (endResult) {
				if (endResult.then) {
					endResult
						.then(function (success) {
							if (success) {
								self.setText(newText);
								var container = self.getViewContainer();
								if (container.length > 0 && container[0].CurrentView && container[0].CurrentView.NodeID == self.NodeID)
									container[0].CurrentView.model.Name = newText;
							}
							else self.setText(self.originalText);
						})
						.catch(function () { self.setText(self.originalText); });
				}
				else this.setText(newText);
			}
			else this.setText(this.originalText);
			this.Editing = false;
		}
    },

    RemoveNodes: function () {
        while (this.Nodes.length > 0) {
            this.RemoveNode(this.Nodes[0]);
        }    
    }
};
// #endregion



// =========================================================================
// TreeView
// =========================================================================
// #region TreeView

var TreeView = function (htmlElement) {
	var self = this;
	this.container = AppContext.GetContainerElement(htmlElement);
	this.container.css({ "overflow": "scroll" });
	this.container.contextmenu(function (e) { self.handleContextMenu(e); });
	this.container.mousemove(function (e) { self.handleMouseMove(e); });
	this.selectedNode = null;
	this.Nodes = [];
};

TreeView.prototype = {
	constructor: TreeView,

	get SelectedNode() {
		return this.selectedNode;
	},

	set SelectedNode(node) {
		if (this.selectedNode)
			this.selectedNode.setIsSelected(false);
		this.selectedNode = node;
		if (this.selectedNode)
			this.selectedNode.setIsSelected(true);
	},

	AddNode: function (node) {
		this.Nodes.push(node);
		this.container.append(node.element);
		this.container.append(node.childenElm);
		node.Parent = this;
		node.setLevel(0);
	},

	RemoveNode: function (node) {
		var index = this.Nodes.indexOf(node);
		if (index >= 0) {
			this.Nodes.splice(index, 1);
			node.element.remove();
			node.childenElm.remove();
			node.Parent = null;
		}
	},

	FindNode: function (nodeType, id, searchSubNodes) {
		for (var index = 0; index < this.Nodes.length; index++) {
			var node = this.Nodes[index];
			if (node.NodeType == nodeType && node.EntityID == id) {
				return node;
			} else if (searchSubNodes && node.Nodes.length > 0) {
				var found = node.FindNode(nodeType, id, searchSubNodes);
				if (found) return found;
			}
		}
		return null;
	},

	ApplyFilter: function (filter) {
		this.filter = filter.toLowerCase();
		$.each(this.container.find("div .treeNodeText"), function (index, item) {
			var nodeText = item.innerText.toLowerCase();
			if (nodeText.indexOf(filter) < 0)
				item.parentNode.style.display = "none";
			else
				item.parentNode.style.display = "inline-block";
		});
	},

	handleContextMenu: function (e) {
		e.preventDefault();
		e.stopPropagation();
		if (this.nodeUnderMouse) {
			console.log("context menu {0}({1}, {2})".Format(this.nodeUnderMouse.getText(), this.mouseX, this.mouseY));
			this.SelectedNode = this.nodeUnderMouse;
			if (this.SelectedNode.OnContextMenuOpening)
				this.SelectedNode.OnContextMenuOpening.call(this.SelectedNode);
			if (this.SelectedNode.GetContextMenu) {
				var menuSetup = this.SelectedNode.GetContextMenu();
				if (menuSetup) {
					var menu = new ContextMenu(menuSetup);
					menu.Target = this.SelectedNode;
					menu.Show(this.mouseX, this.mouseY);
				}
			}
		}
		else console.log("context menu ({0}, {1})".Format(this.mouseX, this.mouseY));
	},

	handleMouseMove: function (e) {
		this.mouseX = e.pageX;
		this.mouseY = e.pageY;
		var nodeElm = e.target;
		if (!nodeElm._node)
			nodeElm = nodeElm.parentNode;
		if (nodeElm._node)
			this.nodeUnderMouse = nodeElm._node;
		else
			this.nodeUnderMouse = null;
	},

	notifyNodesChanged: function (e) {
		var w = this.container[0].scrollWidth;
		if (w > 16)
			this.container.find("> div").width(w - 16);
	}
};
// #endregion


// =========================================================================
// Paginator
// =========================================================================
// #region Paginator
/*

options = {
	PageSize = num,
	CurrentPage = num,
	TotalPages = num

}

*/
var Paginator = function (options) {
    this.options = getDefaults();

    function getDefaults() {
        return $.extend({
            ContainerElm: null, // required
            Controller: null, // required
            LinkClick: 'GoToPage', // controller action method
            PageSize: 20,
            CurrentPage: 1,
            TotalRecords: 0,
            SizeCls: '', // pagination-lg or pagination-sm
            AlignCls: '' // justify-content-center or justify-content-end
        }, options);
    }
}

Paginator.prototype = {
    constructor: Paginator,

    Render: function () {

        var self = this;

        if (self.options.ContainerElm == null) {
            return;
        }

        var allLinks = self._pagination();

        var content = self._wrapperContent(allLinks.join(''));

        self.options.ContainerElm.html(content);

        AppContext.BindActions(self.options.ContainerElm, self.options.Controller);

    },

    _totalPages: function () {

        var otp = this.options;

        return Math.ceil(otp.TotalRecords / otp.PageSize);

    },

    _wrapperContent: function (content) {
        return `<ul class="pagination ${this.options.AlignCls} ${this.options.SizeCls} ">${content}</ul>`;
    },

    _wrapperLink: function (options) {

        options = $.extend({
            Link: '',
            Cls: '',
            IsDisable: false,
            IsActive: false
        }, options);

        if (options.IsDisable) {
            options.Cls = options.Cls + ' disabled'
        }

        if (options.IsActive) {
            options.Cls = options.Cls + ' active'
        }


        return `<li class="page-item ${options.Cls}">${options.Link}</li>`;
    },

    _firstLinkTemplate: function (position) {
        return `<a class="page-link" href="#" aria-label="Previous" action="${this.options.LinkClick}" data-position="${position}">
					<span aria-hidden="true">${position}</span>
					<span class="sr-only">Previous</span>
				</a>`;
    },

    _linkTemplate: function (position) {

        return `<a class="page-link" href="#" action="${this.options.LinkClick}" data-position="${position}">${position}</a>`
    },

    _lastLinkTemplate: function (position) {
        return `<a class="page-link" href="#" aria-label="Next" action="${this.options.LinkClick}" data-position="${position}">
					<span aria-hidden="true">&raquo;</span>
					<span class="sr-only">Next</span>
				</a>`;
    },

    // https://gist.github.com/kottenator/9d936eb3e4e3c3e02598
    _pagination: function () {
        var self = this;

        var options = self.options;

        var current = options.CurrentPage,
            last = self._totalPages(),
            delta = 3,
            left = current - delta,
            right = current + delta + 1,
            range = [],
            rangeWithDots = [],
            l;

        for (let i = 1; i <= last; i++) {
            if (i == 1 || i == last || i >= left && i < right) {
                range.push(i);
            }
        }

        for (let i of range) {
            if (l) {

                if (i - l === 2) {
                    //rangeWithDots.push(l + 1);

                    var linkOtions = {
                        Link: self._linkTemplate(l + 1)
                    };

                    rangeWithDots.push(self._wrapperLink(linkOtions));
                } else if (i - l !== 1) {
                    //rangeWithDots.push('...');
                    var linkOtions = {
                        Link: self._linkTemplate('...'),
                        IsDisable: true
                    };
                    rangeWithDots.push(self._wrapperLink(linkOtions));
                }


            }


            var mark = current == i;

            if (rangeWithDots.length == 0) {

                var linkOtions = {
                    Link: self._linkTemplate(i),
                    IsDisable: mark
                };

                rangeWithDots.push(self._wrapperLink(linkOtions));
            } else if (i == last) {

                var linkOtions = {
                    Link: self._linkTemplate(i),
                    IsDisable: mark
                };

                rangeWithDots.push(self._wrapperLink(linkOtions));

            } else {

                var linkOtions = {
                    Link: self._linkTemplate(i),
                    IsActive: mark
                };

                rangeWithDots.push(self._wrapperLink(linkOtions));
            }
            l = i;
        }

        return rangeWithDots;
    }
}

// #endregion