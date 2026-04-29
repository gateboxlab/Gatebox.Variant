using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gatebox.Variant.Internal;


#nullable enable

namespace Gatebox.Variant
{
	/// <summary>
	/// Javascript 的な配列を表す値型。
	/// <para>
	/// IList&lt;JValue&gt; を実装します。JSON の値の配列として扱うことができます。</para>
	/// <para>
	/// 内部に List&lt;JValue&gt; を持っています。
	/// このクラスは値型ですが「参照を値で持っている」という状態であるため注意してください。
	/// JArray をコピーする、という行為はその参照をコピーすることになり、結果として内部情報である List&lt;JValue&gt; は共有されることになります。</para>
	/// <para>
	/// 多数のメソッドがありますが、
	/// 末尾への追加は Add()、設定は Set(), 取得は Get() です。</para>
	/// <para>
	/// [] による要素へのアクセスもできますが、 [] による要素のアクセスはそれが存在しない場合にそこまで要素を作って返すことに注意してください。
	/// (いきなり array[1000] とかやると 1001 個の要素を作ってその中の一つを返してきます。)</para>
	/// </summary>
	public struct JArray : IList<JValue>, IVariantConvertible
	{
		//==============================================================================
		// static members
		//==============================================================================

		/// <summary>
		/// (internal) 内部情報を受け取っての生成。そのまま内容が共有されます。
		/// </summary>
		internal static JArray CreateInternal(List<JValue>? body)
		{
			var ret = new JArray();
			ret.m_Body = body;
			return ret;
		}

		//==============================================================================
		// instance members
		//==============================================================================


		// 本体。生成時は null. 一度 非 null になったら変更されることはない。
		// null と 0 件は表面的には同等のものとして扱う。
		// JValue は参照型だか、基本的に null は入らないものとする。null を入れることもできるが、そういう使い方はあまり想定していない。
		private List<JValue>? m_Body;

		/// <summary>
		/// IEnumerable からの生成。
		/// <para>
		/// 指定された IEnumerable&lt;JValue&gt; から JArray を生成します。
		/// </para>
		/// </summary>
		public JArray(IEnumerable<JValue>? values)
		{
			m_Body = new List<JValue>(values ?? new List<JValue>());
		}

		/// <summary>
		/// List からの生成
		/// <para>
		/// List はシャロウコピーされます。
		/// つまり、配列自身は違うオブジェクトになりますが、内部の JValue は参照が共有されます。
		/// </para>
		/// </summary>
		public JArray(List<JValue>? values)
		{
			m_Body = new List<JValue>(values ?? new List<JValue>());
		}

		/// <summary>
		/// コピーによるコンストラクタ
		/// <para>
		/// 引数で与えられた JArray と内容を共有します。</para>
		/// <para>
		/// 値型なのでわざわざこれを呼ぶ必要はありません、単純に代入するのと結果は同じです。
		/// </para>
		/// </summary>
		public JArray(JArray other)
		{
			m_Body = other.GetBody();
		}


		/// <summary>
		/// 要素数。
		/// </summary>
		public readonly int Count => m_Body?.Count ?? 0;

		/// <summary>
		/// 読み取り専用か。
		/// <para>
		/// IList の実装です。常に false を返します。</para>
		/// </summary>
		public readonly bool IsReadOnly => false;

		/// <summary>
		/// インデクサ。
		/// <para>
		/// 範囲外の index の取得は それが入るところまで Null で埋められたのち、その Null を示す JValue を返します。</para>
		/// <para>
		/// 範囲外の index へ設定は、それが入るところまで Null で埋められます。</para>
		/// <para>
		/// 取得操作で内容が変更されることがあることに注意してください。
		/// その挙動が望ましくない場合は Get() を利用してください。</para>
		/// </summary>
		/// <param name="index">インデックス</param>
		public JValue this[int index]
		{
			get
			{
				EnsureIndex(index);
				return m_Body[index];
			}
			set
			{
				Set(index, value);
			}
		}

		/// <summary>
		/// インデクサ
		/// </summary>
		public JVariant this[Index index]
		{
			get
			{
				var offset = index.GetOffset(Count);
				EnsureIndex(offset);
				return m_Body[offset];
			}
			set
			{
				var offset = index.GetOffset(Count);
				Set(offset, value);
			}
		}

		/// <summary>
		/// 要素を持っていないとき true.
		/// </summary>
		public readonly bool IsEmpty() => (Count == 0);

		/// <summary>追加。</summary>
		public void Add(JValue? v)
		{
			EnsureBody();
			m_Body.Add(v ?? new JValue());
		}

		/// <summary>
		/// 要素の取得。
		/// 指定された要素が存在しないときは Null を示す JValue を返します。
		/// (このメソッドで Null が入っているのと存在しないのを区別することはできません。Count などを利用してください。)</summary>
		public readonly JValue Get(int index)
		{
			if (m_Body == null || index < 0 || index >= m_Body.Count)
			{
				return new JValue();
			}
			return m_Body[index];
		}

		/// <summary>
		/// 要素設定。
		/// <para>範囲外のインデックスを指定した場合、それが入るところまで Null で埋められます。</para>
		/// </summary>
		public void Set(int index, JValue? item)
		{
			EnsureIndex(index).Assign(item ?? new JValue());
		}

		/// <summary>
		/// サイズ変更。
		/// <para>
		/// this.Count が指定されたサイズより大きければ要素を後ろから削除し、
		/// this.Count が指定されたサイズより小さければ Null を表す JValue を後ろに追加します。</para>
		/// </summary>
		public void Resize(int size)
		{
			if (size < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(size), "size must be non-negative");
			}

			EnsureBody();
			int current = m_Body.Count;

			if (size < current)
			{
				m_Body.RemoveRange(size, current - size);
			}
			else if (size > current)
			{
				for (int i = 0; i < (size - current); i++)
				{
					m_Body.Add(new JVariant());
				}
			}
		}

		/// <summary>
		/// クリア。
		/// <para>
		/// 内容を空にします。
		/// この（値型としての）変数のクリアではなく、持っている配列のクリアであることに注意してください。
		/// 配列が共有されている場合はそれにも影響を与えます。</para>
		/// </summary>
		public readonly void Clear()
		{
			// オブジェクトが共有されているということに従うならば、そのオブジェクトをクリアすべき。
			// この実装はそれに従う。
			m_Body?.Clear();
		}

		/// <summary>
		/// 指定された要素が含まれるか？
		/// </summary>
		public readonly bool Contains(JValue item)
		{
			return m_Body == null ? false : m_Body.Contains(item);
		}

		/// <summary>
		/// 配列へのコピー
		/// </summary>
		public readonly void CopyTo(JValue[] array, int arrayIndex)
		{
			m_Body?.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// 反復子を返す。
		/// </summary>
		public IEnumerator<JValue> GetEnumerator()
		{
			return EnsureBody().GetEnumerator();
		}

		/// <summary>
		/// 指定された要素があればその位置を返す。なければ -1 を返す。
		/// </summary>
		public readonly int IndexOf(JValue item)
		{
			return m_Body == null ? -1 : m_Body.IndexOf(item);
		}


		/// <summary>
		/// 要素挿入
		/// </summary>
		public void Insert(int index, JValue item)
		{
			EnsureBody().Insert(index, item);
		}

		/// <summary>
		/// 指定要素削除
		/// </summary>
		public bool Remove(JValue item)
		{
			return EnsureBody().Remove(item);
		}

		/// <summary>
		/// 指定位置要素削除
		/// </summary>
		public void RemoveAt(int index)
		{
			EnsureBody().RemoveAt(index);
		}

		/// <summary>	
		/// 内部データを返す。
		/// <para>
		/// この JArray の内部データを返します。参照をそのまま返すのでこれを編集する場合は注意してください。
		/// JArray は内部情報が null の場合と 0 件の場合があり、表面的にはそれを同等のものとして扱っています。</para>
		/// <para>
		/// このメソッドは内部状態が null の場合、 0 件の情報を生成してそれを返します。(null を返すことはありません) </para>
		/// </summary>
		internal List<JValue> GetBody()
		{
			return EnsureBody();
		}





		/// <summary>
		/// オブジェクトに変換する。
		/// <para>
		/// インデックスを文字列に変えたオブジェクトを返します。
		/// [0,1,2] であれば { "0":0, "1":1, "2":2 } が返却されます。
		/// </para>
		/// </summary>
		public readonly JObject ConvertToObject()
		{
			if (m_Body == null)
			{
				return new JObject();
			}

			JObject ret = JObject.CreateWithCapacity(m_Body.Count + 4);
			for (int i = 0; i < m_Body.Count; i++)
			{
				ret.Set(i.ToString(), m_Body[i]);
			}
			return ret;
		}

		/// <summary>
		/// 文字列化
		/// <para>
		/// なんとなく内部状態を示す文字列を返します。JSON 表現は ToJson もしくは Stringify を利用してください。</para>
		/// </summary>
		public override readonly string ToString()
		{
			if (m_Body == null || m_Body.Count == 0)
			{
				return "[]";
			}
			using var sb = LocalTextBuilder.Acquire();
			sb.Append("[ ");

			for (int i = 0; i < m_Body.Count; i++)
			{
				if (i != 0)
				{
					sb.Append(", ");
				}
				sb.Append(m_Body[i]?.GetSimpleString() ?? "null");
			}
			sb.Append(" ]");
			return sb.ToString();
		}

		/// <summary>
		/// 内容がシンプルなとき true.
		/// <para>
		/// JSON変換の際の改行の判定に利用されます。
		/// プリミティブのみからなるとき true になります。</para>
		/// </summary>
		public readonly bool IsSimple()
		{
			for (int i = 0; i < this.Count; i++)
			{
				if (Get(i).IsComposite())
				{
					return false;
				}
			}
			return true;
		}

		public string Stringify(JsonFormatPolicy? policy = null)
		{
			return AsVariant().Stringify(policy);
		}
		public string ToJson(JsonFormatPolicy? policy = null)
		{
			return AsVariant().ToJson(policy);
		}
		public U8View ToU8Json(JsonFormatPolicy? policy = null)
		{
			return AsVariant().ToU8Json(policy);
		}


		internal readonly void ConvertToJSON(ref StringifyContext context)
		{
			try
			{
				context.Push(m_Body!);
				var appender = context.GetAppender(IsEmpty(), IsSimple());

				appender.Append('[');

				// 各要素、続きならカンマ、改行、要素
				for (int i = 0; i < this.Count; i++)
				{
					if (i != 0)
					{
						appender.AppendItemSeparator();
					}
					appender.AppendNewLine();
					this.Get(i).ConvertToJson(ref context);
				}

				appender.AppendNewLine(-1);
				appender.Append(']');
			}
			finally
			{
				context.Pop(m_Body!);
			}
		}

		/// <summary>
		/// ディープコピー
		/// <para>
		/// この JArray がもつ内容と同じ内容を持つ JArray を新たに作成して返す。</para>
		/// <para>
		/// 各項目はそれぞれ再帰的に内容のコピーを作成します。</para>
		/// <para>
		/// JArray は項目に自分自身を持ちえますが、そのような場合の配慮はされていません。（永久ループになります）
		/// ループするようなオブジェクトの構造はまずないとは思いますが、念の為配慮してください。
		/// </para>
		/// </summary>
		public readonly JArray Duplicate()
		{
			JArray ret = new JArray();

			if (IsEmpty())
			{
				return ret;
			}

			for (int i = 0; i < m_Body!.Count; i++)
			{
				ret.Add(m_Body[i].Duplicate());
			}

			return ret;
		}

		/// <summary>
		/// JVariant として自分を返す。
		/// </summary>
		public readonly JVariant AsVariant()
		{
			return new JVariant(this);
		}



		internal bool EquivalentTo(JArray other, int maxDepth, int depth)
		{
			if (Count != other.Count)
			{
				return false;
			}

			if (ReferenceEquals(this.m_Body, other.m_Body))
			{
				return true;
			}

			for (int i = 0; i < Count; i++)
			{
				if (!Get(i).EquivalentTo(other.Get(i), maxDepth, depth + 1))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// 反復子を返す。(非ジェネリック)
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return EnsureBody().GetEnumerator();
		}

		// 指定されたインデックスの要素がある状態にする。（サイズじゃなくてインデックスなので注意）
		[MemberNotNull(nameof(m_Body))]
		private JValue EnsureIndex(int index)
		{
			if (index < 0)
			{
				throw new ArgumentException();
			}
			if (index >= this.Count)
			{
				this.Resize(index + 1);
			}

			return m_Body![index];
		}


		// body が null ならば新しいのを作る
		[MemberNotNull(nameof(m_Body))]
		private List<JValue> EnsureBody()
		{

			m_Body ??= new List<JValue>();
			return m_Body;
		}
	}
}
