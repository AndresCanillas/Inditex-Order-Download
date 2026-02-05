using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.IPC
{
	static partial class DynamicCodeGen
	{
		//================================================================================
		//                              ClientProxy Generator
		//
		// Creates an implementation of the given service that acts as a proxy, this proxy
		// can be used to send requests to a remote service.
		//
		// See Templates/ClientProxyExample.cs for an example of how the generated class
		// would look.
		//================================================================================

		internal static ClientProxy CreateClientProxy(IScope scope, Type serviceContract, int contractid)
		{
			Type t;

			// Create the proxy class
			lock (DynamicCodeGen.SyncRoot)
			{
				string proxyTypeName = serviceContract.Name + "_ClientProxy";
				t = DynamicCodeGen.moduleBuilder.GetType(proxyTypeName);
				if (t == null)
				{
					// Validate to see if the serviceContract can be implemented as a proxy to a remote service,
					// several validations are performed on each member of the interface to see if a proxy can be created.
					DynamicCodeGen.ValidateInterfaceType(serviceContract);

					TypeBuilder typeBuilder = DynamicCodeGen.moduleBuilder.DefineType(
						proxyTypeName,                                      // The name of the dynamic type
						TypeAttributes.Public | TypeAttributes.Class,       // Type attributes
						typeof(ClientProxy),                                // The base type of the class
						new Type[] { serviceContract });                    // Interfaces implemented by the new class

					ImplementClientProxyConstructor(typeBuilder, serviceContract);

					// Implement the interface events and wires all the necesary code to raise them when we receive an event from the remote service.
					ImplementClientProxyEvents(typeBuilder, serviceContract);

					// Implement the interface methods and wire all the necesary code to invoke them on the remote service.
					ImplementClientProxyMethods(typeBuilder, serviceContract);

					t = typeBuilder.CreateType();
				}
			}
			// The use of activator is unavoidable at this point, but this code is executed only once during the whole application life time.
			return (ClientProxy)Activator.CreateInstance(t, scope, contractid);
		}


		private static void ImplementClientProxyConstructor(TypeBuilder typeBuilder, Type serviceContract)
		{
			ConstructorBuilder ctor = typeBuilder.DefineConstructor(
					MethodAttributes.Public,
					CallingConventions.Standard,
					new Type[] { typeof(IScope), typeof(int) });

			var il = ctor.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, ClientProxyConstructor);
			il.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// This method takes care of implementing all the events defined in the service interface.
		/// </summary>
		/// <remarks>
		/// For each event we need to declare a private field that holds the actual delegate,
		/// and also declare a public event with its add/remove accessors.
		/// 
		/// We also need to override the OnReceiveEvent method in order to raise the events as necesary.
		/// </remarks>
		private static void ImplementClientProxyEvents(TypeBuilder typeBuilder, Type serviceContract)
		{
			// Implement the OnReceiveEvent override
			MethodBuilder onReceiveEventMethod = typeBuilder.DefineMethod("OnReceiveEvent",
				MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig,
				typeof(void), new Type[] { typeof(int), typeof(ProtocolBuffer) });

			ILGenerator il = onReceiveEventMethod.GetILGenerator();
			Label methodEnd = il.DefineLabel();

			il.Emit(OpCodes.Ldarg_1);                   // if: eventName == null
			il.Emit(OpCodes.Brfalse, methodEnd);        // then: jump to methodEnd

			EventInfo[] events = serviceContract.GetEventsEx();
			foreach (EventInfo e in events)
			{
				// Define the underlying delegate field, the event itself and the add/remove methods of each event.
				FieldBuilder eventField = typeBuilder.DefineField("_" + e.Name, e.EventHandlerType, FieldAttributes.Private);
				EventBuilder eventBuilder = typeBuilder.DefineEvent(e.Name, EventAttributes.None, e.EventHandlerType);
				MethodBuilder addMethod = CreateEventAddMethod(typeBuilder, e, eventField);
				MethodBuilder removeMethod = CreateEventRemoveMethod(typeBuilder, e, eventField);
				eventBuilder.SetAddOnMethod(addMethod);
				eventBuilder.SetRemoveOnMethod(removeMethod);

				// Also add code to raise the event in the OnReceiveEvent method
				AddEventProcessingCode(il, e, eventField, methodEnd);
			}
			il.MarkLabel(methodEnd);
			il.Emit(OpCodes.Ret);
		}


		private static MethodBuilder CreateEventAddMethod(TypeBuilder typeBuilder, EventInfo e, FieldBuilder eventField)
		{
			MethodInfo delegateCombine = typeof(Delegate).GetMethod("Combine", new Type[] { typeof(Delegate), typeof(Delegate) });

			MethodBuilder method = typeBuilder.DefineMethod("add_" + e.Name,
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
				CallingConventions.Standard | CallingConventions.HasThis,
				typeof(void), new Type[] { e.EventHandlerType });

			ILGenerator il = method.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, eventField);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Call, delegateCombine);
			il.Emit(OpCodes.Castclass, e.EventHandlerType);
			il.Emit(OpCodes.Stfld, eventField);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldc_I4, GetEventID(e));
			il.Emit(OpCodes.Call, RegisterEvent);
			il.Emit(OpCodes.Ret);
			return method;
		}


		private static MethodBuilder CreateEventRemoveMethod(TypeBuilder typeBuilder, EventInfo e, FieldBuilder eventField)
		{
			MethodInfo delegateRemove = typeof(Delegate).GetMethod("Remove", new Type[] { typeof(Delegate), typeof(Delegate) });

			MethodBuilder method = typeBuilder.DefineMethod("remove_" + e.Name,
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
				CallingConventions.Standard | CallingConventions.HasThis,
				typeof(void), new Type[] { e.EventHandlerType });

			ILGenerator il = method.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldc_I4, GetEventID(e));
			il.Emit(OpCodes.Call, UnregisterEvent);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, eventField);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Call, delegateRemove);
			il.Emit(OpCodes.Castclass, e.EventHandlerType);
			il.Emit(OpCodes.Stfld, eventField);
			il.Emit(OpCodes.Ret);
			return method;
		}


		private static int GetEventID(EventInfo e)
		{
			int id = GetMethodID(e.EventHandlerType.GetMethod("Invoke")) << 8;
			int hash = JenkinsHash.Compute(e.Name);
			return id + hash;
		}


		internal static int GetMethodID(MethodInfo e)
		{
			StringBuilder sb = new StringBuilder(50);
			sb.Append(e.Name);
			sb.Append("( ");
			foreach (ParameterInfo p in e.GetParameters())
				sb.Append(p.ParameterType.Name).Append(",");
			sb.Remove(sb.Length - 1, 1);
			sb.Append(")");
			return JenkinsHash.Compute(sb.ToString());
		}


		private static void AddEventProcessingCode(
			ILGenerator il,
			EventInfo eventInfo,
			FieldBuilder eventField,
			Label methodEnd)
		{
			Label eventEnd = il.DefineLabel();

			// Check to see if the event received is the one referenced by this eventInfo.
			il.Emit(OpCodes.Ldarg_1);                           // load arg1 into stack... arg1 is the ID of the received event
			il.Emit(OpCodes.Ldc_I4, GetEventID(eventInfo));     // load the event id of the eventInfo
			il.Emit(OpCodes.Bne_Un, eventEnd);                  // if: [receivedEventID] != [eventInfo.ID]
																// then: jump to the end of this event processing code

			// checks if there are any subscribers for this event
			il.Emit(OpCodes.Ldarg_0);                           // if: eventField == null
			il.Emit(OpCodes.Ldfld, eventField);                 // then: Jump to the end of the method
			il.Emit(OpCodes.Brfalse, methodEnd);

			// All is good and we can invoke the event...
			// First, extract the event arguments into local variables.
			MethodInfo delegateMethod = eventInfo.EventHandlerType.GetMethod("Invoke");
			ParameterInfo[] parameters = delegateMethod.GetParameters();
			LocalBuilder[] locals = new LocalBuilder[parameters.Length];
			int i = 0;
			foreach (ParameterInfo param in parameters)
			{
				if (param.Position == 0 && param.Name == "sender" && param.ParameterType == typeof(object))
					locals[i++] = DynamicCodeGen.ExtractThis(il);   // if the first argument of the delegate is of data type object and is called sender, then we supply the delegate with a reference to the service proxy.
				else
				{
					LocalBuilder local = il.DeclareLocal(param.ParameterType);
					locals[i++] = local;
					il.Emit(OpCodes.Ldarg_2);
					DynamicCodeGen.GetValueFromBuffer(il, param.ParameterType);
					il.Emit(OpCodes.Stloc, local);
				}
			}

			// Now perform a call to the event handlers.
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, eventField);
			foreach (LocalBuilder local in locals)
			{
				il.Emit(OpCodes.Ldloc, local);
			}
			il.Emit(OpCodes.Callvirt, delegateMethod);

			// Finally, jump to the end of the method since we are done processing the event.
			il.Emit(OpCodes.Br, methodEnd);

			// Don't forget to mark the place where this event ends.
			il.MarkLabel(eventEnd);
		}


		/// <summary>
		/// This method implements the code of the methods defined in the service interface.
		/// </summary>
		/// <remarks>
		/// In order to make the call to the remote method we have to add several pieces of information in the SendBuffer:
		///		- Add the invocation header (0x7AF5)
		///		- Add the length place holder
		///		- Add all the method parameters
		///		- Invoke SendMessage (from base class)
		///		- After SendMessage completes (and if no exception is thrown), that means that the call has completed.
		///		- Finally we need to extract the return value from the RecvBuffer (unless the method is void) and return it.
		/// </remarks>
		private static void ImplementClientProxyMethods(TypeBuilder typeBuilder, Type serviceContract)
		{
			MethodInfo[] methods = serviceContract.GetMethodsEx();
			foreach (MethodInfo method in methods)
			{
				if (method.IsSpecialName) continue;

				if (DynamicCodeGen.IsAsyncMethod(method))
					ImplementClientProxyAsyncMethod(typeBuilder, serviceContract, method);
				else
					ImplementClientProxyMethod(typeBuilder, method);
			}
		}


		private static void ImplementClientProxyMethod(TypeBuilder typeBuilder, MethodInfo method)
		{
			ParameterInfo[] parameters = method.GetParameters();

			int i = 0;
			Type[] parameterTypes = new Type[parameters.Length];
			foreach (ParameterInfo param in parameters)
				parameterTypes[i++] = param.ParameterType;

			MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name,
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
				CallingConventions.Standard | CallingConventions.HasThis,
				method.ReturnType, parameterTypes);

			ILGenerator il = methodBuilder.GetILGenerator();
			Label methodEnd = il.DefineLabel();
			LocalBuilder rq = il.DeclareLocal(typeof(RequestInfo));
			LocalBuilder retVal = null;

			// Create RequestInfo
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldc_I4, GetMethodID(method));
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Call, StartRequestMethod);
			il.Emit(OpCodes.Stloc, rq);

			il.BeginExceptionBlock();
			{
				foreach (ParameterInfo param in parameters)
				{
					il.Emit(OpCodes.Ldloc, rq);
					il.Emit(OpCodes.Ldfld, RequestInfoOutputField);
					il.Emit(OpCodes.Ldarg, param.Position + 1);
					DynamicCodeGen.AddValueToBuffer(il, param.ParameterType);
				}
				il.Emit(OpCodes.Ldloc, rq);
				il.Emit(OpCodes.Ldfld, RequestInfoOutputField);
				il.Emit(OpCodes.Callvirt, ProtocolBufferEndMessage);

				il.Emit(OpCodes.Ldloc, rq);
				il.Emit(OpCodes.Callvirt, RequestInfoSendRequest);

				// Check if we need to extract a return value from the receive buffer
				if (method.ReturnType != typeof(void))
				{
					retVal = il.DeclareLocal(method.ReturnType);
					il.Emit(OpCodes.Ldloc, rq);
					il.Emit(OpCodes.Ldfld, RequestInfoInputField);
					DynamicCodeGen.GetValueFromBuffer(il, method.ReturnType);
					il.Emit(OpCodes.Stloc, retVal);
				}
				il.Emit(OpCodes.Leave_S, methodEnd);
			}
			il.BeginFinallyBlock();
			{
				var endFinallyBlock = il.DefineLabel();
				il.Emit(OpCodes.Ldloc, rq);
				il.Emit(OpCodes.Brfalse, endFinallyBlock);
				il.Emit(OpCodes.Ldloc, rq);
				il.Emit(OpCodes.Callvirt, IDisposableDispose);
				il.MarkLabel(endFinallyBlock);
			}
			il.EndExceptionBlock();

			il.MarkLabel(methodEnd);

			if (retVal != null)
				il.Emit(OpCodes.Ldloc, retVal);

			il.Emit(OpCodes.Ret);
		}


		private static void ImplementClientProxyAsyncMethod(TypeBuilder typeBuilder, Type serviceContract, MethodInfo method)
		{
			ParameterInfo[] parameters = method.GetParameters();

			int i = 0;
			Type[] parameterTypes = new Type[parameters.Length];
			foreach (ParameterInfo param in parameters)
				parameterTypes[i++] = param.ParameterType;

			MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name,
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
				CallingConventions.Standard | CallingConventions.HasThis,
				method.ReturnType, parameterTypes);

			ILGenerator il = methodBuilder.GetILGenerator();

			LocalBuilder rq = il.DeclareLocal(typeof(RequestInfo));

			// Create RequestInfo
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldc_I4, GetMethodID(method));
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Call, StartRequestMethod);
			il.Emit(OpCodes.Stloc, rq);

			foreach (ParameterInfo param in parameters)
			{
				il.Emit(OpCodes.Ldloc, rq);
				il.Emit(OpCodes.Ldfld, RequestInfoOutputField);
				il.Emit(OpCodes.Ldarg, param.Position + 1);
				DynamicCodeGen.AddValueToBuffer(il, param.ParameterType);
			}

			il.Emit(OpCodes.Ldloc, rq);
			il.Emit(OpCodes.Ldfld, RequestInfoOutputField);
			il.Emit(OpCodes.Callvirt, ProtocolBufferEndMessage);

			il.Emit(OpCodes.Ldloc, rq);
			if(method.ReturnType.IsGenericType)
				il.Emit(OpCodes.Callvirt, GetSendTypedRequestAsyncMethod(method.ReturnType.GetGenericArguments()[0]));
			else
				il.Emit(OpCodes.Callvirt, SendVoidRequestAsync);

			il.Emit(OpCodes.Ret);
		}
	}
}
