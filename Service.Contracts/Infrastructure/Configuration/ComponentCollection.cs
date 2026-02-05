using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
    public class ComponentCollection : IEnumerable<Type>
    {
        private ConcurrentDictionary<Type, int> componentIndex = new ConcurrentDictionary<Type, int>();
        private ConcurrentDictionary<Type, ConcurrentDictionary<Type, int>> implIndex = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, int>>();

        public void RegisterComponentType(Type componentType)
        {
            componentIndex[componentType] = 1;
        }

        public void Add<Contract, Implementation>()
			where Contract : class
			where Implementation: class, Contract
		{
			Add(typeof(Contract), typeof(Implementation));
		}

		public void Add(Type contract, Type implementation)
		{
			if (!contract.IsInterface)
				throw new InvalidOperationException("Component Contract Type must be an interface.");
			ConcurrentDictionary<Type, int> implementations;
			if (!implIndex.TryGetValue(contract, out implementations))
			{
				implementations = new ConcurrentDictionary<Type, int>();
				if (!implIndex.TryAdd(contract, implementations))
					implementations = implIndex[contract];
			}
			implementations.TryAdd(implementation, 0); // Ignore if component was already registered
		}

		public bool Contains(Type contract)
		{
			return componentIndex.ContainsKey(contract);
		}

		public IEnumerable<Type> GetImplementations(Type contract)
		{
			ConcurrentDictionary<Type, int> implementations;
			if (implIndex.TryGetValue(contract, out implementations))
			{
				foreach(var key in implementations.Keys)
					yield return key;
			}
		}

		public IEnumerator<Type> GetEnumerator()
		{
			return implIndex.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return implIndex.Keys.GetEnumerator();
		}
	}
}
