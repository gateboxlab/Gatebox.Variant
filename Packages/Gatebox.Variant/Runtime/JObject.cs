using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gatebox.Variant.Internal;


#nullable enable

namespace Gatebox.Variant
{

	public struct JObject : IDictionary<string, JValue>, IVariantConvertible
	{
		//==============================================================================
		// static members
		//==============================================================================

		public static JObject CreateWithCapacity( int capacity) => new JObject(capacity);

		internal static JObject CreateInternal(JObjectBody body) => new JObject(body);

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
		private JObject(JObjectBody body)
		{
			m_Body = body;
		}

		

		/// <summary>
		/// (private) キャパシティを指定しての生成。
		/// </summary>
		private JObject( int capacity)
		{
			m_Body = new JObjectBody(capacity:capacity);
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
		/// 追加。
		/// <para>
		/// すでに同じキーがある場合は AugumentException を投げます。
		/// この挙動が望ましくない場合は Set() を利用してください。</para>
		/// </summary>
		public void Add(string key, bool v) { AddInternal(key, new JVariant(v)); }
		public void Add(string key, long v) { AddInternal(key, new JVariant(v)); }
		public void Add(string key, double v) { AddInternal(key, new JVariant(v)); }
		public void Add(string key, string v) { AddInternal(key, new JVariant(v)); }
		public void Add(string key, JArray v) { AddInternal(key, new JVariant(v)); }
		public void Add(string key, JObject v) { AddInternal(key, new JVariant(v)); }
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
		/// </summary>
		public void Set(StringView key, bool value) { EnsureItem(key).Assign(value); }
		public void Set(StringView key, long value) { EnsureItem(key).Assign(value); }
		public void Set(StringView key, double value) { EnsureItem(key).Assign(value); }
		public void Set(StringView key, string value) { EnsureItem(key).Assign(value); }
		public void Set(StringView key, JArray value) { EnsureItem(key).Assign(value); }
		public void Set(StringView key, JObject value) { EnsureItem(key).Assign(value); }
		public void Set(StringView key, JVariant value) { EnsureItem(key).Assign(value); }



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

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// 要素削除
		/// </summary>
		public bool Remove(string key)
		{
			return m_Body?.Remove(key) ?? false;
		}
		public bool Remove(StringView key)
		{
			return m_Body?.Remove(key) ?? false;
		}

		/// <summary>
		/// 要素削除
		/// <para>ICollection の実装のためのものです。</para>
		/// </summary>
		bool ICollection<KeyValuePair<string, JValue>>.Remove(KeyValuePair<string, JValue> item)
		{
			return m_Body?.Remove(new StringView(item.Key)) ?? false;
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
			// TODO :　キーのすべてが int として解釈可能であれば、そのインデックスに各要素を詰めた配列を p に返す。
			p = default;
			return false;
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
			return new JVariant(this).ToString();
		}



		
		public JVariant AsVariant()
		{
			return new JVariant(this);
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
		private void AddInternal(string key, JVariant value)
		{
			// TODO : 実装
		}

		// Set の実装のため。確実に key の要素が存在するようにしてそれを返す。Setはそこに Assign すると無駄がない。
		private JValue EnsureItem(StringView key)
		{
			// TODO : 実装
			return new JValue();
		}

		
	}
}
