using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using Service.Contracts.Logging;
using Service.Contracts.LabelService;
using System.ComponentModel;
using Service.Contracts.PDFDocumentService;

#if NET461
using System.Web.Http.Dependencies;
#else
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Service.Contracts
{
	public partial class ServiceFactory: IFactory
	{
		// =================================================================================
		// IFactory Implementation
		// =================================================================================

		private static int currentID;
        public int ID;

		private bool disposed;
		private readonly bool isScope;
		private ServiceFactory scopeOwner;
		private int scopeCount;

		private ConcurrentDictionary<Type, FactoryTypeInfo> registrations = new ConcurrentDictionary<Type, FactoryTypeInfo>();
		private readonly ConcurrentDictionary<Type, object> scopedInstances = new ConcurrentDictionary<Type, object>();

		// This class contains data required to handle type registration and instantiation.
		class FactoryTypeInfo
		{
			public readonly object SyncObj = new object();
			public readonly Type ServiceType;
			public readonly Type ImplementationType;
			public readonly FactoryObjectCreator Creator;
			public readonly ServiceLifeTime LifeTime;
			public readonly Action<object> ConfigCallback;
			public object Instance;

			public FactoryTypeInfo(Type serviceType, Type implementationType, FactoryObjectCreator creator, ServiceLifeTime lifeTime, object instance, Action<object> configCallback = null)
			{
				SyncObj = new object();
				Instance = null;
				ServiceType = serviceType;
				ImplementationType = implementationType;
				Creator = creator;
				LifeTime = lifeTime;
				ConfigCallback = configCallback;
				if (LifeTime == ServiceLifeTime.Singleton)
				{
					if (instance != null)
						Instance = instance;
					else if(creator == null && !implementationType.IsGenericTypeDefinition)
						throw new ArgumentNullException(nameof(creator));
				}
			}
		}


		private ServiceFactory(ServiceFactory scopeOwner)
		{
			isScope = true;
			scopeCount = 1;
			this.scopeOwner = scopeOwner;
			registrations[typeof(IFactory)] = new FactoryTypeInfo(typeof(IFactory), typeof(ServiceFactory), null, ServiceLifeTime.Singleton, this, null);
			registrations[typeof(IScope)] = new FactoryTypeInfo(typeof(IScope), typeof(ServiceFactory), null, ServiceLifeTime.Singleton, this, null);
		}


		public ServiceFactory()
			: this(configure: null)
		{
		}


		public ServiceFactory(Action<IFactory> configure)
		{
			ID = Interlocked.Increment(ref currentID);
			Setup(configure);
		}


		public void Setup<T>() where T : class
		{
			GetInstance<T>();
		}


		public void Dispose()
		{
			if (isScope)
			{
				disposed = true;
				if (Interlocked.Decrement(ref scopeCount) == 0)
				{
					scopeOwner = null;
					foreach (var instance in scopedInstances.Values)
					{
						if (instance is IDisposable)
							(instance as IDisposable).Dispose();
					}
					scopedInstances.Clear();
				}
			}
		}


		public virtual IFactory RegisterTransient<TImplementation>()
			where TImplementation : class
		{
			var implType = typeof(TImplementation);
			ValidateRegistration(implType, implType);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			if (!registrations.TryAdd(implType, new FactoryTypeInfo(implType, implType, creator, ServiceLifeTime.Transient, null)))
				throw new InvalidOperationException("Type " + implType.FullName + " has already been registered, use ReplaceTransient if you intend to overwrite previous registration.");
			return this;
		}

		public virtual IFactory RegisterTransient<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			var srvType = typeof(TService);
			var implType = typeof(TImplementation);
			ValidateRegistration(srvType, implType);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			if (!registrations.TryAdd(srvType, new FactoryTypeInfo(srvType, implType, creator, ServiceLifeTime.Transient, null)))
				throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceTransient if you intend to overwrite previous registration.");
			return this;
		}

		public IFactory RegisterTransient<TService>(Func<IScope, object> createCallback)
			where TService : class
		{
			var srvType = typeof(TService);
			ValidateRegistration(srvType, null);
			FactoryObjectCreator creator = (f) => createCallback(f);
			if (!registrations.TryAdd(srvType, new FactoryTypeInfo(srvType, null, creator, ServiceLifeTime.Transient, null)))
				throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceTransient if you intend to overwrite previous registration.");
			return this;
		}


		public IFactory RegisterTransient(Type srvType, Type implType)
		{
			return RegisterTransient(srvType, implType, false);
		}


		public IFactory RegisterTransient(Type srvType, Func<IScope, object> createCallback)
		{
			return RegisterTransient(srvType, createCallback, false);
		}


		public IFactory RegisterTransient(Type srvType, Type implType, bool overwriteExisting)
		{
			if (srvType == null || implType == null)
				throw new ArgumentNullException("IFactory RegisterTransient(Type, Type) does not accept null arguments.");
			if (!overwriteExisting)
				ValidateRegistration(srvType, implType);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			var typeInfo = new FactoryTypeInfo(srvType, implType, creator, ServiceLifeTime.Transient, null);
			if (!registrations.TryAdd(srvType, typeInfo))
			{
				if (overwriteExisting)
					registrations[srvType] = typeInfo;
				else
					throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceTransient if you intend to overwrite previous registration.");
			}
			return this;
		}


		public IFactory RegisterTransient(Type srvType, Func<IScope, object> createCallback, bool overwriteExisting)
		{
			if (srvType == null || createCallback == null)
				throw new ArgumentNullException("IFactory RegisterTransient(srvType, createCallback) does not accept null arguments.");
			if (!overwriteExisting)
				ValidateRegistration(srvType, null);
			FactoryObjectCreator creator = (f) => createCallback(f);
			var typeInfo = new FactoryTypeInfo(srvType, null, creator, ServiceLifeTime.Transient, null);
			if (!registrations.TryAdd(srvType, typeInfo))
			{
				if (overwriteExisting)
					registrations[srvType] = typeInfo;
				else
					throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceTransient if you intend to overwrite previous registration.");
			}
			return this;
		}


		public IFactory RegisterScoped<TImplementation>()
			where TImplementation : class
		{
			var implType = typeof(TImplementation);
			ValidateRegistration(implType, implType);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			if (!registrations.TryAdd(implType, new FactoryTypeInfo(implType, implType, creator, ServiceLifeTime.Scoped, null)))
				throw new InvalidOperationException("Type " + implType.FullName + " has already been registered, use ReplaceScoped if you intend to overwrite previous registration.");
			return this;
		}


		public IFactory RegisterScoped<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			var srvType = typeof(TService);
			var implType = typeof(TImplementation);
			ValidateRegistration(srvType, implType);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			if (!registrations.TryAdd(srvType, new FactoryTypeInfo(srvType, implType, creator, ServiceLifeTime.Scoped, null)))
				throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceScoped if you intend to overwrite previous registration.");
			return this;
		}


		public virtual IFactory RegisterScoped<TService>(TService instance)
			where TService : class
		{
			if (instance == null) throw new ArgumentNullException(nameof(instance));
			var srvType = typeof(TService);
			var implType = instance.GetType();
			ValidateRegistration(srvType, implType);
			if (!registrations.TryAdd(srvType, new FactoryTypeInfo(srvType, implType, null, ServiceLifeTime.Scoped, null)))
				throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceSingleton if you intend to overwrite previous registration.");
			scopedInstances[srvType] = instance;
			return this;
		}


		public IFactory RegisterScoped(Type srvType, Type implType)
		{
			return RegisterScoped(srvType, implType, false);
		}


		public IFactory RegisterScoped(Type srvType, Func<IScope, object> createCallback)
		{
			return RegisterScoped(srvType, createCallback, false);
		}


		public IFactory RegisterScoped(Type srvType, Type implType, bool overwriteExisting)
		{
			if (srvType == null || implType == null)
				throw new ArgumentNullException("IFactory AddScoped(Type, Type) does not accept null arguments.");
			if (!overwriteExisting)
				ValidateRegistration(srvType, implType);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			var typeInfo = new FactoryTypeInfo(srvType, implType, creator, ServiceLifeTime.Scoped, null);
			if (!registrations.TryAdd(srvType, typeInfo))
			{
				if (overwriteExisting)
					registrations[srvType] = typeInfo;
				else
					throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceScoped if you intend to overwrite previous registration.");
			}
			return this;
		}


		public IFactory RegisterScoped(Type srvType, Func<IScope, object> createCallback, bool overwriteExisting)
		{
			if (srvType == null || createCallback == null)
				throw new ArgumentNullException("IFactory AddScoped(srvType, createCallback) does not accept null arguments.");
			if (!overwriteExisting)
				ValidateRegistration(srvType, null);
			FactoryObjectCreator creator = (f) => createCallback(f);
			var typeInfo = new FactoryTypeInfo(srvType, null, creator, ServiceLifeTime.Scoped, null);
			if (!registrations.TryAdd(srvType, typeInfo))
			{
				if (overwriteExisting)
					registrations[srvType] = typeInfo;
				else
					throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceScoped if you intend to overwrite previous registration.");
			}
			return this;
		}


		public virtual IFactory RegisterSingleton<TImplementation>()
			where TImplementation : class
		{
			var implType = typeof(TImplementation);
			ValidateRegistration(implType, implType);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			if (!registrations.TryAdd(implType, new FactoryTypeInfo(implType, implType, creator, ServiceLifeTime.Singleton, null)))
				throw new InvalidOperationException("Type " + implType.FullName + " has already been registered, use ReplaceSingleton if you intend to overwrite previous registration.");
			return this;
		}


		public virtual IFactory RegisterSingleton<TService>(TService instance)
			where TService : class
		{
			if (instance == null) throw new ArgumentNullException(nameof(instance));
			var srvType = typeof(TService);
			var implType = instance.GetType();
			ValidateRegistration(srvType, implType);
			if (!registrations.TryAdd(srvType, new FactoryTypeInfo(srvType, implType, null, ServiceLifeTime.Singleton, instance)))
				throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceSingleton if you intend to overwrite previous registration.");
			return this;
		}


		public virtual IFactory RegisterSingleton<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			var srvType = typeof(TService);
			var implType = typeof(TImplementation);
			ValidateRegistration(srvType, implType);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			if (!registrations.TryAdd(srvType, new FactoryTypeInfo(srvType, implType, creator, ServiceLifeTime.Singleton, null)))
				throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceSingleton if you intend to overwrite previous registration.");
			return this;
		}


		public IFactory RegisterSingleton(Type srvType, object instance)
		{
			return RegisterSingleton(srvType, instance, false);
		}


		public IFactory RegisterSingleton(Type srvType, Type implType)
		{
			return RegisterSingleton(srvType, implType, false);
		}


		public IFactory RegisterSingleton(Type srvType, Func<IScope, object> createCallback)
		{
			return RegisterSingleton(srvType, createCallback, false);
		}


		private IFactory RegisterSingleton(Type srvType, object instance, bool overwriteExisting)
		{
			if (srvType == null || instance == null)
				throw new ArgumentNullException("IFactory AddSingleton(srvType, instance) does not accept null arguments.");
			if (!overwriteExisting)
				ValidateRegistration(srvType, null);
			var typeInfo = new FactoryTypeInfo(srvType, instance.GetType(), null, ServiceLifeTime.Singleton, instance);
			if (!registrations.TryAdd(srvType, typeInfo))
			{
				if (overwriteExisting)
					registrations[srvType] = typeInfo;
				else
					throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceSingleton if you intend to overwrite previous registration.");
			}
			return this;
		}


		private IFactory RegisterSingleton(Type srvType, Type implType, bool overwriteExisting)
		{
			if (srvType == null || implType == null)
				throw new ArgumentNullException("IFactory AddSingleton(Type, Type) does not accept null arguments.");
			if (!overwriteExisting)
				ValidateRegistration(srvType, implType);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			var typeInfo = new FactoryTypeInfo(srvType, implType, creator, ServiceLifeTime.Singleton, null);
			if (!registrations.TryAdd(srvType, typeInfo))
			{
				if (overwriteExisting)
					registrations[srvType] = typeInfo;
				else
					throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceSingleton if you intend to overwrite previous registration.");
			}
			return this;
		}


		private IFactory RegisterSingleton(Type srvType, Func<IScope, object> createCallback, bool overwriteExisting)
		{
			if (srvType == null || createCallback == null)
				throw new ArgumentNullException("IFactory AddSingleton(srvType, createCallback) does not accept null arguments.");
			if (!overwriteExisting)
				ValidateRegistration(srvType, null);
			FactoryObjectCreator creator = (f) => createCallback(f);
			var typeInfo = new FactoryTypeInfo(srvType, null, creator, ServiceLifeTime.Singleton, null);
			if (!registrations.TryAdd(srvType, typeInfo))
			{
				if (overwriteExisting)
					registrations[srvType] = typeInfo;
				else
					throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use ReplaceSingleton if you intend to overwrite previous registration.");
			}
			return this;
		}


		public virtual IFactory ReplaceTransient<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			var srvType = typeof(TService);
			var implType = typeof(TImplementation);
			ValidateReplace(srvType, implType, ServiceLifeTime.Transient);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			registrations[srvType] = new FactoryTypeInfo(srvType, implType, creator, ServiceLifeTime.Transient, null);
			return this;
		}


		public IFactory ReplaceScoped<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			var srvType = typeof(TService);
			var implType = typeof(TImplementation);
			ValidateReplace(srvType, implType, ServiceLifeTime.Scoped);
			FactoryObjectCreator creator = GetObjectCreator(implType);
			registrations[srvType] = new FactoryTypeInfo(srvType, implType, creator, ServiceLifeTime.Scoped, null);
			return this;
		}


		public virtual IFactory ReplaceSingleton<TService>(TService instance)
			where TService : class
		{
			if (instance == null) throw new ArgumentNullException(nameof(instance));
			var srvType = typeof(TService);
			var implType = instance.GetType();
			ValidateReplace(srvType, implType, ServiceLifeTime.Singleton);
			registrations[srvType] = new FactoryTypeInfo(srvType, implType, null, ServiceLifeTime.Singleton, instance);
			return this;
		}


		public virtual IFactory ReplaceSingleton<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			var srvType = typeof(TService);
			var implType = typeof(TImplementation);
			ValidateReplace(srvType, implType, ServiceLifeTime.Singleton);
			FactoryObjectCreator creator = GetObjectCreator(typeof(TImplementation));
			registrations[srvType] = new FactoryTypeInfo(srvType, typeof(TImplementation), creator, ServiceLifeTime.Singleton, null);
			return this;
		}


		public T GetInstance<T>() where T : class
		{
			if (disposed) throw new ObjectDisposedException(this.GetType().Name);
			Type t = typeof(T);
			if (t.IsGenericType)
				return (T)GetGenericInstance(t);
			else
				return (T)GetNonGenericInstance(t);
		}


		public object GetInstance(Type t)
		{
			if (disposed) throw new ObjectDisposedException(this.GetType().Name);
			if (t.IsGenericType)
				return GetGenericInstance(t);
			else
				return GetNonGenericInstance(t);
		}


		public object GetGenericInstance(Type genericType, params Type[] typeParameters)
		{
			if (!genericType.IsGenericTypeDefinition)
				throw new InvalidOperationException("You need to specify a generic type that is a concrete class as the first argument, creating generic types from interface, abstract or value types is not supported.");

			if (genericType.IsInterface || genericType.IsAbstract)
				throw new InvalidOperationException("You need to specify a generic type that is a concrete class as the first argument, creating generic types from interface, abstract or value types is not supported.");

			if (!genericType.IsClass)
				throw new InvalidOperationException("You need to specify a generic type that is a concrete class as the first argument, creating generic types from interface, abstract or value types is not supported.");

			var stn = genericType.Name;
			var genericTypeImpl = genericType.MakeGenericType(typeParameters);
			return GetInstance(genericTypeImpl);
		}


		private object GetNonGenericInstance(Type t)
		{ 
			var registeredTypes = registrations;
			var foundRegistration = registeredTypes.TryGetValue(t, out var ti);
			if (!foundRegistration && isScope)
			{
				registeredTypes = scopeOwner.registrations;
				foundRegistration = registeredTypes.TryGetValue(t, out ti);
			}
			if (foundRegistration)
			{
				if (ti.LifeTime == ServiceLifeTime.Transient)
				{
					var instance = ti.Creator(this);
					ti.ConfigCallback?.Invoke(instance);
					return instance;
				}
				else if (ti.LifeTime == ServiceLifeTime.Singleton)
				{
					if (ti.Instance == null)
					{
						var instance = ti.Creator(this);
						ti.ConfigCallback?.Invoke(instance);
						lock (ti.SyncObj)
						{
							if (ti.Instance == null)
								ti.Instance = instance;
							else if (instance is IDisposable)
								(instance as IDisposable).Dispose();
						}
					}
					return ti.Instance;
				}
				else
				{
					if (!scopedInstances.TryGetValue(t, out var scopedInstance))
					{
						scopedInstance = registeredTypes[t].Creator(this);
						ti.ConfigCallback?.Invoke(scopedInstance);
						if (!scopedInstances.TryAdd(t, scopedInstance))
						{
							if (scopedInstance is IDisposable)
								(scopedInstance as IDisposable).Dispose();
							scopedInstance = scopedInstances[t];
						}
					}
					return scopedInstance;
				}
			}
			else
			{
				if (t.IsInterface)
				{
					throw new TypeResolutionException("Type " + t.Name + " is not registered in the system configuration.", t.FullName);
				}
				else
				{
					FactoryObjectCreator creator = GetObjectCreator(t);
					var instance = creator(this);
					registrations[t] = new FactoryTypeInfo(t, instance.GetType(), creator, ServiceLifeTime.Transient, null);
					return instance;
				}
			}
		}


		private object GetGenericInstance(Type t)
		{
			if (t.IsGenericTypeDefinition)
				throw new InvalidOperationException("Cannot create generic instance without type arguments.");
			var registeredTypes = registrations;
			var foundRegistration = registeredTypes.TryGetValue(t, out var ti);
			if (!foundRegistration && isScope)
			{
				registeredTypes = scopeOwner.registrations;
				foundRegistration = registeredTypes.TryGetValue(t, out ti);
			}
			if (!foundRegistration)
			{
				registeredTypes = registrations;
				var genericType = t.GetGenericTypeDefinition();
				foundRegistration = registeredTypes.TryGetValue(genericType, out ti);
				if (!foundRegistration && isScope)
				{
					registeredTypes = scopeOwner.registrations;
					foundRegistration = registeredTypes.TryGetValue(genericType, out ti);
				}
				if (foundRegistration)
				{
					var stn = t.Name;
					var genericTypeArgs = t.GetGenericArguments();
					var genericTypeImpl = ti.ImplementationType.MakeGenericType(genericTypeArgs);
					switch (ti.LifeTime)
					{
						case ServiceLifeTime.Transient:
							RegisterTransient(t, genericTypeImpl);
							break;
						case ServiceLifeTime.Scoped:
							RegisterScoped(t, genericTypeImpl);
							break;
						case ServiceLifeTime.Singleton:
							RegisterSingleton(t, genericTypeImpl);
							break;
					}
				}
				else
				{
					if (t.IsInterface)
					{
						throw new TypeResolutionException("Type " + genericType.Name + " was not registered in the container.", genericType.FullName);
					}
					else
					{
						FactoryObjectCreator creator = GetObjectCreator(t);
						var instance = creator(this);
						registrations[t] = new FactoryTypeInfo(t, t, creator, ServiceLifeTime.Transient, null);
						return instance;
					}
				}
			}
			return GetNonGenericInstance(t);
		}


		public virtual Type GetInstanceType(Type t)
		{
			var obj = GetInstance(t);
			if (obj != null)
				return obj.GetType();
			else
				throw new TypeResolutionException("Factory could not create an instance of type " + t.FullName, t.FullName);
		}

		public virtual void Clear()
		{
			registrations.Clear();
		}

		public void ForEach(Action<Type, Type, bool, object> action)
		{
			foreach(var r in registrations.Values)
			{
				action(r.ServiceType, r.ImplementationType, r.LifeTime == ServiceLifeTime.Singleton, r.Instance);
			}
		}


		public IScope CreateScope()
		{
			if (isScope)
			{
				Interlocked.Increment(ref scopeCount);
				return this;
			}
			else return new ServiceFactory(this);
		}


		public ServiceLifeTime GetServiceLifeTime<TService>() where TService : class
		{
			var t = typeof(TService);
			return GetServiceLifeTime(t);
		}


		public ServiceLifeTime GetServiceLifeTime(Type t)
		{
			var registeredTypes = registrations;
			var foundRegistration = registeredTypes.TryGetValue(t, out var ti);

			if (!foundRegistration && isScope)
			{
				registeredTypes = scopeOwner.registrations;
				foundRegistration = registeredTypes.TryGetValue(t, out ti);
			}

			if (foundRegistration)
				return ti.LifeTime;

			throw new TypeResolutionException($"Type {t.Name} was not registered in the container.", t.Name);
		}


		private void ValidateRegistration(Type srvType, Type implType)
		{
			if (disposed) throw new ObjectDisposedException(this.GetType().Name);
			if (implType != null && implType.IsInterface)
				throw new InvalidOperationException("Implementation type must be a concrete class.");
			if (registrations.ContainsKey(srvType))
				throw new InvalidOperationException("Type " + srvType.FullName + " has already been registered, use Replace if you intend to overwrite previous registration.");
		}


		private void ValidateReplace(Type srvType, Type implType, ServiceLifeTime lifetime)
		{
			if (disposed) throw new ObjectDisposedException(this.GetType().Name);
			if (implType.IsInterface)
				throw new InvalidOperationException("Implementation type must be a concrete class.");
			if (registrations.TryGetValue(srvType, out var info))
			{
				if (info.LifeTime != lifetime)
				{
					if (info.LifeTime == ServiceLifeTime.Transient)
						throw new Exception($"Cannot change registration type from {lifetime} to Transient. Use Replace{lifetime} instead.");
					if (info.LifeTime == ServiceLifeTime.Scoped)
						throw new Exception($"Cannot change registration type from {lifetime} to Scoped. Use Replace{lifetime} instead.");
					if (info.LifeTime == ServiceLifeTime.Singleton)
						throw new Exception($"Cannot change registration type from {lifetime} to Singleton. Use Replace{lifetime} instead.");
				}
			}
		}



		// ================================================================================
		// Dynamic Object Instantiation
		// ================================================================================

		private delegate object FactoryObjectCreator(IFactory f);

		private static ConcurrentDictionary<Type, FactoryObjectCreator> delegateCache = new ConcurrentDictionary<Type, FactoryObjectCreator>();

		private static FactoryObjectCreator GetObjectCreator(Type t)
		{
			string tname = t.Name;
			if (delegateCache.TryGetValue(t, out var cachedDelegate))
			{
				return cachedDelegate;
			}
			else
			{
				if (t.IsGenericTypeDefinition)
					return null;
				ConstructorInfo[] constructors = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
				if (constructors.Length <= 0)
					throw new TypeResolutionException("Type " + t.FullName + " requires a public constructor to be registered.", t.FullName);
				var constructorInfo = constructors[0];
				var method = GetCreator_NDC(constructorInfo);
				var creator = (FactoryObjectCreator)method.CreateDelegate(typeof(FactoryObjectCreator));
				if (delegateCache.TryAdd(t, creator))
					return creator;
				else
					return delegateCache[t];
			}
		}


		private static DynamicMethod GetCreator_NDC(ConstructorInfo constructor)
		{
			string methodName = String.Format("__{0}_DynamicObjectCreator", constructor.DeclaringType.Name);
			DynamicMethod method = new DynamicMethod(methodName, constructor.DeclaringType, new Type[] { typeof(IScope) }, true);
			ILGenerator gen = method.GetILGenerator();
			MethodInfo getInstance = typeof(IScope).GetMethod("GetInstance", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);

			ParameterInfo[] parameters = constructor.GetParameters();
			foreach (ParameterInfo p in parameters)
			{
				if (p.ParameterType.IsValueType || p.ParameterType.IsPrimitive)
				{
					LoadDefaultValue(gen, p);
				}
				else
				{
					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Callvirt, getInstance.MakeGenericMethod(p.ParameterType));
				}
			}
			gen.Emit(OpCodes.Newobj, constructor);
			gen.Emit(OpCodes.Ret);
			return method;
		}

		private static void LoadDefaultValue(ILGenerator il, ParameterInfo p)
		{
			if (p.ParameterType.IsPrimitive)
			{
				//Initializes the field using diferent "zero" numeric constants
				if (p.ParameterType == typeof(Byte))
					il.Emit(OpCodes.Ldc_I4_0);
				else if (p.ParameterType == typeof(Int16))
					il.Emit(OpCodes.Ldc_I4_0);
				else if (p.ParameterType == typeof(Int32))
					il.Emit(OpCodes.Ldc_I4_0);
				else if (p.ParameterType == typeof(Int64))
				{
					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Conv_I8);
				}
				else if (p.ParameterType == typeof(Single))
					il.Emit(OpCodes.Ldc_R4, 0.0f);
				else if (p.ParameterType == typeof(Double))
					il.Emit(OpCodes.Ldc_R8, 0.0d);
				else if (p.ParameterType == typeof(Char))
					il.Emit(OpCodes.Ldc_I4_0);
				else if (p.ParameterType == typeof(Boolean))
					il.Emit(OpCodes.Ldc_I4_0);
				else throw new Exception($"Invalid data type detected. Parameter: {p.Name}, Type: {p.ParameterType.Name}");  //Unhandled primitive data type, throw an exception
			}
			else if (p.ParameterType.IsValueType)
			{
				//Initializes the field using OpCode:Initobj (usable on any struct, usually a DateTime field)
				LocalBuilder tmp = il.DeclareLocal(p.ParameterType);
				il.Emit(OpCodes.Ldloca, tmp.LocalIndex);
				il.Emit(OpCodes.Initobj, p.ParameterType);
				il.Emit(OpCodes.Ldloc, tmp);
			}
			else il.Emit(OpCodes.Ldnull);       //Initializes the parameter with null since its a reference type
		}

		private static Delegate CreateDelegate(ConstructorInfo constructor, Type delegateType)
		{
			string methodName = String.Format("__{0}_DynamicObjectCreator", constructor.DeclaringType.Name);
			DynamicMethod method = new DynamicMethod(methodName, constructor.DeclaringType, new Type[] { typeof(IFactory) }, true);
			ILGenerator gen = method.GetILGenerator();
			MethodInfo getInstance = typeof(IScope).GetMethod("GetInstance", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
			ParameterInfo[] parameters = constructor.GetParameters();
			foreach (ParameterInfo p in parameters)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Callvirt, getInstance.MakeGenericMethod(p.ParameterType));
			}
			gen.Emit(OpCodes.Newobj, constructor);
			gen.Emit(OpCodes.Ret);
			return method.CreateDelegate(delegateType);
		}


#if NET461
		// =================================================================================
		// IDependencyResolver Implementation
		// =================================================================================
		private ConcurrentDictionary<Type, int> unresolvedTypes = new ConcurrentDictionary<Type, int>();

		public object GetService(Type serviceType)
		{
			try
			{
				if (unresolvedTypes.ContainsKey(serviceType))
					return null;
				return GetInstance(serviceType);
			}
			catch (TypeResolutionException)
			{
				unresolvedTypes.TryAdd(serviceType, 0);
				return null;
			}
		}

		public IEnumerable<object> GetServices(Type serviceType)
		{
			try
			{
				if (unresolvedTypes.ContainsKey(serviceType))
					return new List<object>();
				return new List<object> { GetInstance(serviceType) };
			}
			catch (TypeResolutionException)
			{
				unresolvedTypes.TryAdd(serviceType, 0);
				return new List<object>();
			}
		}

		public IDependencyScope BeginScope()
		{
			return this;
		}
#endif
	}


	[Serializable]
	public class TypeResolutionException: Exception, ISerializable
	{
		public string TypeName;

		public TypeResolutionException()
			: base()
		{
		}

		public TypeResolutionException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
			TypeName = (String)info.GetValue("typename", typeof(string));
		}

		public TypeResolutionException(string message, string typename)
			: base(message)
		{
			TypeName = typename;
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext ctx)
		{
			base.GetObjectData(info, ctx);
			info.AddValue("typename", TypeName);
		}
	}
}
