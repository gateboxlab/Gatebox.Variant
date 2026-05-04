using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gatebox.Variant.Internal
{
	public class DynamicConvertTrait<T> : ConvertTrait<T>
	{
		private static readonly Type s_TargetType = typeof(T);
		private static readonly DynamicMember[] s_Members = CollectMembers();

		public DynamicConvertTrait()
		{
		}

		public override JVariant CreateVariant(T v)
		{
			if (v == null)
			{
				return new JVariant();
			}

			var obj = s_Members.Length > 0 ? JObject.CreateWithCapacity(s_Members.Length) : new JObject();
			var context = ConvertContext.Acquire();
			try
			{
				foreach (var member in s_Members)
				{
					var value = member.GetValue(v);
					obj.Add(member.JsonName, CreateMemberVariant(context.Converter, value, member.MemberType).GetValue());
				}
			}
			finally
			{
				context.Release();
			}

			return obj.AsVariant();
		}

		public override T ConvertVariant(JVariant variant)
		{
			if (!variant.IsObject())
			{
				throw new VariantConvertException($"Unable to convert {variant.VariantType} to {s_TargetType.Name}.");
			}

			var instance = CreateInstance();
			var obj = variant.AsObject();
			var boxed = (object?)instance!;
			var context = ConvertContext.Acquire();
			try
			{
				foreach (var member in s_Members)
				{
					if (!obj.ContainsKey(member.JsonName))
					{
						continue;
					}

					var memberVariant = obj.Get(member.JsonName);
					var value = ConvertMemberValue(context.Converter, memberVariant, member.MemberType);
					member.SetValue(boxed!, value);
				}
			}
			finally
			{
				context.Release();
			}

			return (T)boxed!;
		}

		private static T CreateInstance()
		{
			if (s_TargetType.IsValueType)
			{
				return (T)Activator.CreateInstance(s_TargetType)!;
			}

			if (!s_TargetType.IsDefaultConstructible())
			{
				throw new VariantConvertException($"{s_TargetType.Name} requires a public no parameter constructor.");
			}

			return (T)Activator.CreateInstance(s_TargetType)!;
		}

		private static object? ConvertMemberValue(VariantConverter converter, JVariant variant, Type memberType)
		{
			if (variant.IsNull())
			{
				if (!memberType.IsValueType || Nullable.GetUnderlyingType(memberType) != null)
				{
					return null;
				}

				throw new VariantConvertException($"Value is null, but {memberType.Name} is not a nullable type.");
			}

			if (VariantConverter.ConvertVariantFixed(variant, memberType, out var fixedValue))
			{
				return fixedValue;
			}

			if (VariantConverter.IsUnsupported(memberType))
			{
				throw new VariantConvertException($"Conversion to type {memberType} is not supported.");
			}

			if (!memberType.IsConcrete())
			{
				throw new VariantConvertException($"Conversion to non-concrete type {memberType} is not supported.");
			}

			var trait = converter.GetTrait(memberType);
			if (trait == null)
			{
				throw new VariantConvertException($"Unable to convert type {memberType}.");
			}

			return trait.FromVariant(variant);
		}

		private static JVariant CreateMemberVariant(VariantConverter converter, object? value, Type memberType)
		{
			if (value == null)
			{
				return new JVariant();
			}

			var fixedValue = VariantConverter.CreateVariantFixed(value);
			if (fixedValue != null)
			{
				return new JVariant(fixedValue);
			}

			return converter.CreateVariantFrom(value, memberType);
		}

		private static DynamicMember[] CollectMembers()
		{
			var members = new List<DynamicMember>();
			var clrNames = new HashSet<string>();
			var jsonNames = new HashSet<string>();

			for (var type = s_TargetType; type != null && type != typeof(object); type = type.BaseType)
			{
				const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

				foreach (var property in type.GetProperties(flags))
				{
					if (!clrNames.Add(property.Name))
					{
						continue;
					}

					if (!IsTargetProperty(property) || HasJsonIgnore(property))
					{
						continue;
					}

					var jsonName = GetJsonName(property);
					if (jsonNames.Add(jsonName))
					{
						members.Add(DynamicMember.FromProperty(property, jsonName));
					}
				}

				foreach (var field in type.GetFields(flags))
				{
					if (!clrNames.Add(field.Name))
					{
						continue;
					}

					if (!IsTargetField(field) || HasJsonIgnore(field))
					{
						continue;
					}

					var jsonName = GetJsonName(field);
					if (jsonNames.Add(jsonName))
					{
						members.Add(DynamicMember.FromField(field, jsonName));
					}
				}
			}

			return members.ToArray();
		}

		private static bool IsTargetProperty(PropertyInfo property)
		{
			return property.GetIndexParameters().Length == 0 &&
				property.GetMethod != null &&
				property.SetMethod != null &&
				property.GetMethod.IsPublic &&
				property.SetMethod.IsPublic &&
				!IsInitOnly(property);
		}

		private static bool IsTargetField(FieldInfo field)
		{
			return !field.IsStatic && !field.IsInitOnly;
		}

		private static bool IsInitOnly(PropertyInfo property)
		{
			var setMethod = property.SetMethod;
			if (setMethod == null)
			{
				return false;
			}

			return setMethod.ReturnParameter
				.GetRequiredCustomModifiers()
				.Any(t => t.FullName == "System.Runtime.CompilerServices.IsExternalInit");
		}

		private static bool HasJsonIgnore(MemberInfo member)
		{
			return member.GetCustomAttributes(inherit: true)
				.Any(attr => attr.GetType().FullName == "System.Text.Json.Serialization.JsonIgnoreAttribute");
		}

		private static string GetJsonName(MemberInfo member)
		{
			var attr = member.GetCustomAttributes(inherit: true)
				.FirstOrDefault(attr => attr.GetType().FullName == "System.Text.Json.Serialization.JsonPropertyNameAttribute");

			if (attr == null)
			{
				return member.Name;
			}

			return attr.GetType().GetProperty("Name")?.GetValue(attr) as string ?? member.Name;
		}

		private sealed class DynamicMember
		{
			private readonly PropertyInfo? m_Property;
			private readonly FieldInfo? m_Field;

			private DynamicMember(string jsonName, Type memberType, PropertyInfo property)
			{
				JsonName = jsonName;
				MemberType = memberType;
				m_Property = property;
			}

			private DynamicMember(string jsonName, Type memberType, FieldInfo field)
			{
				JsonName = jsonName;
				MemberType = memberType;
				m_Field = field;
			}

			public string JsonName { get; }
			public Type MemberType { get; }

			public static DynamicMember FromProperty(PropertyInfo property, string jsonName)
			{
				return new DynamicMember(jsonName, property.PropertyType, property);
			}

			public static DynamicMember FromField(FieldInfo field, string jsonName)
			{
				return new DynamicMember(jsonName, field.FieldType, field);
			}

			public object? GetValue(object instance)
			{
				if (m_Property != null)
				{
					return m_Property.GetValue(instance);
				}

				return m_Field!.GetValue(instance);
			}

			public void SetValue(object instance, object? value)
			{
				if (m_Property != null)
				{
					m_Property.SetValue(instance, value);
					return;
				}

				m_Field!.SetValue(instance, value);
			}
		}
	}
}
