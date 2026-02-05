using Rebex.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class TaskNode<TItem>
		where TItem: WorkItem
	{
		private TaskNode<TItem> parent;
		private TaskDescriptor<TItem> descriptor;
		private List<TaskDescriptor<TItem>> exceptionHandlers = new List<TaskDescriptor<TItem>>();
		private TaskNodeCollection<TItem> children = new TaskNodeCollection<TItem>();
		private List<string> routes = new List<string>();
		private bool hasWakeTimeout;
		private TimeSpan wakeTimeout;

		public TaskNode()
		{
			parent = null;
			descriptor = new TaskDescriptor<TItem>() { Name = "Insert", NodeType = WFNodeType.RootBlock };
		}

		public TaskNode(TaskNode<TItem> parent)
		{
			this.parent = parent;
			descriptor = new TaskDescriptor<TItem>() { NodeType = WFNodeType.RootBlock };
		}

		public TaskNode(TaskNode<TItem> parent, TaskDescriptor<TItem> descriptor)
		{
			this.parent = parent;
			this.descriptor = descriptor;
		}

		public int WorkflowID { get; set; }

		public string Name => descriptor.Name;

		public string Description => descriptor.Description;

		public WFNodeType NodeType => descriptor.NodeType;

		public WFActionType ActionType => descriptor.ActionType;

		public TaskNode<TItem> LastChild
		{
			get
			{
				if (children.Count > 0)
					return children[children.Count - 1];
				else
					return null;
			}
		}

		public Type TaskType => descriptor.TaskType;

		public Type EventType => descriptor.EventType;

		public Type ExceptionType => descriptor.ExceptionType;

		public Action<TItem> Callback => descriptor.Callback;

		public bool CanRunOutOfFlow => descriptor.CanRunOutOfFlow;

		public bool ParallelExecution => descriptor.ParallelExecution;

		public string RouteCode => descriptor.RouteCode;

		public bool HasWakeTimeout => hasWakeTimeout;

		public TimeSpan WakeTimeout => wakeTimeout;

		public string RejectReason => descriptor.RejectReason;

		public TimeSpan? DelayTime => descriptor.DelayTime;

		public Expression<Predicate<TItem>> Expression => descriptor.Expression;

		public Predicate<TItem> Predicate => descriptor.Predicate;

		public Expression WakeExpression => descriptor.WakeExpression;

		public IEnumerable<string> Routes => routes;

		public TaskNode<TItem> Parent => parent;

		public TaskNodeCollection<TItem> Children => children;

		public IEnumerable<TaskDescriptor<TItem>> ExceptionHandlers => exceptionHandlers;

		public TaskOptionsAttribute Options => descriptor.Options;

		internal void AddExceptionHandler(TaskDescriptor<TItem> td)
		{
			exceptionHandlers.Add(td);
		}

		internal void SetTimeout(TimeSpan timeout)
		{
			hasWakeTimeout = true;
			wakeTimeout = timeout;
		}

		internal void AddRoute(string routeCode)
		{
			if (routes.FindIndex(c => String.Compare(c, routeCode, true) == 0) >= 0)
			{
				if (routeCode == "")
					throw new InvalidOperationException("A default route has already been defined.");
				else
					throw new InvalidOperationException($"A route \"{routeCode}\" has already been defined.");
			}
			routes.Add(routeCode);
		}

		internal IEnumerable<TaskNode<TItem>> DeepSearch(Predicate<TaskNode<TItem>> predicate)
		{
			List<TaskNode<TItem>> result = new List<TaskNode<TItem>>();
			DeepSearchInternal(children, predicate, result);
			return result;
		}

		private void DeepSearchInternal(TaskNodeCollection<TItem> nodes, Predicate<TaskNode<TItem>> predicate, List<TaskNode<TItem>> result)
		{
			foreach(var node in nodes)
			{
				if(node.children.Count > 0)
					DeepSearchInternal(node.children, predicate, result);
			}
			result.AddRange(nodes.Where(item => predicate(item)).ToList());
		}

		internal IEnumerable<TaskNode<TItem>> ReverseSearch(Predicate<TaskNode<TItem>> predicate)
		{
			List<TaskNode<TItem>> result = new List<TaskNode<TItem>>();
			ReverseSearchInternal(this, predicate, result);
			return result;
		}

		internal void ReverseSearchInternal(TaskNode<TItem> node, Predicate<TaskNode<TItem>> predicate, List<TaskNode<TItem>> result)
		{
			if (node != null)
			{
				for (int i = 0; i < node.children.Count; i++)
				{
					var n = node.children[i];
					if (predicate(n))
						result.Add(n);
				}
				ReverseSearchInternal(node.parent, predicate, result);
			}
		}

		public override string ToString()
		{
			return $"{Name??descriptor.NodeType.ToString()} [{children.Count}]";
		}
	}


	class TaskNodeCollection<TItem> : IEnumerable<TaskNode<TItem>> 
		where TItem: WorkItem
	{
		private List<TaskNode<TItem>> list = new List<TaskNode<TItem>>();

		public int Count { get => list.Count; }

		public TaskNode<TItem> this[int index] { get => list[index]; }

		internal void Add(TaskNode<TItem> node)
		{
			list.Add(node);
		}

		public IEnumerator<TaskNode<TItem>> GetEnumerator()
		{
			return list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}
	}
}
