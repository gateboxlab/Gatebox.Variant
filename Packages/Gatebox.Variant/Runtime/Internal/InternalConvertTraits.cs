using System;
using System.Collections.Generic;
using System.Reflection;

#nullable enable

namespace Gatebox.Variant.Internal
{
	/// <summary>
	/// 配列を変換するためのトレイト
	/// <para>
	/// <see cref="VariantConverter"/> 内部で利用されているものです。</para>
	/// </summary>
	internal class ArrayTypeConvertTrait<T> : ConvertTrait<T[]>
	{
		public override T[] ConvertVariant(JVariant variant)
		{
			if (!variant.IsArray())
			{
				throw new VariantConvertException($"Unable to convert {variant.VariantType}  to {typeof(T[]).Name}");
			}

			T[] obj = new T[variant.Count];
			for (int i = 0; i < variant.Count; i++)
			{
				obj[i] = variant[i].As<T>()!;
			}

			return obj;
		}

		public override JVariant CreateVariant(T[] v)
		{
			JArray array = new JArray();
			foreach (var item in v)
			{
				array.Add(JVariant.Create(item));
			}
			return array.AsVariant();
		}
	}

	/// <summary>
	/// IVariantConvertible からの変換を行うトレイト
	/// <para>
	/// <see cref="VariantConverter"/> 内部で利用されているものです。</para>
	/// </summary>
	internal class JVariantConvertibleTrait<T> : ConvertTrait<IVariantConvertible>
	{
		private readonly ConstructorInfo m_Constructor;

		public JVariantConvertibleTrait()
		{
			m_Constructor = typeof(T).GetConstructor(new Type[] { typeof(JVariant) });
		}

		public override IVariantConvertible ConvertVariant(JVariant variant)
		{
			if (m_Constructor == null)
			{
				throw new VariantConvertException($"{typeof(T)} requires a constructor that accepts a {nameof(JVariant)}.");
			}

			return (IVariantConvertible)m_Constructor.Invoke(new object[] { variant });
		}

		public override JVariant CreateVariant(IVariantConvertible v)
		{
			return v.AsVariant();
		}
	}

	/// <summary>
	/// enum を変換するためのトレイト
	/// <para>
	/// <see cref="VariantConverter"/> 内部で利用されているものです。</para>
	/// </summary>
	internal class EnumTypeConvertTrait<ENUM> : ConvertTrait<ENUM> where ENUM : struct, Enum
	{
		private const double MaxSafeIntegerDouble = 9007199254740991d;

		public override ENUM ConvertVariant(JVariant variant)
		{
			try
			{
				if (variant.IsString())
				{
					return (ENUM)Enum.Parse(typeof(ENUM), variant.AsString());
				}
				if (variant.IsNumber())
				{
					var underlying = Enum.GetUnderlyingType(typeof(ENUM));
					var numericValue = ConvertEnumNumericValue(variant, underlying);
					return (ENUM)Enum.ToObject(typeof(ENUM), numericValue);
				}
			}
			catch (Exception e) when (e is ArgumentException || e is OverflowException)
			{
				throw new VariantConvertException($"Unable to convert \"{variant.ToString()}\"  to {typeof(ENUM).Name}", e);
			}

			throw new VariantConvertException($"Unable to convert {variant.VariantType}  to {typeof(ENUM).Name}");
		}

		private static object ConvertEnumNumericValue(JVariant variant, Type underlyingType)
		{
			if (variant.VariantType == VariantType.Integer)
			{
				long integerValue = variant.AsLong();
				return ConvertIntegerValue(integerValue, underlyingType);
			}

			double floatingValue = variant.AsDouble();
			if (!double.IsFinite(floatingValue))
			{
				throw new VariantConvertException($"Unable to convert non-finite number to {underlyingType.Name}.");
			}
			if (floatingValue != Math.Truncate(floatingValue))
			{
				throw new VariantConvertException($"Unable to convert non-integer number to {underlyingType.Name}.");
			}
			if (Math.Abs(floatingValue) > MaxSafeIntegerDouble)
			{
				throw new VariantConvertException($"Unable to safely convert {floatingValue} to {underlyingType.Name}.");
			}

			return ConvertIntegerValue(checked((long)floatingValue), underlyingType);
		}

		private static object ConvertIntegerValue(long value, Type underlyingType)
		{
			if (underlyingType == typeof(sbyte))
			{
				return checked((sbyte)value);
			}
			if (underlyingType == typeof(byte))
			{
				return checked((byte)value);
			}
			if (underlyingType == typeof(short))
			{
				return checked((short)value);
			}
			if (underlyingType == typeof(ushort))
			{
				return checked((ushort)value);
			}
			if (underlyingType == typeof(int))
			{
				return checked((int)value);
			}
			if (underlyingType == typeof(uint))
			{
				return checked((uint)value);
			}
			if (underlyingType == typeof(long))
			{
				return value;
			}
			if (underlyingType == typeof(ulong))
			{
				if (value < 0)
				{
					throw new OverflowException();
				}
				return (ulong)value;
			}

			throw new VariantConvertException($"Unsupported enum underlying type: {underlyingType}.");
		}

		public override JVariant CreateVariant(ENUM v)
		{
			return JVariant.Create(v.ToString());
		}
	}

	/// <summary>
	/// Nullable を変換するためのトレイト
	/// <para>
	/// <see cref="VariantConverter"/> 内部で利用されているものです。</para>
	/// </summary>
	internal class NullableTypeConvertTrait<T> : ConvertTrait<Nullable<T>> where T : struct
	{
		public override Nullable<T> ConvertVariant(JVariant variant)
		{
			if (variant.IsNull())
			{
				return null;
			}
			else
			{
				return variant.As<T>();
			}
		}

		public override JVariant CreateVariant(Nullable<T> v)
		{
			if (v.HasValue)
			{
				return JVariant.Create(v.Value);
			}
			else
			{
				return new JVariant();
			}
		}
	}


	/// <summary>
	/// <see cref="IDictionary{string, V}"/> 変換するためのトレイト
	/// <para>
	/// <see cref="IDictionary{string, V}"/> を実装した具体型。
	/// V は IDictionary の Value の型。</para>
	/// <para>
	/// デフォルトコンストラクタを持ち、IDictionary を実装した型はそれを通して JObject と相互変換する。</para>
	/// <para>
	/// <see cref="VariantConverter"/> 内部で利用されているものです。</para>
	/// </summary>
	internal class DictionaryTypeConvertTrait<T, V> : ConvertTrait<T> where T : IDictionary<string, V>, new()
	{
		public override T ConvertVariant(JVariant variant)
		{
			if (!variant.IsObject())
			{
				throw new VariantConvertException($"Unable to convert {variant.VariantType}  to {typeof(T).Name}");
			}

			T obj = new();
			IDictionary<string, V> dict = obj;

			foreach (var pair in variant.AsObject())
			{
				string key = pair.Key;
				V value = pair.Value.As<V>()!;
				dict.Add(key, value);
			}

			return obj;
		}

		public override JVariant CreateVariant(T v)
		{
			IDictionary<string, V> dict = v;

			JObject obj = new JObject();
			foreach (var pair in dict)
			{
				obj.Add(pair.Key, JVariant.Create(pair.Value));
			}

			return obj.AsVariant();
		}
	}

	/// <summary>
	/// <see cref="ICollection{ V}"/> 変換するためのトレイト
	/// <para>
	/// <see cref="ICollection{V}"/> を実装した具体型。
	/// <para>
	/// デフォルトコンストラクタを持ち、ICollection を実装した型はそれを通して JArray と相互変換する。</para>
	/// <para>
	/// <see cref="VariantConverter"/> 内部で利用されているものです。</para>
	/// </summary>
	internal class CollectionTypeConvertTrait<T, V> : ConvertTrait<T> where T : ICollection<V>, new()
	{
		public override T ConvertVariant(JVariant variant)
		{
			if (!variant.IsArray())
			{
				throw new VariantConvertException($"Unable to convert {variant.VariantType}  to {typeof(T).Name}");
			}

			T obj = new();
			ICollection<V> list = obj;

			foreach (var item in variant.AsArray())
			{
				V value = item.As<V>()!;
				list.Add(value);
			}

			return obj;
		}

		public override JVariant CreateVariant(T v)
		{
			ICollection<V> list = v;

			JArray array = new JArray();
			foreach (var item in list)
			{
				array.Add(JVariant.Create(item));
			}
			return array.AsVariant();
		}
	}





}
