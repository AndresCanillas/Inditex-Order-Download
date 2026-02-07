using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	public interface IProjectProductionTypeStrategy
	{

		ProductionType GetProductionType(string sendToCode, IProject project, string articleCode);

		//ProductionType GetProductionType(int proyectID);

	}

	public interface ISetIDTFactoryStrategy : IProjectProductionTypeStrategy
	{

	}

	public interface ISetLocalAsFirstOptionStrategy : IProjectProductionTypeStrategy
	{

	}

	public interface ISetLocalStrategy : IProjectProductionTypeStrategy
	{

	}
}
