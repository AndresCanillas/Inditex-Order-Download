using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Service.Contracts
{
	public static class Reflex
	{
		public static string DumpObject(object o)
		{
			Type t;
			FieldInfo fi;
			PropertyInfo pi;
			StringBuilder sb = new StringBuilder(1000);
			t = o.GetType();
			MemberInfo[] members = t.GetMembers(BindingFlags.Public | BindingFlags.Instance);
			foreach (MemberInfo m in members)
			{
				switch (m.MemberType)
				{
					case MemberTypes.Field:
						fi = t.GetField(m.Name);
						sb.AppendLine(String.Format("{0}: {1}", m.Name, fi.GetValue(o)));
						break;
					case MemberTypes.Property:
						pi = t.GetProperty(m.Name);
						if (pi.IsSpecialName)
							sb.AppendLine("Indexer");
						else
							sb.AppendLine(String.Format("{0}: {1}", m.Name, pi.GetValue(0, null)));
						break;
				}
			}
			return sb.ToString();
		}


		/// <summary>
		/// Gets the value of a property.
		/// </summary>
		/// <param name="target">The object from which to read the property.</param>
		/// <param name="property">The name of the property to read.</param>
		/// <returns>Returns the value of the property.</returns>
		public static object GetProperty(object target, string property)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (pInfo != null)
			{
				return pInfo.GetValue(target, null);
			}
			return null;
		}

		/// <summary>
		/// Gets the value of a property that accepts arguments.
		/// </summary>
		/// <param name="target">The object from which to read the property.</param>
		/// <param name="property">The name of the property to read.</param>
		/// <param name="args">The arguments of the property.</param>
		/// <returns>Returns the value of the property.</returns>
		public static object GetProperty(object target, string property, params object[] args)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (pInfo != null)
			{
				return pInfo.GetValue(target, args);
			}
			return null;
		}


		/// <summary>
		/// Gets the value of a field.
		/// </summary>
		/// <param name="target">The object from which to read the field.</param>
		/// <param name="property">The name of the field to read.</param>
		/// <returns>Returns the value of the field.</returns>
		public static object GetField(object target, string field)
		{
			FieldInfo fInfo = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (fInfo != null)
			{
				return fInfo.GetValue(target);
			}
			return null;
		}


		/// <summary>
		/// Attempts to get the value of a field.
		/// </summary>
		/// <param name="target">The object from which to read the field.</param>
		/// <param name="field">The name of the field or property to read.</param>
		/// <returns>Returns the value of the field.</returns>
		public static bool TryGetFieldOrProperty(object target, string field, out object value)
		{
			Type t = target.GetType();
			PropertyInfo pInfo = t.GetProperty(field);
			if (pInfo != null)
			{
				value = pInfo.GetValue(target);
				return true;
			}
			FieldInfo fInfo = t.GetField(field);
			if (fInfo != null)
			{
				value = fInfo.GetValue(target);
				return true;
			}
			value = null;
			return false;
		}

		/// <summary>
		/// Gets the value of a property. Use if you know that the property is of type string.
		/// </summary>
		/// <param name="target">The object from which to read the property.</param>
		/// <param name="property">The name of the property to read.</param>
		/// <returns>Returns the value of the property.</returns>
		public static string GetPropertyAsString(object target, string pName)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				return pInfo.GetValue(target, null).ToString();
			}
			else
			{
				return String.Empty;
			}
		}

		/// <summary>
		/// Gets the value of a property. Use if you know that the property is of type long.
		/// </summary>
		/// <param name="target">The object from which to read the property.</param>
		/// <param name="property">The name of the property to read.</param>
		/// <returns>Returns the value of the property.</returns>
		public static long GetPropertyAsLong(object target, string pName)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				return Convert.ToInt64(pInfo.GetValue(target, null));
			}
			else
			{
				return 0L;
			}
		}

		/// <summary>
		/// Gets the value of a property. Use if you know that the property is of type int.
		/// </summary>
		/// <param name="target">The object from which to read the property.</param>
		/// <param name="property">The name of the property to read.</param>
		/// <returns>Returns the value of the property.</returns>
		public static int GetPropertyAsInt(object target, string pName)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				return Convert.ToInt32(pInfo.GetValue(target, null));
			}
			else
			{
				return 0;
			}
		}

		/// <summary>
		/// Gets the value of a property. Use if you know that the property is of type DateTime
		/// </summary>
		/// <param name="target">The object from which to read the property.</param>
		/// <param name="property">The name of the property to read.</param>
		/// <returns>Returns the value of the property.</returns>
		public static DateTime GetPropertyAsDateTime(object target, string pName)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				return (DateTime)pInfo.GetValue(target, null);
			}
			else
			{
				return DateTime.MinValue;
			}
		}

		/// <summary>
		/// Gets the value of a property. Use if you know that the property is of type bool.
		/// </summary>
		/// <param name="target">The object from which to read the property.</param>
		/// <param name="property">The name of the property to read.</param>
		/// <returns>Returns the value of the property.</returns>
		public static bool GetPropertyAsBool(object target, string pName)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				return (bool)pInfo.GetValue(target, null);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Gets the value of a field. Use if you know that the type of the field is string.
		/// </summary>
		/// <param name="target">The object from which to read the field.</param>
		/// <param name="property">The name of the field to read.</param>
		/// <returns>Returns the value of the field.</returns>
		public static string GetFieldAsString(object target, string fieldName)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo != null)
			{
				return fInfo.GetValue(target).ToString();
			}
			else
			{
				return String.Empty;
			}
		}

		/// <summary>
		/// Gets the value of a field. Use if you know that the type of the field is long.
		/// </summary>
		/// <param name="target">The object from which to read the field.</param>
		/// <param name="property">The name of the field to read.</param>
		/// <returns>Returns the value of the field.</returns>
		public static long GetFieldAsLong(object target, string fieldName)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo != null)
			{
				return Convert.ToInt64(fInfo.GetValue(target));
			}
			else
			{
				return 0L;
			}
		}

		/// <summary>
		/// Gets the value of a field. Use if you know that the type of the field is int.
		/// </summary>
		/// <param name="target">The object from which to read the field.</param>
		/// <param name="property">The name of the field to read.</param>
		/// <returns>Returns the value of the field.</returns>
		public static int GetFieldAsInt(object target, string fieldName)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo != null)
			{
				return Convert.ToInt32(fInfo.GetValue(target));
			}
			else
			{
				return 0;
			}
		}

		/// <summary>
		/// Gets the value of a field. Use if you know that the type of the field is DateTime.
		/// </summary>
		/// <param name="target">The object from which to read the field.</param>
		/// <param name="property">The name of the field to read.</param>
		/// <returns>Returns the value of the field.</returns>
		public static DateTime GetFieldAsDateTime(object target, string fieldName)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo != null)
			{
				return (DateTime)fInfo.GetValue(target);
			}
			else
			{
				return DateTime.MinValue;
			}
		}

		/// <summary>
		/// Gets the value of a field. Use if you know that the type of the field is bool.
		/// </summary>
		/// <param name="target">The object from which to read the field.</param>
		/// <param name="property">The name of the field to read.</param>
		/// <returns>Returns the value of the field.</returns>
		public static bool GetFieldAsBool(object target, string fieldName)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo != null)
			{
				return (bool)fInfo.GetValue(target);
			}
			else
			{
				return false;
			}
		}


		/// <summary>
		/// Sets the specified property in the target object. If the property does not exists the call will do nothing.
		/// </summary>
		/// <param name="target">The object whose property we want to set.</param>
		/// <param name="pName">The name of the property to set</param>
		/// <param name="value">The value of the property. Note: The type of the value and the type of the property must be the same.</param>
		public static void SetProperty(object target, string pName, object value)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (pInfo != null)
			{
				pInfo.SetValue(target, value, null);
			}
		}

		/// <summary>
		/// Sets the value of the specified property. Use if you know that the property is of type string.
		/// </summary>
		/// <param name="target">The object on which the property is going to be set.</param>
		/// <param name="pName">The name of the property to set.</param>
		/// <param name="value">The value of the property.</param>
		public static void SetPropertyAsString(object target, string pName, string value)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				pInfo.SetValue(target, value, null);
			}
		}

		/// <summary>
		/// Sets the value of the specified property. Use if you know that the property is of type long.
		/// </summary>
		/// <param name="target">The object on which the property is going to be set.</param>
		/// <param name="pName">The name of the property to set.</param>
		/// <param name="value">The value of the property.</param>
		public static void SetPropertyAsLong(object target, string pName, long value)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				pInfo.SetValue(target, value, null);
			}
		}

		/// <summary>
		/// Sets the value of the specified property. Use if you know that the property is of type int.
		/// </summary>
		/// <param name="target">The object on which the property is going to be set.</param>
		/// <param name="pName">The name of the property to set.</param>
		/// <param name="value">The value of the property.</param>
		public static void SetPropertyAsInt(object target, string pName, int value)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				pInfo.SetValue(target, value, null);
			}
		}

		/// <summary>
		/// Sets the value of the specified property. Use if you know that the property is of type DateTime.
		/// </summary>
		/// <param name="target">The object on which the property is going to be set.</param>
		/// <param name="pName">The name of the property to set.</param>
		/// <param name="value">The value of the property.</param>
		public static void SetPropertyAsDateTime(object target, string pName, DateTime value)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				pInfo.SetValue(target, value, null);
			}
		}

		/// <summary>
		/// Sets the value of the specified property. Use if you know that the property is of type bool.
		/// </summary>
		/// <param name="target">The object on which the property is going to be set.</param>
		/// <param name="pName">The name of the property to set.</param>
		/// <param name="value">The value of the property.</param>
		public static void SetPropertyAsBool(object target, string pName, bool value)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				pInfo.SetValue(target, value, null);
			}
		}

		/// <summary>
		/// Sets the value of the specified property. Use if you know that the property is an enumeration.
		/// </summary>
		/// <param name="target">The object on which the property is going to be set.</param>
		/// <param name="pName">The name of the property to set.</param>
		/// <param name="value">The value of the property.</param>
		public static void SetPropertyAsEnum(object target, string pName, string value)
		{
			PropertyInfo pInfo = target.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			if (pInfo != null)
			{
				pInfo.SetValue(target, Enum.Parse(pInfo.PropertyType, value), null);
			}
		}


		/// <summary>
		/// Sets the value of the specified field. Use if you know that the property is of type string.
		/// </summary>
		/// <param name="target">The object on which the field is going to be set.</param>
		/// <param name="pName">The name of the field to set.</param>
		/// <param name="value">The value of the field.</param>
		public static void SetFieldAsString(object target, string fieldName, string value)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo != null)
			{
				fInfo.SetValue(target, value);
			}
		}

		/// <summary>
		/// Sets the value of the specified field. Use if you know that the property is of type long.
		/// </summary>
		/// <param name="target">The object on which the field is going to be set.</param>
		/// <param name="pName">The name of the field to set.</param>
		/// <param name="value">The value of the field.</param>
		public static void SetFieldAsLong(object target, string fieldName, long value)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo != null)
			{
				fInfo.SetValue(target, value);
			}
		}

		/// <summary>
		/// Sets the value of the specified field. Use if you know that the property is of type int.
		/// </summary>
		/// <param name="target">The object on which the field is going to be set.</param>
		/// <param name="pName">The name of the field to set.</param>
		/// <param name="value">The value of the field.</param>
		public static void SetFieldAsInt(object target, string fieldName, int value)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo != null)
			{
				fInfo.SetValue(target, value);
			}
		}

		/// <summary>
		/// Sets the value of the specified field. Use if you know that the property is of type DateTime.
		/// </summary>
		/// <param name="target">The object on which the field is going to be set.</param>
		/// <param name="pName">The name of the field to set.</param>
		/// <param name="value">The value of the field.</param>
		public static void SetFieldAsDateTime(object target, string fieldName, DateTime value)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo != null)
			{
				fInfo.SetValue(target, value);
			}
		}

		/// <summary>
		/// Sets the value of the specified field. Use if you know that the property is of type bool.
		/// </summary>
		/// <param name="target">The object on which the field is going to be set.</param>
		/// <param name="pName">The name of the field to set.</param>
		/// <param name="value">The value of the field.</param>
		public static void SetFieldAsBool(object target, string fieldName, bool value)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo != null)
			{
				fInfo.SetValue(target, value);
			}
		}


		/// <summary>
		/// Sets the value of the specified field.
		/// </summary>
		/// <param name="target">The object on which the field is going to be set.</param>
		/// <param name="pName">The name of the field to set.</param>
		/// <param name="value">The value of the field.</param>
		public static void SetField(object target, string fieldName, object value)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (fInfo != null)
			{
				if (Nullable.GetUnderlyingType(fInfo.FieldType) != null)
				{
					var newTarget = fInfo.GetValue(target);
					PropertyInfo pInfo = fInfo.FieldType.GetProperty("Value");
					pInfo.SetValue(target, value);
				}
				else
				{
					fInfo.SetValue(target, value);
				}
			}
		}


		/// <summary>
		/// Tests to see if the specified field is null. NOTE: Always returns false for value types.
		/// </summary>
		/// <param name="target">The object on which the field is going to be tested.</param>
		/// <param name="pName">The name of the field to test.</param>
		/// <returns>Returns true if the field is null, false otherwise.</returns>
		public static bool FieldIsNull(object target, string fieldName)
		{
			FieldInfo fInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			if (fInfo.FieldType.IsClass)
			{
				object value = fInfo.GetValue(target);
				return (value == null);
			}
			else return false;
		}


		public static bool HasMethod(object target, string method)
		{
			MethodInfo mInfo = target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance);
			return mInfo != null;
		}


		/// <summary>
		/// Invokes a method of the specified object.
		/// </summary>
		/// <param name="t">Type where the static method is defined.</param>
		/// <param name="method">The name of the method to call.</param>
		/// <param name="arguments">The arguments for the method (if any).</param>
		/// <returns>If the method has a return value, returns the result of the method, null otherwise.</returns>
		public static object InvokeStatic(Type t, string method, params object[] arguments)
		{
			List<Type> types = new List<Type>();
			if (arguments != null)
			{
				foreach (var obj in arguments)
					types.Add(obj.GetType());
			}
			MemberInfo[] mInfo = t.GetMember(method);
			try
			{
				if (mInfo != null && mInfo.Length > 0)
				{
					return (mInfo[0] as MethodInfo).Invoke(null, arguments);
				}
				else
				{
					return null;
				}
			}
			catch (TargetInvocationException ex)
			{
				Exception actualException = ex as Exception;
				while (actualException.InnerException != null)
					actualException = actualException.InnerException;
				throw actualException;
			}
		}



		/// <summary>
		/// Invokes a method of the specified object.
		/// </summary>
		/// <param name="target">The object on which the method will be called.</param>
		/// <param name="method">The name of the method to call.</param>
		/// <param name="arguments">The arguments for the method (if any).</param>
		/// <returns>If the method has a return value, returns the result of the method, null otherwise.</returns>
		public static object Invoke(object target, string method, params object[] arguments)
		{
			List<Type> types = new List<Type>();
			if (arguments != null)
			{
				foreach (var obj in arguments)
					types.Add(obj.GetType());
			}
			MethodInfo mInfo = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, types.ToArray(), null);
			
			if (mInfo != null)
			{
				return mInfo.Invoke(target, arguments);
			}
			else
			{
				return null;
			}
			
		}


		/// <summary>
		/// Invokes a method from the specified object, using the one that matches the specified signature arguments.
		/// </summary>
		/// <param name="target">The object on which the method will be called.</param>
		/// <param name="method">The name of the method to call.</param>
		/// <param name="returnType">The return type expected from the method</param>
		/// <param name="argumentTypes">The types of the arguments that the method receives</param>
		/// <param name="arguments">The arguments for the method (if any).</param>
		/// <returns>If the method has a return value, returns the result of the method, null otherwise.</returns>
		public static object InvokeSig(object target, string method, Type returnType, Type[] argumentTypes, params object[] arguments)
		{
			MethodInfo mInfo = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public, null, argumentTypes, null);
			if (mInfo == null || mInfo.ReturnType != returnType)
				throw new Exception("The specified method signature was not found.");
			try
			{
				return mInfo.Invoke(target, arguments);
			}
			catch (TargetInvocationException ex)
			{
				Exception actualException = ex as Exception;
				while (actualException.InnerException != null)
					actualException = actualException.InnerException;
				throw actualException;
			}
		}


		public static T Copy<T>(T dest, object src)
		{
			Type srcType = src.GetType();
			FieldInfo[] fields = dest.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (FieldInfo f in fields)
			{
				FieldInfo srcField = srcType.GetField(f.Name, BindingFlags.Public | BindingFlags.Instance);
				if (srcField != null)
					SetField(dest, f.Name, srcField.GetValue(src));
				else
				{
					PropertyInfo srcProp = srcType.GetProperty(f.Name, BindingFlags.Public | BindingFlags.Instance);
					if (srcProp != null)
						SetField(dest, f.Name, srcProp.GetValue(src, null));
				}
			}
			PropertyInfo[] properties = dest.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (PropertyInfo p in properties)
			{
				ParameterInfo[] parameters = p.GetIndexParameters();
				if (p.CanWrite && p.CanRead && !p.IsSpecialName && parameters.Length == 0)
				{
					PropertyInfo srcProp = srcType.GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance);
					if (srcProp != null)
						SetProperty(dest, p.Name, srcProp.GetValue(src, null));
					else
					{
						FieldInfo srcField = srcType.GetField(p.Name, BindingFlags.Public | BindingFlags.Instance);
						if (srcField != null)
							SetProperty(dest, p.Name, srcField.GetValue(src));
					}
				}
			}
			return dest;
		}


		public static T Copy<T>(T dest, object src, string[] ignoreList)
		{
			Type srcType = src.GetType();

			var fields = dest.GetType()
				.GetFields(BindingFlags.Public | BindingFlags.Instance)
				.Where(f => ignoreList.FirstOrDefault(p => p == f.Name) == null)
				.ToList();

			foreach (FieldInfo f in fields)
			{
				FieldInfo srcField = srcType.GetField(f.Name, BindingFlags.Public | BindingFlags.Instance);
				if (srcField != null)
					SetField(dest, f.Name, srcField.GetValue(src));
				else
				{
					PropertyInfo srcProp = srcType.GetProperty(f.Name, BindingFlags.Public | BindingFlags.Instance);
					if (srcProp != null)
						SetField(dest, f.Name, srcProp.GetValue(src, null));
				}
			}

			var properties = dest.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(f => ignoreList.FirstOrDefault(p => p == f.Name) == null)
				.ToList();

			foreach (PropertyInfo p in properties)
			{
				ParameterInfo[] parameters = p.GetIndexParameters();
				if (p.CanWrite && p.CanRead && !p.IsSpecialName && parameters.Length == 0)
				{
					PropertyInfo srcProp = srcType.GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance);
					if (srcProp != null)
						SetProperty(dest, p.Name, srcProp.GetValue(src, null));
					else
					{
						FieldInfo srcField = srcType.GetField(p.Name, BindingFlags.Public | BindingFlags.Instance);
						if (srcField != null)
							SetProperty(dest, p.Name, srcField.GetValue(src));
					}
				}
			}

			return dest;
		}


		public static bool OverridesMethod(Type t, string methodName)
		{
			MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (methods == null || methods.Length == 0)
			{
				return false;
			}
			else
			{
				foreach (MethodInfo mi in methods)
				{
					if (string.Compare(methodName, mi.Name, true) == 0)
					{
						if (mi.DeclaringType == t)
						{
							return true;
						}
					}
				}
				return false;
			}
		}


		public static string GetMethodDescription(Type t, string methodName)
		{
			MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (methods == null || methods.Length == 0)
			{
				return "";
			}
			else
			{
				foreach (MethodInfo mi in methods)
				{
					if (string.Compare(methodName, mi.Name, true) == 0)
					{
						if (mi.DeclaringType == t)
						{
							var attrs = mi.GetCustomAttributes();
							var descriptionAttr = attrs.Where(a => a is Description).FirstOrDefault();
							if (descriptionAttr != null)
								return (descriptionAttr as Description).Text;
						}
					}
				}
				return "";
			}
		}


		/// <summary>
		/// Gets the value of a property or field.
		/// </summary>
		/// <param name="target">The object from which to read the property or field.</param>
		/// <param name="member">The name of the property or field to read.</param>
		/// <returns>Returns the value of the property or field.</returns>
		public static object GetMember(object target, string name)
		{
			int level = 0;
			var currentTarget = target;
			string[] tokens = name.Split('.');
			foreach (string token in tokens)
			{
				MemberInfo[] members = currentTarget.GetType().GetMember(token, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (members != null && members.Length == 1)
				{
					MemberInfo member = members[0];
					if (level < tokens.Length - 1)
					{
						level++;
						switch (member.MemberType)
						{
							case MemberTypes.Property:
								currentTarget = GetProperty(currentTarget, token);
								break;
							case MemberTypes.Field:
								currentTarget = GetField(currentTarget, token);
								break;
						}
					}
					else
					{
						switch (member.MemberType)
						{
							case MemberTypes.Property:
								return GetProperty(currentTarget, token);
							case MemberTypes.Field:
								return GetField(currentTarget, token);
						}
					}
				}
			}
			return null;
		}


		/// <summary>
		/// Sets the value of a property or field.
		/// </summary>
		/// <param name="target">The object on which to set the property or field.</param>
		/// <param name="member">The name of the property or field to set.</param>
		/// <param name="value">The value.</param>
		public static void SetMember(object target, string name, object value)
		{
			int level = 0;
			var currentTarget = target;
			string[] tokens = name.Split('.');
			foreach (string token in tokens)
			{
				MemberInfo[] members = currentTarget.GetType().GetMember(token, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (members != null && members.Length == 1)
				{
					MemberInfo member = members[0];
					if (level < tokens.Length - 1)
					{
						level++;
						switch (member.MemberType)
						{
							case MemberTypes.Property:
								currentTarget = GetProperty(currentTarget, token);
								break;
							case MemberTypes.Field:
								currentTarget = GetField(currentTarget, token);
								break;
						}
					}
					else
					{
						switch (member.MemberType)
						{
							case MemberTypes.Property:
								SetProperty(currentTarget, token, value);
								break;
							case MemberTypes.Field:
								SetField(currentTarget, token, value);
								break;
						}
					}
				}
			}
		}


		public static T Clone<T>(T o) where T: class
		{
			if (o == null)
				return null;
			var json = JsonConvert.SerializeObject(o);
			return JsonConvert.DeserializeObject<T>(json);
		}


		public static List<string> Search(object target, string searchTerm)
		{
			var list = new List<string>();
			SearchRecursive(list, target, searchTerm);
			return list;
		}

		public static void SearchRecursive(List<string> list, object target, string searchTerm, string prefix = "", int level = 0)
		{
			level++;
			if (level > 20) return;
			var members = target.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var m in members)
			{
				if (m.Name.Contains(searchTerm))
				{
					if (prefix != "")
						list.Add($"{prefix}{m.Name}");
					else
						list.Add(m.Name);
				}
				if (m.MemberType == MemberTypes.Property)
				{
					var pInfo = (m as PropertyInfo);
					if ((pInfo.PropertyType.IsClass && pInfo.PropertyType != typeof(string)) || pInfo.PropertyType.IsInterface)
					{
						if (pInfo.GetMethod.IsSpecialName) continue;
						var v = pInfo.GetValue(target);
						if (v != null)
							SearchRecursive(list, v, searchTerm, $"{prefix}{m.Name}.", level);
					}
				}
				else if (m.MemberType == MemberTypes.Field)
				{
					var fInfo = (m as FieldInfo);
					if ((fInfo.FieldType.IsClass && fInfo.FieldType != typeof(string)) || fInfo.FieldType.IsInterface)
					{
						var v = fInfo.GetValue(target);
						if (v != null)
							SearchRecursive(list, v, searchTerm, $"{prefix}{m.Name}.", level);
					}
				}
			}
		}


		public static List<string> Search(object target, Type searchType)
		{
			var list = new List<string>();
			SearchRecursive(list, target, searchType);
			return list;
		}

		public static void SearchRecursive(List<string> list, object target, Type searchType, string prefix = "", int level = 0)
		{
			level++;
			if (level > 20) return;
			var members = target.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var m in members)
			{
				if (m.MemberType == MemberTypes.Property)
				{
					var pInfo = (m as PropertyInfo);
					if (searchType.IsAssignableFrom(pInfo.PropertyType))
					{
						if (prefix != "")
							list.Add($"{prefix}{m.Name}");
						else
							list.Add(m.Name);
					}
					if ((pInfo.PropertyType.IsClass && pInfo.PropertyType != typeof(string)) || pInfo.PropertyType.IsInterface)
					{
						if (pInfo.GetMethod.IsSpecialName) continue;
						var v = pInfo.GetValue(target);
						if (v != null)
							SearchRecursive(list, v, searchType, $"{prefix}{m.Name}.", level);
					}
				}
				else if (m.MemberType == MemberTypes.Field)
				{
					var fInfo = (m as FieldInfo);
					if (searchType.IsAssignableFrom(fInfo.FieldType))
					{
						if (prefix != "")
							list.Add($"{prefix}{m.Name}");
						else
							list.Add(m.Name);
					}
					if ((fInfo.FieldType.IsClass && fInfo.FieldType != typeof(string)) || fInfo.FieldType.IsInterface)
					{
						var v = fInfo.GetValue(target);
						if (v != null)
							SearchRecursive(list, v, searchType, $"{prefix}{m.Name}.", level);
					}
				}
			}
		}
	}
}
