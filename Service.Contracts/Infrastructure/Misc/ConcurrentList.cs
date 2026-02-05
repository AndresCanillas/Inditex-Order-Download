using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	public class ConcurrentList<T> : IEnumerable<T>
	{
		private object syncObj = new object();
		private List<T> list = new List<T>();

		public int Count
		{
			get
			{
				lock (syncObj)
					return list.Count;
			}
		}

		public void Add(T element)
		{
			lock (syncObj)
				list.Add(element);
		}

		public void Remove(T element)
		{
			lock (syncObj)
				list.Remove(element);
		}

		public void Clear()
		{
			lock (syncObj)
				list.Clear();
		}

		public IEnumerator<T> GetEnumerator()
		{
			List<T> snapshot;
			lock (syncObj)
				snapshot = new List<T>(list);

			foreach (var element in snapshot)
				yield return element;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
