using System;
using System.Collections.Generic;


namespace Service.Contracts.PrintCentral
{
    public interface IOrderProcessingPlugin : IDisposable
	{
		void OrderInserted(OrderPluginData orderData);
		void OrderReceived(OrderPluginData orderData);
		void OrderProcessed(OrderPluginData orderData);
		void OrderValidated(OrderPluginData orderData);
		void OrderCompleted(OrderPluginData orderData);
		void OrderCancelled(OrderPluginData orderData);
	}

    public class ExceptionComposition
    {
        public int ExceptionID { get; set; }
        public string SectionID { get; set; }
        public string FiberID { get; set; }
        public string Exception { get; set; }
        public string Section { get; set; }
        public string Fiber { get; set; }
        public string Type { get; set; }
    }

    public class FiberConcatenation
    {
        
        public string FiberID { get; set; }
        public string SectionID { get; set; }
    }

    public class OrderPluginData
	{
		public int OrderGroupID;
		public int OrderID;
		public string OrderNumber;
		public int CompanyID;
		public int BrandID;
		public int ProjectID;
		public int FillingWeightId; 
		public string FillingWeightText;
        public int ExceptionsLocation;
        public List<ExceptionComposition> ExceptionsComposition;
        public bool UsesFreeExceptionComposition;
        public FiberConcatenation FiberConcatenation;
        public int ArticleID;

    }

	public abstract class BaseOrderProcessingPlugin: IOrderProcessingPlugin
	{
		public virtual void OrderInserted(OrderPluginData orderData)
		{

		}

		public virtual void OrderReceived(OrderPluginData orderData)
		{
		}

		public virtual void OrderProcessed(OrderPluginData orderData)
		{
		}

		public virtual void OrderValidated(OrderPluginData orderData)
		{
		}

		public virtual void OrderCompleted(OrderPluginData orderData)
		{
		}

		public virtual void OrderCancelled(OrderPluginData orderData)
		{
		}

		public abstract void Dispose();
	}
}
