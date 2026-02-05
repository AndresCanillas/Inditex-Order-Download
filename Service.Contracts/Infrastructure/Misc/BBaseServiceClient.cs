using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Contracts
{
    /* Load balance requests to multiple instance of a given service. Usable only if the service is stateless and each request can be executed with complete
	 * independence of previous requests.
	 * 
	 * IMPORTANT: Document Service does not meet this requirement as it stands right now, but the label service and PDF service do, se we could teoretically
	 * have multiple instances of those services and load balance them.
	 * 
	 * NOTES:
	 *  - Sice we dont have service discovery implemented, service instances need to be known ahead of time, this is done by calling SetEndPoints method.
	 *  - This class will mark downed instances and avoid sending requests to them (for some time).
	 *  - BBaseServiceClient will send requests to downed instances every now and then to see if they have come back online, if not, it will keep avoiding them,
	 *    these retries however will spend a retry attempt, so it might not be ideal in an scenario where many instances are down.
	 */

    public enum EndPointTarget
    {
        Any,
        ForClientLocations,
        ForFactory,
        ForWeb
    }

    public class LSEP
    {
        public string Url;

        // Used to skip this end point in case it is reporting errors. This value is increased by a large amount when a call to the end point fails, 
        // and a small amount when the service responds with Success set to false. If the value is greather than 0, this endpoint is skipped over
        // and its etc value reduced by 1. The intention is to reduce calls to end points that are failing, and redirect calls to other healty end
        // points.
        public int etc = 0;

        // Used to count consecutive hard failures
        public int hfc = 0;

        public EndPointTarget Target = EndPointTarget.Any;
    }

    public interface IBBaseServiceRequest
    {
        EndPointTarget Target { get; set; }
    }

    public abstract class ABBaseServiceRequest : IBBaseServiceRequest
    {
        private EndPointTarget _epType = EndPointTarget.Any;
        public EndPointTarget Target { get => _epType; set { _epType = value; } }
    }


    public abstract class BBaseServiceClient : BaseServiceClient
	{
		private object syncObj = new object();
		private List<LSEP> endPoints;
        private Dictionary<EndPointTarget, int> currentByTarget;
        private int maxCount = 0;
		protected ILogService log;


		public BBaseServiceClient(ILogService log)
		{
			base.Url = "";  // Need to clear out the base url cause it will be changing with each consecutive call.
			this.log = log;
            currentByTarget = new Dictionary<EndPointTarget, int>();
		}


		public override string Url
		{
			get => "";		// Base url cannot be changed, instead use SetEndPoints
			set { }
		}


		public int MaxRetries { get; set; } = 5;

        public int NumberOfAvailablesEndpoints { get => maxCount; }


        [Obsolete("Require to update all config files,  LabelService.Endpoints key, keep for compatibility with PrintLocal App ")]
		public void SetEndPoints(List<string> endpointsAvailables)
		{
			if (endpointsAvailables == null || endpointsAvailables.Count == 0)
				throw new InvalidOperationException("Cannot accept a null or empty list of end points. At least one is required.");
			lock (syncObj)
			{
				endPoints = new List<LSEP>(endpointsAvailables.Count);
				foreach (var ep in endpointsAvailables)
					endPoints.Add(new LSEP() { Url = ep, Target = EndPointTarget.Any });


                var allTArgets = Enum.GetValues(typeof(EndPointTarget)).Cast<EndPointTarget>().Select(e => e);

                foreach (var t in allTArgets)
                    currentByTarget.Add(t, 0);

				maxCount = endPoints.Count;
			}
		}

        public void SetEndPoints(List<LSEP> endpointsAvailables)
        {
            if (endpointsAvailables == null || endpointsAvailables.Count == 0)
                throw new InvalidOperationException("Cannot accept a null or empty list of end points. At least one is required.");
            lock (syncObj)
            {
                endPoints = new List<LSEP>(endpointsAvailables);
                var allTArgets = Enum.GetValues(typeof(EndPointTarget)).Cast<EndPointTarget>().Select(e => e);

                foreach (var t in allTArgets)
                    currentByTarget.Add(t, 0);

                maxCount = endPoints.Count;
            }
        }
        
        private LSEP GetNextEndPoint(EndPointTarget target = EndPointTarget.Any)
        {
            
            if (maxCount == 0)
                throw new InvalidOperationException("No end points have been configured for the label service.");

            lock (syncObj)
            {
                if (maxCount > 1)
                {
                    LSEP result = null;
                    do
                    {
                        var availableNodesByTarget = endPoints
                            .Where(w => w.Target == EndPointTarget.Any || w.Target == target)
                            .OrderByDescending(o => o.Target) // use wildcard  endpoints (EndPointTarget.Any) as last option
                            .ToList();

                        var maxCountInTarget = availableNodesByTarget.Count;

                        if(currentByTarget.TryGetValue(target, out int currentInTarget ))
                        {
                            result = availableNodesByTarget.ElementAt(currentInTarget % maxCountInTarget);
                            currentInTarget++;
                            currentByTarget[target] = currentInTarget;

                            if (result.etc > 0)
                                result.etc--;
                        }

                    } while (result is null || result.etc > 0);
                    log.LogMessage($"Enpoint to use: {result.Url}");
                    return result;
                }
                else
                {
                    return endPoints[0];
                }
            }
        }

        // Hard failures are any HTTP error code (such as 403, 500, etc) or connection errors (socket exceptions). 
        // This type of issue will cause the system to avoid the impacted end point for a larger amount of time.
        private void RegisterEndPointHardFailure(LSEP ep, Exception ex)
		{
			lock (syncObj)
			{
				if(ep.hfc < 10)
					ep.hfc++;
				ep.etc += ep.hfc*2 + 2;            // This will cause this end point to be skipped for some cicles, before any request is sent to them again. The number of cicles increase with each consecutive failed attempt.
			}
			log.LogException($"Hard Failure [{ep.Url}]", ex);
		}


		protected override Output Get<Output>(string controller)
		{
			int retryCount = 0;
			do
			{
				var ep = GetNextEndPoint();
				try
				{
					var result = base.Get<Output>(ep.Url + controller);
					lock(syncObj) ep.hfc = 0;
					return result;
				}
				catch (Exception ex)
				{
					RegisterEndPointHardFailure(ep, ex);
					retryCount++;
					if (retryCount >= MaxRetries)
						throw;
				}
			} while (true);
		}

		protected async override Task<Output> GetAsync<Output>(string controller)
		{
			int retryCount = 0;
			do
			{
				var ep = GetNextEndPoint();
				try
				{
					var result = await base.GetAsync<Output>(ep.Url + controller);
					lock (syncObj) ep.hfc = 0;
					return result;
				}
				catch (Exception ex)
				{
					RegisterEndPointHardFailure(ep, ex);
					if (endPoints.Count == 1)
						await Task.Delay(500);
					retryCount++;
					if (retryCount >= MaxRetries)
						throw;
				}
			} while (true);
		}
        
		protected override Output Invoke<Input, Output>(string controller, Input input)
		{
			int retryCount = 0;
			do
			{
                var epType = EndPointTarget.Any;
                if ((input as IBBaseServiceRequest) != null)
                    epType = (input as IBBaseServiceRequest).Target;

                var ep = GetNextEndPoint(epType);
                try
				{
					var result = base.Invoke<Input, Output>(ep.Url + controller, input);
					lock (syncObj) ep.hfc = 0;
					return result;
				}
				catch (Exception ex)
				{
					RegisterEndPointHardFailure(ep, ex);
					retryCount++;
					if (retryCount >= MaxRetries)
						throw;
				}
			} while (true);
		}


		protected override async Task<Output> InvokeAsync<Input, Output>(string controller, Input input)
		{
			int retryCount = 0;
			do
			{
                var epType = EndPointTarget.Any;
                if ((input as IBBaseServiceRequest) != null)
                    epType = (input as IBBaseServiceRequest).Target;

                var ep = GetNextEndPoint(epType);
                try
				{
					var result = await base.InvokeAsync<Input, Output>(ep.Url + controller, input);
					lock (syncObj) ep.hfc = 0;
					return result;
				}
				catch (Exception ex)
				{
					RegisterEndPointHardFailure(ep, ex);
					if (endPoints.Count == 1)
						await Task.Delay(500);
					retryCount++;
					if (endPoints.Count == 1)
						await Task.Delay(500);
					if (retryCount >= MaxRetries)
						throw;
				}
			} while (true);
		}


		protected override async Task InvokeAsync<Input>(string controller, Input input)
		{
			int retryCount = 0;
			do
			{
                var epType = EndPointTarget.Any;
                if ((input as IBBaseServiceRequest) != null)
                    epType = (input as IBBaseServiceRequest).Target;

                var ep = GetNextEndPoint(epType);
                try
				{
					await base.InvokeAsync<Input>(ep.Url + controller, input);
					lock (syncObj) ep.hfc = 0;
					break;
				}
				catch (Exception ex)
				{
					RegisterEndPointHardFailure(ep, ex);
					if (endPoints.Count == 1)
						await Task.Delay(500);
					retryCount++;
					if (retryCount >= MaxRetries)
						throw;
				}
			} while (true);
		}


		protected override async Task<Output> InvokeAsync<Output>(string controller)
		{
			int retryCount = 0;
			do
			{
				var ep = GetNextEndPoint();
				try
				{
					var result = await base.InvokeAsync<Output>(ep.Url + controller);
					lock (syncObj) ep.hfc = 0;
					return result;
				}
				catch (Exception ex)
				{
					RegisterEndPointHardFailure(ep, ex);
					if (endPoints.Count == 1)
						await Task.Delay(500);
					retryCount++;
					if (retryCount >= MaxRetries)
						throw;
				}
			} while (true);
		}


		protected override void DownloadFile<Input>(string controller, Input input, string filePath)
		{
			int retryCount = 0;
			do
			{
                var epType = EndPointTarget.Any;
                if ((input as IBBaseServiceRequest) != null)
                    epType = (input as IBBaseServiceRequest).Target;

                var ep = GetNextEndPoint(epType);
                try
				{
					base.DownloadFile<Input>(ep.Url + controller, input, filePath);
					lock (syncObj) ep.hfc = 0;
					break;
				}
				catch (Exception ex)
				{
					RegisterEndPointHardFailure(ep, ex);
					retryCount++;
					if (retryCount >= MaxRetries)
						throw;
				}
			} while (true);
		}


		protected override async Task DownloadFileAsync(string controller, string filePath)
		{
			int retryCount = 0;
			do
			{
				var ep = GetNextEndPoint();
				try
				{
					await base.DownloadFileAsync(ep.Url + controller, filePath);
					lock (syncObj) ep.hfc = 0;
					break;
				}
				catch (Exception ex)
				{
					RegisterEndPointHardFailure(ep, ex);
					if (endPoints.Count == 1)
						await Task.Delay(500);
					retryCount++;
					if (retryCount >= MaxRetries)
						throw;
				}
			} while (true);
		}


		protected override async Task<Output> UploadFileAsync<Output>(string controller, string filePath)
		{
			int retryCount = 0;
			do
			{
				var ep = GetNextEndPoint();
				try
				{
					var result = await base.UploadFileAsync<Output>(controller, filePath);
					lock (syncObj) ep.hfc = 0;
                    return result;
				}
				catch (Exception ex)
				{
					RegisterEndPointHardFailure(ep, ex);
					if (endPoints.Count == 1)
						await Task.Delay(500);
					retryCount++;
					if (retryCount >= MaxRetries)
						throw;
				}
			} while (true);
		}


		protected override async Task HttpGetFileAsync(string controller, string filePath)
		{
			int retryCount = 0;
			do
			{
				var ep = GetNextEndPoint();
				try
				{
					await base.HttpGetFileAsync(ep.Url + controller, filePath);
					lock (syncObj) ep.hfc = 0;
					break;
				}
				catch (Exception ex)
				{
					RegisterEndPointHardFailure(ep, ex);
					if (endPoints.Count == 1)
						await Task.Delay(500);
					retryCount++;
					if (retryCount >= MaxRetries)
						throw;
				}
			} while (true);
		}
	}


	public class MaxRetryCountReachedException : Exception
	{
		public MaxRetryCountReachedException(string message)
			: base(message)
		{

		}
	}
}
