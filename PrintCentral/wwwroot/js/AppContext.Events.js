// =========================================================================
// GenericEventListener
// =========================================================================
// #region GenericEventListener
AppContext.GenericEventListener = function (url, controller) {
	var socket = null;
	var loc = window.location;
	var protocol = (loc.protocol === "https:") ? "wss:" : "ws:";
	var fullUrl = protocol + "//" + loc.host + url;
	var connected = false;
	var disposed = false;
	var reconnecting = false;

	var interface = {
		Open: function () {
			try {
				if (disposed) return;
				socket = new WebSocket(fullUrl);
				socket.binaryType = 'blob';
				socket.onopen = function () {
					connected = true;
					if (controller.OnOpen)
						controller.OnOpen();
				};
				socket.onclose = function () {
					if (controller.OnClose)
						controller.OnClose();
					connected = false;
					if (!disposed && !reconnecting) {
						reconnecting = true;
						setTimeout(interface.Open, 10000);
					}
				};
				socket.onerror = function (evt) {
					if (controller.OnError)
						controller.OnError();
					else
						AppContext.ShowError("Disconnected from the server");
					if (!disposed && !reconnecting) {
						reconnecting = true;
						setTimeout(interface.Open, 10000);
					}
				};
				socket.onmessage = function (e) {
					if (!e.data) return;
					var eventData = JSON.parse(e.data);
					if (controller.OnReceive)
						controller.OnReceive(eventData);
				};
				reconnecting = false;
			}
			catch (err) {
				if (!disposed && !reconnecting) {
					reconnecting = true;
					setTimeout(interface.Open, 10000);
				}
			}
		},

		IsConnected: function () {
			return connected;
		},

		Dispose: function () {
			if (!disposed) {
				disposed = true;
				socket.onopen = null;
				socket.onclose = null;
				socket.onerror = null;
				socket.onmessage = null;
				try {
					socket.close();
				}
				catch (error) { }
				controller = null;
				connected = false;
			}
		}
	};

	return interface;
};
//#endregion



// =========================================================================
// AppEventListener
// =========================================================================
// #region AppEventListener
AppContext.AppEventListener = (function () {
	var subscribers = [];
	var created = false;
	var listenerController = {
		OnReceive: function (eventData) {
			for (var i = 0; i < subscribers.length; i++) {
				var subscriber = subscribers[i];
				try {
					subscriber.Callback.call(subscriber.Target, eventData);
				}
				catch (error) {
					console.log(error);
				}
			}
		}
	};
	var listener = null;

	function CreateListener() {
		listener = new AppContext.GenericEventListener("/events/listen", listenerController);
		listener.Open();
		created = true;
	}

	function DisposeListener() {
		if (created) {
			created = false;
			listener.Dispose();
			listener = null;
		}
	}

	return {
		Subscribe: function (target, callback) {
			var subscriber = { Target: target, Callback: callback };
			subscribers.push(subscriber);
			if (!created) CreateListener();
		},

		Unsubscribe: function (target, callback) {
			for (var i = 0; i < subscribers.length; i++) {
				if (subscribers[i].Target === target && subscribers[i].Callback === callback)
					subscribers.splice(i, 1);
			}
			if (subscribers.length === 0) DisposeListener();
		}
	};
})();
//#endregion



// =========================================================================
// PrinterJobEvents
// =========================================================================
// #region PrinterJobEvents
AppContext.PrinterJobEvents = function (target, callback) {
	var Target = target;
	var Callback = callback;

	function HandleEvent(eventData) {
		if (eventData.EventName === "PrinterJobEvent") {
			Callback.call(Target, eventData.Type, eventData.Data);
		}
	}

	return {
		Listen: function () {
			AppContext.AppEventListener.Subscribe(this, HandleEvent);
		},

		Dispose: function () {
			AppContext.AppEventListener.Unsubscribe(this, HandleEvent);
		}
	};
};
//#endregion



// =========================================================================
// AppEvent
// =========================================================================
// #region AppEvent
var AppEvent = function () {
	var subscribers = [];
	var disposed = false;

	return {
		constructor: AppEvent,

		Subscribe: function (target, callback) {
			if (target == null)
				throw "target argument cannot be null";
			if (target != null && typeof target === "function")
				subscribers.push({ Target: null, Callback: target });
			else
				subscribers.push({ Target: target, Callback: callback });
		},

		Unsubscribe: function (callback) {
			for (var i = 0; i < subscribers.length; i++) {
				if (subscribers[i].Callback === callback) {
					var elm = subscribers.splice(i, 1);
					elm[0].Callback = null;
					elm[0].Target = null;
					return;
				}
			}
		},

		Raise: function (e) {
			for (var i = 0; disposed == false && i < subscribers.length; i++) {
				var sub = subscribers[i];
				if (sub.Target != null)
					sub.Callback.call(sub.Target, e);
				else
					sub.Callback(e);
			}
		},

		Clear: function () {
			for (var i = 0; disposed == false && i < subscribers.length; i++) {
				subscribers[i].Callback = null;
				subscribers[i].Target = null;
			}
			subscribers = [];
		},

		Dispose: function () {
			disposed = true;

			this.Clear();
			subscribers = null;
		}
	};
};
// #endregion

