using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.IPC
{
    static partial class DynamicCodeGen
    {
		//====================================================================================
		//                              ServerProxy Generator
		//
		// Creates an implementation of the given service that can be used to handle requests
		// comming from a remote connection.
		//
		// See Templates/ServerProxyExample.cs for an example of how the generated class
		// would look.
		//====================================================================================

		internal static ServerProxy CreateServerProxy(Type serviceContract, IMsgPeer peer, ILogService log, object serviceImplementation)
		{
			Type t;
			Type serviceImpl = serviceImplementation.GetType();
			if (!serviceContract.IsAssignableFrom(serviceImpl))
				throw new Exception($"The object supplied as serviceImplementation argument ({serviceImpl.FullName}) must implement the service contract {serviceContract.FullName}.");

			lock (DynamicCodeGen.SyncRoot)
			{
				var handlers = new Dictionary<string, MethodBuilder>();
				string dynamicTypeName = serviceContract.Name + "_ServerProxy";
				t = DynamicCodeGen.moduleBuilder.GetType(dynamicTypeName);
				if (t == null)
				{
					// Validate to see if the serviceInterface can be implemented as a proxy to a remote service,
					// several validations are performed on each member of the interface to see if a proxy can be created.
					DynamicCodeGen.ValidateInterfaceType(serviceContract);
					DynamicCodeGen.ValidateIDCollisions(serviceContract);

					TypeBuilder typeBuilder = DynamicCodeGen.moduleBuilder.DefineType(
						dynamicTypeName,                                    // The name of the dynamic type
						TypeAttributes.Public | TypeAttributes.Class,       // Type attributes
						typeof(ServerProxy));                              // The base type of the class

					// declare the field that stores the service instance
					var serviceField = typeBuilder.DefineField("service", serviceContract, FieldAttributes.Public);

					//// Implement the method that creates the service instance
					ImplementServerProxyConstructor(typeBuilder, serviceContract, serviceField);

					//// Implement the method that creates the service instance
					ImplementServerProxyStartMethod(typeBuilder, serviceContract, serviceImpl, serviceField, handlers);

					// Implement the method that disposes the service instance
					ImplementServerProxyStopMethod(typeBuilder, serviceContract, serviceImpl, serviceField, handlers);

					// Implement the interface synchornous methods and wire all the necesary code to invoke them from a remote end point.
					ImplementServerProxyInvokeMethod(typeBuilder, serviceContract, serviceField);

					// Implement the interface asynchornous methods (Task return type) and wire all the necesary code to invoke them from a remote end point.
					ImplementServerProxyInvokeMethodAsync(typeBuilder, serviceContract, serviceField);

					t = typeBuilder.CreateType();
				}
			}
			// The use of activator is unavoidable at this point, but this code is executed only once during the whole application life time.
			ServerProxy srv = (ServerProxy)Activator.CreateInstance(t, peer, log, serviceImplementation);
			return srv;
		}


		private static void ImplementServerProxyConstructor(TypeBuilder typeBuilder, Type serviceType, FieldBuilder serviceField)
		{
			ConstructorBuilder ctor = typeBuilder.DefineConstructor(
					MethodAttributes.Public,
					CallingConventions.Standard,
					new Type[] { typeof(MsgPeer), typeof(ILogService), serviceType });

			var il = ctor.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, ServerProxyConstructor);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Stfld, serviceField);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Stfld, ServerProxyServiceInterfaceField);
			il.Emit(OpCodes.Ret);
		}


		/// <summary>
		/// Implements the method that initializes the service instance and subscribes to all the events.
		/// </summary>
		private static void ImplementServerProxyStartMethod(
			TypeBuilder typeBuilder,
			Type serviceContract,
			Type serviceImpl,
			FieldBuilder serviceField,
			Dictionary<string, MethodBuilder> handlers)
		{
			MethodBuilder method = typeBuilder.DefineMethod("Start",
				MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig,
				typeof(void), Type.EmptyTypes);

			ILGenerator il = method.GetILGenerator();

			// subscribe to each event
			EventInfo[] events = serviceContract.GetEventsEx();
			foreach (EventInfo e in events)
			{
				var delegateCtor = e.EventHandlerType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(object), typeof(IntPtr) }, null);
				var handler = ImplementServerProxyEventHandler(typeBuilder, serviceContract, e);
				handlers.Add("service_" + e.Name, handler);
				MethodInfo eventAddMethod = serviceImpl.GetMethodEx("add_" + e.Name);

				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, serviceField);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldftn, handler);
				il.Emit(OpCodes.Newobj, delegateCtor);
				il.Emit(OpCodes.Callvirt, eventAddMethod);
			}
			il.Emit(OpCodes.Ret);
		}


		/// <summary>
		/// This method implements the event handler of each of the events defined in the interface contract.
		/// The event handler simply creates an event message and sends it to the clients using the SendEvent method of the base class.
		/// </summary>
		private static MethodBuilder ImplementServerProxyEventHandler(TypeBuilder typeBuilder, Type serviceContract, EventInfo e)
		{
			int i = 0;
			MethodInfo invoke = e.EventHandlerType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);
			ParameterInfo[] invokeParams = invoke.GetParameters();
			Type[] paramTypes = new Type[invokeParams.Length];
			foreach (ParameterInfo pInfo in invokeParams)
				paramTypes[i++] = pInfo.ParameterType;

			MethodBuilder method = typeBuilder.DefineMethod("service_" + e.Name,
				MethodAttributes.Private | MethodAttributes.HideBySig,
				CallingConventions.Standard | CallingConventions.HasThis,
				invoke.ReturnType, paramTypes);

			ILGenerator il = method.GetILGenerator();
			LocalBuilder eventData = il.DeclareLocal(typeof(EventData));
			LocalBuilder buffer = il.DeclareLocal(typeof(ProtocolBuffer));
			LocalBuilder locException = il.DeclareLocal(typeof(Exception));
			FieldInfo bufferField = typeof(EventData).GetField("buffer");

			il.BeginExceptionBlock();
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldc_I4, JenkinsHash.Compute(serviceContract.Name));
				il.Emit(OpCodes.Ldc_I4, GetEventID(e));
				il.Emit(OpCodes.Call, CreateEventMethod);
				il.Emit(OpCodes.Stloc, eventData);
				il.Emit(OpCodes.Ldloc, eventData);
				il.Emit(OpCodes.Ldfld, bufferField);
				il.Emit(OpCodes.Stloc, buffer);

				// Add all the event parameters in the buffer
				foreach (ParameterInfo parameterInfo in invokeParams)
				{
					// skip the sender argument if it is the first argument of the delegate and is of data type object (the sender will be suplied on the client side by the ClientProxy).
					if (parameterInfo.Position == 0 && parameterInfo.Name == "sender" && parameterInfo.ParameterType == typeof(object))
						continue;
					il.Emit(OpCodes.Ldloc, buffer);
					il.Emit(OpCodes.Ldarg, parameterInfo.Position + 1);
					DynamicCodeGen.AddValueToBuffer(il, parameterInfo.ParameterType);
				}
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloc, eventData);
				il.Emit(OpCodes.Call, SendEventMethod);
			}
			il.BeginCatchBlock(typeof(Exception));
			{
				il.Emit(OpCodes.Stloc, locException);
				il.Emit(OpCodes.Ldarg_0);                                   // this
				il.Emit(OpCodes.Ldfld, MsgServiceAppLogField);              //      .log
				il.Emit(OpCodes.Ldloc, locException);						//
				il.Emit(OpCodes.Callvirt, AppLogLogExceptionMethod);        //           .LogException(ex)
			}
			il.EndExceptionBlock();

			il.Emit(OpCodes.Ret);
			return method;
		}


		/// <summary>
		/// Implements the method that disposes the service instance.
		/// </summary>
		private static void ImplementServerProxyStopMethod(
			TypeBuilder typeBuilder,
			Type serviceContract,
			Type serviceImpl,
			FieldBuilder serviceField,
			Dictionary<string, MethodBuilder> handlers)
		{
			MethodBuilder method = typeBuilder.DefineMethod("Stop",
				MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig,
				typeof(void), Type.EmptyTypes);

			ILGenerator il = method.GetILGenerator();

			// unsubscribes from each event
			EventInfo[] events = serviceContract.GetEventsEx();
			foreach (EventInfo e in events)
			{
				ConstructorInfo delegateCtor = e.EventHandlerType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(object), typeof(IntPtr) }, null);
				MethodInfo eventRemoveMethod = serviceImpl.GetMethodEx("remove_" + e.Name);

				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, serviceField);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldftn, handlers["service_" + e.Name]);
				il.Emit(OpCodes.Newobj, delegateCtor);
				il.Emit(OpCodes.Callvirt, eventRemoveMethod);
			}
			il.Emit(OpCodes.Ret);
		}


		private static void ImplementServerProxyInvokeMethod(TypeBuilder typeBuilder, Type serviceContract, FieldBuilder serviceField)
		{
			ConstructorInfo exConstructor = typeof(Exception).GetConstructor(new Type[] { typeof(string) });

			// Implement the InvokeMethod override
			MethodBuilder invokeMethod = typeBuilder.DefineMethod("InvokeMethod",
				MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig,
				typeof(void), new Type[] { typeof(int), typeof(RequestInfo) });

			ILGenerator il = invokeMethod.GetILGenerator();
			Label methodEnd = il.DefineLabel();

			MethodInfo[] methods = serviceContract.GetMethodsEx();
			foreach (MethodInfo method in methods)
			{
				if (method.IsSpecialName || IsAsyncMethod(method)) continue;

				Label CaseEnd = il.DefineLabel();
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldc_I4, GetMethodID(method));
				il.Emit(OpCodes.Bne_Un, CaseEnd);

				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, serviceField);
				foreach (ParameterInfo paramInfo in method.GetParameters())
				{
					il.Emit(OpCodes.Ldarg_2);
					il.Emit(OpCodes.Ldfld, RequestInfoInputField);
					DynamicCodeGen.GetValueFromBuffer(il, paramInfo.ParameterType);
				}
				il.Emit(OpCodes.Callvirt, method);

				if (method.ReturnType != typeof(void))
				{
					// Put the return value in the response buffer
					LocalBuilder returnValue = il.DeclareLocal(method.ReturnType);
					il.Emit(OpCodes.Stloc, returnValue);
					il.Emit(OpCodes.Ldarg_2);
					il.Emit(OpCodes.Ldfld, RequestInfoOutputField);
					il.Emit(OpCodes.Ldloc, returnValue);
					DynamicCodeGen.AddValueToBuffer(il, method.ReturnType);
				}
				else
				{
					// For void methods add 0 in the response
					il.Emit(OpCodes.Ldarg_2);
					il.Emit(OpCodes.Ldfld, RequestInfoOutputField);
					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Callvirt, AddInt32);
				}

				il.Emit(OpCodes.Br, methodEnd);
				il.MarkLabel(CaseEnd);
			}

			// Default: throw exception
			il.Emit(OpCodes.Ldstr, "Unknown method.");
			il.Emit(OpCodes.Newobj, exConstructor);
			il.Emit(OpCodes.Throw);

			il.MarkLabel(methodEnd);
			il.Emit(OpCodes.Ret);
		}


		private static void ImplementServerProxyInvokeMethodAsync(TypeBuilder typeBuilder, Type serviceContract, FieldBuilder serviceField)
		{
			ConstructorInfo exConstructor = typeof(Exception).GetConstructor(new Type[] { typeof(string) });

			// Implement the InvokeMethod override
			MethodBuilder invokeMethod = typeBuilder.DefineMethod("InvokeMethodAsync",
				MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig,
				typeof(Task), new Type[] { typeof(int), typeof(RequestInfo) });

			ILGenerator il = invokeMethod.GetILGenerator();
			Label methodEnd = il.DefineLabel();

			MethodInfo[] methods = serviceContract.GetMethodsEx();
			foreach (MethodInfo method in methods)
			{
				if (method.IsSpecialName || !IsAsyncMethod(method)) continue;

				Label CaseEnd = il.DefineLabel();
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldc_I4, GetMethodID(method));
				il.Emit(OpCodes.Bne_Un, CaseEnd);

				var runnerType = CreateAsyncRunnerClass(serviceContract, method);
				var runnerCtor = runnerType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(RequestInfo), serviceContract }, null);
				var executeMethod = runnerType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
				var runnerInstance = il.DeclareLocal(runnerType);

				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, serviceField);
				il.Emit(OpCodes.Newobj, runnerCtor);
				il.Emit(OpCodes.Stloc, runnerInstance);
				il.Emit(OpCodes.Ldloc, runnerInstance);
				il.Emit(OpCodes.Callvirt, executeMethod);

				il.Emit(OpCodes.Br, methodEnd);
				il.MarkLabel(CaseEnd);
			}

			// Default: throw exception
			il.Emit(OpCodes.Ldstr, "Unknown method.");
			il.Emit(OpCodes.Newobj, exConstructor);
			il.Emit(OpCodes.Throw);

			il.MarkLabel(methodEnd);
			il.Emit(OpCodes.Ret);
		}

		private static int asyncRunnerCount = 0;

		internal static Type CreateAsyncRunnerClass(Type serviceContract, MethodInfo method)
		{
			var runnerid = Interlocked.Increment(ref asyncRunnerCount);
			int contractid = JenkinsHash.Compute(serviceContract.Name);
			string dynamicTypeName = $"{method.Name}Runner_{runnerid}";

			TypeBuilder typeBuilder = DynamicCodeGen.moduleBuilder.DefineType(
				dynamicTypeName,                                    // The name of the dynamic type
				TypeAttributes.Public | TypeAttributes.Class,       // Type attributes
				typeof(Object));                                    // The base type of the class

			var rqField = typeBuilder.DefineField("rq", typeof(RequestInfo), FieldAttributes.Private);
			var serviceField = typeBuilder.DefineField("service", serviceContract, FieldAttributes.Private);
			var taskField = typeBuilder.DefineField("task", method.ReturnType, FieldAttributes.Private);
			var constructor = CreateAsyncRunnerConstructor(typeBuilder, serviceContract, rqField, serviceField);
			var completeMethod = CreateAsyncRunnerCompleteMethod(typeBuilder, serviceContract, method, rqField, serviceField, taskField);
			var executeMethod = CreateAsyncRunnerExecuteMethod(typeBuilder, serviceContract, method, rqField, serviceField, taskField, completeMethod);

			var t = typeBuilder.CreateType();
			return t;
		}


		private static ConstructorBuilder CreateAsyncRunnerConstructor(TypeBuilder typeBuilder, Type serviceContract, FieldBuilder rqField, FieldBuilder serviceField)
		{
			ConstructorBuilder ctor = typeBuilder.DefineConstructor(
					MethodAttributes.Public,
					CallingConventions.Standard,
					new Type[] { typeof(RequestInfo), serviceContract });

			var il = ctor.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, ObjectConstructor);

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, rqField);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Stfld, serviceField);
			il.Emit(OpCodes.Ret);

			return ctor;
		}


		private static MethodBuilder CreateAsyncRunnerExecuteMethod(
			TypeBuilder typeBuilder,
			Type serviceContract,
			MethodInfo serviceMethod,
			FieldBuilder rqField,
			FieldBuilder serviceField,
			FieldBuilder taskField,
			MethodBuilder completeMethod)
		{
			MethodBuilder method = typeBuilder.DefineMethod("Execute",
				MethodAttributes.Public, typeof(Task), Type.EmptyTypes);

			ILGenerator il = method.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, serviceField);

			foreach (ParameterInfo paramInfo in serviceMethod.GetParameters())
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, rqField);
				il.Emit(OpCodes.Ldfld, RequestInfoInputField);
				DynamicCodeGen.GetValueFromBuffer(il, paramInfo.ParameterType);
			}
			il.Emit(OpCodes.Callvirt, serviceMethod);
			il.Emit(OpCodes.Stfld, taskField);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, taskField);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldftn, completeMethod);
			il.Emit(OpCodes.Newobj, GetActionConstructor());
			il.Emit(OpCodes.Ldc_I4, (int)TaskContinuationOptions.OnlyOnRanToCompletion);
			il.Emit(OpCodes.Callvirt, GetContinueWithAction(taskField.FieldType));
			il.Emit(OpCodes.Pop);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, taskField);
			il.Emit(OpCodes.Ret);

			return method;
		}


		private static MethodBuilder CreateAsyncRunnerCompleteMethod(
			TypeBuilder typeBuilder,
			Type serviceContract,
			MethodInfo serviceMethod,
			FieldBuilder rqField,
			FieldBuilder serviceField,
			FieldBuilder taskField)
		{
			MethodBuilder method = typeBuilder.DefineMethod("Complete",
				MethodAttributes.Private, typeof(void), new Type[] { typeof(Task) });

			ILGenerator il = method.GetILGenerator();
			var exLocal = il.DeclareLocal(typeof(Exception));

			if (serviceMethod.ReturnType.IsGenericType)
			{
				// For Task<T> methods, add the task result to the output buffer
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, rqField);
				il.Emit(OpCodes.Ldfld, RequestInfoOutputField);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, taskField);
				il.Emit(OpCodes.Callvirt, GetTaskResultMethod(serviceMethod.ReturnType));
				DynamicCodeGen.AddValueToBuffer(il, serviceMethod.ReturnType.GetGenericArguments()[0]);
			}
			else
			{
				// For Task methods, add 0 to the output buffer
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, rqField);
				il.Emit(OpCodes.Ldfld, RequestInfoOutputField);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Callvirt, AddInt32);
			}
			// Call EndMessage
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, rqField);
			il.Emit(OpCodes.Ldfld, RequestInfoOutputField);
			il.Emit(OpCodes.Callvirt, ProtocolBufferEndMessage);


			// Finish up by sending the message
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, rqField);
			il.Emit(OpCodes.Ldfld, RequestInfoSession);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, rqField);
			il.Emit(OpCodes.Callvirt, MsgSessionSendMessage);

			il.Emit(OpCodes.Ret);
			return method;
		}
	}
}
