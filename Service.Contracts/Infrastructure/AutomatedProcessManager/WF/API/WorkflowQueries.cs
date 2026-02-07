using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
    public class WorkflowQueries : IWorkflowQueries
    {
        private readonly object syncObj = new object();
        private readonly IConnectionManager connManager;
        private IEnumerable<WorkflowSummary> workflows;
        private IEnumerable<TaskSummary> tasks;

        public WorkflowQueries(IConnectionManager connManager)
        {
            this.connManager = connManager;
        }

        public async Task<IEnumerable<WorkflowSummary>> GetWorkflows()
        {
            // Workflow & Task information is unlikely to change while the system is running, we cache it in memory
            lock(syncObj)
            {
                if(workflows != null)
                    return workflows;
            }

            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                var workflowList = await conn.SelectAsync<WorkflowSummary>("select WorkflowID, Name from WorkflowData where Detached = 0 order by Name");
                var tasksList = await conn.SelectAsync<TaskSummary>("select WorkflowID, TaskID, TaskName from TaskData where Detached = 0 order by SortOrder");

                workflowList.ForEach(w => w.Tasks = tasksList.Where(t => t.WorkflowID == w.WorkflowID));

                lock(syncObj)
                {
                    if(workflows == null)
                        workflows = workflowList;
                    if(tasks == null)
                        tasks = tasksList;
                }

                return workflows;
            }
        }

        public async Task<IEnumerable<TaskSummary>> GetTasks()
        {
            // Workflow & Task information is unlikely to change while the system is running, we cache it in memory
            lock(syncObj)
            {
                if(tasks != null)
                    return tasks;
            }

            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                var taskList = await conn.SelectAsync<TaskSummary>(@"
                    select WorkflowID, TaskID, TaskName
                    from TaskData WITH (NOLOCK)
                    where Detached = 0 order by SortOrder");

                lock(syncObj)
                {
                    if(tasks == null)
                        tasks = taskList;
                }

                return tasks;
            }
        }

        public async Task<IEnumerable<ItemSummary>> GetItemSummary(PagedItemFilter filter)
        {
            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                return await ExecuteItemSummaryQuery(conn, filter, null);
            }
        }

        public async Task<IEnumerable<ItemSummary>> GetItemSummary<TItem>(
            PagedItemFilter filter,
            Expression<Func<TItem, bool>> expression) where TItem : WorkItem
        {
            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                return await ExecuteItemSummaryQuery(conn, filter, expression);
            }
        }

        private async Task<IEnumerable<ItemSummary>> ExecuteItemSummaryQuery(IDBX conn, PagedItemFilter filter, Expression expression)
        {
            int extraProperties = 0;
            var stateFields = "";
            var limiters = GetItemDataLimiters(filter, expression);
            string pagination = GetItemDataPaginationClause(filter);

            if(filter.IncludedStateProperties != null)
            {
                var sb = new StringBuilder(1000);
                foreach(var field in filter.IncludedStateProperties)
                {
                    sb.Append($", JSON_VALUE(ItemState, '$.{field}') as {field}");
                    extraProperties++;
                }
                stateFields = sb.ToString();
            }

            var result = new List<ItemSummary>();

            var query = $@"
					select
						WorkflowID, TaskID, ItemID, ItemName, ItemStatus, CreatedDate, ProjectID
                        {stateFields}
					from ItemData WITH (NOLOCK) {limiters}
					order by ItemID
					{pagination}";

            using(var reader = await conn.ExecuteReaderAsync(query, limiters.Arguments))
            {
                object value;
                while(reader.Read())
                {
                    var item = new ItemSummary();
                    item.WorkflowID = reader.GetInt32(0);
                    item.TaskID = reader.GetInt32(1);
                    item.ItemID = reader.GetInt64(2);
                    item.ItemName = reader.GetString(3);
                    item.ItemStatus = (ItemStatus)reader.GetInt32(4);
                    item.CreatedDate = reader.GetDateTime(5);
                    item.ProjectID = (value = reader.GetValue(6)) != DBNull.Value ? (int)value : 0;
                    item.ExtraProperties = GetExtraProperties(reader, 7, extraProperties);
                    result.Add(item);
                }
            }

            return result;
        }

        private List<StateProperty> GetExtraProperties(DbDataReader reader, int index, int count)
        {
            var result = new List<StateProperty>();
            int maxBound = index + count;
            while(index < maxBound)
            {
                var name = reader.GetName(index);
                var value = reader.GetValue(index);
                result.Add(new StateProperty() { Name = name, Value = value?.ToString() });
                index++;
            }
            return result;
        }

        public async Task<Dictionary<int, TaskCounterData>> GetItemCountersGroupedByTaskAsync(ItemFilter filter)
        {
            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                var limiters = GetItemDataLimiters(filter);

                var counters = await conn.SelectAsync<TaskItemCounter>($@"
					select TaskID, ItemStatus, count(*) [Counter]
					from ItemData WITH (NOLOCK) {limiters}
					group by TaskID, ItemStatus", limiters.Arguments);

                return WorkflowApi.CreateCountersByTaskDictionary(counters);
            }
        }

        public async Task<Dictionary<int, TaskCounterData>> GetItemCountersGroupedByTaskAsync<TItem>(
            ItemFilter filter,
            Expression<Func<TItem, bool>> expression) where TItem : WorkItem
        {
            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                var limiters = GetItemDataLimiters(filter, expression);

                var counters = await conn.SelectAsync<TaskItemCounter>($@"
					select TaskID, ItemStatus, count(*) [Counter]
					from ItemData WITH (NOLOCK) {limiters}
					group by TaskID, ItemStatus", limiters.Arguments);

                return WorkflowApi.CreateCountersByTaskDictionary(counters);
            }
        }

        public async Task<Dictionary<string, StateCounterData>> GetItemCountersGroupedByItemStateAsync<TValue>(
            ItemFilter filter,
            string itemStatePropertyName,
            IEnumerable<TValue> itemStateValues)
        {
            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                var limiters = GetItemDataLimiters(filter);

                if(itemStateValues != null && itemStateValues.Any())
                {
                    var itemStateConditions = itemStateValues.Select(v => ItemDataModel.FormatJsonValue(v)).Merge(",");
                    limiters.Add($"JSON_VALUE(ItemState, '$.{itemStatePropertyName}') IN ({itemStateConditions})");
                }

                var counters = await conn.SelectAsync<StateItemCounter>($@"
					select T1.[Value], T1.ItemStatus, count(*) [Counter] from
					(
						select JSON_VALUE(ItemState, '$.{itemStatePropertyName}') as [Value], ItemStatus
						from ItemData WITH (NOLOCK) {limiters}
					) as T1
					group by T1.Value, T1.ItemStatus", limiters.Arguments);

                return WorkflowApi.CreateCountersByStateDictionary(counters);
            }
        }

        public async Task<IEnumerable<WorkItemError>> GetTaskErrorsAsync(PagedItemFilter filter)
        {
            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                return await ExecuteTaskErrorsQuery(conn, filter, null);
            }
        }

        public async Task<IEnumerable<WorkItemError>> GetTaskErrorsByItemStateAsync<TItem>(
            PagedItemFilter filter,
            Expression<Func<TItem, bool>> expression) where TItem : WorkItem
        {
            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                return await ExecuteTaskErrorsQuery(conn, filter, expression);
            }
        }

        private async Task<IEnumerable<WorkItemError>> ExecuteTaskErrorsQuery(
            IDBX conn, PagedItemFilter filter, Expression expression)
        {
            int extraFieldsCount = 0;
            var stateFields = "";
            var limiters = GetItemDataLimiters(filter, expression);
            var pagination = GetItemDataPaginationClause(filter);

            if(filter.IncludedStateProperties != null)
            {
                var sb = new StringBuilder(1000);
                foreach(var field in filter.IncludedStateProperties)
                {
                    sb.Append($", JSON_VALUE(ItemState, '$.{field}') as {field}");
                    extraFieldsCount++;
                }
                stateFields = sb.ToString();
            }

            var query= $@"
                with t as (select *
	            from
	            (
		            select  ItemID
		            from ItemData item WITH (NOLOCK)
		            {limiters}
	            ) as T1
	            order by ItemID
	            {pagination} )
	            select  
		            WorkflowID, TaskID, ItemID, ItemName, ItemStatus, CreatedDate, ProjectID
		            {stateFields}
		            , t2.*
	            from itemdata d
	            outer apply GetLastItemError(d.ItemID) as T2
	            where d.ItemID in (select ItemID from t)
            ";

            var result = new List<WorkItemError>();
            using(var reader = await conn.ExecuteReaderAsync(query, limiters.Arguments))
            {
                object value;
                while(reader.Read())
                {
                    var item = new WorkItemError();
                    item.WorkflowID = reader.GetInt32(0);
                    item.TaskID = reader.GetInt32(1);
                    item.ItemID = reader.GetInt64(2);
                    item.ItemName = reader.GetString(3);
                    item.ItemStatus = (ItemStatus)reader.GetInt32(4);
                    item.CreatedDate = reader.GetDateTime(5);
                    item.ProjectID = (value = reader.GetValue(6)) != DBNull.Value ? (int)value : 0;
                    int index = 7 + extraFieldsCount;
                    item.LastErrorMessage = (value = reader.GetValue(index)) != DBNull.Value ? (string)value : "";
                    item.LastErrorDate = (value = reader.GetValue(index + 1)) != DBNull.Value ? (DateTime)value : DateTime.MinValue;
                    item.LastErrorTaskID = (value = reader.GetValue(index + 2)) != DBNull.Value ? (int)value : 0;
                    item.ExtraProperties = GetExtraProperties(reader, 7, extraFieldsCount);
                    result.Add(item);
                }
            }
            return result;
        }

        private string GetItemDataPaginationClause(PagedItemFilter filter)
        {
            var pagination = "";
            if(filter.Page != null && filter.PageSize != null)
            {
                int pageStart = filter.Page.Value * filter.PageSize.Value;
                pagination = $"OFFSET {pageStart} ROWS FETCH NEXT {filter.PageSize.Value} ROWS ONLY";
            }
            return pagination;
        }

        private static QueryLimiters GetItemDataLimiters(ItemFilter filter, Expression expression = null, string alias = null)
        {
            var limiters = new QueryLimiters();

            if(alias != null && !alias.EndsWith("."))
                alias = alias + '.';

            if(filter.WorkflowID != null)
                limiters.Add($"{alias}WorkflowID = {filter.WorkflowID}");

            if(filter.Tasks != null)
                limiters.Add($"{alias}TaskID in ({filter.Tasks.Merge(",")})");

            if(filter.Statuses != null)
                limiters.Add($"{alias}ItemStatus in ({filter.Statuses.Select(p => (int)p).Merge(",")})");

            if(filter.ItemID != null)
                limiters.Add($"{alias}ItemID = {filter.ItemID}");

            if(filter.Keywords != null)
                limiters.Add($"CHARINDEX(@keywords, [Keywords]) > 0", filter.Keywords);

            if(expression != null)
                limiters.Add(new ItemStateQueryTranslator().Translate(expression, alias));

            return limiters;
        }

        public async Task<IEnumerable<ItemExceptionInfo>> GetItemsLastExceptionsAsync(long workflowId, IEnumerable<long> itemIds)
        {
            if(itemIds == null || !itemIds.Any())
                return Enumerable.Empty<ItemExceptionInfo>();

            using(var conn = await connManager.OpenDBAsync("APM"))
            {
                string items = string.Join(", ", itemIds.Select(p => p.ToString()));

                var query = $@"
                    select *
				    from
				    (
                        select ItemId from ItemData WITH(NOLOCK)
					    where
						    WorkflowID = @workflowid and
                            ItemStatus in (4,8) and
						    ItemID in ( {items} )
                    ) as T1
                    outer apply GetLastItemError(T1.ItemID) as T2
                    ";

                var result = await conn.SelectAsync<ItemExceptionInfo>(query, workflowId);

                if(result == null)
                    return Enumerable.Empty<ItemExceptionInfo>();
                return result;
            }
        }

        class QueryLimiters
        {
            private List<string> conditions;
            private List<object> arguments;

            public QueryLimiters()
            {
                conditions = new List<string>();
                arguments = new List<object>();
            }

            public void Add(string condition, params object[] arguments)
            {
                conditions.Add(condition);
                this.arguments.AddRange(arguments);
            }

            public override string ToString()
            {
                var whereclause = "";
                if(conditions.Count > 0)
                    whereclause = $"where {conditions.Merge(" AND ")}";
                return whereclause;
            }

            public object[] Arguments => arguments.ToArray();
        }
    }
}
