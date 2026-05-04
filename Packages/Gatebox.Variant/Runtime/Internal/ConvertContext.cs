using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

#nullable enable

namespace Gatebox.Variant.Internal
{

	/// <summary>
	/// JVaraint の変換中のコンテキスト。
	/// <para>
	/// これは <see cref="JVariant"/> <see cref="VariantConverter"/> の内部で使われるものです。</para>
	/// <para>
	/// 変換の仕方は VariantConverter が受け持つ一方、
	/// 変換の仕方を ConvertTrait を経由して外部に任せることがあり、
	/// 外部から指定されたコードを通して複雑な経路を辿って再帰的に変換が呼ばれることがあります。</para>
	/// <para>
	/// この時、変換の仕方を VariantConverter が受け持つのであれば、再帰的に呼ばれた変換も同じ VariantConverter が受け持つべきです。</para>
	/// <para>
	/// このような理由から、最初に変換を開始した時点で ConvertContext を生成、 ThreadLocal で管理することとします。
	/// これにより深いネストによる循環参照の疑いのチェックや、 VariantConverter の引き継ぎを行っています。</para>
	/// </summary>
	internal class ConvertContext
	{
		//==============================================================================
		// static members
		//==============================================================================

		private readonly static ThreadLocal<ConvertContext?> s_Current = new ();

		/// <summary>
		/// ConvertContext のインスタンスを取得する。
		/// <para>
		/// コンテキスト内の上位で生成されている場合はそれを、なければここで生成して ConvertContext を返します。</para>
		/// <para>
		/// 変換が終わったら確実に<see cref="Release()" />を呼び出してください。</para>
		/// </summary>
		/// <remarks>
		/// ConvertContextScope みたいなものを返して IDisposable で Release を呼び出すようにするのもありかもしれませんが、
		/// どうしてもそのためだけのオブジェクトを生成する必要が生じ、
		/// Gatebox.Variant 内のみの利用と考えたときあまりメリットがありません。
		/// 間違えないようにすれば良い、と考えます。
		/// </remarks>
		public static ConvertContext Acquire()
		{
			var current = s_Current.Value;
			if (current != null)
			{
				current.Increment();
				return current;
			}

			var context = new ConvertContext();
			s_Current.Value = context;
			return context;
		}

		//==============================================================================
		// instance members
		//==============================================================================

		private int m_Depth;
		private List<VariantConverter>? m_Converters;

		// コンストラクタ
		private ConvertContext()
		{
			m_Depth = 0;
		}


		/// <summary>
		/// 現在の変換の深さ
		/// </summary>
		public int Depth => m_Depth;

		/// <summary>
		/// 現在の VariantConverter
		/// </summary>
		public VariantConverter Converter
		{
			get
			{
				if (m_Converters is null || m_Converters.Count == 0)
				{
					return VariantConverter.Default;
				}
				return m_Converters[^1];
			}
		}

		/// <summary>
		/// VariantConverter を Push する。
		/// <para>
		/// VariantConverter を明示的に指定する場合にこれを呼び出してください。
		/// その変換の最中に新たな変換が行われた場合
		/// ここで Push した VariantConverter が引き継がれて利用されます。</para>
		/// <para>
		/// 変換のスコープに合わせて <see cref="PopConverter"/> を呼び出してください。
		/// </para>
		/// </summary>
		public void PushConverter(VariantConverter converter)
		{
			m_Converters ??= new List<VariantConverter>();
			m_Converters.Add(converter);
		}

		/// <summary>
		/// VariantConverter を Pop する。
		/// <para>
		/// <see cref="PushConverter(VariantConverter)"/> と対応させて確実に呼び出してください。</para>
		/// </summary>
		public void PopConverter()
		{
			if (m_Converters is null || m_Converters.Count == 0)
			{
				throw new InvalidOperationException("No converter to pop.");
			}
			m_Converters.RemoveAt(m_Converters.Count - 1);
		}

		/// <summary>
		/// スコープ終了
		/// <para>
		/// <see cref="Acquire"/> で取得した ConvertContext のスコープの末尾で呼び出してください。</para>
		/// </summary>
		public void Release()
		{
			if (Decrement() <= 0)
			{
				s_Current.Value = null;
			}
		}

		private int Decrement()
		{
			m_Depth -= 1;
			return m_Depth;
		}

		private void Increment()
		{
			m_Depth += 1;
			if (Depth > JVariant.DefaultMaxDepth)
			{
				throw new InvalidOperationException("Too deep conversion. Circular reference suspected.");
			}
		}

	}
}
