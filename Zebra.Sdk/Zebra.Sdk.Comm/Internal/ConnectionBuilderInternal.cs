using System;
using System.Collections.Generic;
using System.Reflection;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Comm.Internal
{
	internal class ConnectionBuilderInternal
	{
		private readonly static string MULTICHANNEL_BLUETOOTH_CONNECTION;

		public static HashSet<object> implementingClasses;

		private static bool isConnBuilderVerbose;

		public static bool ConnBuilderVerbosity
		{
			get
			{
				return ConnectionBuilderInternal.isConnBuilderVerbose;
			}
			set
			{
				ConnectionBuilderInternal.isConnBuilderVerbose = value;
			}
		}

		static ConnectionBuilderInternal()
		{
			ConnectionBuilderInternal.MULTICHANNEL_BLUETOOTH_CONNECTION = "Zebra.Sdk.Comm.MultichannelBluetoothConnection";
			ConnectionBuilderInternal.implementingClasses = null;
			ConnectionBuilderInternal.isConnBuilderVerbose = false;
			try
			{
				ConnectionBuilderInternal.ReflectivelyLoadImplementingClasses();
			}
			catch
			{
			}
		}

		public ConnectionBuilderInternal()
		{
		}

		public static void AddConnectionType(object c)
		{
			if (ConnectionBuilderInternal.implementingClasses == null)
			{
				ConnectionBuilderInternal.implementingClasses = new HashSet<object>();
			}
			ConnectionBuilderInternal.implementingClasses.Add(c);
		}

		public static Connection Build(string descriptionString)
		{
			if (ConnectionBuilderInternal.implementingClasses == null)
			{
				throw new ArgumentException("Builder not correctly implemented");
			}
			VerbosePrinter verbosePrinter = new VerbosePrinter(ConnectionBuilderInternal.isConnBuilderVerbose);
			verbosePrinter.WriteLine(string.Concat("Building connection for the string \"", descriptionString, "\""));
			ConnectionInfo connectionInfo = new ConnectionInfo(descriptionString);
			List<Connection> connections = new List<Connection>();
			foreach (object implementingClass in ConnectionBuilderInternal.implementingClasses)
			{
				try
				{
					connections.Add(ConnectionBuilderInternal.ReflectivelyInstatiateConnection(connectionInfo, implementingClass));
				}
				catch (TargetInvocationException)
				{
				}
				catch (ArgumentNullException)
				{
					throw new ConnectionException(string.Concat(implementingClass.GetType().GetTypeInfo().FullName, " does not implement a constructor that takes a ConnectionInfo object"));
				}
				catch (Exception exception)
				{
					throw new ConnectionException(exception.Message);
				}
			}
			if (connections.Count == 0)
			{
				verbosePrinter.WriteLine(string.Concat("Could not determine connection type of the value \"", descriptionString, "\""));
				throw new ConnectionException("Invalid connection type");
			}
			if (connections.Count == 1)
			{
				verbosePrinter.WriteLine(string.Concat("Determined connection string \"", descriptionString, "\" is of type ", connections[0].GetType().GetTypeInfo().Name));
				return connections[0];
			}
			Connection @try = ConnectionBuilderInternal.DetermineConnectionToTry(connections, verbosePrinter);
			if (@try == null)
			{
				throw new ConnectionException(string.Concat("Could not open connection string \"", descriptionString, "\""));
			}
			verbosePrinter.WriteLine(string.Concat("Success!", Environment.NewLine));
			return @try;
		}

		private static string CreateClassesToTestMessage(List<Connection> possibleConnections)
		{
			string str = string.Concat("The following are possible connection types:", Environment.NewLine);
			foreach (Connection possibleConnection in possibleConnections)
			{
				string name = possibleConnection.GetType().GetTypeInfo().Name;
				str = string.Concat(str, "   ", name, Environment.NewLine);
			}
			return str;
		}

		private static Connection DetermineConnectionToTry(List<Connection> possibleConnections, VerbosePrinter sysOutPrinter)
		{
			sysOutPrinter.WriteLine(ConnectionBuilderInternal.CreateClassesToTestMessage(possibleConnections));
			Connection connection = null;
			foreach (Connection possibleConnection in possibleConnections)
			{
				try
				{
					sysOutPrinter.Write(string.Concat("Trying ", possibleConnection.GetType().GetTypeInfo().Name, "... "));
					possibleConnection.Open();
					possibleConnection.Close();
					if (possibleConnection.GetType().GetTypeInfo().FullName.Equals(ConnectionBuilderInternal.MULTICHANNEL_BLUETOOTH_CONNECTION))
					{
						Sleeper.Sleep((long)1000);
					}
					connection = possibleConnection;
					return connection;
				}
				catch (ConnectionException connectionException)
				{
					sysOutPrinter.WriteLine(string.Concat("Failed - ", connectionException.Message));
				}
			}
			return connection;
		}

		private static Connection ReflectivelyInstatiateConnection(ConnectionInfo connectionInfo, object thisClass)
		{
			ConstructorInfo constructorInfo = null;
			ConstructorInfo[] constructors = ((TypeInfo)thisClass).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (constructors.Length != 0)
			{
				ConstructorInfo[] constructorInfoArray = constructors;
				for (int i = 0; i < (int)constructorInfoArray.Length; i++)
				{
					ConstructorInfo constructorInfo1 = constructorInfoArray[i];
					foreach (ParameterInfo parameterInfo in new List<ParameterInfo>(constructorInfo1.GetParameters()))
					{
						if (parameterInfo.Name != "connectionInfo")
						{
							continue;
						}
						constructorInfo = constructorInfo1;
						break;
					}
				}
			}
			if (constructorInfo == null)
			{
				throw new ArgumentNullException();
			}
			return (Connection)constructorInfo.Invoke(new object[] { connectionInfo });
		}

		private static void ReflectivelyLoadImplementingClasses()
		{
			try
			{
				Assembly.GetEntryAssembly().GetReferencedAssemblies();
				Assembly.Load(new AssemblyName("SdkApi_Desktop")).GetType("Zebra.Sdk.Comm.ConnectionBuilder", true, true).GetTypeInfo().GetMethod("InitializeClasses", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
			}
			catch (Exception)
			{
			}
		}

		public static void RemoveConnectionType(object c)
		{
			ConnectionBuilderInternal.implementingClasses.Remove(c);
		}
	}
}