using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts.Database
{
	public interface IEntity
	{
		/// <summary>
		/// The ID assigned to this entity.
		/// </summary>
		int ID { get; set; }
	}

	public interface ICanRename
	{
		void Rename(string name);
	}

	public interface IBasicTracing
	{
		string CreatedBy { get; set; }
		DateTime CreatedDate { get; set; }
		string UpdatedBy { get; set; }
		DateTime UpdatedDate { get; set; }
	}
}
