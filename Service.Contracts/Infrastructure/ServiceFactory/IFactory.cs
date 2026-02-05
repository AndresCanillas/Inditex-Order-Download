using System;
#if NET461
using System.Web.Http.Dependencies;
#endif

namespace Service.Contracts
{
	public interface IFactory : IDisposable, IScope
#if NET461
		, IDependencyResolver
#endif
	{
		void Setup<T>() where T : class;

		IFactory RegisterTransient<TImplementation>()
			where TImplementation : class;

		IFactory RegisterTransient<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService;

		IFactory RegisterTransient<TService>(Func<IScope, object> createCallback)
			where TService : class;

		IFactory RegisterTransient(Type srvType, Type implType);

		IFactory RegisterTransient(Type srvType, Func<IScope, object> createCallback);

		IFactory RegisterScoped<TImplementation>()
			where TImplementation : class;

		IFactory RegisterScoped<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService;

		IFactory RegisterScoped<TService>(TService instance)
			where TService : class;

		IFactory RegisterScoped(Type srvType, Type implType);

		IFactory RegisterScoped(Type srvType, Func<IScope, object> createCallback);

		IFactory RegisterSingleton<TImplementation>()
			where TImplementation : class;

		IFactory RegisterSingleton<TService>(TService instance)
			where TService : class;

		IFactory RegisterSingleton<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService;

		IFactory RegisterSingleton(Type srvType, object instance);
		
		IFactory RegisterSingleton(Type srvType, Type implType);

		IFactory RegisterSingleton(Type srvType, Func<IScope, object> createCallback);

		IFactory ReplaceTransient<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService;

		IFactory ReplaceScoped<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService;

		IFactory ReplaceSingleton<TService>(TService instance)
			where TService : class;

		IFactory ReplaceSingleton<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService;

		void Clear();

		void ForEach(Action<Type, Type, bool, object> action);

		ServiceLifeTime GetServiceLifeTime<TService>() where TService : class;
		ServiceLifeTime GetServiceLifeTime(Type t);
	}


	public enum ServiceLifeTime
	{
		Transient,
		Scoped,
		Singleton
	}
}
