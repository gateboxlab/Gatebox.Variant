using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace Gatebox.Variant
{
	public class VariantConverter
	{
		//==============================================================================
		// static members
		//==============================================================================

		/// <summary>
		/// プリミティブ、JVaraint 関連の型を JVariant に変換する。
		/// <para>
		/// 変換できないときは null を返します。
		/// null に対しては JVaraint の Null を返却するため、失敗とは区別できます。</para>
		/// <para>
		/// この挙動は ConvertTrait インスタンスによる変換とは異なり固定で行われ、カスタマイズすることはできません。</para>
		/// </summary>
		public static JValue? CreateVariantFixed(object v)
		{
			if (v == null)
			{
				return new JVariant();
			}

			if (v is JVariant variant)
			{
				return variant.Value;
			}

			if (v is JArray array)
			{
				return new JVariant(array);
			}

			if (v is JObject obj)
			{
				return new JVariant(obj);
			}

			if (v is JValue value)
			{
				return value;
			}

			if (v is string s)
			{
				return new JVariant(s);
			}

			if (v is int i)
			{
				return new JVariant(i);
			}

			if (v is bool b)
			{
				return new JVariant(b);
			}

			if (v is double d)
			{
				return new JVariant(d);
			}
			if (v is float f)
			{
				return new JVariant(f);
			}

			if (v is char c)
			{
				return new JVariant(c);
			}

			// int 系 ulong 以外。結果的に long に変換される。
			if (v is long || v is short || v is sbyte || v is uint || v is ushort || v is byte)
			{
				return new JVariant(Convert.ToInt64(v));
			}

			// ulong. long の範囲内なら long にして、そうでないときは double にする。しょうがない。
			if (v is ulong)
			{
				// long で表現できるか？
				ulong ul = (ulong)v;
				if (ul <= (ulong)long.MaxValue)
				{
					return new JVariant((long)ul);
				}

				// 入らない。double にするしかない。
				return new JVariant((double)ul);
			}

			return null;
		}


		/// <summary>
		/// JVariant を固定のルールでプリミティブ及び、JVariant 関連の型に変換する。
		/// <para>
		/// json の型を越えた変換は行わない。
		/// 変換先の型が対象であれば true を返す。
		/// 実際に変換できるなら result に返す。
		/// （つまり、対象の型だが変換できないときは true を返しつつ、result は null )
		/// </para>
		/// </summary>
		public static bool ConvertVariantFixedStrict(JVariant variant, Type type, out object? result)
		{
			result = null;

			if (type == typeof(JVariant))
			{
				result = variant;
				return true;
			}

			if (type == typeof(JValue))
			{
				result = variant.Value;
				return true;
			}

			if (type == typeof(JArray))
			{
				if (variant.VariantType == VariantType.Array)
				{
					result = variant.AsArray();
				}
				return true;
			}

			if (type == typeof(JObject))
			{
				if (variant.VariantType == VariantType.Object)
				{
					result = variant.AsObject();
				}
				return true;
			}

			if (type == typeof(string))
			{
				if (variant.VariantType == VariantType.String)
				{
					result = variant.AsString();
				}
				return true;
			}

			if (type == typeof(bool))
			{
				if (variant.VariantType == VariantType.Boolean)
				{
					result = variant.AsBool();
				}
				return true;
			}


			if (type == typeof(int))
			{
				result = variant.IsNumber() ? variant.AsInt() : null;
				return true;
			}
			if (type == typeof(char))
			{
				result = variant.IsNumber() ? (char)variant.AsInt() : null;
				return true;
			}
			if (type == typeof(long))
			{
				result = variant.IsNumber() ? variant.AsLong() : null;
				return true;
			}
			if (type == typeof(short))
			{
				result = variant.IsNumber() ? (short)variant.AsInt() : null;
				return true;
			}
			if (type == typeof(sbyte))
			{
				result = variant.IsNumber() ? (sbyte)variant.AsInt() : null;
				return true;
			}
			if (type == typeof(uint))
			{
				result = variant.IsNumber() ? (uint)variant.AsLong() : null;
				return true;
			}
			if (type == typeof(ushort))
			{
				result = variant.IsNumber() ? (ushort)variant.AsInt() : null;
				return true;
			}
			if (type == typeof(byte))
			{
				result = variant.IsNumber() ? (byte)variant.AsInt() : null;
				return true;
			}
			if (type == typeof(double))
			{
				result = variant.IsNumber() ? variant.AsDouble() : null;
				return true;
			}
			if (type == typeof(float))
			{
				result = variant.IsNumber() ? variant.AsFloat() : null;
				return true;
			}

			if (type == typeof(ulong))
			{
				if (variant.IsNumber())
				{

					if (variant.VariantType == VariantType.Integer)
					{
						long l = variant.AsLong();
						if (l >= 0)
						{
							result = (ulong)l;
							return true;
						}
					}
					result = (ulong)variant.AsDouble();
				}

				return true;
			}


			return false;
		}

		/// <summary>
		/// プリミティブ及び、JVariant 関連の型を指定の型に変換する。
		/// </summary>
		public static bool ConvertVariantFixed(JVariant variant, Type type, out object? result)
		{

			if (type == typeof(JVariant))
			{
				result = variant;
				return true;
			}

			if (type == typeof(JArray))
			{
				result = variant.AsArray();
				return true;
			}

			if (type == typeof(JObject))
			{
				result = variant.AsObject();
				return true;
			}

			if (type == typeof(JValue))
			{
				result = variant.Value;
				return true;
			}

			if (type == typeof(string))
			{
				result = variant.AsString();
				return true;
			}

			if (type == typeof(int))
			{
				result = variant.AsInt();
				return true;
			}

			if (type == typeof(bool))
			{
				result = variant.AsBool();
				return true;
			}

			if (type == typeof(double))
			{
				result = variant.AsDouble();
				return true;
			}
			if (type == typeof(float))
			{
				result = variant.AsFloat();
				return true;
			}

			if (type == typeof(char))
			{
				result = (char)variant.AsInt();
				return true;
			}

			if (type == typeof(long))
			{
				result = variant.AsLong();
				return true;
			}

			if (type == typeof(short))
			{
				result = (short)variant.AsInt();
				return true;
			}

			if (type == typeof(sbyte))
			{
				result = (sbyte)variant.AsInt();
				return true;
			}

			if (type == typeof(uint))
			{
				result = (uint)variant.AsLong();
				return true;
			}

			if (type == typeof(ushort))
			{
				result = (ushort)variant.AsInt();
				return true;
			}

			if (type == typeof(byte))
			{
				result = (byte)variant.AsInt();
				return true;
			}

			if (type == typeof(ulong))
			{
				// int で正の数であればそのまま ulong にできる。そうでないときは double として解釈する。
				if (variant.VariantType == VariantType.Integer)
				{
					long l = variant.AsLong();
					if (l >= 0)
					{
						result = (ulong)l;
						return true;
					}
				}
				result = (ulong)variant.AsDouble();
				return true;
			}

			result = null;
			return false;
		}


	}
}
