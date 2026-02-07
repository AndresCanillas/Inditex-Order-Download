using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Graphics;

namespace Zebra.Sdk.Util.Internal
{
	internal class ReflectionUtil
	{
		public ReflectionUtil()
		{
		}

		private static Type[] GetClasses(string namespaceName)
		{
			List<Type> types = new List<Type>();
			try
			{
				List<Type> list = (
					from t in Assembly.Load(new AssemblyName("SdkApi_Desktop")).GetTypes()
					where string.Equals(t.Namespace, namespaceName, StringComparison.Ordinal)
					select t).ToList<Type>();
				if (list.Any<Type>())
				{
					types.AddRange(list);
				}
			}
			catch (ReflectionTypeLoadException reflectionTypeLoadException1)
			{
				ReflectionTypeLoadException reflectionTypeLoadException = reflectionTypeLoadException1;
				if (reflectionTypeLoadException.Types == null)
				{
					throw new IOException(reflectionTypeLoadException.Message, reflectionTypeLoadException);
				}
				types.AddRange(
					from t in (IEnumerable<Type>)reflectionTypeLoadException.Types
					where t != null
					select t);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (exception.InnerException == null)
				{
					throw new IOException(exception.Message, exception);
				}
				throw new IOException(exception.InnerException.Message, exception.InnerException);
			}
			return types.ToArray();
		}

		private static HashSet<Type> GetClassesInNamespace(string namespaceName)
		{
			return new HashSet<Type>(ReflectionUtil.GetClasses(namespaceName).ToList<Type>());
		}

		public static HashSet<Type> GetClassesInNamespaceExtending(string namespaceName, Type baseClass)
		{
			HashSet<Type> classesInNamespace = ReflectionUtil.GetClassesInNamespace(namespaceName);
			HashSet<Type> types = new HashSet<Type>(classesInNamespace);
			IEnumerator<Type> enumerator = classesInNamespace.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Type current = enumerator.Current;
				if (!current.GetTypeInfo().IsAbstract && baseClass.GetTypeInfo().IsAssignableFrom(current))
				{
					continue;
				}
				types.Remove(current);
			}
			return types;
		}

		internal static Dictionary<string, string> InvokeUsbConnection_GetConnectionAttributes(Connection connection)
		{
			Dictionary<string, string> value;
			try
			{
				value = (Dictionary<string, string>)Assembly.Load(new AssemblyName("SdkApi_Desktop")).GetType("Zebra.Sdk.Comm.UsbConnection", true, true).GetTypeInfo().GetDeclaredProperty("ConnectionAttributes").GetValue(connection);
			}
			catch (TypeLoadException)
			{
				return null;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (exception.InnerException == null)
				{
					throw new IOException(exception.Message, exception);
				}
				throw new IOException(exception.InnerException.Message, exception.InnerException);
			}
			return value;
		}

		public static ZebraImageI InvokeZebraImageFactory_GetImage(string imagePath)
		{
			ZebraImageI zebraImageI;
			try
			{
				zebraImageI = (ZebraImageI)Assembly.Load(new AssemblyName("SdkApi_Desktop")).GetType("Zebra.Sdk.Graphics.ZebraImageFactory", true, true).GetTypeInfo().GetMethod("GetImage", new Type[] { typeof(string) }).Invoke(null, new object[] { imagePath });
			}
			catch (TypeLoadException)
			{
				return null;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (exception.InnerException == null)
				{
					throw new IOException(exception.Message, exception);
				}
				throw new IOException(exception.InnerException.Message, exception.InnerException);
			}
			return zebraImageI;
		}

		public static ZebraImageI InvokeZebraImageFactory_GetImage(Stream stream)
		{
			ZebraImageI zebraImageI;
			try
			{
				zebraImageI = (ZebraImageI)Assembly.Load(new AssemblyName("SdkApi_Desktop")).GetType("Zebra.Sdk.Graphics.ZebraImageFactory", true, true).GetTypeInfo().GetMethod("GetImage", new Type[] { typeof(Stream) }).Invoke(null, new object[] { stream });
			}
			catch (TypeLoadException)
			{
				return null;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (exception.InnerException == null)
				{
					throw new IOException(exception.Message, exception);
				}
				throw new IOException(exception.InnerException.Message, exception.InnerException);
			}
			return zebraImageI;
		}

		public static bool IsDriverConnection(Connection connection)
		{
			bool flag = false;
			try
			{
				flag = Assembly.Load(new AssemblyName("SdkApi_Desktop")).GetType("Zebra.Sdk.Comm.DriverPrinterConnection", true, true).IsInstanceOfType(connection);
			}
			catch
			{
			}
			return flag;
		}

		public static bool IsUsbDirectConnection(Connection connection)
		{
			bool flag = false;
			try
			{
				flag = Assembly.Load(new AssemblyName("SdkApi_Desktop")).GetType("Zebra.Sdk.Comm.UsbConnection", true, true).IsInstanceOfType(connection);
			}
			catch
			{
			}
			return flag;
		}

		internal static ConnectionReestablisher LoadTcpCardConnectionReestablisher(Connection connection, int thresholdTime)
		{
			ConnectionReestablisher connectionReestablisher;
			try
			{
				connectionReestablisher = (ConnectionReestablisher)Assembly.Load(new AssemblyName("SdkApi_Card_Core")).GetType("Zebra.Sdk.Card.Comm.Internal.TcpCardConnectionReestablisher", true, true).GetTypeInfo().GetConstructor(new Type[] { typeof(Connection), typeof(int) }).Invoke(new object[] { connection, thresholdTime });
			}
			catch (TypeLoadException)
			{
				return null;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (exception.InnerException == null)
				{
					throw new IOException(exception.Message, exception);
				}
				throw new IOException(exception.InnerException.Message, exception.InnerException);
			}
			return connectionReestablisher;
		}

		internal static ConnectionReestablisher LoadUsbCardConnectionReestablisher(Connection connection, int thresholdTime)
		{
			ConnectionReestablisher connectionReestablisher;
			try
			{
				connectionReestablisher = (ConnectionReestablisher)Assembly.Load(new AssemblyName("SdkApi_Card_Desktop")).GetType("Zebra.Sdk.Card.Comm.Internal.UsbCardConnectionReestablisher", true, true).GetTypeInfo().GetConstructor(new Type[] { typeof(Connection), typeof(int) }).Invoke(new object[] { connection, thresholdTime });
			}
			catch (TypeLoadException)
			{
				return null;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (exception.InnerException == null)
				{
					throw new IOException(exception.Message, exception);
				}
				throw new IOException(exception.InnerException.Message, exception.InnerException);
			}
			return connectionReestablisher;
		}
	}
}