using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Misc
{
	public class Node<T>
	{
		internal int level;
		internal Node<T> parent;
		private NodeCollection<T> children;
		private T data;

		public Node(T data)
		{
			children = new NodeCollection<T>(this);
			this.data = data;
		}

		public int Level => level;
		public Node<T> Parent => parent;
		public NodeCollection<T> Children => children;
		public T NodeData => data;
	}

	public class NodeCollection<T> : IEnumerable<Node<T>>
	{
		private Node<T> parent;
		private List<Node<T>> nodes = new List<Node<T>>();

		public NodeCollection(Node<T> parent)
		{
			this.parent = parent;
		}

		public int Count { get => nodes.Count; }
		public Node<T> this[int index] { get => nodes[index]; }
		public IEnumerator<Node<T>> GetEnumerator() => nodes.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => nodes.GetEnumerator();

		public void Add(Node<T> node)
		{
			node.parent = parent;
			node.level = parent.Level + 1;
			nodes.Add(node);
		}
	}
}
