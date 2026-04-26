using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gatebox.Variant.Internal;

#nullable enable

namespace Gatebox.Variant
{
	public struct JArray
	{
		internal static JArray CreateInternal(List<JValue>? body) => new JArray(body);



		private List<JValue>? m_Body;





		public JArray(IEnumerable<JValue> values)
		{
			m_Body = new List<JValue>(values);
		}

		private JArray(List<JValue>? values)
		{
			m_Body = values;
		}

		public readonly int Count => m_Body?.Count ?? 0;

		public JValue this[int index]
		{
			get
			{
				// TODO : 実装
				return new JValue();
			}
			set
			{
				// TODO : 実装
			}
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
		/// 要素を持っていないとき true.
		/// </summary>
		public readonly bool IsEmpty() => (Count == 0);


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


		/// <summary>
		/// 要素の取得。
		/// 指定された要素が存在しないときは Null を示す JVariant を返します。
		/// (このメソッドで Null が入っているのと存在しないのを区別することはできません。Count などを利用してください。)
		/// </summary>
		public readonly JVariant Get(int index)
		{
			if (m_Body == null || index < 0 || index >= m_Body.Count)
			{
				return new JVariant();
			}
			return m_Body[index];
		}

		public void Set(int index, JValue item)
		{
			// TODO : 実装
		}

		public void Add(JValue? item)
		{
			// TODO : 実装
		}


		// body が null ならば新しいのを作る
		private List<JValue> EnsureBody()
		{

			m_Body ??= new List<JValue>();
			return m_Body;
		}

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
					this.Get(i).Value.ConvertToJson(ref context);
				}

				appender.AppendNewLine(-1);
				appender.Append(']');
			}
			finally
			{
				context.Pop(m_Body!);
			}
		}
	}
}
