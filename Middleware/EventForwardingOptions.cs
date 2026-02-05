using Service.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Middleware
{
	public class EventForwardingOptions
	{
		private ConcurrentDictionary<string, Type> publisehdEvents = new ConcurrentDictionary<string, Type>();

		public EventForwardingOptions() { }

		public EventForwardingOptions Allow<T>() where T: EQEventInfo
		{
			var t = typeof(T);
			publisehdEvents.TryAdd(t.Name, t);
			return this;
		}

		public bool IsAllowed(EQEventInfo e)
		{
			if (e == null) return false;
			return publisehdEvents.ContainsKey(e.GetType().Name);
		}
	}
}
