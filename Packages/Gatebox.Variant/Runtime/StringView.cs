using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Gatebox.Variant
{

	/// <summary>
	/// 文字列への参照と、その文字列内の位置を持つ不変な値型
	/// <para>
	/// 変化しない文字列の一部分を取り出して扱うためのものです。</para>
	/// <para>
	/// 一時的に「文字列のここからここまで」という情報がほしいときに、
	/// String.SubString 等を利用すると都度 String オブジェクトがアロケートされます。これは GCに余計な作業をさせることになります。</para>
	/// <para>
	/// そのような場合には ReadOnlySpan&lt;char&gt; を利用することもできますが、ReadOnlySpan には ref struct 由来の使いづらさがあるので、
	/// 通常の struct として扱う選択肢としてこの StringView があります。</para>
	/// <para>
	/// string から暗黙に変換することができ、ReadOnlySpan&lt;char&gt; へ暗黙に変換できます。
	/// そのため、個々の関数へのつなぎとしても利用することができるようになっています。</para>
	/// <para>
	/// あまり大きなスコープで StringView の変数を持たないようにしてください。
	/// 大きな文字列の小さな部分を保持しているとき、無駄におおきな文字列を保持し続けてしまいます。</para>
	/// </summary>
	public readonly struct StringView : IEquatable<StringView>, IComparable<StringView>, IComparable
	{
		//==============================================================================
		// operators
		//==============================================================================

		/// <summary>
		/// string からの暗黙生成。
		/// </summary>
		public static implicit operator StringView(string str)
		{
			return new StringView(str);
		}


		/// <summary>
		/// ReadOnlySpan への暗黙変換
		/// </summary>
		public static implicit operator ReadOnlySpan<char>(StringView view)
		{
			return view.AsSpan();
		}


		/// <summary>
		/// 比較
		/// </summary>
		public static bool operator ==(StringView s1, StringView s2)
		{
			return s1.Equals(s2);
		}

		public static bool operator !=(StringView s1, StringView s2)
		{
			return !s1.Equals(s2);
		}


		/// <summary>
		/// 大小比較
		/// </summary>
		public static bool operator <(StringView s1, StringView s2)
		{
			return s1.CompareTo(s2) < 0;
		}

		public static bool operator >(StringView s1, StringView s2)
		{
			return s1.CompareTo(s2) > 0;
		}

		public static bool operator <=(StringView s1, StringView s2)
		{
			return !(s1 > s2);
		}

		public static bool operator >=(StringView s1, StringView s2)
		{
			return !(s1 < s2);
		}


		/// <summary>
		/// bool への変換
		/// </summary>
		public static bool operator true(StringView v)
		{
			return !v.IsEmpty();
		}

		public static bool operator false(StringView v)
		{
			return v.IsEmpty();
		}

		public static bool operator !(StringView v)
		{
			return v.IsEmpty();
		}

		//==============================================================================
		// static members
		//==============================================================================

		// 文字が '0' - '9' ならば 0 - 9 を返す。それ以外では -1 を返す。
		private static int GetDigit(char ch)
		{
			return ch switch
			{
				>= '0' and <= '9' => ch - '0',
				_ => -1,
			};
		}

		// Trim 系で引数省略したときに利用される関数
		private static bool IsSpaceSimple(char ch)
		{
			return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r' || ch == '\v' || ch == '\f';
		}



		/// <summary>
		/// カルチャを一切無視してアスキーコードで見て文字を大文字化する。
		/// </summary>
		public static char ToUpperCase(char ch)
		{
			if (ch >= 'a' && ch <= 'z')
			{
				ch = (char)(ch - 'a' + 'A');
			}
			return ch;
		}

		/// <summary>
		/// カルチャを一切無視してアスキーコードで見て文字を小文字化する。
		/// </summary>
		public static char ToLowerCase(char ch)
		{
			if (ch >= 'A' && ch <= 'Z')
			{
				ch = (char)(ch - 'A' + 'a');
			}
			return ch;
		}

		public static bool IsNumber(char ch)
		{
			return ch >= '0' && ch <= '9';
		}
		public static bool IsAlphabet(char ch)
		{
			return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
		}
		public static bool IsAlphaNumeric(char ch)
		{
			return IsAlphabet(ch) || IsNumber(ch);
		}


		/// <summary>
		/// strtoll っぽいもの。ReadOnlySpanによる文字列を long として解釈。
		/// <para>
		/// ReadOnlySpan によって示される文字列を10進数であるとして解釈し、long で返します。
		/// 先頭には '+' '-' が許されます。また数字の並びの左側に 0 があることも許されます。(8進数解釈にはなりません)</para>
		/// <para>
		/// このメソッドは例外を <b>投げません。</b> 左から処理し、解釈不能になった時点でそこまでの解釈結果を返します。
		/// つまり、 +123XXX のような文字列にたいして呼んだ場合、 123 が返却されます。</para>
		/// <para>
		/// どこまで解釈できたのか last に文字位置でかえします。</para>
		/// <para>
		/// カルチャによる NumberFormatInfo などは一切加味しません。</para>
		/// </summary>
		public static (long value, int last) ParseInt64(ReadOnlySpan<char> span)
		{
			if (span.IsEmpty)
			{
				return (0, 0);
			}

			int i = 0;
			bool negate = false;

			// 最初に + と - が許される。
			char ch = span[0];
			if (ch == '+')
			{
				i = 1;
			}
			else if (ch == '-')
			{
				negate = true;
				i = 1;
			}

			// + だけ、 - だけ、という場合はここまで、失敗したので last は 0.
			// この判定により、次の文字が読めることは保証される
			if (i >= span.Length)
			{
				return (0, 0);
			}

			// パースできる最大値をとっておく、
			// limit より大きい時、次の桁を読むことはできず、
			// limit のときは次の桁は last_digit まで許される。
			long limit;
			int last_digit;

			if (negate)
			{
				// - 9223372036854775808
				limit = 922337203685477580L;
				last_digit = 8;
			}
			else
			{
				// 9223372036854775807
				limit = 922337203685477580L;
				last_digit = 7;
			}


			long value = 0;

			while (true)
			{
				// 次の文字を数字として
				int d = GetDigit(span[i]);

				// 数字じゃないっぽい。ここまで。
				if (d < 0)
				{
					return (negate ? -value : value, i);
				}

				// 最大値桁
				if (value > limit)
				{
					return (negate ? -value : value, i);
				}
				if (value == limit)
				{
					if (d > last_digit)
					{
						return (negate ? -value : value, i);
					}
				}

				// 10倍して一桁目として足す
				value *= 10;
				value += d;

				// ここまで読んだ
				++i;

				// 次の文字が読めないならここまで（成功）
				if (i >= span.Length)
				{
					return (negate ? -value : value, i);
				}
			}
		}


		/// <summary>
		/// strtol っぽいもの。ReadOnlySpanによる文字列を int として解釈。
		/// <para>
		/// ReadOnlySpan によって示される文字列を10進数であるとして解釈し、int で返します。
		/// 先頭には '+' '-' が許されます。また数字の並びの左側に 0 があることも許されます。(8進数解釈にはなりません)</para>
		/// <para>
		/// このメソッドは例外を <b>投げません。</b> 左から処理し、解釈不能になった時点でそこまでの解釈結果を返します。
		/// つまり、 +123XXX のような文字列にたいして呼んだ場合、 123 が返却されます。</para>
		/// <para>
		/// どこまで解釈できたのか last に文字位置でかえします。</para>
		/// <para>
		/// カルチャによる NumberFormatInfo などは一切加味しません。</para>
		/// </summary>
		public static (int value, int last) ParseInt32(ReadOnlySpan<char> span)
		{
			if (span.IsEmpty)
			{
				return (0, 0);
			}

			int i = 0;
			bool negate = false;

			// 最初に + と - が許される。
			char ch = span[0];
			if (ch == '+')
			{
				i = 1;
			}
			else if (ch == '-')
			{
				negate = true;
				i = 1;
			}

			// + だけ、 - だけ、という場合はここまで、失敗したので last は 0.
			// この判定により、次の文字が読めることは保証される
			if (i >= span.Length)
			{
				return (0, 0);
			}

			// パースできる最大値をとっておく、
			// limit より大きい時、次の桁を読むことはできず、
			// limit のときは次の桁は last_digit まで許される。
			int limit;
			int last_digit;

			if (negate)
			{
				// - 2147483648
				limit = 214748364;
				last_digit = 8;
			}
			else
			{
				// 2147483647
				limit = 214748364;
				last_digit = 7;
			}

			int value = 0;

			while (true)
			{
				// 次の文字を数字として
				int d = GetDigit(span[i]);

				// 数字じゃないっぽい。ここまで。
				if (d < 0)
				{
					return (negate ? -value : value, i);
				}

				// 最大値桁
				if (value > limit)
				{
					return (negate ? -value : value, i);
				}
				if (value == limit)
				{
					if (d > last_digit)
					{
						return (negate ? -value : value, i);
					}
				}


				// 10倍して一桁目として足す
				value *= 10;
				value += d;

				// ここまで読んだ
				++i;

				// 次の文字が読めないならここまで（成功）
				if (i >= span.Length)
				{
					return (negate ? -value : value, i);
				}
			}
		}

		/// <summary>
		/// strtod っぽいもの。ReadOnlySpanによる文字列を double として解釈。
		/// <para>
		/// ReadOnlySpan によって示される文字列を10進数であるとして解釈し、double で返します。
		/// 先頭には '+' '-' が許されます。</para>
		/// <para>
		/// このメソッドは例外を <b>投げません。</b> 左から処理し、解釈不能になった時点でそこまでの解釈結果を返します。
		/// つまり、 +123XXX のような文字列にたいして呼んだ場合、 123 が返却されます。</para>
		/// <para>
		/// どこまで解釈できたのか last に文字位置でかえします。</para>
		/// <para>
		/// カルチャによる NumberFormatInfo などは一切加味しません。</para>
		/// </summary>
		public static (double value, int last) ParseDouble(ReadOnlySpan<char> span)
		{
			if (span.IsEmpty)
			{
				return (0, 0);
			}

			int i = 0;
			bool negate = false;
			double value = 0.0;
			bool hasFraction = false;
			double factor = 0.1;

			// 最初に + と - が許される。
			var ch = span[0];
			if (ch == '+')
			{
				i = 1;
			}
			else if (ch == '-')
			{
				negate = true;
				i = 1;
			}

			if (i >= span.Length)
			{
				return (0, 0);
			}

			while (i < span.Length)
			{
				ch = span[i];
				if (ch >= '0' && ch <= '9')
				{
					if (hasFraction)
					{
						value += (ch - '0') * factor;
						factor *= 0.1;
					}
					else
					{
						value = value * 10 + (ch - '0');
					}
				}
				else if (ch == '.' && !hasFraction)
				{
					hasFraction = true;
				}
				else
				{
					break;
				}
				i++;
			}

			if (negate)
			{
				value = -value;
			}

			// Exponential part
			if (i < span.Length && (span[i] == 'e' || span[i] == 'E'))
			{
				i++;
				bool expNegate = false;
				if (i < span.Length && (span[i] == '-' || span[i] == '+'))
				{
					expNegate = span[i] == '-';
					i++;
				}

				int exponent = 0;
				while (i < span.Length && span[i] >= '0' && span[i] <= '9')
				{
					exponent = exponent * 10 + (span[i] - '0');
					i++;
				}

				if (expNegate)
				{
					exponent = -exponent;
				}

				value *= Math.Pow(10, exponent);
			}

			return (value, i);
		}



		//==============================================================================
		// instance members
		//==============================================================================

		private readonly string? m_Source;
		private readonly int m_Begin;
		private readonly int m_Length;


		/// <summary>
		/// コンストラクタ。
		/// <pre>
		/// 文字列全体を示す StringView を構築します。</pre>
		/// </summary>
		public StringView(string str)
		{
			m_Source = str;
			if (str is null)
			{
				m_Begin = 0;
				m_Length = 0;
			}
			else
			{
				m_Begin = 0;
				m_Length = str.Length;
			}
		}




		/// <summary>
		/// コンストラクタ。
		/// <para>
		/// もととなる文字列と、その文字列内の開始位置と終了位置を指定してその範囲を示す StringView を構築します。
		/// (第３引数は長さではなく終了位置(それを含まない)であるため注意してください。)</para>
		/// <para>
		/// 第３引数を省略した場合は最後までになります。</para>
		/// <para>
		/// 範囲外の begin を指定した場合、例外を投げるのではなく空文字列を示す StringView として構築されます。</para>
		/// </summary>
		public StringView(string str, int begin, int end = -1)
		{
			if (str is null || begin < 0 || begin >= str.Length)
			{
				m_Source = null;
				m_Begin = 0;
				m_Length = 0;
				return;
			}

			if (end < 0 || end >= str.Length)
			{
				end = str.Length;
			}

			if (end < begin)
			{
				end = begin;
			}

			m_Source = str;
			m_Begin = begin;
			m_Length = end - begin;
		}

		/// <summary>
		/// Range によるコンストラクタ
		/// </summary>
		public StringView(string? str, Range range)
		{
			if (str is null)
			{
				m_Source = null;
				m_Begin = 0;
				m_Length = 0;
				return;
			}

			int begin = range.Start.GetOffset(str.Length);
			int end = range.End.GetOffset(str.Length);

			if (begin < 0 || begin >= str.Length)
			{
				m_Source = null;
				m_Begin = 0;
				m_Length = 0;
				return;
			}

			if (end < 0 || end >= str.Length)
			{
				end = str.Length;
			}

			if (end < begin)
			{
				end = begin;
			}

			m_Source = str;
			m_Begin = begin;
			m_Length = end - begin;
		}


		/// <summary>
		/// もととなる文字列。 null であることがあるので注意してください。
		/// </summary>
		public readonly string? Original { get => m_Source; }

		/// <summary>
		/// 開始位置
		/// </summary>
		public readonly int Begin { get => m_Begin; }

		/// <summary>
		/// 長さ
		/// </summary>
		public readonly int Length { get => m_Length; }

		/// <summary>
		/// 終了位置（ここは含まない）
		/// </summary>
		public readonly int End => m_Begin + m_Length;

		/// <summary>
		/// インデクサ。
		/// <para>
		/// 指定位置の文字を返します。設定をすることはできません。</para>
		/// <para>
		/// インデックスは Length まで許容され、その場合は 0 が返却されます。
		/// 負の数あるいは Length より大きい数を指定した場合は未定義とします。
		/// (元のstring 内でたまたまそこにあった文字が返却されるか、例外を投げます。)</para>
		/// </summary>
		/// <exception cref="IndexOutOfRangeException" />
		public readonly char this[int i]
		{
			get
			{
				if (m_Source is null)
				{
					if (i != 0)
					{
						throw new IndexOutOfRangeException();
					}
					return '\0';
				}
				if (i >= m_Length)
				{
					return '\0';
				}

				return m_Source[m_Begin + i];
			}
		}


		/// <summary>
		/// ^ 表記インデクサ
		/// <para>
		/// [^1] で最後の文字。</para>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException" />
		public readonly char this[Index i]
		{
			get
			{
				if (m_Source is null)
				{
					throw new ArgumentOutOfRangeException();
				}
				return m_Source[m_Begin + i.GetOffset(m_Length)];
			}
		}

		/// <summary>
		/// Range 表記インデクサ
		/// <para>
		/// <see cref="View(Range)"/> と同じです。</para>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException" />
		public readonly StringView this[Range range]
		{
			get
			{
				if (m_Source is null)
				{
					throw new ArgumentOutOfRangeException();
				}
				(var begin, var length) = range.GetOffsetAndLength(m_Length);
				return new StringView(m_Source, (m_Begin + begin), (m_Begin + begin + length));
			}
		}


		/// <summary>
		/// 文字取得
		/// <pre>
		/// 指定位置の文字を返します。範囲外へのアクセスは char の 0 を返します。</pre>
		/// </summary>
		public readonly char At(int i)
		{
			if (i < 0 || i >= m_Length || m_Source is null)
			{
				return (char)0;
			}
			return m_Source[m_Begin + i];
		}


		

		/// <summary>
		/// string への変換。
		/// </summary>
		public readonly override string ToString()
		{
			if (IsEmpty())
			{
				return string.Empty;
			}
			if (m_Begin == 0 && m_Length == m_Source.Length)
			{
				return m_Source;
			}
			return m_Source.Substring(m_Begin, m_Length);
		}


		/// <summary>
		/// 等価性比較
		/// <para>
		/// 与えられた Object に対して、
		/// それが null であればこの StringView が ExceptEmpty であるかどうか、
		/// それが StringView であれば文字列として等しいかどうか、
		/// それ以外の場合は ToString の結果がこの StringView と文字列として等しいかどうかを返します。
		/// 文字列としての比較にカルチャは一切考慮しません。文字の並びとして全く等しい場合にのみ true です。</para>
		/// </summary>
		public readonly override bool Equals(object other)
		{
			if (other is null)
			{
				return IsEmpty();
			}

			if (other is StringView sv)
			{
				return Equals(sv);
			}

			return Equals(new StringView(other.ToString()));
		}


		/// <summary>
		/// 等価性比較
		/// <para>
		/// 与えられた StringView と文字列として等しいときに true を返します。
		/// カルチャは一切考慮しません。文字の並びとして全く等しい場合にのみ true です。</para>
		/// <para>
		/// IEquatable&lt;StringView&gt; の実装です。
		/// </para>
		/// </summary>
		public readonly bool Equals(StringView other)
		{
			return AsSpan().SequenceEqual(other.AsSpan());
		}

		/// <summary>
		/// 大文字小文字を同一視する比較
		/// <para>
		/// 与えられた StringView と大文字小文字を同一視して文字列の並びとして等しいとき true を返します。</para>
		/// <para>
		/// ここで言う「大文字小文字」にはカルチャの考慮は一切ありません。
		/// Unicode U+0041 から U+005A と U+0061 から U+007A を同一視して比較します。 
		/// </para>
		/// </summary>
		public readonly bool EqualsIgnoreCase(StringView other)
		{
			if (Length != other.Length)
			{
				return false;
			}

			var span1 = AsSpan();
			var span2 = other.AsSpan();

			for (int i = 0; i < Length; i++)
			{
				char c1 = span1[i];
				char c2 = span2[i];
				if (c1 >= 'a' && c1 <= 'z')
				{
					c1 = (char)(c1 - 'a' + 'A');
				}
				if (c2 >= 'a' && c2 <= 'z')
				{
					c2 = (char)(c2 - 'a' + 'A');
				}

				if (c1 != c2)
				{
					return false;
				}
			}
			return true;
		}


		/// <summary>
		/// ハッシュ値
		/// </summary>
		public readonly override int GetHashCode()
		{
			if (IsEmpty())
			{
				return 0;
			}
			unchecked
			{
				int hash = 17;
				foreach (var c in AsSpan())
				{
					hash = hash * 31 + c;
				}
				return hash;
			}
		}


		/// <summary>
		/// 大小比較
		/// <para>
		/// カルチャを一切無視して char の並びとして辞書順比較を行い、 1, 0 -1 のどれかを返します。
		/// </para>
		/// </summary>
		public readonly int CompareTo(StringView other)
		{
			for (int i = 0; i < Math.Min(Length, other.Length); i++)
			{
				char c1 = InternalAt(i);
				char c2 = other.InternalAt(i);

				if (c1 > c2)
				{
					return 1;
				}
				if (c1 < c2)
				{
					return -1;
				}
			}

			if (Length > other.Length)
			{
				return 1;
			}
			if (Length < other.Length)
			{
				return -1;
			}

			return 0;
		}


		/// <summary>
		/// 指定範囲のみの string の StringView を返す。
		/// <para>
		/// ToString() の結果から StringView を構築して返します。
		/// とても長い文字列の一部分だけを保持している場合に、それを他の箇所に渡すとメモリ上の無駄となる場合があります。
		/// そのような場合にこのメソッドによって明示的に部分文字列を作りながら、しかもそれを StringView として渡すことができます。</para>
		/// <para>
		/// StringView は string から暗黙に構築可能なのでほとんどの場合 ToString でことは足りますが、好みの問題です。</para>
		/// </summary>
		public readonly StringView Shrink()
		{
			return new StringView(ToString());
		}


		/// <summary>
		/// ReadOnlySpan に変換
		/// </summary>
		public readonly ReadOnlySpan<char> AsSpan()
		{
			if (IsEmpty())
			{
				return new Span<char>();
			}

			return m_Source.AsSpan(m_Begin, m_Length);
		}


		/// <summary>
		/// 文字の探索
		/// <pre>
		/// 先頭から探索して指定された文字があればそのインデックスを、なければ -1 を返します。</pre>
		/// </summary>
		/// <param name = "ch" >探す文字</ param >
		/// <param name="offset">開始位置</param>
		public readonly int IndexOf(char ch, int offset = 0)
		{
			if( IsEmpty() )
			{
				return -1;
			}

			for (int i = m_Begin + offset; i < m_Begin + m_Length; i++)
			{
				if (m_Source[i] == ch)
				{
					return i - m_Begin;
				}
			}
			return -1;
		}


		/// <summary>IndexOf() のシノニム</summary>
		public readonly int Find(char ch, int offset = 0)
		{
			return IndexOf(ch, offset);
		}


		/// <summary>
		/// 条件を満たす文字を検索。
		/// <para>
		/// 先頭から探索して個々の文字に対して pred が true を返す文字があればそのインデックスを、なければ -1 を返します。</pre>
		/// </summary>
		public readonly int Find(Predicate<char> pred, int offset = 0)
		{
			if (IsEmpty())
			{
				return -1;
			}

			for (int i = m_Begin + offset; i < m_Begin + m_Length; i++)
			{
				if (pred(m_Source[i]))
				{
					return i - m_Begin;
				}
			}
			return -1;
		}


		/// <summary>
		/// 部分文字列の探索
		/// <pre>
		/// 先頭から探索して部分文字列があればそのインデックスを、なければ -1 を返します。</pre>
		/// <pre>
		/// 探す文字が空文字列の場合は 0 を返します。</pre>
		/// </summary>
		/// <param name="s">探す文字列</param>
		/// <param name="offset">開始位置</param>
		public readonly int IndexOf(ReadOnlySpan<char> s, int offset = 0)
		{
			if (s.IsEmpty)
			{
				return 0;
			}

			int lastCandidate = m_Length - s.Length;

			for (int i = offset; i <= lastCandidate; i++)
			{
				bool ok = true;
				for (int j = 0; j < s.Length; j++)
				{
					if (InternalAt(i + j) != s[j])
					{
						ok = false;
						break;
					}
				}
				if (ok)
				{
					return i;
				}
			}

			return -1;
		}


		/// <summary>IndexOf() のシノニム</summary>
		public readonly int Find(ReadOnlySpan<char> s, int offset = 0)
		{
			return IndexOf(s, offset);
		}


		/// <summary>
		/// 指定された文字列で開始しているかどうか。
		/// <para>
		/// この StartsWith の先頭が引数 s と等しいとき true を返します。
		/// s が長さ 0 の文字列であるとき true が返却されるので注意してください。
		/// </para>
		public readonly bool StartsWith(StringView s)
		{
			if (Length < s.Length)
			{
				return false;
			}

			for (int i = 0; i < s.Length; i++)
			{
				if (InternalAt(i) != s.InternalAt(i))
				{
					return false;
				}
			}
			return true;
		}

		public bool StartsWithIgnoreCase(StringView s)
		{
			if (Length < s.Length)
			{
				return false;
			}

			var part = Slice(0, s.Length);
			return part.EqualsIgnoreCase(s);
		}



		/// <summary>
		/// 指定された文字列で終了しているか。
		/// <para>
		/// この StartsWith の末尾が引数 s と等しいとき true を返します。
		/// s が長さ 0 の文字列であるとき true が返却されるので注意してください。
		/// </para>
		/// </summary>
		public readonly bool EndsWith(StringView s)
		{
			if (s.IsEmpty())
			{
				return true;
			}

			int offset = m_Length - s.Length;
			if (offset < 0)
			{
				return false;
			}

			for (int i = 0; i < s.Length; i++)
			{
				if (InternalAt(offset + i) != s.InternalAt(i))
				{
					return false;
				}
			}
			return true;
		}


		/// <summary>
		/// 部分文字列を返す。
		/// <para>
		/// この StringView 内の m_Begin から end まで(endはふくまない)のStringViewを返します。
		/// end を省略した場合は最後までになります。(位置であって長さではないので注意してください)
		/// end がこの StringView の長さを超えていた場合も最後までの部分文字列を返します。</para>
		/// <para>
		/// m_Begin が 0 より小さい場合は空文字列を示す StringView を返します。</para>
		/// </summary>
		/// <param name="begin">開始位置</param>
		/// <param name="end">終了位置、省略時は最後まで</param>
		public readonly StringView Slice(int begin, int end = -1)
		{
			if (begin < 0)
			{
				return new StringView();
			}

			if (end < 0 || end > Length)
			{
				end = Length;
			}
			if (begin > end)
			{
				return new StringView();
			}

			return new StringView(m_Source, (begin + m_Begin)..(end + m_Begin));
		}


		/// <summary>
		/// 部分文字列を返す。
		/// <para>
		/// この StringView 内の m_Begin から m_Length 文字の StringView を返します。
		/// m_Length を省略した場合は最後までになります。
		/// m_Begin + m_Length がこの StringView の長さを超えていた場合も最後までの部分文字列を返します。</para>
		/// <para>
		/// m_Begin が 0 より小さい場合は空文字列を示す StringView を返します。</para>
		/// </summary>
		/// <param name="begin">開始位置</param>
		/// <param name="end">終了位置、省略時は最後まで</param>
		public readonly StringView SubView(int begin, int length = -1)
		{
			if (length < 0)
			{
				return Slice(begin, -1);
			}
			return Slice(begin, begin + length);
		}


		/// <summary>
		/// 自分自身をそのまま返す。
		/// <para>
		/// string 拡張メソッド View に対応するものです。
		/// </para>
		/// </summary>
		public readonly StringView View()
		{
			return this;
		}


		/// <summary>
		/// 範囲指定による部分文字列を返す。
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException" />
		public readonly StringView View(Range range)
		{
			var (begin, length) = range.GetOffsetAndLength(Length);
			return SubView(begin, length);
		}


		/// <summary>
		/// 先頭から空白を取り除いた StringView を返す。
		/// <para>
		/// ここで言う「空白」は ( string.Trim() とは異なり) 半角空白と改行、タブのことです。
		/// 全角空白は取り除かれないので注意してください。</para>
		/// <para>
		/// プログラム上で扱う多くのデータでは多バイト文字を使う場合は強い意図を持って行われていることが多く、
		/// 文章としての「空白」を扱うことはその現実に即していないことが多いと判断し、このような仕様にしています。
		/// (あえて全角空白を使うときはそれを「空白」として扱ってほしくないときが多い)</para>
		/// <para>
		/// string.Trim() とおなじ条件にするには 
		/// <c>TrimStart(Char.IsWhiteSpace)</c> としてください。Char.IsWhiteSpace は全角空白や U+200X 系の空白も取り除かれます。</para>
		/// </summary>
		public readonly StringView TrimStart()
		{
			int i;
			for (i = 0; i < m_Length; i++)
			{
				if (!IsSpaceSimple(InternalAt(i)))
				{
					break;
				}
			}
			return Slice(i);
		}


		/// <summary>
		/// 先頭から指定された文字を取り除いた StringView を返す。
		/// <para>
		/// 先頭から spaces に指定された文字以外の文字がはじめに出現する箇所以降を示す StringView を返します。
		/// </para>
		/// </summary>
		public readonly StringView TrimStart(char[] spaces)
		{
			return TrimStart((ch) => Array.IndexOf(spaces, ch) >= 0);
		}


		/// <summary>
		/// 先頭から指定された文字を取り除いた StringView を返す。
		/// <para>
		/// 先頭から pred が false を返す文字がはじめに出現する箇所以降を示す StringView を返します。</para>
		/// <para>
		/// 引数なし版の挙動が string と異なるため、 string と同等の挙動にする場合はこれに Char.IsWhiteSpace を与えて利用してください。</para>
		/// </summary>
		/// <param name="pred">条件。取り除かれるべきとき true を返してください。</param>
		public readonly StringView TrimStart(Predicate<char> pred)
		{
			int i;
			for (i = 0; i < m_Length; i++)
			{
				if (!pred(InternalAt(i)))
				{
					break;
				}
			}
			return Slice(i);
		}


		/// <summary>
		/// 末尾から空白を取り除いた StringView を返す。
		/// <para>
		/// ここで言う「空白」は ( string.Trim() とは異なり) 半角空白と改行、タブのことです。
		/// 全角空白は取り除かれないので注意してください。</para>
		/// <para>
		/// プログラム上で扱う多くのデータでは多バイト文字を使う場合は強い意図を持って行われていることが多く、
		/// 文章としての「空白」を扱うことはその現実に即していないことが多いと判断し、このような仕様にしています。
		/// (あえて全角空白を使うときはそれを「空白」として扱ってほしくないときが多い)</para>
		/// <para>
		/// string.Trim() とおなじ条件にするには 
		/// <c>TrimEnd(Char.IsWhiteSpace)</c> としてください。このコードは全角空白や U+200X 系の空白も取り除かれます。</para>
		/// </summary>
		public readonly StringView TrimEnd()
		{
			int i = m_Length;
			while (i > 0)
			{
				if (!IsSpaceSimple(InternalAt(i - 1)))
				{
					break;
				}
				--i;
			}
			return Slice(0, i);
		}


		/// <summary>
		/// 末尾から指定された文字を取り除いた StringView を返す。
		/// <para>
		/// 末尾から spaces に指定された文字以外の文字がはじめに出現する箇所を検索し、そこまでを示す StringView を返します。
		/// </para>
		/// </summary>
		public readonly StringView TrimEnd(char[] spaces)
		{
			return TrimEnd((ch) => Array.IndexOf(spaces, ch) >= 0);
		}


		/// <summary>
		/// 末尾から指定された文字を取り除いた StringView を返す。
		/// <para>
		/// 末尾から pred が false を返す文字がはじめに出現する箇所を検索し、そこまでを示す StringView を返します。</para>
		/// <para>
		/// 引数なし版の挙動が string と異なるため、 string と同等の挙動にする場合はこれに Char.IsWhiteSpace を与えて利用してください。</para>
		/// </summary>
		/// <param name="pred">条件。取り除かれるべきとき true を返してください。</param>
		public readonly StringView TrimEnd(Predicate<char> pred)
		{
			int i = m_Length;
			while (i > 0)
			{
				if (!pred(InternalAt(i - 1)))
				{
					break;
				}
				--i;
			}
			return Slice(0, i);
		}


		/// <summary>
		/// 前後から空白を取り除いた StringView を返す。
		/// <para>
		/// ここで言う「空白」は ( string.Trim() とは異なり) 半角空白と改行、タブのことです。
		/// 全角空白は取り除かれないので注意してください。</para>
		/// <para>
		/// プログラム上で扱う多くのデータでは多バイト文字を使う場合は強い意図を持って行われていることが多く、
		/// 文章としての「空白」を扱うことはその現実に即していないことが多いと判断し、このような仕様にしています。
		/// (あえて全角空白を使うときはそれを「空白」として扱ってほしくないときが多い)</para>
		/// <para>
		/// string.Trim() とおなじ条件にするには 
		/// <c>Trim(Char.IsWhiteSpace)</c> としてください。Char.IsWhiteSpace は全角空白や U+200X 系の空白も取り除かれます。</para>
		/// </summary>
		public readonly StringView Trim()
		{
			return TrimStart().TrimEnd();
		}


		/// <summary>
		/// 前後から指定された文字を取り除いた StringView を返す。
		/// <para>
		/// 前後から spaces に指定された文字以外の文字がはじめに出現する箇所をそれぞれ検索し、その範囲を示す StringView を返します。
		/// </para>
		/// </summary>
		public readonly StringView Trim(char[] spaces)
		{
			return Trim((ch) => Array.IndexOf(spaces, ch) >= 0);
		}


		/// <summary>
		/// 前後から指定された文字を取り除いた StringView を返す。
		/// <para>
		/// 前後から pred が false を返す文字がはじめに出現する箇所をそれぞれ検索し、その範囲を示す StringView を返します。</para>
		/// <para>
		/// 引数なし版の挙動が string と異なるため、 string と同等の挙動にする場合はこれに Char.IsWhiteSpace を与えて利用してください。</para>
		/// </summary>
		/// <param name="pred">条件。取り除かれるべきとき true を返してください。</param>
		public readonly StringView Trim(Predicate<char> pred)
		{
			return TrimStart(pred).TrimEnd(pred);
		}


		/// <summary>
		/// 空文字列であるか空白のみからなるとき true.
		/// <para>
		/// Trim() をした結果が空であるかどうかと同じです。
		/// </para>
		/// </summary>
		public readonly bool IsBlank()
		{
			for (int i = 0; i < m_Length; i++)
			{
				if (!IsSpaceSimple(InternalAt(i)))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// 長さが 0 のとき true
		/// </summary>
		[MemberNotNullWhen(false, nameof(m_Source))]
		public readonly bool IsEmpty()
		{
			return Length == 0;
		}

		/// <summary>
		/// 長さが 0 でないとき true
		/// </summary>
		[MemberNotNullWhen(true, nameof(m_Source))]
		public readonly bool HasContent()
		{
			return Length > 0;
		}


		/// <summary>
		/// 特定の文字を探し、その前後を Tuple で返す。
		/// <para>
		/// 見つかった文字はどちらにも含まれません。見つからなかったときは Item1 に自分自身を、 Item2 に空文字列をもつ Tuple を返します。
		/// </para>
		/// </summary>
		public readonly (StringView Head, StringView Tail) Divide(char sep)
		{
			int f = Find(sep);
			if (f < 0)
			{
				return (this, new StringView());
			}
			return (Slice(0, f), Slice(f + 1));
		}


		/// <summary>
		/// 条件を満たす文字を探し、その前後を Tuple で返す。
		/// <para>
		/// 見つかった文字はどちらにも含まれません。見つからなかったときは Item1 に自分自身を、 Item2 に空文字列をもつ Tuple を返します。
		/// </para>
		/// </summary>
		public readonly (StringView Head, StringView Tail) Divide(Predicate<char> pred)
		{
			int f = Find(pred);
			if (f < 0)
			{
				return (this, new StringView());
			}
			return (Slice(0, f), Slice(f + 1));
		}


		/// <summary>
		/// 文字列を指定された文字で分割して List で返す。
		/// <para>
		/// 先頭から引数 sep が現れる箇所で分割してそれぞれを List に格納して返します。</para>
		/// <para>
		/// String.Split がサポートしている文字列による分割や、最大件数などには対応していません。
		/// 単純に文字で（最後まで）分割します。</para>
		/// <para>
		/// String.Split の引数は ParamArrayAttribute 引数（可変長引数）ですが、このメソッドは char を単体で受けます。
		/// 複数の文字で分割する場合は char[] を受けるバージョンを利用してください。
		/// </para>
		/// </summary>
		/// <param name="sep">分割する区切り文字</param>
		/// <param name="toRemoveEmpty">空の要素を取り除くか、省略時は false(取り除かない)</param>
		public readonly List<StringView> Split(char sep, bool toRemoveEmpty = false)
		{
			List<StringView> ret = new List<StringView>();
			Split(sep, ret, toRemoveEmpty);
			return ret;
		}


		/// <summary>
		/// 文字列を指定された文字で分割して List で返す。
		/// <para>
		/// 先頭から引数 separators に含まれる文字現れる箇所で分割してそれぞれを List に格納して返します。</para>
		/// <para>
		/// String.Split がサポートしている文字列による分割や、最大件数などには対応していません。
		/// 単純に文字で（最後まで）分割します。</para>
		/// <para>
		/// String.Split は null を渡すと空白で分割するという裏技的な仕様がありますが、そのようなことは対応していません。
		/// 同様のことがしたい場合は（普通に考えてそんな要求ないと思いますけど）
		/// Predicate&lt;char&gt; を受けるバージョンに Char.IsWhiteSpace を与えて利用してください。</para>
		/// </summary>
		/// <param name="separators">分割する区切り文字</param>
		/// <param name="toRemoveEmpty">空の要素を取り除くか、省略時は false(取り除かない)</param>
		/// <returns></returns>
		public readonly List<StringView> Split(char[] separators, bool toRemoveEmpty = false)
		{
			List<StringView> ret = new List<StringView>();
			Split(separators, ret, toRemoveEmpty);
			return ret;
		}


		/// <summary>
		/// 文字列を指定された文字で分割する。
		/// <para>
		/// char と bool を受けるバージョンと同様ですが、結果を戻り値ではなく引数 ret に格納して返します。</para>
		/// <para>
		/// 引数として受けた ret は Clear() しない（そのまま Add する）ので注意してください。
		/// （利用者がこのバージョンの Split を呼ぶ前に Clear() するようにしてください、
		/// 　逆にあえて Clear() せず、複数の分割を一つのListにマージするような利用も可能です。）
		/// </para>
		/// </summary>
		/// <param name="sep">分割する区切り文字</param>
		/// <param name="ret">結果格納先</param>
		/// <param name="toRemoveEmpty">空の要素を取り除くか、省略時は false(取り除かない)</param>
		public readonly void Split(char sep, ICollection<StringView> ret, bool toRemoveEmpty = false)
		{
			var rest = this;
			while (true)
			{
				var (v1, v2) = rest.Divide(sep);

				// 見つからなかった場合
				if (v1 == rest && v2.IsEmpty())
				{
					if (!toRemoveEmpty || !v1.IsEmpty())
					{
						ret.Add(v1);
					}
					break;
				}

				if (!toRemoveEmpty || !v1.IsEmpty())
				{
					ret.Add(v1);
				}
				rest = v2;
			}
		}


		/// <summary>
		/// 文字列を指定された文字で分割する。
		/// <para>
		/// char[] と bool を受けるバージョンと同様ですが、結果を戻り値ではなく引数 ret に格納して返します。</para>
		/// <para>
		/// 引数として受けた ret は Clear() しない（そのまま Add する）ので注意してください。
		/// （利用者がこのバージョンの Split を呼ぶ前に Clear() するようにしてください、
		/// 　逆にあえて Clear() せず、複数の分割を一つのListにマージするような利用も可能です。）
		/// </para>
		/// </summary>
		/// <param name="separators">分割する区切り文字</param>
		/// <param name="ret">結果格納先</param>
		/// <param name="toRemoveEmpty">空の要素を取り除くか、省略時は false(取り除かない)</param>
		public readonly void Split(char[] separators, ICollection<StringView> ret, bool toRemoveEmpty = false)
		{
			Split((ch) => Array.IndexOf(separators, ch) >= 0, ret, toRemoveEmpty);
		}


		/// <summary>
		/// 文字列を指定された条件を満たす文字で分割する。
		/// <para>
		/// 先頭から個々の文字について pred を呼び、それが true を返す箇所で分割してそれぞれを引数 ret に格納して返します。</para>
		/// <para>
		/// 引数として受けた ret は Clear() しない（そのまま Add する）ので注意してください。
		/// （利用者がこのバージョンの Split を呼ぶ前に Clear() するようにしてください、
		/// 　逆にあえて Clear() せず、複数の分割を一つのListにマージするような利用も可能です。）</para>
		/// </summary>
		/// <param name="pred">分割する区切り文字を判定する predicate. 分割されるべきとき true を返してください</param>
		/// <param name="ret">結果格納先</param>
		/// <param name="toRemoveEmpty">空の要素を取り除くか、省略時は false(取り除かない)</param>
		public readonly void Split(Predicate<char> pred, ICollection<StringView> ret, bool toRemoveEmpty = false)
		{
			var rest = this;
			while (true)
			{
				var (v1, v2) = rest.Divide(pred);
				if (!toRemoveEmpty || !v1.IsEmpty())
				{
					ret.Add(v1);
				}

				if (v2.IsEmpty())
				{
					break;
				}
				rest = v2;
			}
		}


		/// <summary>
		/// int として解釈。
		/// <para>
		/// この StringView が持っている文字列を10進数であるとして解釈し、int で返します。
		/// 先頭には '+' '-' が許されます。また数字の並びの左側に 0 があることも許されます。(8進数解釈にはなりません)</para>
		/// <para>
		/// このメソッドは例外を <b>投げません。</b> 左から処理し、解釈不能になった時点でそこまでの解釈結果を返します。
		/// つまり、 +123XXX のような文字列にたいして呼んだ場合、 123 が返却されます。</para>
		/// <para>
		/// このため、全く解釈できなかったのか 0 だったのかの判定はできません。それが必要な場合は TryParseInt() を利用してください。</para>
		/// <para>
		/// カルチャによる NumberFormatInfo などは一切加味しません。</para>
		/// </summary>
		public readonly int ParseInt()
		{
			if (IsEmpty())
			{
				return 0;
			}
			return ParseInt32(AsSpan()).value;
		}


		/// <summary>
		/// int として解釈。
		/// <para>
		/// この StringView が持っている文字列を10進数であるとして解釈し引数 v に返します。
		/// 先頭には '+' '-' が許されます。また数字の並びの左側に 0 があることも許されます。(8進数解釈にはなりません)</para>
		/// <para>
		/// StringView 全体が数字の並びとして有効だった場合には true. そうではない場合は false を返却します。
		/// false を返却した場合にも引数 v にはそこまでの解釈の結果が格納されます。</para>
		/// <para>
		/// カルチャによる NumberFormatInfo などは一切加味しません。</para>
		/// </summary>
		public readonly bool TryParseInt(out int v)
		{
			if (IsEmpty())
			{
				v = 0;
				return false;
			}
			var (value, last) = ParseInt32(AsSpan());
			v = value;
			return last == Length;
		}

		/// <summary>
		/// int として解釈、解釈できない場合は def を返す。
		/// </summary>
		public readonly int ParseIntOrDefault(int def = 0)
		{
			return TryParseInt(out int v) ? v : def;
		}


		/// <summary>
		/// long として解釈。
		/// <para>
		/// この StringView が持っている文字列を10進数であるとして解釈し、long で返します。
		/// 先頭には '+' '-' が許されます。また数字の並びの左側に 0 があることも許されます。(8進数解釈にはなりません)</para>
		/// <para>
		/// このメソッドは例外を <b>投げません。</b> 左から処理し、解釈不能になった時点でそこまでの解釈結果を返します。
		/// つまり、 +123XXX のような文字列にたいして呼んだ場合、 123 が返却されます。</para>
		/// <para>
		/// このため、全く解釈できなかったのか 0 だったのかの判定はできません。それが必要な場合は TryParseLong() を利用してください。</para>
		/// <para>
		/// カルチャによる NumberFormatInfo などは一切加味しません。</para>
		/// </summary>
		public readonly long ParseLong()
		{
			if (IsEmpty())
			{
				return 0;
			}
			return ParseInt64(AsSpan()).value;
		}


		/// <summary>
		/// long として解釈。
		/// <para>
		/// この StringView が持っている文字列を10進数であるとして解釈し引数 v に返します。
		/// 先頭には '+' '-' が許されます。また数字の並びの左側に 0 があることも許されます。(8進数解釈にはなりません)</para>
		/// <para>
		/// StringView 全体が数字の並びとして有効だった場合には true. そうではない場合は false を返却します。
		/// false を返却した場合にも引数 v にはそこまでの解釈の結果が格納されます。
		/// <para>
		/// カルチャによる NumberFormatInfo などは一切加味しません。</para>
		/// </summary>
		public readonly bool TryParseLong(out long v)
		{
			if (IsEmpty())
			{
				v = 0;
				return false;
			}
			var (value, last) = ParseInt64(AsSpan());
			v = value;
			return last == Length;
		}

		/// <summary>
		/// long として解釈、解釈できない場合は def を返す。
		/// </summary>
		public readonly long ParseLongOrDefault(long def = 0)
		{
			return TryParseLong(out long v) ? v : def;
		}


		public readonly double ParseDouble()
		{
			if (IsEmpty())
			{
				return 0;
			}
			return ParseDouble(AsSpan()).value;
		}

		public readonly bool TryParseDouble(out double v)
		{
			if (IsEmpty())
			{
				v = 0;
				return false;
			}
			var (value, last) = ParseDouble(AsSpan());
			v = value;
			return last == Length;
		}

		/// <summary>
		/// カルチャを無視してアスキーコードで大文字化して string にして返す。
		/// </summary>
		public readonly string ToUpperString()
		{
			if (IsEmpty())
			{
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder(Length);
			for (int i = 0; i < Length; ++i)
			{
				sb.Append(ToUpperCase(InternalAt(i)));
			}
			return sb.ToString();
		}

		/// <summary>
		/// カルチャを無視してアスキーコードで小文字化して string にして返す。
		/// </summary>
		public readonly string ToLowerString()
		{
			if (IsEmpty())
			{
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder(Length);
			for (int i = 0; i < Length; ++i)
			{
				sb.Append(ToLowerCase(InternalAt(i)));
			}
			return sb.ToString();
		}


		/// <summary>
		/// 特定の文字の出現回数を返す。
		/// </summary>
		public readonly int Count(char ch)
		{
			int r = 0;
			for (int i = 0; i < Length; i++)
			{
				if (ch == InternalAt(i))
				{
					r += 1;
				}
			}
			return r;
		}

		/// <summary>
		/// アルファベットか数字で構成されているとき true.
		/// <para>
		/// 空文字列でも true になるので注意してください。</para>
		/// </summary>
		public readonly bool IsAlphaNumeric()
		{
			for (int i = 0; i < Length; i++)
			{
				if (!IsAlphaNumeric(InternalAt(i)))
				{
					return false;
				}
			}
			return true;
		}



		/// <summary>
		/// 大小比較(非ジェネリック版)
		/// <para>
		/// 与えられた object について、
		/// それが null である場合は、この StringView が ExceptEmpty である場合に等しく、それ以外ではこちらのほうが大きいとみなした結果を
		/// StringView である場合は文字同士の大小比較を
		/// それ以外の場合は ToString() の結果に対して文字同士の大小比較を返します。
		/// </para>
		/// </summary>
		int IComparable.CompareTo(object obj)
		{
			if (obj is null)
			{
				return IsEmpty() ? 0 : 1;
			}
			if (obj is StringView sv)
			{
				return CompareTo(sv);
			}
			return CompareTo(new StringView(obj.ToString()));
		}


		// At の内部用。 m_Source が null ではないこと、範囲外にアクセスしないことを前提とする。
		// inline 化してくれるといいなぁ指定。
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private readonly char InternalAt(int i)
		{
			// これにしても string.this[int] は余計な範囲チェック入ってるから無駄がある。
			return m_Source![i + m_Begin];
		}
	}
}
