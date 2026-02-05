using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
    class WFManager
    {
        private object syncObj = new object();
        private bool started;
        private IFactory factory;
        private List<WorkflowDefinition> workflows;
        private List<IWFRunner> wfRunners;
        private List<IWorkflow> wfApiObjects;
        private WorkflowDataModel model;
        private ConcurrentDictionary<int, ReadyItemQueue> itemQueues;


        public WFManager(IFactory factory)
        {
            this.factory = factory;
            model = factory.GetInstance<WorkflowDataModel>();
            itemQueues = new ConcurrentDictionary<int, ReadyItemQueue>();
        }

        public async Task Start(ICollection<WorkflowDefinition> workflows)
        {
            if (started)
                throw new InvalidOperationException("Workflow Manager is already started.");

            this.workflows = new List<WorkflowDefinition>();
            this.workflows.AddRange(workflows);

            wfRunners = new List<IWFRunner>(workflows.Count);
            foreach (var wf in this.workflows)
            {
                var runner = factory.GetGenericInstance(typeof(WFRunner<>), wf.ItemType) as IWFRunner;
                wfRunners.Add(runner);
                await runner.Start(wf);
            }

            wfApiObjects = new List<IWorkflow>();
            started = true;
        }

        public void Stop()
        {
            if (!started)
                return;
            foreach (var runner in wfRunners)
                runner.Stop();
            started = false;
        }

        public async Task WaitForStop()
        {
            foreach (var runner in wfRunners)
                await runner.WaitForStop();
        }


        public async Task<IEnumerable<IWorkflow>> GetWorkflows()
        {
            List<IWorkflow> result = new List<IWorkflow>(workflows.Count);
            List<WorkflowData> workflowList = await model.GetWorkflows();

            foreach (var wf in workflowList)
            {
                var apiObject = await GetWorkflow(wf.WorkflowID);
                result.Add(apiObject);
            }

            return result;
        }


        public async Task<IWorkflow> GetWorkflow(int workflowid)
        {
            lock (syncObj)
            {
                var apiObject = wfApiObjects.FirstOrDefault(p => p.WorkflowID == workflowid);
                if (apiObject != null)
                    return apiObject;
            }

            WorkflowData workflowData = await model.GetWorkflowByID(workflowid);
            if (workflowData == null)
                throw new InvalidOperationException($"The specified workflow (WorkflowID {workflowid}) could not be found.");

            var instance = factory.GetInstance<WorkflowApi>();
            await instance.Initialize(workflowData);

            lock (syncObj)
            {
                var apiObject = wfApiObjects.FirstOrDefault(w => w.WorkflowID == workflowid);
                if (apiObject == null)
                {
                    wfApiObjects.Add(instance);
                    return instance;
                }
                else return apiObject;
            }
        }


        public async Task<IWorkflow> GetWorkflow(string name)
        {
            lock (syncObj)
            {
                var apiObject = wfApiObjects.FirstOrDefault(p => String.Compare(p.Name, name, true) == 0);
                if (apiObject != null)
                    return apiObject;
            }

            WorkflowData workflowData = await model.GetWorkflowByName(name);
            if (workflowData == null)
                throw new InvalidOperationException($"The specified workflow ({name}) could not be found.");

            var instance = factory.GetInstance<WorkflowApi>();
            await instance.Initialize(workflowData);

            lock (syncObj)
            {
                var apiObject = wfApiObjects.FirstOrDefault(w => w.WorkflowID == workflowData.WorkflowID);
                if (apiObject == null)
                {
                    wfApiObjects.Add(instance);
                    return instance;
                }
                else return apiObject;
            }
        }


        public WFRunner<TItem> GetRunner<TItem>(int workflowid) where TItem : WorkItem, new()
        {
            var runner = wfRunners.Where(p => p.WorkflowID == workflowid).FirstOrDefault();
            return runner as WFRunner<TItem>;
        }

        public WFRunner<TItem> GetRunner<TItem>(string workflowName) where TItem : WorkItem, new()
        {
            var runner = wfRunners.Where(p => p.Name == workflowName).FirstOrDefault();
            return runner as WFRunner<TItem>;
        }

        internal ReadyItemQueue GetWorkflowQueue(int workflowid)
        {
            if (!itemQueues.TryGetValue(workflowid, out var queue))
            {
                queue = factory.GetInstance<ReadyItemQueue>();
                queue.WorkflowID = workflowid;
                if (!itemQueues.TryAdd(workflowid, queue))
                    queue = itemQueues[workflowid];
            }
            return queue;
        }
    }
}
