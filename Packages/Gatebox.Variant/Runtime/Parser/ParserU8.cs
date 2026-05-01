using System;
using System.Collections.Generic;
using System.Text;
using Gatebox.Variant.Internal;

#nullable enable

namespace Gatebox.Variant.Parser
{
	public readonly struct ParserU8
	{
		//==============================================================================
		// inner types
		//==============================================================================

		/// <summary>
		/// U8View を１文字づつ読みすすめていくもの。
		/// <para>
		/// 変更可能な struct なので注意</para>
		/// </summary>
		internal struct Reader
		{
			private Position m_Position;

			public Reader(U8View s)
			{
				Source = s;
				m_Position = new Position();
			}

			/// <summary>
			/// 位置
			/// <para>
			/// ミュータブルな struct なので注意。
			/// （返却される一時オブジェクトを変更しても意味はない。）</para>
			/// <para>
			/// 設定は <see cref="SetPosition(Position)"/> で。 </para>
			/// </summary>
			public readonly Position Position => m_Position;

			/// <summary>
			/// 元の文字列
			/// </summary>
			public readonly U8View Source { get; }

			/// <summary>
			/// 終わっているか
			/// <para>
			/// 末尾 +1 を指しているか？</para>
			/// </summary>
			public readonly bool IsEnd => (m_Position.Index >= Source.Length);

			/// <summary>
			/// 今の文字
			/// </summary>
			public readonly int Current => Source[m_Position.Index];

			/// <summary>
			/// 次の文字
			/// <para>
			/// StringView の仕様として、末尾 +1 へのアクセスは許されていて、 0 が返却される。 </para>
			/// </summary>
			public readonly int Next => Source[m_Position.Index + 1];


			public readonly U8View PeekAhead(int begin, int end = -1)
			{
				return Source.Slice(m_Position.Index + begin, m_Position.Index + end);
			}

			public readonly U8View Peek(int begin, int end = -1)
			{
				return Source.Slice(begin, end);
			}

			// 進める
			public void Advance()
			{
				var ch = Current;

				if (ch == '\n')
				{
					m_Position.Return();
				}
				else if (ch != 0)
				{
					m_Position.Increment();
				}
			}

			public void Advance(int n)
			{
				for (int i = 0; i < n; i++)
				{
					Advance();
				}
			}

			public void SetPosition(Position pos)
			{
				m_Position = pos;
			}
		}

		//==============================================================================
		// instance members
		//==============================================================================



		private readonly IStringCache m_StringCache;


		/// <summary>
		/// コンストラクタ
		/// </summary>
		public ParserU8(IStringCache cache)
		{
			m_StringCache = cache;
		}

		/// <summary>
		/// パース
		/// </summary>
		public JVariant Parse(U8View source)
		{
			var reader = new Reader(source);
			SkipTrivial(ref reader);

			// なにもない？
			if (reader.IsEnd)
			{
				throw Fail(reader.Position, "empty json.");
			}

			var ret = ParseVariant(ref reader);

			SkipTrivial(ref reader);

			if (!reader.IsEnd)
			{
				throw Fail(reader.Position, "unexpected token.");
			}

			return ret;
		}


		// 値のパース
		private JValue ParseVariant(ref Reader reader)
		{
			var ret = new JValue();

			Position starts = reader.Position;

			var ch = reader.Current;

			// オブジェクト
			if (ch == '{')
			{
				return ParseObject(ref reader);
			}

			// 配列
			if (ch == '[')
			{
				return ParseArray(ref reader);
			}

			// 文字列
			if (ch == '\'' || ch == '\"')
			{
				return ParseString(ref reader, ch);
			}

			// トークンっぽい部分を読み込む
			var pos = reader.Position;
			ReadToken(ref reader);
			var token = reader.Source.Slice(pos.Index, reader.Position.Index);

			if (token == Literal.Null.U8)
			{
				return new JVariant();
			}

			if (token == Literal.True.U8)
			{
				ret.Assign(true);
				return new JVariant(true);
			}

			if (token == Literal.False.U8)
			{
				return new JVariant(false);
			}

			if (token.TryParseLong(out long l))
			{
				return new JVariant(l);
			}

			if (token == Literal.NaN.U8)
			{
				return new JVariant(double.NaN);
			}

			if (token == Literal.Infinity.U8)
			{
				return new JVariant(double.PositiveInfinity);
			}
			if (token == Literal.NegativeInfinity.U8)
			{
				return new JVariant(double.NegativeInfinity);
			}
			if (token.TryParseDouble(out double d))
			{
				return new JVariant(d);
			}

			if (token.IsEmpty())
			{
				throw Fail(pos, $"invalid token. [{reader.Current}]");
			}
			else
			{
				throw Fail(pos, $"invalid token. [{token}]");
			}
		}


		private JObject ParseObject(ref Reader reader)
		{
			var starts = reader.Position;

			// ローカル関数。（読み飛ばし中にコメントが終わらない、というパターンがあるので…）
			void Skip(ref Reader r)
			{
				SkipTrivial(ref r);
				if (r.IsEnd)
				{
					throw Fail(starts, "unclosed object.");
				}
			}

			var ret = new JObject();

			// { の分進める
			reader.Advance();

			while (true)
			{
				Skip(ref reader);

				// {}
				var ch = reader.Current;
				if (ch == '}')
				{
					reader.Advance();
					break;
				}


				// キー
				string key;

				if (ch == '\"' || ch == '\'')
				{
					key = ParseString(ref reader, (char)ch);
				}
				else
				{
					var pos = reader.Position;
					ReadToken(ref reader);
					var token = reader.Source.Slice(pos.Index, reader.Position.Index);

					if (token.IsEmpty())
					{
						throw Fail(pos, "object syntax error. object key expected.");
					}

					key = m_StringCache.GetString(token);
				}

				// キーの次は : 
				Skip(ref reader);

				ch = reader.Current;
				reader.Advance();

				if (ch != ':')
				{
					throw Fail(reader.Position, "object syntax error. ':' expected.");
				}

				// value のパース
				Skip(ref reader);

				JVariant value = ParseVariant(ref reader);

				// 格納
				ret.Set(key, value);

				Skip(ref reader);

				// } か ,
				ch = reader.Current;
				reader.Advance();

				if (ch == ',')
				{
					continue;
				}

				if (ch == '}')
				{
					break;
				}

				throw Fail(reader.Position, "object syntax error. '}'  or  ',' expected.");
			}

			return ret;
		}


		// array のパース
		private JArray ParseArray(ref Reader reader)
		{
			var starts = reader.Position;

			// ローカル関数。（読み飛ばし中にコメントが終わらない、というパターンがあるので…）
			void Skip(ref Reader r)
			{
				SkipTrivial(ref r);
				if (r.IsEnd)
				{
					throw Fail(starts, "unclosed array.");
				}
			}

			reader.Advance();
			var ret = new JArray();

			while (true)
			{
				Skip(ref reader);

				var ch = reader.Current;
				if (ch == ']')
				{
					break;
				}

				// value (ネストはここから再帰する)
				JVariant value = ParseVariant(ref reader);

				ret.Add(value);

				Skip(ref reader);

				ch = reader.Current;

				if (ch == ']')
				{
					break;
				}

				if (ch == ',')
				{
					reader.Advance();
					continue;
				}

				throw Fail(reader.Position, "array syntax error. ']'  or  ',' expected.");
			}

			// reader は ] を指している
			reader.Advance();

			return ret;
		}


		// 文字列パース
		private string ParseString(ref Reader reader, int delimiter)
		{
			var starts = reader.Position;

			// 最初の ' " を読み飛ばす。
			reader.Advance();

			// 最初から１文字ずつ進むのと、範囲を決めてから中を処理するのと判断しづらいが、後者で行く。
			// エスケープが含まれている場合無駄が生じるが、エスケープシーケンスが含まれていることは少ない、という判断。

			int begin = reader.Position.Index;
			Position escaped = Position.Invalid;
			while (true)
			{
				var ch = reader.Current;

				if (ch == 0)
				{
					throw Fail(starts, "unterminated string.");
				}

				if (ch == delimiter)
				{
					break;
				}

				if (ch == '\\')
				{
					if (escaped.IsInvalid())
					{
						escaped = reader.Position;
					}
					if (reader.Next == delimiter)
					{
						reader.Advance();
					}
				}

				reader.Advance();
			}

			// この時点で reader は末尾の " を指している。
			var end = reader.Position.Index;

			// この view は " の内側。
			var view = reader.Source.Slice(begin, end);

			// エスケープがない場合、そのまま返せば良い。
			if (escaped.IsInvalid())
			{
				reader.Advance();
				return m_StringCache.GetString(view);
			}

			// エスケープがある場合、エスケープされたままキャッシュされていることがある。
			var ret = m_StringCache.TryGetString(view);
			if (ret != null)
			{
				reader.Advance();
				return ret;
			}

			var sb = StringBuilderPool.Rent();
			sb.Append(reader.Source.Slice(begin, escaped.Index).ToString());

			reader.SetPosition(escaped);

			try
			{
				while (true)
				{
					// この時点で reader は \ を指している。
					reader.Advance();

					var ch = reader.Current;

					switch ((int)ch)
					{
						case 'b':
							sb.Append('\b');
							break;
						case 'f':
							sb.Append('\f');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 't':
							sb.Append('\t');
							break;
						case '\"':
							sb.Append('\"');
							break;
						case '/':
							sb.Append('/');
							break;
						case '\\':
							sb.Append('\\');
							break;

						// これは行末 \
						// 行末 \ は存在しないものとする。\r\n が来る可能性もあるので、その場合は 1 文字読み飛ばし。
						case '\n':
							break;
						case '\r':
							if (reader.Next == '\n')
							{
								reader.Advance();
							}
							break;

						// unicode
						case 'u':
							var unicode = reader.PeekAhead(1, 5);
							if (unicode.Length != 4)
							{
								throw Fail(reader.Position, "invalid Unicode sequence.");
							}

							char letter = ParseUnicode(unicode);
							if (letter == '\0')
							{
								throw Fail(reader.Position, $"invalid Unicode sequence. ({letter})");
							}

							sb.Append(letter);
							reader.Advance(4);
							break;
						default:
							sb.Append((char)ch);
							break;
					}

					reader.Advance();

					// 文字列の終わりか \ をさがす
					int p1 = reader.Position.Index;
					while (true)
					{
						ch = reader.Current;
						if (ch == delimiter || ch == '\\')
						{
							break;
						}
						reader.Advance();
					}

					// そこまでを追記
					sb.Append(reader.Peek(p1, reader.Position.Index).ToString());

					// エスケープならばもう一回
					if (ch == '\\')
					{
						continue;
					}

					break;
				}

				// この時点で reader は delimiter そのものを指している。
				reader.Advance();

				// sb の内容を返却すればいいのだが、一応キャッシュしておく
				ret = sb.ToString();
				m_StringCache.SetString(view, ret);

				return ret;
			}
			finally
			{
				StringBuilderPool.Return(sb);
			}
		}



		// 失敗時
		private static JsonParseException Fail(Position pos, string message)
		{
			return new JsonParseException($"({pos}) {message}");
		}


		// ホワイトスペース読み飛ばし
		private static void SkipWhiteSpace(ref Reader reader)
		{
			while (true)
			{
				var ch = reader.Current;
				if (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t' || ch == 0xFE || ch == 0xFF)
				{
					reader.Advance();
					continue;
				}
				break;
			}
		}


		// ホワイトスペース及びコメント読み飛ばし
		private static void SkipTrivial(ref Reader reader)
		{
			while (true)
			{
				// 空白
				SkipWhiteSpace(ref reader);

				// / で止まったとき、コメントの可能性があり、
				// コメントのあとはもう一回 SkipWhiteSpace する必要がある。
				bool toContinue = false;

				if (reader.Current == '/')
				{
					if (reader.Next == '/')
					{
						toContinue = true;

						reader.Advance(2);
						while (true)
						{
							var ch = reader.Current;
							reader.Advance();

							if (ch == 0 || ch == '\n')
							{
								break;
							}
						}
					}
					else if (reader.Next == '*')
					{
						toContinue = true;

						var pos = reader.Position;
						reader.Advance(2);

						while (true)
						{
							var ch = reader.Current;
							reader.Advance();

							// */ なら / を喰って終わり
							if (ch == '*' && reader.Next == '/')
							{
								reader.Advance();
								break;
							}

							// コメントが終了していない。
							if (ch == 0)
							{
								throw Fail(pos, "unterminated comment.");
							}
						}
					}
				}

				if (!toContinue)
				{
					return;
				}
			}
		}


		// \\uXXXX 表記の Unicode 文字をパース、失敗したら '\0' を返す。
		private static char ParseUnicode(U8View s)
		{
			if (s.Length != 4)
			{
				return '\0';
			}
			int i3 = Parse1DigitHex(s[0]);
			int i2 = Parse1DigitHex(s[1]);
			int i1 = Parse1DigitHex(s[2]);
			int i0 = Parse1DigitHex(s[3]);

			if (i3 < 0 || i2 < 0 || i1 < 0 || i0 < 0)
			{
				return '\0';
			}

			return (char)((i3 << 12) | (i2 << 8) | (i1 << 4) | i0);
		}

		// トークンっぽい部分を読む
		private static void ReadToken(ref Reader reader)
		{
			while (true)
			{
				var ch = reader.Current;
				if (IsSymbolChar(ch))
				{
					reader.Advance();
					continue;
				}
				break;
			}
		}


		// 0 - F で 0 - 15 を返す。おかしいときは -1。
		private static int Parse1DigitHex(int ch)
		{
			if (ch >= '0' && ch <= '9')
			{
				return ch - '0';
			}

			if (ch >= 'a' && ch <= 'z')
			{
				ch = (char)(ch - 'a' + 'A');
			}

			if (ch >= 'A' && ch <= 'F')
			{
				return ch - 'A' + 0x0A;
			}
			return -1;
		}


		// シンボルっぽい文字かどうか返す。
		private static bool IsSymbolChar(int ch)
		{
			// 文字コード自体で比較して判定
			if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
			{
				return true;
			}
			return (ch == '+' || ch == '-' || ch == '.' || ch == '_');
		}

	}





}
