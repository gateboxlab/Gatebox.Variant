using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace Gatebox.Variant
{

	/// <summary>
	/// JVariant と他の型との変換を行うためのもの
	/// <para>
	/// 継承する場合デフォルトコンストラクタで構築可能にしてください。</para>
	/// <para>
	/// 変換対象の値が型と整合せず変換できない場合は <see cref="VariantConvertException"/> を投げてください。
	/// これは入力値に起因する通常の変換失敗を表します。</para>
	/// <para>
	/// <see cref="VariantException"/> はより広い基底例外であり、
	/// 変換失敗以外の JVariant 関連エラーも含みます。
	/// <see cref="JVariant.As{T}(bool)"/> は throws が false のとき <see cref="VariantConvertException"/> のみを補足して default を返します。</para>
	/// </summary>
	public abstract class ConvertTrait
	{
		/// <summary>
		/// JVaraint への変換。
		/// <para>
		/// サブクラスでオーバーライドし、v を JVariant に変換して返してください。 
		/// 変換できなかったときは <see cref="VariantConvertException"/> を投げてください。</para>
		/// </summary>
		public abstract JVariant ToVariant(object v);


		/// <summary>
		/// JVariant からの変換。
		/// <para>
		/// サブクラスでオーバーライドし、variant を指定クラスに変換してください。
		/// 変換できなかったときは <see cref="VariantConvertException"/> を投げてください。</para>
		/// </summary>
		public abstract object? FromVariant(JVariant variant);
	}


	/// <summary>
	/// ConvertTrait のジェネリック版
	/// </summary>
	public abstract class ConvertTrait<T> : ConvertTrait
	{
		public sealed override JVariant ToVariant(object v)
		{
			if (v is T t)
			{
				return CreateVariant(t);
			}
			throw new VariantConvertException($"Unable to convert {v.GetType().Name} to {typeof(T).Name}.");
		}

		public sealed override object? FromVariant(JVariant variant)
		{
			return ConvertVariant(variant);
		}

		/// <summary>
		/// JVaraint への変換。
		/// <para>
		/// サブクラスでオーバーライドし、v を JVariant に変換して返してください。 
		/// 変換できなかったときは <see cref="VariantConvertException"/> を投げてください。</para>
		/// </summary>
		public abstract JVariant CreateVariant(T v);


		/// <summary>
		/// JVaraint への変換。
		/// <para>
		/// サブクラスでオーバーライドし、JVariant を T に変換してください。
		/// 変換できなかったときは <see cref="VariantConvertException"/> を投げてください。</para>
		/// </summary>
		public abstract T ConvertVariant(JVariant variant);
	}






	/// <summary>
	/// ConvertTrait を静的に指定するための属性。
	/// <para>
	/// ConvertTrait の実装にこの属性を付けておくと、アセンブリから自動的に登録されます。</para>
	/// <para>
	/// TargetType は ConvertTrait で変換する型を指定します。
	/// 基本的には具体型で、<see cref="ConvertTrait{T}"/> の T を指定しますが、
	/// ConvertTrait と同じジェネリック型引数で生成できるジェネリック定義型も指定できます。</para>
	/// <para>
	/// つまり、
	/// <code><![CDATA[ class MyValue<T> ]]></code> というジェネリック定義型があるとして、
	/// <code><![CDATA[ [ConvertTrait(typeof(MyValue<>))] 
	///  class MyValueTrait<T> : ConvertTrait<MyValue<T>> {}]]></code>
	/// とすることで、
	/// たとえば <c>MyValue&lt;int&gt;</c> に対して、<c>MyValueTrait&lt;int&gt;</c> が生成されて利用されます。
	/// </para>
	/// </summary>
#if UNITY_2021_2_OR_NEWER
	[AttributeUsage(AttributeTargets.Class)]
	public class ConvertTraitAttribute : UnityEngine.Scripting.PreserveAttribute
#else
	[AttributeUsage(AttributeTargets.Class)]
	public class ConvertTraitAttribute : Attribute
#endif
	{
		public ConvertTraitAttribute(Type t)
		{
			TargetType = t;
		}

		public Type TargetType { get; }
	}

}
