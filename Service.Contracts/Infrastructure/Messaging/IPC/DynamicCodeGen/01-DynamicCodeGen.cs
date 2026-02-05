using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using Services.Core;

namespace Service.Contracts.IPC
{
	static partial class DynamicCodeGen
	{
		class ContractIDs
		{
			public List<int> methodids = new List<int>();
			public Dictionary<Tuple<int, int>, int> eventids = new Dictionary<Tuple<int, int>, int>();
		}

		private static object SyncRoot;
		private static AssemblyBuilder asmBuilder;
		private static ModuleBuilder moduleBuilder;
		private static Dictionary<int, ContractIDs> generatedContracts;

		private static string lastValidationError;

		static DynamicCodeGen()
		{
			SyncRoot = new object();
			AssemblyName asmName = new AssemblyName("MsgClientDynamicProxies");
			asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
			moduleBuilder = asmBuilder.DefineDynamicModule("MsgClientDynamicProxies");
			generatedContracts = new Dictionary<int, ContractIDs>();
		}





		//====================================================================================
		//                                  Helper Methods
		//====================================================================================

		internal static void ValidateIDCollisions(Type serviceInterface)
		{
			int contractid = JenkinsHash.Compute(serviceInterface.Name);
			if (generatedContracts.ContainsKey(contractid))
				throw new Exception("Found a contractid collision. Type: " + serviceInterface.Name);
			else
				generatedContracts.Add(contractid, new ContractIDs());

			var contractIDs = generatedContracts[contractid];

			EventInfo[] events = serviceInterface.GetEventsEx();
			foreach (EventInfo e in events)
			{
				int eventid = GetEventID(e);
				Tuple<int, int> key = new Tuple<int, int>(contractid, eventid);
				if (contractIDs.eventids.ContainsKey(key))
					throw new Exception("Found a contractid.eventid collision. Type: " + serviceInterface.Name + ", Event: " + e.Name);
				else
					contractIDs.eventids.Add(key, 0);
			}

			MethodInfo[] methods = serviceInterface.GetMethodsEx();
			foreach (MethodInfo method in methods)
			{
				if (method.IsSpecialName)
					continue;

				int methodid = GetMethodID(method);
				if (contractIDs.methodids.Contains(methodid))
					throw new Exception("Found a contractid.methodid collision. Type: " + serviceInterface.Name + ", Method: " + method.Name);
				else
					contractIDs.methodids.Add(methodid);
			}
		}


		internal static void ValidateInterfaceType(Type serviceInterface)
		{
			if (!serviceInterface.IsInterface)
				throw new Exception("The serviceInterface parameter must be an interface type.");

			MemberInfo[] members = serviceInterface.GetMembers();

			// Verify that the interface members are either events or methods. (Properties, Indexers, etc. are not allowed).
			foreach (MemberInfo member in members)
			{
				if (member.MemberType != MemberTypes.Event && member.MemberType != MemberTypes.Method)
					throw new Exception("Invalid serviceInterface type: The service interface type can only define Methods and Events. The following member cannot be allowed in the interface: " + member.Name + " (" + member.MemberType + ")");
			}

			// Verify that all the events have valid data types.
			EventInfo[] events = serviceInterface.GetEventsEx();
			foreach (EventInfo e in events)
			{
				MethodInfo method = e.EventHandlerType.GetMethod("Invoke");
				if (method.ReturnType != typeof(void))
					throw new Exception("Invalid serviceInterface type: The underlying delegate for event " + e.Name + " must have a void return type.");

				ParameterInfo[] parameters = method.GetParameters();
				foreach (ParameterInfo param in parameters)
				{
					if (param.IsOut || param.IsIn || param.IsOptional)
						throw new Exception("Invalid serviceInterface type: The underlying delegate for event " + e.Name + " has one or more arguments marked as in, out, ref or optional; these options are not supported.");

					if (param.ParameterType.Name.EndsWith("&"))
						throw new Exception("Invalid serviceInterface type: The underlying delegate for event " + e.Name + " has one or more arguments marked as in, out, ref or optional; these options are not supported.");

					try
					{
						if (!IsValidDataType(param.ParameterType, true))
							throw new Exception("Invalid serviceInterface type: The underlying delegate for event " + e.Name + " has an argument of an unsupported data type (" + param.ParameterType.Name + ").");
					}
					catch (Exception ex)
					{
						throw new Exception("Invalid serviceInterface type: The underlying delegate for event " + e.Name + " has an argument of an unsupported data type (" + param.ParameterType.Name + "): " + ex.Message);
					}
				}
			}

			// Verify that all methods have valid data types.
			MethodInfo[] methods = serviceInterface.GetMethodsEx();
			foreach (MethodInfo method in methods)
			{
				lastValidationError = "";

				if (method.IsSpecialName)
					continue;

				if (!IsValidReturnDataType(method.ReturnType))
					throw new Exception("Invalid serviceInterface type: Method " + method.Name + " has a return type that is not supported (" + method.ReturnType.Name + "). " + lastValidationError);

				ParameterInfo[] parameters = method.GetParameters();
				foreach (ParameterInfo param in parameters)
				{
					if (param.IsOut || param.IsIn || param.IsOptional)
						throw new Exception("Invalid serviceInterface type: One or more arguments in method " + method.Name + " are marked as in, out, ref or optional; these options are not supported.");

					if (param.ParameterType.Name.EndsWith("&"))
						throw new Exception("Invalid serviceInterface type: One or more arguments in method " + method.Name + " are marked as in, out, ref or optional; these options are not supported.");

					if (!IsValidDataType(param.ParameterType))
						throw new Exception("Invalid serviceInterface type: The method " + method.Name + " has an argument of an unsupported data type (" + param.ParameterType.Name + "). " + lastValidationError);
				}
			}
		}


		private static bool IsValidReturnDataType(Type returnType)
		{
			if (typeof(Task).IsAssignableFrom(returnType))
			{
				if (!returnType.IsGenericType)
					return true;
				else if (IsValidDataType(returnType.GetGenericArguments()[0]))
					return true;
				else
					return false;
			}
			else
			{
				return IsValidDataType(returnType);
			}
		}


		internal static bool IsValidDataType(Type type, bool isEvent = false)
		{
			if (type == typeof(bool) || type == typeof(byte) || type == typeof(char) ||
				type == typeof(int) || type == typeof(long) || type == typeof(float) ||
				type == typeof(double) || type == typeof(string) || type == typeof(DateTime) ||
				type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(void) ||
				type.IsEnum || type == typeof(decimal) ||
				(type.IsArray && IsValidDataType(type.GetElementType(), isEvent)) ||
				(typeof(IList).IsAssignableFrom(type) && type.IsGenericType && IsValidDataType(type.GetGenericArguments()[0], isEvent)) ||
				(typeof(Stream).IsAssignableFrom(type) && !isEvent) ||
				((type.IsClass && typeof(ICanSerialize).IsAssignableFrom(type)) || (type.IsClass && CanAutoSerialize(type))) || type.IsInterface)
			{
				return true;
			}
			else
			{
				return false;
			}
		}


		internal static bool CanAutoSerialize(Type type)
		{
			ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
			if (constructor == null)
			{
				lastValidationError = String.Format("User defined type ({0}) must have a parameterless constructor. {1}", type.Name, lastValidationError);
				return false;
			}
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo p in properties)
			{
				if (!IsValidDataType(p.PropertyType))
				{
					lastValidationError = String.Format("User defined type ({0}) contains a Property that cannot be serialized automatically because its type ({1}) is not supported. {2}", type.Name, p.PropertyType.Name, lastValidationError);
					return false;
				}
			}
			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (FieldInfo f in fields)
			{
				if (!IsValidDataType(f.FieldType))
				{
					lastValidationError = String.Format("User defined type ({0}) contains a Field that cannot be serialized automatically because its type ({1}) is not supported. {2}", type.Name, f.FieldType.Name, lastValidationError);
					return false;
				}
			}
			return true;
		}


		internal static bool IsAsyncMethod(MethodInfo method)
		{
			if (typeof(Task).IsAssignableFrom(method.ReturnType))
				return true;
			else
				return false;
		}


		internal static LocalBuilder ExtractThis(ILGenerator il)
		{
			LocalBuilder local = il.DeclareLocal(typeof(object));
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Stloc, local);
			return local;
		}


		/// <summary>
		/// Adds the necesary code to extract a value from the RecvBuffer, the value is left in the local variable represented by the LocalBuilder returned by this method.
		/// </summary>
		internal static void GetValueFromBuffer(ILGenerator il, Type type)
		{
			if (type == typeof(bool))
				il.Emit(OpCodes.Callvirt, GetBoolean);
			else if (type == typeof(byte))
				il.Emit(OpCodes.Callvirt, GetByte);
			else if (type == typeof(char))
				il.Emit(OpCodes.Callvirt, GetChar);
			else if (type == typeof(int) || type.IsEnum)
				il.Emit(OpCodes.Callvirt, GetInt32);
			else if (type == typeof(long))
				il.Emit(OpCodes.Callvirt, GetInt64);
			else if (type == typeof(float))
				il.Emit(OpCodes.Callvirt, GetSingle);
			else if (type == typeof(double))
				il.Emit(OpCodes.Callvirt, GetDouble);
			else if (type == typeof(decimal))
				il.Emit(OpCodes.Callvirt, GetDecimal);
			else if (type == typeof(string))
				il.Emit(OpCodes.Callvirt, GetString);
			else if (type == typeof(DateTime))
				il.Emit(OpCodes.Callvirt, GetDateTime);
			else if (type == typeof(TimeSpan))
				il.Emit(OpCodes.Callvirt, GetTimeSpan);
			else if (type == typeof(Guid))
				il.Emit(OpCodes.Callvirt, GetGuid);
			else if (type.IsArray)
			{
				MethodInfo method = GetArray.MakeGenericMethod(type.GetElementType());
				il.Emit(OpCodes.Callvirt, method);
			}
			else if (typeof(IList).IsAssignableFrom(type))
			{
				MethodInfo method = GetList.MakeGenericMethod(type.GetGenericArguments()[0]);
				il.Emit(OpCodes.Callvirt, method);
			}
			else if (typeof(Stream).IsAssignableFrom(type))
			{
				il.Emit(OpCodes.Callvirt, GetStream);
			}
			else if (typeof(ICanSerialize).IsAssignableFrom(type) || type.IsClass || type.IsInterface)
			{
				MethodInfo method = GetObject.MakeGenericMethod(type);
				il.Emit(OpCodes.Callvirt, method);
			}
			else
			{
				throw new Exception("Cannot handle a value of type " + type.FullName);
			}
		}


		/// <summary>
		/// Adds the necesary code to place a method argument in the ResponseBuffer
		/// </summary>
		internal static void AddValueToBuffer(ILGenerator il, Type type)
		{
			if (type == typeof(bool))
				il.Emit(OpCodes.Callvirt, AddBoolean);
			else if (type == typeof(byte))
				il.Emit(OpCodes.Callvirt, AddByte);
			else if (type == typeof(char))
				il.Emit(OpCodes.Callvirt, AddChar);
			else if (type == typeof(int) || type.IsEnum)
				il.Emit(OpCodes.Callvirt, AddInt32);
			else if (type == typeof(long))
				il.Emit(OpCodes.Callvirt, AddInt64);
			else if (type == typeof(float))
				il.Emit(OpCodes.Callvirt, AddSingle);
			else if (type == typeof(double))
				il.Emit(OpCodes.Callvirt, AddDouble);
			else if (type == typeof(decimal))
				il.Emit(OpCodes.Callvirt, AddDecimal);
			else if (type == typeof(string))
			{
				il.Emit(OpCodes.Ldc_I4_0); // pass argument value for useUTF as false
				il.Emit(OpCodes.Callvirt, AddString);
			}
			else if (type == typeof(DateTime))
				il.Emit(OpCodes.Callvirt, AddDateTime);
			else if (type == typeof(TimeSpan))
				il.Emit(OpCodes.Callvirt, AddTimeSpan);
			else if (type == typeof(Guid))
				il.Emit(OpCodes.Callvirt, AddGuid);
			else if (type.IsArray)
			{
				MethodInfo method = AddArray.MakeGenericMethod(type.GetElementType());
				il.Emit(OpCodes.Callvirt, method);
			}
			else if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
			{
				MethodInfo method = AddList.MakeGenericMethod(type.GetGenericArguments()[0]);
				il.Emit(OpCodes.Callvirt, method);
			}
			else if (typeof(Stream).IsAssignableFrom(type))
			{
				il.Emit(OpCodes.Callvirt, AddStream);
			}
			else if (typeof(ICanSerialize).IsAssignableFrom(type) || type.IsClass || type.IsInterface)
			{
				MethodInfo method = AddObject.MakeGenericMethod(type);
				il.Emit(OpCodes.Callvirt, method);
			}
			else
				throw new Exception("Cannot handle value of type " + type.FullName);
		}


		internal static MethodInfo GetSendTypedRequestAsyncMethod(Type returnType)
		{
			var method = typeof(RequestInfo).GetMethod("SendTypedRequestAsync");
			return method.MakeGenericMethod(returnType);
		}

		internal static ConstructorInfo GetActionConstructor()
		{
			var actionType = typeof(Action<Task>);
			var ctor = actionType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(object), typeof(IntPtr) }, null);
			return ctor;
		}


		internal static ConstructorInfo GetFuncConstructor(Type returnType)
		{
			var actionType = typeof(Func<>).MakeGenericType(returnType);
			var ctor = actionType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(object), typeof(IntPtr) }, null);
			return ctor;
		}


		internal static MethodInfo GetContinueWithFunction(Type returnType)
		{
			var funcType = typeof(Func<,>).MakeGenericType(typeof(Task), returnType);
			var taskType = typeof(Task<>).MakeGenericType(returnType);
			var methods = taskType.GetMethods().Where(m => m.Name == "ContinueWith")
				.Select(m => new
				{
					Method = m,
					Params = m.GetParameters(),
					Args = m.GetGenericArguments()
				})
				.Where(
					m2 => m2.Method.IsGenericMethod &&
					m2.Params.Length == 1 &&
					m2.Args.Length == 1 &&
					m2.Params[0].ParameterType.IsGenericType &&
					m2.Params[0].ParameterType.GetGenericArguments()[0] == typeof(Task) &&
					m2.Params[0].ParameterType.GetGenericArguments()[1].IsGenericParameter
				).ToList();
			var continueWith = methods[0].Method.MakeGenericMethod(returnType);
			return continueWith;
		}


		internal static MethodInfo GetContinueWithAction(Type taskType)
		{
			var method = taskType.GetMethod("ContinueWith", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(Action<Task>), typeof(TaskContinuationOptions) }, null);
			return method;
		}


		internal static MethodInfo GetTaskExceptionMethod(Type taskType)
		{
			var property = taskType.GetProperty("Exception", BindingFlags.Public | BindingFlags.Instance);
			return property.GetGetMethod();
		}

		internal static MethodInfo GetTaskResultMethod(Type taskType)
		{
			var property = taskType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
			return property.GetGetMethod();
		}


		private static ConstructorInfo MsgCallbackConstructor = typeof(int).GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }); //MsgCallback)
		private static MethodInfo StartRequestMethod = typeof(ClientProxy).GetMethod("StartRequest", BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo StartRequestAsyncMethod = typeof(ClientProxy).GetMethod("StartRequestAsync", BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo SendRequestMethod = typeof(ClientProxy).GetMethod("SendRequest", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(RequestInfo) }, null);
		private static MethodInfo ClearPrincipalMethod = typeof(ClientProxy).GetMethod("ClearPrincipal", BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo SendRequestAsyncMethod = typeof(ClientProxy).GetMethod("SendRequestAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(RequestInfo) }, null);
		private static MethodInfo RegisterEvent = typeof(ClientProxy).GetMethod("RegisterEvent", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(int) }, null);
		private static MethodInfo UnregisterEvent = typeof(ClientProxy).GetMethod("UnregisterEvent", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(int) }, null);
		private static MethodInfo CreateEventMethod = typeof(ServerProxy).GetMethod("CreateEvent", BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo CreateResponseMethod = typeof(ServerProxy).GetMethod("CreateResponse", BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo SendEventMethod = typeof(ServerProxy).GetMethod("SendEvent", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(EventData) }, null);
		private static MethodInfo EnqueueMessageMethod = typeof(MsgSession).GetMethod("EnqueueMessage", BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo SetErrorMethod = typeof(ProtocolBuffer).GetMethod("SetError", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo MsgServiceCallback = typeof(ServerProxy).GetMethod("MsgServiceCallback", BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo StringEquals = typeof(string).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null);
		private static MethodInfo AddBoolean = typeof(SerializationBuffer).GetMethod("AddBoolean", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetBoolean = typeof(SerializationBuffer).GetMethod("GetBoolean", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddByte = typeof(SerializationBuffer).GetMethod("AddByte", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetByte = typeof(SerializationBuffer).GetMethod("GetByte", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddChar = typeof(SerializationBuffer).GetMethod("AddChar", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetChar = typeof(SerializationBuffer).GetMethod("GetChar", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddInt32 = typeof(SerializationBuffer).GetMethod("AddInt32", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetInt32 = typeof(SerializationBuffer).GetMethod("GetInt32", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddInt64 = typeof(SerializationBuffer).GetMethod("AddInt64", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetInt64 = typeof(SerializationBuffer).GetMethod("GetInt64", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddSingle = typeof(SerializationBuffer).GetMethod("AddSingle", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetSingle = typeof(SerializationBuffer).GetMethod("GetSingle", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddDouble = typeof(SerializationBuffer).GetMethod("AddDouble", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetDouble = typeof(SerializationBuffer).GetMethod("GetDouble", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddDecimal = typeof(SerializationBuffer).GetMethod("AddDecimal", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetDecimal = typeof(SerializationBuffer).GetMethod("GetDecimal", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddString = typeof(SerializationBuffer).GetMethod("AddString", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetString = typeof(SerializationBuffer).GetMethod("GetString", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddDateTime = typeof(SerializationBuffer).GetMethod("AddDateTime", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetDateTime = typeof(SerializationBuffer).GetMethod("GetDateTime", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddTimeSpan = typeof(SerializationBuffer).GetMethod("AddTimeSpan", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetTimeSpan = typeof(SerializationBuffer).GetMethod("GetTimeSpan", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddGuid = typeof(SerializationBuffer).GetMethod("AddGuid", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetGuid = typeof(SerializationBuffer).GetMethod("GetGuid", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddArray = typeof(SerializationBuffer).GetMethod("AddArray", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetArray = typeof(SerializationBuffer).GetMethod("GetArray", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddList = typeof(SerializationBuffer).GetMethod("AddList", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetList = typeof(SerializationBuffer).GetMethod("GetList", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddStream = typeof(SerializationBuffer).GetMethod("AddStream", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetStream = typeof(SerializationBuffer).GetMethod("GetStream", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AddObject = typeof(SerializationBuffer).GetMethod("AddObject", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetObject = typeof(SerializationBuffer).GetMethod("GetObject", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo GetDefaultValue = typeof(SerializationBuffer).GetMethod("GetDefaultValue", BindingFlags.Public | BindingFlags.Static);
		private static MethodInfo GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
		private static MethodInfo TimeSpanFromSeconds = typeof(TimeSpan).GetMethod("FromSeconds", BindingFlags.Public | BindingFlags.Static);
		private static MethodInfo DebugWriteLineMethod = typeof(Debug).GetMethod("WriteLine", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
		private static FieldInfo MsgServiceAppLogField = typeof(ServerProxy).GetField("log", BindingFlags.NonPublic | BindingFlags.Instance);
		private static FieldInfo ServerProxyServiceInterfaceField = typeof(ServerProxy).GetField("ServiceInterface", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo AppLogLogExceptionMethod = typeof(ILogService).GetMethod("LogException", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(Exception) }, null);
		private static ConstructorInfo ServerProxyConstructor = typeof(ServerProxy).GetConstructor(BindingFlags.Public|BindingFlags.FlattenHierarchy|BindingFlags.Instance, null, new Type[] { typeof(MsgPeer), typeof(ILogService) }, null);
		private static ConstructorInfo ClientProxyConstructor = typeof(ClientProxy).GetConstructor(new Type[] { typeof(IScope), typeof(int) });
		private static ConstructorInfo ProtocolBufferConstructor = typeof(ProtocolBuffer).GetConstructor(new Type[] { typeof(IScope) });
		private static FieldInfo ProtocolBufferMsgID = typeof(ProtocolBuffer).GetField("msgid", BindingFlags.NonPublic|BindingFlags.Instance);
		private static MethodInfo ProtocolBufferCreateResponse = typeof(ProtocolBuffer).GetMethod("CreateResponse", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
		private static MethodInfo ProtocolBufferStartMessage = typeof(ProtocolBuffer).GetMethod("StartMessage", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(MsgOpcode), typeof(int) }, null);
		private static MethodInfo ProtocolBufferEndMessage = typeof(ProtocolBuffer).GetMethod("EndMessage", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
		private static MethodInfo ProtocolBufferMark = typeof(ProtocolBuffer).GetMethod("Mark", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
		private static MethodInfo ProtocolBufferSetError = typeof(ProtocolBuffer).GetMethod("SetError", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(Exception) }, null);
		private static MethodInfo MsgSessionSendMessage = typeof(MsgSession).GetMethod("SendMessage", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(RequestInfo) }, null);

		private static FieldInfo RequestInfoOutputField = typeof(RequestInfo).GetField("output", BindingFlags.Public | BindingFlags.Instance);
		private static FieldInfo RequestInfoInputField = typeof(RequestInfo).GetField("input", BindingFlags.Public | BindingFlags.Instance);
		private static FieldInfo RequestInfoSession = typeof(RequestInfo).GetField("session", BindingFlags.Public | BindingFlags.Instance);
		private static FieldInfo RequestInfoErrorField = typeof(RequestInfo).GetField("error", BindingFlags.Public | BindingFlags.Instance);
		private static MethodInfo RequestInfoSendRequest = typeof(RequestInfo).GetMethod("SendRequest", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
		private static MethodInfo RequestInfoSendRequestAsync = typeof(RequestInfo).GetMethod("SendRequestAsync", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
		private static MethodInfo RequestInfoDispose = typeof(RequestInfo).GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
		private static MethodInfo SendVoidRequestAsync = typeof(RequestInfo).GetMethod("SendVoidRequestAsync", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
		private static MethodInfo IDisposableDispose = typeof(IDisposable).GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
		private static ConstructorInfo ObjectConstructor = typeof(Object).GetConstructor(Type.EmptyTypes);
	}


	static class MessagingTypeExtensions
	{
		public static MethodInfo[] GetMethodsEx(this Type t)
		{
			List<MethodInfo> result = new List<MethodInfo>();
			Type[] baseTypes = t.GetInterfaces();
			foreach (Type bt in baseTypes)
				result.AddRange(bt.GetMethodsEx());
			MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public);
			foreach (MethodInfo m in methods)
				result.Add(m);
			return result.ToArray();
		}

		public static EventInfo[] GetEventsEx(this Type t)
		{
			List<EventInfo> result = new List<EventInfo>();
			Type[] baseTypes = t.GetInterfaces();
			foreach (Type bt in baseTypes)
				result.AddRange(bt.GetEventsEx());
			EventInfo[] events = t.GetEvents(BindingFlags.Instance | BindingFlags.Public);
			foreach (EventInfo e in events)
				result.Add(e);
			return result.ToArray();
		}


		public static MethodInfo GetMethodEx(this Type t, string name)
		{
			MethodInfo m = t.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
			if (m != null)
			{
				return m;
			}
			else
			{
				Type[] baseTypes = t.GetInterfaces();
				foreach (Type bt in baseTypes)
				{
					m = bt.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
					if (m != null)
						return m;
				}
				return null;
			}
		}
	}
}
