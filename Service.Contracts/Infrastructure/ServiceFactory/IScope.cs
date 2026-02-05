using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public interface IScope : IDisposable
	{
		T GetInstance<T>()
			where T : class;
		object GetInstance(Type t);
		Type GetInstanceType(Type t);
		object GetGenericInstance(Type genericType, params Type[] typeParameters);
		IScope CreateScope();
	}
}
