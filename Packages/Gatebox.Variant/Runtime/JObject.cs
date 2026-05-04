using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Gatebox.Variant.Internal;

#nullable enable

namespace Gatebox.Variant
{

	/// <summary>
	/// javascript 的なオブエジェクトを表す値型。
	/// <para>
	/// IDictionary&lt;string, JValue&gt; を実装します。string と JSON の値のマップとして扱うことができます。</para>
	/// <para>
	/// このクラスは値型ですが「参照を値で持っている」という状態であるため注意してください。
	/// JObject をコピーする、という行為はその参照をコピーすることになり、
	/// 結果として内部情報は共有されることになります。</para>
	/// </summary>
	public struct JObject : IDictionary<string, JValue>, IVariantConvertible
	{
		//==============================================================================
		// static members
		//==============================================================================

		/// <summary>
		/// キャパシティを指定しての生成。
		/// </summary>
		public static JObject CreateWithCapacity(int capacity) => new JObject(capacity);

		/// <summary>
		/// (internal) 内部データからの生成。
		/// </summary>
		internal static JObject CreateInternal(JObjectBody? body) => new JObject(body);

		//==============================================================================
		// instance members
		//==============================================================================

		private JObjectBody? m_Body;


		// public JObject()
		// {
		//   m_Body = null;
		// }

		/// <summary>
		/// JVariant からの生成。
		/// <para>
		///	内容が Object の場合はそれをそのまま要求。
		///	null の場合は Null を示す JObject として初期化。
		///	それ以外では VariantException を投げます。
		/// </para>
		/// </summary>
		public JObject(JVariant v)
		{
			if (v.IsObject())
			{
				m_Body = v.AsObject().GetBody();
			}
			else if (v.IsNull())
			{
				m_Body = null;
			}
			else
			{
				throw new VariantException();
			}
		}


		/// <summary>
		/// (private) JObjectBody からの生成。
		/// </summary>
		private JObject(JObjectBody? body)
		{
			m_Body = body;
		}



		/// <summary>
		/// (private) キャパシティを指定しての生成。
		/// </summary>
		private JObject(int capacity)
		{
			m_Body = new JObjectBody(capacity: capacity);
		}



		/// <summary>
		/// キーの配列
		/// </summary>
		public readonly ICollection<string> Keys
		{
			get => m_Body?.Keys ?? Array.Empty<string>();
		}

		/// <summary>
		/// 値の配列
		/// </summary>
		public readonly ICollection<JValue> Values
		{
			get => m_Body?.Values ?? (ICollection<JValue>)Array.Empty<JValue>();
		}

		/// <summary>
		/// 要素数
		/// </summary>
		public readonly int Count
		{
			get
			{
				if (m_Body == null)
				{
					return 0;
				}
				return m_Body.Count;
			}
		}

		/// <summary>
		/// 読み取り専用か。
		/// <para>
		/// IDictionary の実装のためのものです。常に false を返します。</para>
		/// </summary>
		public readonly bool IsReadOnly => false;


		/// <summary>
		/// インデクサ
		/// <para>
		/// 存在しないキーに対するアクセスは、そのキーの要素を追加した後にそれを返します。
		/// 取得操作で内容が変更されることがあるため注意してください。その挙動が望ましくない場合は Get() を利用してください。</para>
		/// </summary>
		public JValue this[string key]
		{
			get => EnsureItem(key);
			set => Set(key, value);
		}

		public JValue this[StringView key]
		{
			get => EnsureItem(key);
			set => Set(key, value);
		}

		/// <summary>
		/// 要素を持っていないとき true
		/// </summary>
		public readonly bool IsEmpty() => (Count == 0);


		/// <summary>
		/// 内容がシンプルなとき true.
		/// <para>
		/// JSON変換の際の改行の判定に利用されます。
		/// プリミティブな要素を一つだけ持つ場合、シンプルとみなされます。</para>
		/// </summary>
		public readonly bool IsSimple()
		{
			if (this.Count == 0)
			{
				return true;
			}
			if (this.Count == 1)
			{
				var v = m_Body!.GetValueAt(0);
				return (!v.IsComposite());
			}
			return false;
		}


		/// <summary>
		/// 追加。
		/// <para>
		/// すでに同じキーがある場合は AugumentException を投げます。
		/// この挙動が望ましくない場合は Set() を利用してください。</para>
		/// </summary>
		public void Add(string key, JValue v) { AddInternal(key, v); }
		
		/// <summary>
		/// KeyValuePair による追加。
		/// <para>
		/// IDictionary の実装のためのものです。</para>
		/// </summary>
		public void Add(KeyValuePair<string, JValue> item)
		{
			if (item.Value == null)
			{
				item = new KeyValuePair<string, JValue>(item.Key, new JValue());
			}

			EnsureBody();
			m_Body.Add(item.Key, item.Value);
		}


		/// <summary>
		/// 要素の取得。
		/// <para>
		/// 指定された要素が存在しないときは Null を示す JVariant を返します。</para>
		/// <para>
		/// このメソッドで Null が入っているのと存在しないのを区別することはできません。
		/// <see cref="ContainsKey"/> などを利用してください。</para>
		/// </summary>
		public readonly JVariant Get(StringView key)
		{
			if (m_Body == null || !m_Body.ContainsKey(key))
			{
				return new JVariant();
			}

			var v = m_Body!.GetOrDefault(key);
			return v == null ? new JVariant() : new JVariant(v);
		}

		/// <summary>
		/// 内容の設定。
		/// <para>
		/// 個々の要素である JValue は可変の参照型ですが、
		/// 設定時は基本的に参照を替えるのではなく、内容を書き換えようとします。</para>
		/// </summary>
		public void Set(StringView key, JValue value) { EnsureItem(key).Assign(value); }



		/// <summary>
		/// クリア
		/// </summary>
		public readonly void Clear()
		{
			m_Body?.Clear();
		}

		/// <summary>
		/// 指定された要素を含むか。
		/// <para>IDictionary の実装のためのものです。</para>
		/// </summary>
		readonly bool ICollection<KeyValuePair<string, JValue>>.Contains(KeyValuePair<string, JValue> item)
		{
			return m_Body?.Contains(item) ?? false;
		}

		/// <summary>
		/// 指定されたキーの要素を含むか
		/// </summary>
		public readonly bool ContainsKey(string key)
		{
			return m_Body?.ContainsKey(key) ?? false;
		}
		public readonly bool ContainsKey(StringView key)
		{
			return m_Body?.ContainsKey(key) ?? false;
		}

		/// <summary>
		/// 配列へのコピー。
		/// <para>IDictionary の実装のためのものです。</para>
		/// </summary>
		readonly void ICollection<KeyValuePair<string, JValue>>.CopyTo(KeyValuePair<string, JValue>[] array, int arrayIndex)
		{
			if (m_Body != null)
			{
				((ICollection<KeyValuePair<string, JValue>>)m_Body).CopyTo(array, arrayIndex);
			}
		}



		/// <summary>
		/// IEnumerator を返す。
		/// </summary>
		public readonly IEnumerator<KeyValuePair<string, JValue>> GetEnumerator()
		{
			if (m_Body == null)
			{
				yield break;
			}

			foreach (var x in m_Body)
			{
				yield return x;
			}
		}

		readonly IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// 要素削除
		/// </summary>
		public readonly bool Remove(string key)
		{
			return m_Body?.Remove(key) ?? false;
		}

		/// <summary>
		/// 要素削除
		/// </summary>
		public readonly bool Remove(StringView key)
		{
			return m_Body?.Remove(key) ?? false;
		}

		/// <summary>
		/// 要素削除
		/// <para>ICollection の実装のためのものです。</para>
		/// </summary>
		readonly bool ICollection<KeyValuePair<string, JValue>>.Remove(KeyValuePair<string, JValue> item)
		{
			if( m_Body == null )
			{
				return false;
			}
			int index = m_Body.Find(new StringView(item.Key));
			if( index < 0 )
			{
				return false;
			}

			var v = m_Body.GetValueAt(index);
			if( v == null || !v.Equals(item.Value) )
			{
				return false;
			}
			return m_Body.RemoveAt(index);
		}

		/// <summary>
		/// 要素取得
		/// </summary>
		public readonly bool TryGetValue(string key, out JValue value)
		{
			if (m_Body == null || !m_Body.ContainsKey(key))
			{
				value = new JValue();
				return false;
			}
			value = m_Body.GetOrDefault(key)!;
			return true;
		}





		/// <summary>
		/// なんとなく配列に変換する。
		/// <para>
		/// キーのすべてが int として解釈可能であれば、
		/// そのインデックスに各要素を詰めた配列を p に返します。
		/// 失敗した場合は false を返します。
		/// </para>
		/// <para>
		/// 各項目が int として解釈可能かどうかしか判定していません。
		/// int として解釈した結果同じ値に解決することがありますが( "1" と "+1" とか)
		/// それは配慮されていません、どちらかが失われたうえで、配列に変換可能とされます。
		/// </para>
		/// </summary>
		public readonly bool TryConvertToArray(out JArray p)
		{
			try
			{
				var ret = new JArray();
				foreach (string k in this.Keys)
				{
					int index = int.Parse(k);
					ret.Set(index, Get(k));
				}
				p = ret;
				return true;
			}
			catch (FormatException) { }
			catch (ArgumentException) { }

			p = new JArray();
			return false;
		}



		/// <summary>
		/// JVariant に変換する。
		/// </summary>
		public readonly JVariant AsVariant()
		{
			return new JVariant(this);
		}

		public readonly string Stringify(JsonFormatPolicy? policy = null)
		{
			return AsVariant().Stringify(policy);
		}
		public string ToJson(JsonFormatPolicy? policy = null)
		{
			return AsVariant().ToJson(policy);
		}
		public readonly U8View ToU8Json(JsonFormatPolicy? policy = null)
		{
			return AsVariant().ToU8Json(policy);
		}


		/// <summary>
		/// ドット表記による子要素の参照
		/// <para>
		/// JVariant.Pick() を参照してください。</para>
		/// </summary>
		public readonly JVariant Pick(string path)
		{
			return AsVariant().Pick(path);
		}

		/// <summary>
		/// ディープコピー
		/// <para>
		/// この JObject がもつ内容と同じ内容を持つ JObject を新たに作成して返す。</para>
		/// <para>
		/// 各項目はそれぞれ再帰的に内容のコピーを作成します。</para>
		/// <para>
		/// JObject は項目に自分自身を持ちえますが、そのような場合の配慮はされていません。（永久ループになります）
		/// ループするようなオブジェクトの構造はまずないとは思いますが、念の為配慮してください。
		/// </para>
		/// </summary>
		public readonly JObject Duplicate()
		{
			if( IsEmpty() )
			{
				return new JObject();
			}
			var ret = JObject.CreateWithCapacity(this.Count);

			for( int i = 0; i < this.Count; ++i )
			{
				var key = m_Body!.GetKeyAt(i);
				var value = m_Body.GetValueAt(i);
				if( value != null )
				{
					ret.Add(key, value.Duplicate());
				}
				else
				{
					ret.Add(key, new JValue());
				}
			}
			return ret;
		}

		/// <summary>
		/// 文字列化
		/// <para>
		/// なんとなく内部状態を示す文字列を返します。
		/// JSON表現を返すわけではないので注意してください。
		/// </para>
		/// </summary>
		public override readonly string ToString()
		{
			if (m_Body == null || m_Body.Count == 0)
			{
				return "{}";
			}

			using var sb = LocalTextBuilder.Acquire();
			sb.Append("{ ");

			for (int i = 0; i < m_Body.Count; ++i)
			{
				if (i != 0)
				{
					sb.Append(", ");
				}
				var key = m_Body.GetKeyAt(i);
				var value = m_Body.GetValueAt(i);

				sb.Append(key);
				sb.Append(":");
				sb.Append(value?.GetSimpleString() ?? "null");
			}

			sb.Append(" }");
			return sb.ToString();
		}


		public readonly bool EquivalentTo(JObject other, int maxDepth = JVariant.DefaultMaxDepth, int depth = 0)
		{
			// 個数が違うときは等価でない。
			if (Count != other.Count)
			{
				return false;
			}

			// 対象が 0 の時は自分が 0 である必要がある
			if( other.Count == 0)
			{
				return (Count == 0);
			}

			// 自分が 0 のときは対象も 0 である必要があるが、
			// その前の条件で対象が 0 でないときは等価でないので、ここに来るときは自分も対象も 0 でないことが保証される。
			if ( Count == 0)
			{
				return false;
			}

			// この時点で両方とも個数 0 ではない、つまり m_Body は存在することが保証される。
			var o1 = m_Body!;
			var o2 = other.m_Body!;

			// 同じインスタンスを参照しているときは等価。
			if (ReferenceEquals(o1, o2))
			{
				return true;
			}
			
			for( int i =0 ; i< Count ; i++)
			{
				if( o1.GetKeyAt(i) != o2.GetKeyAt(i) )
				{
					return false;
				}
				var v1 = o1.GetValueAt(i);
				var v2 = o2.GetValueAt(i);
				if( !v1.EquivalentTo(v2, maxDepth, depth + 1) )
				{
					return false;
				}
			}

			return true;
		}


		[MemberNotNull(nameof(m_Body))]
		private JObjectBody EnsureBody()
		{
			m_Body ??= new JObjectBody();
			return m_Body;
		}



		// 内部データを返す。
		internal JObjectBody GetBody()
		{
			return EnsureBody();
		}

		// add の実装
		// ArgumentException を投げることがある。
		private void AddInternal(string key, JVariant value)
		{
			m_Body ??= new JObjectBody();
			m_Body.Add(key, new JValue(value));
		}

		// Set の実装のため。確実に key の要素が存在するようにしてそれを返す。Setはそこに Assign すると無駄がない。
		private JValue EnsureItem(StringView key)
		{
			m_Body ??= new JObjectBody();

			int index = m_Body.Find(key);
			if (index >= 0)
			{
				var v = m_Body.GetValueAt(index);
				if (v != null)
				{
					return v;
				}
			}

			var ret = new JValue();
			m_Body[key] = ret;
			return ret;
		}


	

		internal readonly void ConvertToJSON(ref StringifyContext context)
		{
			try
			{
				context.Push(m_Body!);
				var appender = context.GetAppender(IsEmpty(), IsSimple());

				appender.Append('{');

				if (Count > 0)
				{
					var body = m_Body!;
					for (int i = 0; i < body.Count; ++i)
					{
						if (i != 0)
						{
							appender.AppendItemSeparator();
						}
						appender.AppendNewLine();

						var key = body.GetKeyAt(i);
						var value = body.GetValueAt(i);

						appender.Append('"');
						appender.Append(TextUtil.EscapeJsonString(key, context.Policy.EscapeMultiBytes));
						appender.Append("\": ");
						value.ConvertToJson(ref context);
					}
				}

				appender.AppendNewLine(-1);
				appender.Append('}');
			}
			finally
			{
				context.Pop(m_Body!);
			}
		}

	}
}

