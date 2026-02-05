using Service.Contracts.Database;
using Service.Contracts.WF;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public interface IAutomatedProcessManager
	{
		void Setup<T>() where T : class;
		void AddProcess<T>() where T : class, IAutomatedProcess;

		void AddHandler<EventType, HandlerType>()
			where EventType : EQEventInfo
			where HandlerType : EQEventHandler<EventType>;

		IWorkflowDefinition<TItem> AddWorkflow<TItem>(string workflowName)
			where TItem : WorkItem;

		Task<IEnumerable<IWorkflow>> GetWorkflowsAsync();
		Task<IWorkflow> GetWorkflowAsync(int id);
		Task<IWorkflow> GetWorkflowAsync(string name);

		void Start();
		void Stop();
	}


	public interface IAutomatedProcess
	{
		TimeSpan GetIdleTime();
		void OnLoad();
		void OnExecute();
		void OnUnload();
	}

    public interface IAsyncAutomatedProcess
    {
        TimeSpan GetIdleTime();
        Task OnLoadAsync();
        Task OnExecuteAsync();
        Task OnUnloadAsync();
    }

    // Event notification sent whenever a process registered within the AutomatedProcessManager catches an exception.
    // NOTE: Exceptions are automatically logged by the AutomatedProcessManager, so subscribers of this event 
    // are expected to perform aditional actions other than just logging the issue...
    public class APMErrorNotification : EQEventInfo
	{
		public string NotificationKey { get; set; }
		public string HandlerType { get; set; }
		public string Message { get; set; }
		public string StackTrace { get; set; }
		public object Data { get; set; }
		public string Type { get; set; } // notification type : user, customer, technical support

		public APMErrorNotification() { }
	}
}
