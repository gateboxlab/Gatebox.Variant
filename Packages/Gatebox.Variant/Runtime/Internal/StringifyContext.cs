using System;

#nullable enable

namespace Gatebox.Variant.Internal
{
	internal ref struct StringifyContext
	{
		//==============================================================================
		// inner types
		//==============================================================================

		// 追記していくためのもの
		public readonly struct Appender
		{
			private readonly JsonFormatPolicy m_Policy;
			private readonly IBuffer m_Buffer;
			private readonly int m_Depth;
			private readonly bool m_NeedsLine;


			public Appender(JsonFormatPolicy policy, IBuffer buffer, int depth, bool newline)
			{
				m_Policy = policy;
				m_Buffer = buffer;
				m_Depth = depth;
				m_NeedsLine = newline;
			}

			public readonly void Append(char c)
			{
				m_Buffer.Append(c);
			}

			public readonly void Append(string s)
			{
				m_Buffer.Append(s);
			}


			public readonly void AppendItemSeparator()
			{
				m_Buffer.Append(',');
				if (!m_NeedsLine)
				{
					m_Buffer.Append(' ');
				}
			}

			public readonly void AppendNewLine(int indent_difference = 0)
			{
				if (m_NeedsLine)
				{
					m_Buffer.Append('\n');

					for (int i = 0; i < m_Depth + indent_difference; i++)
					{
						if (m_Buffer.BufferType == BufferType.U16)
						{
							m_Buffer.Append(m_Policy.Indent);
						}
						else
						{
							m_Buffer.Append(m_Policy.IndentU8);
						}
					}
				}
			}
		}

		//==============================================================================
		// static members
		//==============================================================================

		public static StringifyContext ForU8(JsonFormatPolicy policy)
		{
			return new StringifyContext(new U8Buffer(), policy);
		}
		public static StringifyContext ForString(JsonFormatPolicy policy)
		{
			return new StringifyContext(new U16Buffer(), policy);
		}

		//==============================================================================
		// instance members
		//==============================================================================

		private readonly IBuffer m_Buffer;
		private readonly JsonFormatPolicy m_Policy;
		private int m_Depth;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public StringifyContext(IBuffer buffer, JsonFormatPolicy policy)
		{
			m_Depth = 0;
			m_Buffer = buffer;
			m_Policy = policy;
		}

		/// <summary>
		/// ポリシー
		/// </summary>
		public readonly JsonFormatPolicy Policy => m_Policy;


		/// <summary>
		/// バッファ
		/// </summary>
		public readonly IBuffer GetBuffer() => m_Buffer;


		/// <summary>
		/// 破棄
		/// </summary>
		public readonly void Dispose()
		{
			m_Buffer.Dispose();
		}

		/// <summary>
		/// 循環参照の検出とインデントの処理のための深さの管理
		/// <para>
		/// 引数は JArray や JObject の内部オブジェクトですが、使っていません。
		/// この引数を使えばちゃんと循環参照を検出することができるのですが、
		/// ほぼありえないことに対応するために仰々しい実装が必要になってしまうので、単純な深さのみで例外を投げます。
		/// </para>
		/// </summary>
		public void Push(object o)
		{
			m_Depth += 1;
			if (m_Depth > m_Policy.MaxDepth)
			{
				throw new JsonFormatException("Exceeded maximum depth. Circular reference suspected.");
			}
		}

		/// <summary>
		/// スタックから pop
		/// <para>
		/// 引数を受ける意味はありません。形式的なものです。
		/// </para>
		/// </summary>
		public void Pop(object o)
		{
			System.Diagnostics.Debug.Assert(m_Depth >= 1);
			m_Depth -= 1;
		}

		/// <summary>
		/// 配列、オブジェクトの内部を追記するための Appender を作って返す。
		/// </summary>
		/// <param name="isEmpty">対象が空の時</param>
		/// <param name="isSimple">対象の内容がCompositな要素を持たない時 true</param>
		public readonly Appender GetAppender(bool isEmpty, bool isSimple)
		{
			var returnPolicy = m_Policy.ReturnPolicy;

			// 改行なしパターン
			if (returnPolicy == ReturnPolicy.Never)
			{
				return new Appender(m_Policy, m_Buffer, m_Depth, false);
			}

			// 常に改行
			if (returnPolicy == ReturnPolicy.Every)
			{
				return new Appender(m_Policy, m_Buffer, m_Depth, true);
			}

			// 空のときは（常に改行ポリシー以外は）改行なし
			if (isEmpty)
			{
				return new Appender(m_Policy, m_Buffer, m_Depth, false);
			}

			// シンプルなときは改行なしというポリシー
			if (returnPolicy == ReturnPolicy.Simple)
			{
				return new Appender(m_Policy, m_Buffer, m_Depth, !isSimple);
			}

			return new Appender(m_Policy, m_Buffer, m_Depth, true);
		}

		public readonly string StringResult()
		{
			return m_Buffer.GetStringView().ToString();
		}

		public readonly U8View U8Result()
		{
			return m_Buffer.GetU8View();
		}
	}
}
