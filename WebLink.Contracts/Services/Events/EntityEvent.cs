using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	public class EntityEvent : EQEventInfo
	{
		public string EntityName { get; set; }
		public string EntityType { get; set; }
		public string EntityData { get; set; }
		public DBOperation Operation { get; set; }

		public EntityEvent() { }

		public EntityEvent(int companyid, IEntity entity, DBOperation operation)
		{
			CompanyID = companyid;
			EntityName = entity.GetType().Name;
			EntityType = entity.GetType().FullName;
			EntityData = JsonConvert.SerializeObject(entity);
			Operation = operation;
		}

		public IEntity Entity
		{
			get
			{
				if (EntityType == null)
					return null;
				var t = Type.GetType(EntityType, true, false);
				return JsonConvert.DeserializeObject(EntityData, t) as IEntity;
			}
		}
	}
}
