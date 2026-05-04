using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gatebox.Variant.Internal;

#nullable enable

namespace Gatebox.Variant
{
	public class VariantConverter
	{
		//==============================================================================
		// static members
		//==============================================================================

		private static readonly Lazy<VariantConverter> s_Default = new (()=> new VariantConverter(collect_definitions:true));

		public static VariantConverter Default => s_Default.Value;


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


		

		// ConvertTraitAttribute が付与されたクラスを収集する。
		private static Dictionary<Type, Type> CollectTraitsDefinitions()
		{
			var ret = new Dictionary<Type, Type>();

			// ConvertTraitAttribute つきクラスを収集
			var types = CollectTypesWithAttribute(typeof(ConvertTraitAttribute));

			foreach (var type in types)
			{
				// 変換対象の型を取得
				var attr = type.GetCustomAttributes().FirstOrDefault(attr => attr is ConvertTraitAttribute) as ConvertTraitAttribute;
				var targetType = attr!.TargetType;

				// ConvertTrait を継承しているはず
				if (!type.IsSubclassOf(typeof(ConvertTrait)))
				{
					throw new VariantException($"Failed to analyze {type.Name}. Classes with the {nameof(ConvertTraitAttribute)} must inherit from {nameof(ConvertTrait)}.");
				}

				if (ret.ContainsKey(targetType))
				{
					throw new VariantException($"${nameof(ConvertTrait)} for {targetType.Name} duplicated.");
				}

				ChackTraitType(type, targetType);
				ret[targetType] = type;
			}
			return ret;
		}

		// 指定された属性を持つクラス定義をアプリケーションドメイン全体から集める。
		private static List<Type> CollectTypesWithAttribute(Type attr)
		{
			var ret = new List<Type>();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					if (Attribute.IsDefined(type, attr))
					{
						ret.Add(type);
					}
				}
			}
			return ret;
		}

		// type が targetType に対して ConvertTrait として利用できるかどうかを判定する
		// だめなときは例外を投げる。
		private static void ChackTraitType(Type type, Type targetType)
		{
			// ConvertTrait を継承しているはず
			if (!type.IsSubclassOf(typeof(ConvertTrait)))
			{
				throw new VariantException($"{type.Name} must inherit from ${nameof(ConvertTrait)}.");
			}

			// 部分的未解決ジェネリックは対応できない。
			if (type.IsGenericType && type.ContainsGenericParameters && (!type.IsGenericTypeDefinition))
			{
				throw new VariantException($"{type.Name}  has partially unresolved type parameters. cannot be used as ${nameof(ConvertTrait)}");
			}

			// 構築可能でなければならない。
			if (!type.IsDefaultConstructible())
			{
				throw new VariantException($"{type.Name} requires a no parameter constructor.");
			}

			var traitTargetType = GetTraitTargetType(type);
			if (traitTargetType == null)
			{
				throw new VariantException($"{type.Name} must inherit from {nameof(ConvertTrait)}<T>.");
			}

			if (type.IsGenericTypeDefinition)
			{
				// Generic type であれば
				// target<T> と definition<T> が対応可能でなければならない。
			
				if (!targetType.IsGenericTypeDefinition)
				{
					throw new VariantException($"The generic type definition {type.Name} does not match {targetType.Name}. ");
				}
				if (!CanConcretizeWithSameArguments(type, targetType))
				{
					throw new VariantException($"The generic type definition {type.Name} does not match {targetType.Name}. The type arguments and constraints must be equivalent.");
				}
				if (!MatchesOpenTargetType(traitTargetType, type, targetType))
				{
					throw new VariantException($"{type.Name} converts {traitTargetType}, not {targetType}.");
				}

				// ここでは(まだ型が開いているので)具体的にはならんのだが、少なくとも abstract, interface であってはならない。
				if (type.IsAbstract || type.IsInterface)
				{
					throw new VariantException($"{type.Name} must be a concrete type.");
				}

			}
			else
			{
				// Generic Type でない場合は具体的型でなければならない。

				if ( ! type.IsConcrete() )
				{
					throw new VariantException($"{type.Name} must be a concrete type.");
				}

				if (traitTargetType != targetType)
				{
					throw new VariantException($"{type.Name} converts {traitTargetType}, not {targetType}.");
				}
			}
		}

		private static Type? GetTraitTargetType(Type type)
		{
			for (var current = type; current != null; current = current.BaseType)
			{
				if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(ConvertTrait<>))
				{
					return current.GetGenericArguments()[0];
				}
			}

			return null;
		}

		private static bool MatchesOpenTargetType(Type traitTargetType, Type traitType, Type targetType)
		{
			if (!traitTargetType.IsGenericType)
			{
				return false;
			}

			if (traitTargetType.GetGenericTypeDefinition() != targetType)
			{
				return false;
			}

			var traitArgs = traitType.GetGenericArguments();
			var targetArgs = traitTargetType.GetGenericArguments();
			if (traitArgs.Length != targetArgs.Length)
			{
				return false;
			}

			for (int i = 0; i < traitArgs.Length; i++)
			{
				if (targetArgs[i] != traitArgs[i])
				{
					return false;
				}
			}

			return true;
		}

		// 2つのジェネリック型定義が同じ型引数で具体化できるかどうかを判定する
		// type2 のほうが多少厳しいのは許される。（つまり [type1 は type2 と同じ型引数で具体化できるか] を返す。）
		private static bool CanConcretizeWithSameArguments(Type type1, Type type2)
		{
			if (!type1.IsGenericTypeDefinition || !type2.IsGenericTypeDefinition)
			{
				return false;
			}

			var args1 = type1.GetGenericArguments();
			var args2 = type2.GetGenericArguments();

			if (args1.Length != args2.Length)
			{
				return false;
			}

			for (int i = 0; i < args1.Length; i++)
			{
				if (args1[i].GenericParameterAttributes != args2[i].GenericParameterAttributes)
				{
					return false;
				}

				var constraint1 = args1[i].GetGenericParameterConstraints();
				var constraint2 = args2[i].GetGenericParameterConstraints();

				foreach (var c1 in constraint1)
				{
					if (!constraint2.Contains(c1))
					{
						return false;
					}
				}
			}
			return true;
		}


		//==============================================================================
		// instance members
		//==============================================================================

		private readonly object m_Lock = new();
		private readonly Dictionary<Type, ConvertTrait?> m_Traits = new();
		private Dictionary<Type,Type> m_TraitDefinitions;
		
		/// <summary>
		/// コンストラクタ
		/// </summary>
		public VariantConverter( bool collect_definitions = false)
		{
			if (collect_definitions)
			{
				m_TraitDefinitions = CollectTraitsDefinitions();
			}
			else
			{
				m_TraitDefinitions = new Dictionary<Type, Type>();
			}
		}


		/// <summary>
		/// 全アセンブリから <see cref="ConvertTraitAttribute"/> が付与されたクラスを収集し、変換定義を更新します。
		/// <para>
		/// <see cref="Default"/> インスタンスは生成時にこのメソッドを呼び出して初期化されています。
		/// 自分で <see cref="VariantConverter"/> を作成したときは、このメソッドを呼び出して変換定義を収集する必要があります。
		/// </para>
		/// </summary>
		public void CollectionMarkedTraitDefinitions()	
		{
			var defs = CollectTraitsDefinitions();

			lock (m_Lock) 
			{
				m_Traits.Clear();

				if( m_TraitDefinitions.Count == 0 )
				{
					m_TraitDefinitions = defs;
				}
				else
				{
					foreach (var def in defs)
					{
						m_TraitDefinitions[def.Key] = def.Value;
					}
				}
			}
		}

		/// <summary>
		/// Trait 定義を追加。
		/// <para>
		/// この型はこの型を通して変換を行う、ということを定義します。</para>
		/// <para>
		/// 基本的には <see cref="ConvertTraitAttribute"/> を付与して自動的に集めることを想定しています。
		/// 同じ型を場合によってを違うロジックで変換したい、
		/// あるいは、変換方法がアプリケーション依存で共有コードとして表現されづらい、のような理由がある場合に利用してください。
		/// </para>
		/// </summary>
		public void RegisterTraitDefinition<TARGET, TRAIT>( bool overwrite = false) where TRAIT : ConvertTrait
		{
			RegisterTraitDefinition(typeof(TARGET), typeof(TRAIT), overwrite);
		}

		/// <summary>
		/// Trait 定義を追加。
		/// </summary>
		public void RegisterTraitDefinition(Type target, Type trait, bool overwrite = false)
		{
			ChackTraitType(trait, target);

			lock (m_Lock)
			{
				if (!overwrite && m_TraitDefinitions.ContainsKey(target))
				{
					throw new VariantException($"Trait for {target.Name} already exists. Set overwrite to true to overwrite it.");
				}
				m_TraitDefinitions[target] = trait;

				// これは消しすぎなのだけど、マッチするものだけを消すのは辛い。
				// 何度も定義を追加するようなことはない（最初に一回定義追加したら後は使うだけ）と考える。
				m_Traits.Clear();
			}
		}


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

		/// <summary>
		/// 任意型の値を JVariant に変換する。
		/// </summary>
		public JVariant CreateVariant<T>( T t)
		{
			if (t is null)
			{
				return new JVariant();
			}

			var v = CreateVariantFixed(t!);
			if (v is not null)
			{
				return v;
			}

			var context = ConvertContext.Acquire();
			try
			{
				context.PushConverter(this);
				return CreateVariantFrom(t, typeof(T));
			}
			finally
			{
				context.PopConverter();
				context.Release();
			}
		}


		/// <summary>
		/// 型からその変換を行う ConvertTrait を取得する。
		/// </summary>
		public ConvertTrait? GetTrait(Type type)
		{
			lock (m_Lock)
			{
				if (m_Traits.TryGetValue(type, out var trait))
				{
					return trait;
				}

				trait = CreateCustomeTrait(type);
				trait ??= CreateTrait(type);
				m_Traits[type] = trait;

				return trait;
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


		internal JVariant CreateVariantFrom(object? v, Type t)
		{
			if( v == null ){
				return new JVariant();
			}

			if (IsUnsupported(t))
			{
				throw new VariantConvertException($"Conversion to type {t} is not supported.");
			}

			var trait = GetTrait(t);
			if (trait == null)
			{
				throw new VariantConvertException($"Unable to convert type {t}.");
			}
			return trait.ToVariant(v!);
		}


		private ConvertTrait? CreateCustomeTrait( Type type)
		{
			Type? trait = null;

			// そのまま Definitions に入っている場合
			if( m_TraitDefinitions.TryGetValue(type, out trait))
			{
				return Activator.CreateInstance(trait) as ConvertTrait;
			}

			// ジェネリック型定義でマッチするものがある場合
			if(type.IsGenericType)
			{
				var genericTypeDefinition = type.GetGenericTypeDefinition();
				if(m_TraitDefinitions.TryGetValue(genericTypeDefinition, out var generic_trait))
				{
					trait = generic_trait.MakeGenericType(type.GetGenericArguments());
					return Activator.CreateInstance(trait) as ConvertTrait;
				}
			}

			return null;
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

			
			var dynamicTraitType = typeof(DynamicConvertTrait<>).MakeGenericType(type);
			return Activator.CreateInstance(dynamicTraitType) as ConvertTrait;
		}
	}
}
