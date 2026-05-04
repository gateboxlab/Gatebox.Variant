using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Gatebox.Variant.Internal;

#nullable enable

namespace Gatebox.Variant
{
	public class VariantConverter
	{
		//==============================================================================
		// static members
		//==============================================================================

		private static Lazy<VariantConverter> s_Default = new Lazy<VariantConverter>(CreateDefault);

		public static VariantConverter Default => s_Default.Value;


		private static VariantConverter CreateDefault(){
			return new VariantConverter();
		}

		/// <summary>
		/// 普通に考えて「そりゃ無理やろ」っていう型
		/// <para>
		/// 二重否定っぽいですが、これが false を返したからといって変換できるわけではないです。
		/// 少なくともこの関数が true を返す型は変換できない、ということです。
		/// </para>
		/// </summary>
		public static bool IsUnsupported( Type type)
		{
			// 処理そのものを表すもの、
			// ポインタ、
			// リフレクション関係、
			// ref struct などは一旦無理ってことにする。
			// いろいろやっていって増やしていくとかしかないんだと思う。


			if (type == null)
			{
				return true;
			}
			
			if ( typeof(Delegate).IsAssignableFrom(type))
			{
				return true;
			}

			if (type.IsPointer)
			{
				return true;
			}

			if (type == typeof(Type))
			{
				return true;
			}
			if (typeof(MemberInfo).IsAssignableFrom(type))
			{
				return true;
			}

			if (type.IsByRefLike)
			{
				return true;
			}

			return false;
		}



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



		//==============================================================================
		// instance members
		//==============================================================================

		/// <summary>
		/// JVariant を指定の型 T に変換する。
		/// <para>
		/// このメソッドはできるだけ厳密に型変換を行い、値が型 T と互換性がない場合は例外をスローします。
		/// 型制約なく T を受け、Null 非許容の T を返します。
		/// そのため、JVariant が null の場合も例外を投げます。
		/// 値型の場合は、T 自体を null 許容とすることができるのでその場合のみ null を返します。
		/// (これは C# の Nullable の仕様に基づくものです。? の意味が class と struct で異なるのに、両者を一つのジェネリクスで受けられてしまう。)
		/// </para>
		/// </summary>
		public T ConvertToStrict<T>( JVariant v)
		{
			// この関数 で null を返すことがあるのは T がNull許容値型の場合のみ。
			if (v.IsNull())
			{
				if (Nullable.GetUnderlyingType(typeof(T)) != null)
				{
					return default(T)!;
				}
				throw new VariantConvertException($"Value is null, but {typeof(T)} is not a nullable type: {this}");
			}

			// 定型変換
			if (ConvertVariantFixedStrict(v, typeof(T), out object? x))
			{
				if (x == null)
				{
					throw new VariantConvertException($"Value cannot be converted to {typeof(T)}: {v}");
				}
				return (T)x!;
			}

			// Context に 自分を push してから内部実装を呼ぶ。
			// これで再帰的に JVariant.As<> が呼ばれたき、おなじ Converter で変換される。
			var context = ConvertContext.Acquire();
			context.PushConverter(this);
			try
			{
				T? result = ConvetrtVariantTo<T>(v);
				return result ?? throw new VariantConvertException($"Value cannot be converted to {typeof(T)}: {v}");
			}
			finally
			{
				context.PopConverter();
				context.Release();
			}
		}


	
		
		/// <summary>
		/// JVariant を指定の型 T に変換する。
		/// <para>
		/// このメソッドはできるだけ広く解釈して型変換を行い、値が null である場合は null を返します。</para>
		/// <para>
		/// 変換できない場合は VariantException を投げます。</para>
		/// </summary>
		/// <typeparam name="T">変換先の型。具体的な型である必要があります。</typeparam>
		/// <param name="v">変換対象</param>
		public T? ConvertTo<T>(JVariant v)
		{
			// null だったら default.
			if (v.IsNull())
			{
				return default;
			}

			// 定型変換
			if (ConvertVariantFixed(v, typeof(T), out object? x))
			{
				return (T?)x;
			}

			
			// Context に 自分を push してから内部実装を呼ぶ。
			// これで再帰的に JVariant.As<> が呼ばれたき、おなじ Converter で変換される。
			var context = ConvertContext.Acquire();
			context.PushConverter(this);
			try
			{
				return ConvetrtVariantTo<T>(v);
			}
			finally
			{
				context.PopConverter();
				context.Release();
			}
		}

		// ConvertTo の 内部実装。プリミティブへの変換はここに来る時点で完了していて、
		// ここでは構造を持つ方への変換を行う。
		internal T? ConvetrtVariantTo<T>(JVariant v)
		{
			// 明確に変換不能な型
			if (IsUnsupported(typeof(T)))
			{
				throw new VariantConvertException($"Conversion to type {typeof(T)} is not supported.");
			}

			if (!typeof(T).IsConcrete())
			{
				throw new VariantConvertException($"Conversion to non-concrete type {typeof(T)} is not supported.");
			}

			var trait = GetTrait(typeof(T));
			if(trait == null)
			{
				throw new VariantConvertException($"Unable to convert type {typeof(T)}.");
			}

			return (T?)trait.FromVariant(v);
		}

		private ConvertTrait? GetTrait(Type type)
		{
			// TODO : 事前登録の対応
			// TODO : キャッシュの対応

			return CreateTrait(type);
		}




		// 型に対して、その型の変換をおこなう ConvertTrait を生成して返す。
		private ConvertTrait? CreateTrait(Type type)
		{
			// IJVariantConvertible
			if (typeof(IVariantConvertible).IsAssignableFrom(type))
			{
				var traitType = typeof(JVariantConvertibleTrait<>).MakeGenericType(type);
				var ctor = traitType.GetConstructor(Array.Empty<Type>());
				return ctor.Invoke(Array.Empty<object>()) as ConvertTrait;
			}

			// 配列
			if (type.IsArray)
			{
				var elementType = type.GetElementType();
				var traitType = typeof(ArrayTypeConvertTrait<>).MakeGenericType(elementType);
				return Activator.CreateInstance(traitType) as ConvertTrait;
			}

			// Enum
			if (type.IsEnum)
			{
				var traitType = typeof(EnumTypeConvertTrait<>).MakeGenericType(type);
				return Activator.CreateInstance(traitType) as ConvertTrait;
			}

			// Nullable<>
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				var valueType = type.GetGenericArguments()[0];
				var traitType = typeof(NullableTypeConvertTrait<>).MakeGenericType(valueType);
				return Activator.CreateInstance(traitType) as ConvertTrait;
			}

			// IDictionary<string,> を実装している？
			var dict = type.GetInterfaces()
					.Where(t =>
						(t.IsGenericType) &&
						(t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) &&
						(t.GetGenericArguments()[0] == typeof(string)))
					.FirstOrDefault();
			if (dict != null && type.IsDefaultConstructible()) 
			{
				var valueType = dict.GetGenericArguments()[1];
				var traitType = typeof(DictionaryTypeConvertTrait<,>).MakeGenericType(type, valueType);
				return Activator.CreateInstance(traitType) as ConvertTrait;
			}

			// ICollection<> を実装している？
			var collection = type.GetInterfaces()
					.Where(t =>
						(t.IsGenericType) &&
						(t.GetGenericTypeDefinition() == typeof(ICollection<>)))
					.FirstOrDefault();
			if (collection != null && type.IsDefaultConstructible())
			{
				var valueType = collection.GetGenericArguments()[0];
				var traitType = typeof(CollectionTypeConvertTrait<,>).MakeGenericType(type, valueType);
				return Activator.CreateInstance(traitType) as ConvertTrait;
			}

			// TODO : リフレクションで対応するパターン等を追加


			return null;
		}
	}
}
