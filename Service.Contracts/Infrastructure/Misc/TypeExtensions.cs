using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Service.Contracts
{
	public static class TypeExtensions
	{
		public static bool Implements(this Type t, Type interfaceType)
		{
			if (interfaceType.IsGenericType)
			{
				return t.GetInterfaces().Any(
					i => i.IsGenericType &&
					i.GetGenericTypeDefinition() == interfaceType);
			}
			else return interfaceType.IsAssignableFrom(t);
		}

		public static Type FindType(string typeName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var type = assembly.GetType(typeName);
				if (type != null)
					return type;
			}
			throw new TypeLoadException($"Could not locate type {typeName}.");
		}

		public static Type GetListType(this Type t)
		{
			var listInterfaceType = t.GetInterfaces().FirstOrDefault(
				i => i.IsGenericType && (
					i.GetGenericTypeDefinition() == typeof(IList<>) ||
					i.GetGenericTypeDefinition() == typeof(ICollection<>)
				));
			if (listInterfaceType != null)
			{
				Type[] types = listInterfaceType.GetGenericArguments();
				return types[0];
			}
			else throw new InvalidOperationException($"Type {t.Name} is not IList<T> or ICollection<T>");
		}

		public static bool HasEmptyConstructor(this Type t)
		{
			ConstructorInfo ctor = t.GetConstructor(Type.EmptyTypes);
			return ctor.IsPublic && ctor != null;
		}

		public static string GetFriendlyName(this Type t)
		{
			string name = t.Name;
			var attrs = t.GetCustomAttributes(typeof(FriendlyName), true);
			if (attrs.Length > 0)
				name = (attrs[0] as FriendlyName).Text;
			return name;
		}

		public static string GetCaption(this Type t)
		{
			string name = t.Name;
			var attrs = t.GetCustomAttributes(typeof(Caption), true);
			if (attrs.Length > 0)
				name = (attrs[0] as Caption).Text;
			return name;
		}

		public static string GetDescription(this Type t)
		{
			string description = "N/A";
			var attrs = t.GetCustomAttributes(typeof(Description), true);
			if (attrs.Length > 0)
				description = (attrs[0] as Description).Text;
			return description;
		}

		public static string GetFriendlyName(this FieldInfo fi)
		{
			string name = fi.Name;
			var attrs = fi.GetCustomAttributes(typeof(FriendlyName), true);
			if (attrs.Length > 0)
				name = (attrs[0] as FriendlyName).Text;
			return name;
		}

		public static string GetCaption(this FieldInfo fi)
		{
			string caption = fi.Name;
			var attrs = fi.GetCustomAttributes(typeof(Caption), true);
			if (attrs.Length > 0)
				caption = (attrs[0] as Caption).Text;
			else
				caption = MakeCaption(fi.Name);
			return caption;
		}

		public static string GetCaption(this PropertyInfo pi)
		{
			string caption = pi.Name;
			var attrs = pi.GetCustomAttributes(typeof(Caption), true);
			if (attrs.Length > 0)
				caption = (attrs[0] as Caption).Text;
			else
				caption = MakeCaption(pi.Name);
			return caption;
		}

		private static string MakeCaption(string text, string suffix = "")
		{
			StringBuilder sb = new StringBuilder(100);
			string caption = text;
			foreach (char c in caption)
			{
				if (Char.IsUpper(c) && sb.Length > 0)
				{
					if (!Char.IsUpper(sb[sb.Length - 1]))
						sb.Append(' ');
				}
				sb.Append(c);
			}
			if(!string.IsNullOrEmpty(suffix))
				sb.Append(suffix);
			return sb.ToString();
		}


		public static string GetDescription(this FieldInfo fi)
		{
			string description = null;
			var attrs = fi.GetCustomAttributes(typeof(Description), true);
			if (attrs.Length > 0)
				description = (attrs[0] as Description).Text;
			return description;
		}

		public static string GetFriendlyName(this PropertyInfo pi)
		{
			string name = pi.Name;
			var attrs = pi.GetCustomAttributes(typeof(FriendlyName), true);
			if (attrs.Length > 0)
				name = (attrs[0] as FriendlyName).Text;
			return name;
		}

		public static string GetDescription(this PropertyInfo pi)
		{
			string description = "N/A";
			var attrs = pi.GetCustomAttributes(typeof(Description), true);
			if (attrs.Length > 0)
				description = (attrs[0] as Description).Text;
			return description;
		}

		public static Type GetConfigurationType(this Type t)
		{
			var configInterfaceType = t.GetInterfaces().FirstOrDefault(
				i => i.IsGenericType);
			if (configInterfaceType != null)
			{
				Type[] types = configInterfaceType.GetGenericArguments();
				return types[0];
			}
			else throw new InvalidOperationException($"Type {t.Name} is not IConfigurable<T>");
		}
	}
}
