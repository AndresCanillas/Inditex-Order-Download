using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public class OrderEntityEvent : EQEventInfo
	{

		public IEntity Entity { get; }
		public string EntityName { get; }
		public DBOperation Operation { get; }
		public OrderEntityEvent() { }
		public OrderEntityEvent(int companyid, IOrder entity, DBOperation operation)
		{
			CompanyID = companyid;
			Entity = entity;
			EntityName = entity.GetType().Name;
			Operation = operation;
		}
	}
}
