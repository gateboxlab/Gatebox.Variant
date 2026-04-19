using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Gatebox.Variant.Extensions;



#nullable enable

namespace Gatebox.Variant.Internal
{

	/// <summary>
	/// JObject の中身 string-JValue の Dictionary
	/// <para>
	/// 通常は JObject を利用してください。
	/// </para>
	/// </summary>
	public class JObjectBody : IDictionary<string, JValue>
	{
		//==============================================================================
		// inner types
		//==============================================================================

		// １件分
		public readonly struct Entity
		{
			public Entity(string k, JValue v) => (Key, Value) = (k, v);
			public readonly string Key;
			public readonly JValue Value;
		}

		// GetEnumerator, Keys, Values の実装で利用
		private abstract class PartEnumeratorBase : IEnumerator
		{
			protected int m_Cursor;
			protected JObjectBody m_Target;

			public PartEnumeratorBase(JObjectBody target)
			{
				// Enumerator は Current の前に MoveNext するから最初は -1
				m_Target = target;
				m_Cursor = -1;
			}

			object IEnumerator.Current => m_Target.GetByIndex(m_Cursor);

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				if (m_Cursor < m_Target.Count)
				{
					++m_Cursor;
				}
				return m_Cursor != m_Target.Count;
			}

			public void Reset()
			{
				m_Cursor = -1;
			}
		}

		// FlatMap<KEY, VALUE> に対する Enumerator
		// GetEnumerator で利用。
		private class Enumerator : PartEnumeratorBase, IEnumerator<KeyValuePair<string, JValue>>
		{
			public Enumerator(JObjectBody target) : base(target) { }
			public KeyValuePair<string, JValue> Current => m_Target.GetByIndex(m_Cursor);
		}

		// Keys の実装用
		private class KeyCollection : ICollection<string>
		{
			private class KeyEnumerator : PartEnumeratorBase, IEnumerator<string>
			{
				public KeyEnumerator(JObjectBody target) : base(target) { }
				public string Current => m_Target.GetKeyAt(m_Cursor);
			}

			private readonly JObjectBody m_Target;

			public KeyCollection(JObjectBody target) => m_Target = target;

			public int Count => m_Target.Count;
			public bool IsReadOnly => true;

			public void Add(string item) => throw new NotSupportedException();
			public void Clear() => throw new NotSupportedException();
			public bool Remove(string item) => throw new NotSupportedException();

			public bool Contains(string key) => m_Target.ContainsKey(key);
			public IEnumerator<string> GetEnumerator() => new KeyEnumerator(m_Target);
			IEnumerator IEnumerable.GetEnumerator() => new KeyEnumerator(m_Target);

			public void CopyTo(string[] array, int index)
			{
				for (int i = 0; i < Count; ++i)
				{
					array[index + i] = m_Target.GetKeyAt(i);
				}
			}
		}

		// Values の実装用
		private class ValueCollection : ICollection<JValue>
		{
			private class ValueEnumerator : PartEnumeratorBase, IEnumerator<JValue>
			{
				public ValueEnumerator(JObjectBody target) : base(target) { }
				public JValue Current => m_Target.GetValueAt(m_Cursor);
			}

			private readonly JObjectBody m_Target;

			public ValueCollection(JObjectBody target) => m_Target = target;

			public int Count => m_Target.Count;
			public bool IsReadOnly => true;

			public void Add(JValue item) => throw new NotSupportedException();
			public void Clear() => throw new NotSupportedException();
			public bool Remove(JValue item) => throw new NotSupportedException();

			public bool Contains(JValue v) => m_Target.ContainsValue(v);
			public IEnumerator<JValue> GetEnumerator() => new ValueEnumerator(m_Target);
			IEnumerator IEnumerable.GetEnumerator() => new ValueEnumerator(m_Target);

			public void CopyTo(JValue[] array, int index)
			{
				for (int i = 0; i < Count; ++i)
				{
					array[index + i] = m_Target.GetValueAt(i);
				}
			}
		}

		private class KeyComparator : IComparer<Entity>
		{
			public int Compare(Entity x, Entity y) => string.CompareOrdinal(x.Key, y.Key);
		}

		//==============================================================================
		// instance members
		//==============================================================================

		public const int DefaultCapacity = 8;

		private static readonly KeyComparator s_KeyComparator = new KeyComparator();

		private Entity[] m_Body;
		private int m_Count;


		/// <summary>
		/// コンストラクタ
		/// <para>
		/// キャパシティを指定して初期化する。
		/// JObject 自身が JObjectBody を生成するかどうかを判断しているため、
		/// JObjectBody が生成されて時点では必ず中身はあるということにする。
		/// </para>
		/// </summary>
		public JObjectBody(int capacity = DefaultCapacity)
		{
			Debug.Assert(capacity > 0);
			m_Body = Allocate(capacity);
			m_Count = 0;
		}

		/// <summary>
		/// 要素数
		/// </summary>
		public int Count => m_Count;

		/// <summary>
		/// 容量
		/// <para>
		/// アロケーションなしで追加できる Count の最大値を返します。</para>
		/// <para>
		/// 設定は <see cref="Reserve(int)"/> を利用してください。</para>
		/// </summary>
		public int Capacity => m_Body.Length;


		/// <summary>
		/// Key のコレクションを返す。
		/// <para>
		/// 返却される Collection への本体の内容が変化した後のアクセスは保証されません。</para>
		/// </summary>
		public ICollection<string> Keys => new KeyCollection(this);

		/// <summary>
		/// Value のコレクションを返す。
		/// <para>
		/// 返却される Collection への本体の内容が変化した後のアクセスは保証されません。</para>
		/// </summary>
		public ICollection<JValue> Values => new ValueCollection(this);


		/// <summary>
		/// ReadOnly には対応しません。
		/// </summary>
		public bool IsReadOnly => false;


		/// <summary>
		/// インデクサ
		/// <para>
		/// 指定されたキーの値を取得または設定します。</para>
		/// <para>
		/// 存在しないキーを取得しようとした場合は KeyNotFoundException を投げます。
		/// (新しい要素を追加してそれを返すのではないので注意してください。)</para>
		/// </summary>
		public JValue this[string key]
		{
			get => this[key.View()];
			set => this[key.View()] = value;
		}

		public JValue this[StringView key]
		{
			get
			{
				int i = Search(key);
				if (i >= 0)
				{
					return m_Body[i].Value;
				}
				throw new KeyNotFoundException(key.ToString());
			}

			set
			{
				// 見つかった場合はそこの Value を置き換えるだけ
				int i = Search(key);
				if (i >= 0)
				{
					m_Body[i] = new Entity(key.ToString(), value);
					return;
				}

				Append(~i, key, value);
			}
		}

		/// <summary>
		/// IDictionary からの代入
		/// </summary>
		public void Assign(IDictionary<string, JValue> dict)
		{
			if (dict == null)
			{
				throw new ArgumentNullException(nameof(dict));
			}

			Reserve(dict.Count);
			m_Count = dict.Count;

			if (Count == 0)
			{
				return;
			}

			// 順番は気にせず一旦入れてしまってからソートする。
			int i = 0;
			foreach (var p in dict)
			{
				m_Body[i] = new Entity(p.Key, p.Value);
				++i;
			}

			Array.Sort(m_Body, 0, Count, s_KeyComparator);
		}

		/// <summary>
		/// JObjectBody からの代入
		/// </summary>
		public void Assign(JObjectBody other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other));
			}

			Reserve(other.Count);
			m_Count = other.Count;

			if (Count == 0)
			{
				return;
			}

			Array.Copy(other.m_Body, m_Body, Count);
		}

		/// <summary>
		/// 容量の確保。
		/// <para>
		/// 要素を確保します。追加が予想される場合などに予め呼んでおくことで効率が高くなります。</para>
		/// <para>
		/// このメソッドの呼出によって容量が小さくなることはありません。
		/// 余計なメモリを開放したいという意図では、
		/// <see cref="Capacity">Capacity</see> への設定か
		/// <see cref="TrimExcess()">TrimExcess()</see> を利用してください。</para>
		/// </summary>
		public void Reserve(int capacity)
		{
			if (m_Body.Length > capacity)
			{
				return;
			}

			var newbie = Allocate(capacity);
			Array.Copy(m_Body, 0, newbie, 0, Count);
			m_Body = newbie;
		}

		/// <summary>
		/// 配列として要素へのアクセス。
		/// <para>
		/// 配列上の指定インデックスの項目を KEY と VALUE のタプルで返します。</para>
		/// <para>
		/// 配列上の指定インデックスの値を返します。
		/// 範囲チェックは行っておらず、そのまま配列にアクセスしますが、
		/// Count 以上のインデックスの指定時の挙動は未定義とします。</para>
		/// </summary>
		public (string, JValue) GetAt(int i) => (GetKeyAt(i), GetValueAt(i));


		/// <summary>
		/// 配列として Key へのアクセス。
		/// <para>
		/// 配列上の指定インデックスのキーを返します。
		/// 範囲チェックは行っておらず、そのまま配列にアクセスしますが、
		/// Count 以上のインデックスの指定時の挙動は未定義とします。</para>
		/// </summary>
		public string GetKeyAt(int index) => m_Body[index].Key;

		/// <summary>
		/// 配列として Value へのアクセス。
		/// <para>
		/// 配列上の指定インデックスの値を返します。
		/// 範囲チェックは行っておらず、そのまま配列にアクセスしますが、
		/// Count 以上のインデックスの指定時の挙動は未定義とします。</para>
		/// </summary>
		public JValue GetValueAt(int index) => m_Body[index].Value;

		/// <summary>
		/// 配列として要素へのアクセス。
		/// <para>
		/// 配列上の指定インデックスの項目の VALUE を設定します。</para>
		/// </summary>
		public void SetValueAt(int i, JValue v) => m_Body[i] = new Entity(GetKeyAt(i), v);

		

		/// <summary>
		/// 要素追加
		/// <para>
		/// 指定されたキーの値を追加します。</para>
		/// <para>
		/// すでに同じキーが存在している場合 ArgumentException を投げます。</para>
		/// </summary>
		public void Add(string key, JValue value)
		{
			int i = Search(key);
			if (i >= 0)
			{
				throw new ArgumentException($"Key Duplicated ({key})");
			}

			Append(~i, key, value);
		}

		/// <summary>
		/// 要素追加
		/// <para>
		/// ２引数を受けるバージョンと同じです。
		/// IDictionary が KeyValuePair の Collection としての動作を要求するために存在しているものです。</para>
		/// <para>
		/// すでに同じキーが存在している場合 ArgumentException を投げます。</para>
		/// </summary>
		public void Add(KeyValuePair<string, JValue> item) => Add(item.Key, item.Value);

		/// <summary>
		/// 指定されたキーの値が存在するか返す。
		/// </summary>
		public bool ContainsKey(string key)
		{
			return (Search(key) >= 0);
		}
		public bool ContainsKey(StringView key)
		{
			return (Search(key) >= 0);
		}


		/// <summary>
		/// 指定されたキーの項目を削除する。
		/// <para>
		/// 引数 key で指定されたキーの項目があればそれを削除して true を返します。
		/// なければ false を返します。
		/// </para>
		/// </summary>
		public bool Remove(StringView key)
		{
			int i = Search(key);
			if (i < 0)
			{
				return false;
			}
			return RemoveAt(i);
		}

		public bool Remove(string key)
		{
			return Remove(key.View());
		}

		/// <summary>
		/// 指定 index の項目を削除する。
		/// <para>
		/// 引数 index が配列の範囲内であればそれを削除して true を返します。
		/// 範囲外であれば false を返します。
		/// </para>
		public bool RemoveAt(int i)
		{
			if (i < 0 || i >= Count)
			{
				return false;
			}

			if (i != (Count - 1))
			{
				Array.Copy(m_Body, i + 1, m_Body, i, Count - 1 - i);
			}

			m_Body[Count - 1] = default(Entity);
			--m_Count;

			return true;
		}

		/// <summary>
		/// 指定されたキーの値があれば取得
		/// <para>
		/// key で示されるキーの値があれば引数 value に代入して true を返します。
		/// なければ false を返します。
		/// </para>
		/// </summary>
#pragma warning disable CS8767
		public bool TryGetValue(string key, [MaybeNullWhen(false)] out JValue value)
		{
			int i = Search(key);
			if (i < 0)
			{
				value = default;
				return false;
			}
			value = m_Body[i].Value;
			return true;
		}
#pragma warning restore CS8767

		/// <summary>
		/// 指定されたキーの値があればそれを、なければ　null を返す。
		/// </summary>
		public JValue? GetOrDefault( string key )
		{
			int i = Search(key);
			if (i < 0)
			{
				return null;
			}
			return m_Body[i].Value;
		}

		/// <summary>
		/// 項目のクリア
		/// <para>
		/// カウントを 0 にするだけです。内部の配列には内容が残り続け、消えるタイミングは上書きされるときです。
		/// ガベージコレクタによって早期に回収されてほしい場合これは問題になることがあります。
		/// 非常に大きな JSON 片を扱う場合には注意が必要です。</para>
		/// <para>
		/// どうしても参照を消してほしい場合は <see cref="TrimExcess()">TrimExcess()</see> が利用できますが、
		/// 内部も削除してほしいのであればオブジェクト自体を作り直したほうが早いかと思われます。
		/// </para>
		/// </summary>
		public void Clear()
		{
			m_Count = 0;
		}

		/// <summary>
		/// <see cref="IEnumerable{T}" /> の実装
		/// </summary>
		public IEnumerator<KeyValuePair<string, JValue>> GetEnumerator()
		{
			return new Enumerator(this);
		}

		/// <summary>
		/// 要素の数に合わせて内部の配列を作り直す。
		/// <para>
		/// 現在の生きている要素の数に合わせて配列を切り詰めます。</para>
		/// <para>
		/// 要素が増えるときには継続して追加されることを期待して少し大きめに領域を確保します。
		/// そのため、これ以上増えないということがわかった時点でこのメソッドを呼ぶことで、メモリを開放することができます。
		/// また、要素数が減った時に、減った場所に入っているObjectを開放しません。（単にアクセスしなくなるだけです）
		/// それが困る場合には TrimExcess() で参照を断ち切ることができます。
		/// </para>
		/// </summary>
		public void TrimExcess()
		{
			if (Count == Capacity)
			{
				return;
			}

			var newbie = Allocate(Count);
			if (Count != 0)
			{
				Array.Copy(m_Body, 0, newbie, 0, Count);
			}
			m_Body = newbie;
		}

		/// <summary>
		/// キーを検索しそのインデックスを返す。
		/// <para>
		/// 見つからないときは -1 を返します。
		/// </para>
		/// </summary>
		public int Find(StringView key)
		{
			var i = Search(key);
			return i < 0 ? -1 : i;
		}

		// IEnumerable の実装
		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

	
		// KeyValuePair として値が存在しているか返す。
		// KEY と VALUE の両方が等しいときのみ true を返します。ICollection の実装のためのものです。
		bool ICollection<KeyValuePair<string, JValue>>.Contains(KeyValuePair<string, JValue> item)
		{
			int i = Search(item.Key);
			if (i < 0)
			{
				return false;
			}
			var v = m_Body[i].Value;
			return EqualityComparer<JValue>.Default.Equals(v, item.Value);
		}

		// KeyValuePair として値を削除。
		// ICollection の実装のため。
		bool ICollection<KeyValuePair<string, JValue>>.Remove(KeyValuePair<string, JValue> item)
		{
			int i = Search(item.Key);
			if (i < 0)
			{
				return false;
			}
			if (!EqualityComparer<JValue>.Default.Equals(m_Body[i].Value, item.Value))
			{
				return false;
			}

			return RemoveAt(i);
		}

		// KeyValuePair へのコピー。
		// ICollection の実装のため。
		void ICollection<KeyValuePair<string, JValue>>.CopyTo(KeyValuePair<string, JValue>[] array, int arrayIndex)
		{
			for (int i = 0; i < Count; ++i)
			{
				array[i + arrayIndex] = new KeyValuePair<string, JValue>(m_Body[i].Key, m_Body[i].Value);
			}
		}
		

	

		// 指定されたキーのインデックスを返す。
		// 見つかった場合はそのインデックスを返し、
		// 見つからなかった場合は挿入すべき位置のインデックスの補数 (~index) を返す。
		private int Search(StringView k)
		{
			// 十分に小さい時は線形探索
			if( m_Count < 5)
			{
				var span = k.AsSpan();
				for (int i = 0;i < Count; ++i) 
				{
					var v = span.SequenceCompareTo(m_Body[i].Key.AsSpan());
					if (v == 0)
					{
						return i;
					}
					if (v < 0)
					{
						return ~i;
					}
				}
			}

			return Array.BinarySearch(m_Body, 0, Count, new Entity(k.ToString(), default!), s_KeyComparator);
		}


		// cap 以上の適当な容量を確保する。
		private void EnsureCapacity(int cap)
		{
			if (cap <= m_Body.Length)
			{
				return;
			}

			cap = Math.Max(cap, DefaultCapacity);
			Reserve(Math.Max( cap, m_Body.Length * 2));
		}

		private void Append(int i, StringView key, JValue value)
		{
			// 一つ増やすから確保
			EnsureCapacity(Count + 1);

			// i 以降をひとつづつずらす
			if (i != Count)
			{
				Array.Copy(m_Body, i, m_Body, i + 1, Count - i);
			}

			// i に入れる
			m_Body[i] = new Entity(key.ToString(), value);
			m_Count += 1;
		}


	

		/// KeyValuePair として配列上の指定インデックスの値を返す。
		private KeyValuePair<string, JValue> GetByIndex(int i)
		{
			if (i < 0 || i >= Count)
			{
				throw new IndexOutOfRangeException();
			}
			return new KeyValuePair<string, JValue>(m_Body[i].Key, m_Body[i].Value);
		}

		// 指定された値が存在するか返す。
		private bool ContainsValue(JValue v)
		{
			for (int i = 0; i < Count; ++i)
			{
				if (EqualityComparer<JValue>.Default.Equals(m_Body[i].Value, v))
				{
					return true;
				}
			}
			return false;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Entity[] Allocate(int size) => new Entity[size];


	}
}
