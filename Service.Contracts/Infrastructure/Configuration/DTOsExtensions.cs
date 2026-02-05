using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Service.Contracts;
using Service.Contracts.Database;

namespace Service.Contracts
{
    public static class DTOsExtensions
    {
		public static ComponentMeta Init(this ComponentMeta meta, Type contract, Type implementation)
		{
			meta.Contract = contract.Name;
			meta.Implementations = new List<ComponentConfigMeta>();
			return meta;
		}

		public static ConfigMeta Init(this ConfigMeta meta, Type configType)
		{
			meta.Fields.Init(configType);
			return meta;
		}

		public static ComponentConfigMeta Init(this ComponentConfigMeta meta, Type contract, Type implementation)
		{
			meta.Contract = contract.Name;
			meta.Implementation = implementation.Name;
			meta.DisplayName = implementation.GetFriendlyName();
			meta.Description = implementation.GetDescription();
			if (implementation.Implements(typeof(IConfigurable<>)))
				meta.Config.Init(implementation.GetConfigurationType());
			return meta;
		}

		public static List<ConfigField> Init(this List<ConfigField> list, Type t)
		{
			if (t.Implements(typeof(IList<>)))
			{
				Type[] genArgs = t.GetGenericArguments();
				t = genArgs[0];
			}

            if(IsBasicConfigType(t) || ConfigurationContext.Components.Contains(t))
                return list;
			
            MemberInfo[] members = t.GetMembers(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
			foreach(var member in members)
			{
				if(member is FieldInfo fInfo)
				{
					list.Add(new ConfigField().Init(fInfo));
				}
				else if(member is PropertyInfo pInfo)
				{
					list.Add(new ConfigField().Init(pInfo));
				}
			}
			return list;
		}

		public static ConfigField Init(this ConfigField field, FieldInfo info)
		{
			field.Name = info.Name;
			field.Caption = info.GetCaption();
			field.Description = info.GetDescription();
			field.Type = GetFieldType(info.FieldType);
            field.Nullable = info.GetCustomAttribute<NullableAttribute>() != null;
            field.Constraints = new Constraints().Init(info);
			if (info.FieldType.IsClass && !info.FieldType.IsAbstract && !IsBasicConfigType(info.FieldType))
				field.SubFields = new List<ConfigField>().Init(info.FieldType);
            return field;
		}

		public static ConfigField Init(this ConfigField field, PropertyInfo info)
		{
			field.Name = info.Name;
			field.Caption = info.GetCaption();
			field.Description = info.GetDescription();
			field.Type = GetFieldType(info.PropertyType);
            field.Nullable = info.GetCustomAttribute<NullableAttribute>() != null;
            field.Constraints = new Constraints().Init(info);
			if (info.PropertyType.IsClass && !info.PropertyType.IsAbstract && !IsBasicConfigType(info.PropertyType))
				field.SubFields = new List<ConfigField>().Init(info.PropertyType);
            return field;
		}

		private static string GetFieldType(Type t)
		{
			if (t.Implements(typeof(IList<>)) || t.Implements(typeof(ICollection<>)))
			{
				StringBuilder sb = new StringBuilder(100);
				sb.Append("List<");
				Type[] argTypes = t.GetGenericArguments();
				sb.Append(GetFieldType(argTypes[0]));
				sb.Append(">");
				return sb.ToString();
			}
			else if (t.IsEnum)
			{
				return t.GetEnumUnderlyingType().Name;
			}
			else return t.Name;
		}

		private static bool IsBasicConfigType(Type t)
		{
			return (t == typeof(bool) || t == typeof(byte) || t == typeof(char) || t == typeof(int) ||
				t == typeof(long) || t == typeof(float) || t == typeof(double) || t == typeof(decimal) ||
				t == typeof(string) || t == typeof(DateTime) || t == typeof(TimeSpan));
		}

		private static bool IsValidListType(Type t)
		{
			return (
				(t.Implements(typeof(IList<>)) || t.Implements(typeof(ICollection<>))) &&
				IsAllowedConfigType(t.GetListType())
			);
		}

		private static bool IsAllowedConfigType(Type t)
		{
			return (
				IsBasicConfigType(t) ||
				IsValidListType(t) ||
					(t.IsClass && (
						(!t.IsAbstract && t.HasEmptyConstructor()) || ConfigurationContext.Components.Contains(t)
					))
			);
		}

		public static Constraints Init(this Constraints constraints, PropertyInfo info)
		{
			Attribute[] attributes = info.GetCustomAttributes() as Attribute[];
			if (attributes != null && attributes.Length > 0)
				constraints.Init(attributes);
			if (info.PropertyType.IsEnum)
				constraints.Items.Add(new Constraint("FixedOptions",  FixedOptions.FromEnum(info.PropertyType)));
            if(ConfigurationContext.Components.Contains(info.PropertyType))
                constraints.Items.Add(new Constraint("FixedOptions", FixedOptions.FromComponents(ConfigurationContext.Components.GetImplementations(info.PropertyType))));
            if(info.PropertyType.Implements(typeof(IList<>)) || info.PropertyType.Implements(typeof(ICollection<>)))
            {
                Type[] argTypes = info.PropertyType.GetGenericArguments();
                var listType = argTypes[0];
                if(ConfigurationContext.Components.Contains(listType))
                    constraints.Items.Add(new Constraint("FixedOptions", FixedOptions.FromComponents(ConfigurationContext.Components.GetImplementations(listType))));
            }

            return constraints;
		}

		public static Constraints Init(this Constraints constraints, FieldInfo info)
		{
			Attribute[] attributes = info.GetCustomAttributes() as Attribute[];
			if (attributes != null && attributes.Length > 0)
				constraints.Init(attributes);
			if (info.FieldType.IsEnum)
				constraints.Items.Add(new Constraint("FixedOptions", FixedOptions.FromEnum(info.FieldType)));
            if(ConfigurationContext.Components.Contains(info.FieldType))
                constraints.Items.Add(new Constraint("FixedOptions", FixedOptions.FromComponents(ConfigurationContext.Components.GetImplementations(info.FieldType))));
            if(info.FieldType.Implements(typeof(IList<>)) || info.FieldType.Implements(typeof(ICollection<>)))
            {
                Type[] argTypes = info.FieldType.GetGenericArguments();
                var listType = argTypes[0];
                if(ConfigurationContext.Components.Contains(listType))
                    constraints.Items.Add(new Constraint("FixedOptions", FixedOptions.FromComponents(ConfigurationContext.Components.GetImplementations(listType))));
            }
            return constraints;
		}

		public static Constraints Init(this Constraints constraints, Attribute[] attributes)
		{
			List<Constraint> items = new List<Constraint>();
			foreach (var attr in attributes)
			{
				if (attr is Readonly)
					items.Add(new Constraint("Readonly"));
				else if (attr is Hidden)
					items.Add(new Constraint("Hidden"));
				else if (attr is Required)
					items.Add(new Constraint("Required"));
				else if (attr is FixedOptions)
					items.Add(new Constraint("FixedOptions", attr as FixedOptions));
				else if (attr is MaxLen)
					items.Add(new Constraint("MaxLength", attr.ToString()));
				else if (attr is Range)
					items.Add(new Constraint("Range", attr.ToString()));
				else if (attr is ValidChars)
					items.Add(new Constraint("ValidChars", attr.ToString()));
				else if (attr is RegEx)
					items.Add(new Constraint("RegEx", attr.ToString()));
				else if (attr is Password)
					items.Add(new Constraint("Password"));
			}
			if (items.Count > 0)
				constraints.Items.AddRange(items);
			return constraints;
		}
	}
}
