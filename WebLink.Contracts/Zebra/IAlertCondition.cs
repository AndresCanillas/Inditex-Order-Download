using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
	public interface IAlertCondition
	{
		string AlertName { get; }
		bool IsToggle { get; }
		bool IsSet { get; }
		DateTime UpdateDate { get; }
	}
}
