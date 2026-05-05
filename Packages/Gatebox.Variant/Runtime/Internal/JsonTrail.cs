using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#nullable enable

namespace Gatebox.Variant.Internal
{

	/// <summary>
	/// Trail 記法
	/// <para>
	/// Gatebox.Varaint 内で利用される、JSON オブジェクト内の位置を表すための記法です。</para>
	/// <para>
	/// 基本は . 区切りで、各パートは前後の空白をトリムして解釈されます。
	/// (特殊なことをしない限り利用するのはこれのみだと思います)
	/// <code>"A.  B  . C"   => ["A","B","C"]</code></para>
	/// <para>
	/// . の間は 次の . もしくは [ までがパートになり、この間はエスケープ等が行われず、そのままの文字列として解釈されます。
	/// </para>
	/// <para>
	/// [ ] で区切ることもでき、 [ の間は \ によるエスケープが行われます。
	/// この [] のあとは . もしくは [ が来ることが期待されます。
	/// <code>"A[B].C" => ["A","B","C"]</code><br/>
	/// <code>"[A.B]C" => ["A.B","C"]</code></para>
	/// <para>
	/// 基本的には配列のインデックスとオブジェクトのキーは同等にそのまま解釈されます。
	/// ただし、構築の文脈においては [] は配列であることを示し、[+] が配列の末尾に要素を追加するという意味を持ちます。</para>
	/// <para>
	/// 自分自身を表す記法はありません。空文字列は空文字列をキーとする子供を意味します。
	/// </para>
	/// </summary>
	public class JsonTrail 
	{
		//==============================================================================
		// inner types
		//==============================================================================

		public enum Kind
		{
			PrefferObject,
			PrefferArray,
			AppendArray,
		}

		public struct Part
		{
			public Part(Kind kind, StringView value)
			{
				Kind = kind;
				Value = value;
			}
			public readonly Kind Kind;
			public readonly StringView Value;
		}

		//==============================================================================
		// static members
		//==============================================================================

		public static IList<StringView> ParseForRead(StringView trail)
		{
			// [ がない場合は全て . 区切り
			if (trail.Find('[') < 0)
			{
				var parts = trail.Split('.');
				return parts.Select(p => p.Trim()).ToList();
			}

			// [ がある場合、パースして、文字列部分だけ抜き取る。
			return ParseTrail(trail).Select(p => p.Value).ToList();
		}

		public static JsonTrail Parse(StringView trail)
		{
			// [ がない場合は全て . 区切り
			if (trail.Find('[') < 0)
			{
				var seg = trail.Split('.');
				var parts = seg.Select(s => new Part(Kind.PrefferObject, s.Trim())).ToList();
				return new JsonTrail(parts);
			}

			return new JsonTrail(ParseTrail(trail).ToList());
		}

		
		private static IEnumerable<Part> ParseTrail(StringView trail)
		{
			bool bracketCloced = false;
			StringBuilder? sb = null;

			while (true)
			{
				if (trail.IsEmpty())
				{
					break;
				}

				// 最初の . もしくは [ まで
				var next = trail.Find(c => c == '.' || c == '[');
				var part = trail.Slice(0, next).Trim();
				var ch = trail.At(next);

				// ] の直後？
				if (bracketCloced)
				{
					// ] の直後は . か [ しか来ない
					if (part.HasContent())
					{
						throw new InvalidDataException("Invalid JSON trail format. '.' or '[' expected after closing bracket.");
					}
				}
				else
				{
					// . か [ の前はそのままパートとして追記される。
					if (ch == '.' || part.HasContent())
					{
						yield return new Part(Kind.PrefferObject, part);
					}
				}
				bracketCloced = false;

				// いま . か [ を指しているはず、それ以前の情報は不要
				trail = trail.Slice(next + 1);

				// . の場合はそのまま次へ
				if (ch == '.')
				{
					continue;
				}

				// ここで文字列が終わった場合、
				if (ch == 0)
				{
					break;
				}

				sb ??= StringBuilderPool.Rent();
				bool escape = false;
				while (true)
				{
					if (trail.IsEmpty())
					{
						throw new InvalidDataException("Invalid JSON trail format. Closing bracket ']' not found.");
					}

					// 位置文字食う
					ch = trail.At(0);
					trail = trail.Slice(1);

					// エスケープ中ならエスケープ解除したものを追加
					if (escape)
					{
						sb.Append(Unescape(ch));
						escape = false;
						continue;
					}

					if (ch == ']')
					{
						string value = sb.ToString();
						sb.Clear();
						if (value == "+")
						{
							yield return new Part(Kind.AppendArray, value);
						}
						else
						{
							yield return new Part(Kind.PrefferArray, value);
						}
						bracketCloced = true;
						break;
					}

					if (ch == '\\')
					{
						escape = true;
						continue;
					}

					sb.Append(ch);
				}
			}

			if (sb != null)
			{
				StringBuilderPool.Return(sb);
			}
		}


		private static char Unescape(char ch)
		{
			return ch switch
			{
				'a' => '\a',
				'b' => '\b',
				't' => '\t',
				'n' => '\n',
				'f' => '\f',
				'r' => '\r',
				'v' => '\v',
				'\'' => '\'',
				'"' => '"',
				_ => ch
			};
		}

		//==============================================================================
		// instance members
		//==============================================================================

		private readonly IReadOnlyList<Part> m_Parts;

		public JsonTrail(IReadOnlyList<Part> parts)
		{
			m_Parts = parts;
		}


		public int Count => m_Parts.Count;

		public Part this[int index] 
		{
			get
			{
				return m_Parts[index];
			}
		}

	}



}
