using System;


namespace Gatebox.Variant
{


	/// <summary>
	/// UTF-8 のバイト列を文字列として扱うための構造体
	/// <para>
	/// 内部には byte[] を保持し、その範囲を示すビューとして機能します。
	/// readonly struct ではありますが、byte[] 自体は外部で変更されることがあるため、意味的にはこの構造体は immutable ではありません。
	/// 利用する場合にはもととなった byte[] を外部で変更しないように注意してください。
	/// </para>
	/// </summary>
	public readonly struct U8View : IEquatable<U8View>, IComparable<U8View>, IComparable
	{
		//==============================================================================
		// operators
		//==============================================================================

		/// <summary>
		/// ReadOnlySpan への暗黙変換
		/// </summary>
		public static implicit operator ReadOnlySpan<byte>(U8View view)
		{
			return view.AsSpan();
		}

		/// byte[] からの暗黙変換
		/// </summary>
		public static implicit operator U8View(byte[] bytes)
		{
			if (bytes == null)
			{
				return default;
			}

			return new U8View(bytes);
		}


		/// <summary>
		/// 比較
		/// </summary>
		public static bool operator ==(U8View s1, U8View s2)
		{
			return s1.Equals(s2);
		}

		public static bool operator !=(U8View s1, U8View s2)
		{
			return !s1.Equals(s2);
		}

		/// <summary>
		/// 大小比較
		/// </summary>
		public static bool operator <(U8View s1, U8View s2)
		{
			return s1.CompareTo(s2) < 0;
		}

		public static bool operator >(U8View s1, U8View s2)
		{
			return s1.CompareTo(s2) > 0;
		}

		public static bool operator <=(U8View s1, U8View s2)
		{
			return !(s1 > s2);
		}

		public static bool operator >=(U8View s1, U8View s2)
		{
			return !(s1 < s2);
		}


		/// <summary>
		/// bool への変換
		/// </summary>

		public static bool operator true(U8View view)
		{
			return !view.IsEmpty();
		}

		public static bool operator false(U8View view)
		{
			return view.IsEmpty();
		}

		public static bool operator !(U8View v)
		{
			return v.IsEmpty();
		}


		//==============================================================================
		// static members
		//==============================================================================

		/// <summary>
		/// string から U8View を生成。
		/// <para>
		/// string の内容を byte[] に変換し、それを持つ U8View を返します。</para>
		/// </summary>
		public static U8View Create(string str)
		{
			if (str == null)
			{
				return default;
			}

			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
			return new U8View(bytes);
		}

		// 文字が '0' - '9' ならば 0 - 9 を返す。それ以外では -1 を返す。
		private static int GetDigit(byte ch)
		{
			return ch switch
			{
				>= (byte)'0' and <= (byte)'9' => (ch - '0'),
				_ => -1,
			};
		}

		// Trim 系で引数省略したときに利用される関数
		private static bool IsSpaceSimple(byte ch)
		{
			return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r' || ch == '\v' || ch == '\f';
		}

		/// <summary>
		/// アスキーコードで見て文字を大文字化する。
		/// </summary>
		public static byte ToUpperCase(byte ch)
		{
			if (ch >= 'a' && ch <= 'z')
			{
				ch = (byte)(ch - 'a' + 'A');
			}
			return ch;
		}

		/// <summary>
		/// カルチャを一切無視してアスキーコードで見て文字を小文字化する。
		/// </summary>
		public static byte ToLowerCase(byte ch)
		{
			if (ch >= 'A' && ch <= 'Z')
			{
				ch = (byte)(ch - 'A' + 'a');
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
		public static (long value, int last) ParseInt64(ReadOnlySpan<byte> span)
		{
			if (span.IsEmpty)
			{
				return (0, 0);
			}

			int i = 0;
			bool negate = false;

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
		public static (int value, int last) ParseInt32(ReadOnlySpan<byte> span)
		{
			if (span.IsEmpty)
			{
				return (0, 0);
			}

			int i = 0;
			bool negate = false;

			// 最初に + と - が許される。
			byte ch = span[0];
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
		/// ReadOnlySpan によって示される文字列を10進数であるとして解釈し、int で返します。
		/// 先頭には '+' '-' が許されます。</para>
		/// <para>
		/// このメソッドは例外を <b>投げません。</b> 左から処理し、解釈不能になった時点でそこまでの解釈結果を返します。
		/// つまり、 +123XXX のような文字列にたいして呼んだ場合、 123 が返却されます。</para>
		/// <para>
		/// どこまで解釈できたのか last に文字位置でかえします。</para>
		/// <para>
		/// カルチャによる NumberFormatInfo などは一切加味しません。</para>
		/// </summary>
		public static (double value, int last) ParseDouble(ReadOnlySpan<byte> span)
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


		private readonly byte[] m_Bytes;
		private readonly int m_Begin;
		private readonly int m_Length;


		/// <summary>
		/// byte[] からのコンストラクタ
		/// </summary>
		public U8View(byte[] bytes)
		{
			bytes ??= Array.Empty<byte>();

			m_Bytes = bytes;
			m_Begin = 0;
			m_Length = bytes.Length;
		}

		/// <summary>
		/// 開始位置と長さによるコンストラクタ
		/// <para>
		/// もととなる文字列と、その文字列内の開始位置と終了位置を指定してその範囲を示す U8View を構築します。
		/// (第３引数は長さではなく終了位置(それを含まない)であるため注意してください。)</para>
		/// <para>
		/// 範囲外の位置を指定した場合、例外ではなく空の範囲を示す U8View が返されます。</para>
		/// </summary>
		public U8View(byte[] bytes, int begin, int end = -1)
		{
			if (bytes == null || begin < 0 || begin >= bytes.Length)
			{
				m_Bytes = null;
				m_Begin = 0;
				m_Length = 0;
				return;
			}

			if (end < 0 || end > bytes.Length)
			{
				end = bytes.Length;
			}
			if (end < begin)
			{
				end = begin;
			}

			m_Bytes = bytes;
			m_Begin = begin;
			m_Length = end - begin;
		}

		/// <summary>
		/// Range によるコンストラクタ
		/// <para>
		/// 範囲外の位置を指定した場合、例外ではなく空の範囲を示す U8View が返されます。</para>
		/// </summary>
		public U8View(byte[] bytes, Range range)
		{
			if (bytes == null)
			{
				m_Bytes = null;
				m_Begin = 0;
				m_Length = 0;
				return;
			}

			int begin = range.Start.GetOffset(bytes.Length);
			int end = range.End.GetOffset(bytes.Length);

			if (begin < 0 || begin >= bytes.Length)
			{
				m_Bytes = null;
				m_Begin = 0;
				m_Length = 0;
				return;
			}

			if (end < 0 || end > bytes.Length)
			{
				end = bytes.Length;
			}
			if (end < begin)
			{
				end = begin;
			}

			m_Bytes = bytes;
			m_Begin = begin;
			m_Length = end - begin;
		}

		/// <summary>
		/// もととなる byte[]. null である可能性があります。
		/// </summary>
		public readonly byte[] Original => m_Bytes;

		/// <summary>
		/// 開始位置
		/// </summary>
		public readonly int Begin => m_Begin;

		/// <summary>
		/// 長さ
		/// </summary>
		public readonly int Length => m_Length;

		/// <summary>
		/// 終了位置（ここは含まない）
		/// </summary>
		public readonly int End => m_Begin + m_Length;

		/// <summary>
		/// インデクサ。読み取り専用です。
		/// <para>
		/// インデックスは Length(末尾+1) まで許容され、その場合は 0 が返却されます。</para>
		/// <para>
		/// i が負の値であるか、Length より大きい値である場合は IndexOutOfRangeException が投げられます。</para>
		/// </para>
		/// </summary>
		/// <exception cref="IndexOutOfRangeException"/>
		public readonly byte this[int i]
		{
			get
			{
				if (m_Bytes == null)
				{
					if (i == 0)
					{
						return 0;
					}
					throw new IndexOutOfRangeException();
				}
				if (i >= m_Length)
				{
					return 0;
				}

				if (i < 0 || i > m_Length)
				{
					throw new IndexOutOfRangeException();
				}
				return m_Bytes[m_Begin + i];
			}
		}

		/// <summary>
		/// インデクサ。読み取り専用です。
		/// <para>
		/// [^1] で最後のバイトを返します。</para>
		/// </summary>
		/// <exception cref="IndexOutOfRangeException" />
		public readonly byte this[Index index]
		{
			get
			{
				if (m_Bytes == null)
				{
					throw new IndexOutOfRangeException();
				}
				return m_Bytes[m_Begin + index.GetOffset(m_Length)];
			}
		}



		/// <summary>
		/// Range インデクサ
		/// </summary>
		public readonly U8View this[Range range]
		{
			get
			{
				if (m_Bytes == null)
				{
					return default;
				}
				var (begin, length) = range.GetOffsetAndLength(m_Length);
				return new U8View(m_Bytes, m_Begin + begin, m_Begin + begin + length);
			}
		}

		/// <summary>
		/// バイト取得
		/// <pre>
		/// 指定位置の文字を返します。範囲外へのアクセスは 0 を返します。</pre>
		/// </summary>
		public readonly byte At(int i)
		{
			if (i < 0 || i >= m_Length || m_Bytes is null)
			{
				return 0;
			}
			return m_Bytes[m_Begin + i];
		}

	

		/// <summary>
		/// ReadOnlySpan を返す。
		/// </summary>
		public readonly ReadOnlySpan<byte> AsSpan()
		{
			if (IsEmpty())
			{
				return ReadOnlySpan<byte>.Empty;
			}
			return new ReadOnlySpan<byte>(m_Bytes, m_Begin, m_Length);
		}

		/// <summary>
		/// ReadOnlyMemory を返す。
		/// </summary>
		public readonly ReadOnlyMemory<byte> AsMemory()
		{
			if (IsEmpty())
			{
				return Memory<byte>.Empty;
			}
			return new Memory<byte>(m_Bytes, m_Begin, m_Length);
		}


		/// <summary>
		/// 文字列化
		/// </summary>
		public readonly override string ToString()
		{
			if(m_Bytes == null)
			{
				return string.Empty;
			}
			return System.Text.Encoding.UTF8.GetString(m_Bytes, m_Begin, m_Length);
		}


		/// <summary>
		/// 比較
		/// </summary>
		public readonly bool Equals(U8View other)
		{
			return AsSpan().SequenceEqual(other.AsSpan());
		}

		/// <summary>
		/// 比較
		/// </summary>
		public readonly override bool Equals(object obj)
		{
			if (obj is U8View view)
			{
				return Equals(view);
			}
			return false;
		}

		/// <summary>
		/// 大文字小文字を同一視する比較
		/// <para>
		/// 与えられた U8View と大文字小文字を同一視して文字列の並びとして等しいとき true を返します。</para>
		/// <para>
		/// ここで言う「大文字小文字」にはカルチャの考慮は一切ありません。
		/// Unicode U+0041 から U+005A と U+0061 から U+007A を同一視して比較します。 
		/// </para>
		/// </summary>
		public readonly bool EqualsIgnoreCase(U8View other)
		{
			if (Length != other.Length)
			{
				return false;
			}

			for (int i = 0; i < Length; i++)
			{
				byte c1 = this[i];
				byte c2 = other[i];

				if (c1 >= 'a' && c1 <= 'z')
				{
					c1 = (byte)(c1 - 'a' + 'A');
				}
				if (c2 >= 'a' && c2 <= 'z')
				{
					c2 = (byte)(c2 - 'a' + 'A');
				}
				if (c1 != c2)
				{
					return false;
				}
			}
			return true;
		}


		/// <summary>
		/// ハッシュコード
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
		/// カルチャ等を一切無視して辞書順比較
		/// </summary>
		public readonly int CompareTo(U8View other)
		{
			for (int i = 0; i < Math.Min(Length, other.Length); i++)
			{
				var a = this[i];
				var b = other[i];
				if (a < b)
				{
					return -1;
				}
				if (a > b)
				{
					return 1;
				}
			}

			if (Length < other.Length)
			{
				return -1;
			}
			if (Length > other.Length)
			{
				return 1;
			}

			return 0;
		}

		/// <summary>
		/// 指定範囲のみの byte[] を持った U8View を返す。
		/// </summary>
		public readonly U8View Shrink()
		{
			if (IsEmpty() || this.Length == m_Bytes.Length)
			{
				return this;
			}

			byte[] newBytes = new byte[m_Length];
			Array.Copy(m_Bytes, m_Begin, newBytes, 0, m_Length);
			return new U8View(newBytes);
		}



		/// <summary>
		/// 大小比較(非ジェネリック版)
		/// </summary>
		int IComparable.CompareTo(object obj)
		{
			if (obj == null)
			{
				return IsEmpty() ? 0 : 1;
			}

			if (obj is U8View view)
			{
				return CompareTo(view);
			}
			throw new ArgumentException($"cannot compare a {obj.GetType()} to {nameof(U8View)}");
		}


		/// <summary>
		/// 文字の探索
		/// <pre>
		/// 先頭から探索して指定された文字があればそのインデックスを、なければ -1 を返します。</pre>
		/// </summary>
		public readonly int Find(byte ch, int offset = 0)
		{
			for (int i = offset; i < Length; i++)
			{
				if (this[i] == ch)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// 条件を満たす文字を検索。
		/// <para>
		/// 先頭から探索して個々の文字に対して pred が true を返す文字があればそのインデックスを、なければ -1 を返します。</pre>
		/// </summary>
		public readonly int Find(Predicate<byte> pred, int offset = 0)
		{
			for (int i = offset; i < Length; i++)
			{
				if (pred(this[i]))
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// 部分を返す。
		/// <para>
		/// この U8View 内の m_Begin から m_Length 文字の U8View を返します。
		/// m_Length を省略した場合は最後までになります。
		/// m_Begin + m_Length がこの U8View の長さを超えていた場合も最後までの部分文字列を返します。</para>
		/// <para>
		/// m_Begin が 0 より小さい場合は空文字列を示す U8View を返します。</para>
		/// </summary>
		/// <param name="begin">開始位置</param>
		/// <param name="end">終了位置、省略時は最後まで</param>
		public readonly U8View SubView(int begin, int length = -1)
		{
			if (length < 0)
			{
				return Slice(begin, -1);
			}
			return Slice(begin, begin + length);
		}

		/// <summary>
		/// 自分自身をそのまま返す。
		/// </summary>
		public readonly U8View View()
		{
			return this;
		}

		/// <summary>
		/// 範囲指定による部分文字列を返す。
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException" />
		public readonly U8View View(Range range)
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
		public readonly U8View TrimStart()
		{
			int i;
			for (i = 0; i < m_Length; i++)
			{
				if (!IsSpaceSimple(At(i)))
				{
					break;
				}
			}
			return Slice(i);
		}

		/// <summary>
		/// 先頭から指定された文字を取り除いた U8View を返す。
		/// <para>
		/// 先頭から spaces に指定された文字以外の文字がはじめに出現する箇所以降を示す U8View を返します。
		/// </para>
		/// </summary>
		public readonly U8View TrimStart(byte[] spaces)
		{
			return TrimStart((ch) => Array.IndexOf(spaces, ch) >= 0);
		}

		/// <summary>
		/// 先頭から指定された文字を取り除いた U8View を返す。
		/// </summary>
		/// <param name="pred">条件。取り除かれるべきとき true を返してください。</param>
		public readonly U8View TrimStart(Predicate<byte> pred)
		{
			int i;
			for (i = 0; i < m_Length; i++)
			{
				if (!pred(At(i)))
				{
					break;
				}
			}
			return Slice(i);
		}

		/// <summary>
		/// 末尾から空白を取り除いた U8View を返す。
		/// <para>
		/// ここで言う「空白」は ( string.Trim() とは異なり) 半角空白と改行、タブのことです。</para>
		/// </summary>
		public readonly U8View TrimEnd()
		{
			int i = m_Length;
			while (i > 0)
			{
				if (!IsSpaceSimple(At(i - 1)))
				{
					break;
				}
				--i;
			}
			return Slice(0, i);
		}

		/// <summary>
		/// 末尾から指定された文字を取り除いた U8View を返す。
		/// <para>
		/// 末尾から spaces に指定された文字以外の文字がはじめに出現する箇所を検索し、そこまでを示す U8View を返します。
		/// </para>
		/// </summary>
		public readonly U8View TrimEnd(byte[] spaces)
		{
			return TrimEnd((ch) => Array.IndexOf(spaces, ch) >= 0);
		}

		/// <summary>
		/// 末尾から指定された文字を取り除いた StringView を返す。
		/// </summary>
		/// <param name="pred">条件。取り除かれるべきとき true を返してください。</param>
		public readonly U8View TrimEnd(Predicate<byte> pred)
		{
			int i = m_Length;
			while (i > 0)
			{
				if (!pred(At(i - 1)))
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
		/// ここで言う「空白」は ( string.Trim() とは異なり) 半角空白と改行、タブのことです。</para>
		/// </summary>
		public readonly U8View Trim()
		{
			return TrimStart().TrimEnd();
		}

		/// <summary>
		/// 前後から指定された文字を取り除いた StringView を返す。
		/// <para>
		/// 前後から spaces に指定された文字以外の文字がはじめに出現する箇所をそれぞれ検索し、その範囲を示す StringView を返します。
		/// </para>
		/// </summary>
		public readonly U8View Trim(byte[] spaces)
		{
			return Trim((ch) => Array.IndexOf(spaces, ch) >= 0);
		}

		/// <summary>
		/// 前後から指定された文字を取り除いた StringView を返す。
		/// </summary>
		/// <param name="pred">条件。取り除かれるべきとき true を返してください。</param>
		public readonly U8View Trim(Predicate<byte> pred)
		{
			return TrimStart(pred).TrimEnd(pred);
		}


		/// <summary>
		/// なにもない時 true.
		/// </summary>
		public readonly bool IsEmpty() => m_Length == 0;

		/// <summary>
		/// 長さが 0 でないとき true
		/// </summary>
		public readonly bool HasContent()
		{
			return Length > 0;
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
				if (!IsSpaceSimple(At(i)))
				{
					return false;
				}
			}
			return true;
		}


		/// <summary>
		/// 特定の文字を探し、その前後を Tuple で返す。
		/// <para>
		/// 見つかった文字はどちらにも含まれません。見つからなかったときは Item1 に自分自身を、 Item2 に空文字列をもつ Tuple を返します。
		/// </para>
		/// </summary>
		public readonly (U8View Head, U8View Tail) Divide(byte sep)
		{
			int f = Find(sep);
			if (f < 0)
			{
				return (this, new U8View());
			}
			return (Slice(0, f), Slice(f + 1));
		}

		/// <summary>
		/// 条件を満たす文字を探し、その前後を Tuple で返す。
		/// <para>
		/// 見つかった文字はどちらにも含まれません。見つからなかったときは Item1 に自分自身を、 Item2 に空文字列をもつ Tuple を返します。
		/// </para>
		/// </summary>
		public readonly (U8View Head, U8View Tail) Divide(Predicate<byte> pred)
		{
			int f = Find(pred);
			if (f < 0)
			{
				return (this, new U8View());
			}
			return (Slice(0, f), Slice(f + 1));
		}




		/// <summary>
		/// int として解釈。
		/// <para>
		/// この U8View が持っている文字列を10進数であるとして解釈し、int で返します。
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
		/// この U8View が持っている文字列を10進数であるとして解釈し引数 v に返します。
		/// 先頭には '+' '-' が許されます。また数字の並びの左側に 0 があることも許されます。(8進数解釈にはなりません)</para>
		/// <para>
		/// U8View 全体が数字の並びとして有効だった場合には true. そうではない場合は false を返却します。
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
		/// この U8View が持っている文字列を10進数であるとして解釈し、long で返します。
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
		/// この U8View が持っている文字列を10進数であるとして解釈し引数 v に返します。
		/// 先頭には '+' '-' が許されます。また数字の並びの左側に 0 があることも許されます。(8進数解釈にはなりません)</para>
		/// <para>
		/// U8View 全体が数字の並びとして有効だった場合には true. そうではない場合は false を返却します。
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
		/// 部分文字列を返す。
		/// <para>
		/// この StringView 内の m_Begin から end まで(end はふくまない)の U8View を返します。
		/// end を省略した場合は最後までになります。(位置であって長さではないので注意してください)
		/// end がこの U8View の長さを超えていた場合も最後までの部分文字列を返します。</para>
		/// <para>
		/// m_Begin が 0 より小さい場合は空文字列を示す U8View を返します。</para>
		/// </summary>
		/// <param name="begin">開始位置</param>
		/// <param name="end">終了位置、省略時は最後まで</param>
		public readonly U8View Slice(int begin, int end = -1)
		{
			if (begin < 0)
			{
				return new U8View();
			}

			if (end < 0 || end > Length)
			{
				end = Length;
			}
			if (begin > end)
			{
				return new U8View();
			}

			return new U8View(m_Bytes, (begin + m_Begin), (end + m_Begin));
		}

	}
}
