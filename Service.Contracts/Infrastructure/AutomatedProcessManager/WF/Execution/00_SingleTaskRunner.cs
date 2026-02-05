using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
namespace Service.Contracts.WF
{
	abstract class SingleTaskRunner<TItem> : BaseTaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		public SingleTaskRunner(IFactory factory) : base(factory)
		{
		}

		protected override async Task ExecuteItems()
		{
			while (readyItems.TryDequeue(out var item))
			{
				try
				{
					await ExecuteItem(
						item,
						async () => await ExecuteItemAsync(item, ExecutionMode.InFlow, cts.Token)
					);
				}
				catch (Exception ex)
				{
					// We should not reach this point unless there is an issue with the environment (database or network down for instance)
					// If we reach this exception handler, it might be that there is an issue in the code of ExecuteItem that should be
					// researched and fixed. At any rate this call will ensure we do not retry the same item over and over without giving
					// other items a chance to run...
					log.LogException($"WF ERROR - Exception while trying to execute item {item.ItemName}, item will be put back in the delayed state.", ex);
					await itemModel.MakeDelayedInTask(item, "Unhandled Error in Workflow System", TimeSpan.FromMinutes(5), new SystemIdentity());
				}
			}
		}
	}
}