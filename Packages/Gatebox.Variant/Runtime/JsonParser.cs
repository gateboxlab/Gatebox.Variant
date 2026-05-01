using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace Gatebox.Variant
{

	/// <summary>
	/// JSON をパースするもの。
	/// <para>
	/// JSON 文字列を解析し、JVariant オブジェクトとして返します。
	/// スレッドセーフではありません。
	/// 複数のスレッドで同時に使用する場合は、各スレッドで個別の JsonParser インスタンスを作成してください。</para>
	/// <para>
	/// 内部的には文字列キャッシュを利用しており、同じ文字列が出てきた場合その文字列インスタンスを再利用します。
	/// そのため、同じ構造の JSON を何度もパースする場合は
	/// それ用の JsonParser インスタンスを作成して使い回したほうが効率が良くなります。
	/// </para>
	/// </summary>
	public class JsonParser
	{

		//==============================================================================
		// static members
		//==============================================================================

		/// <summary>
		/// ローカル利用のための生成
		/// </summary>
		public static JsonParser CreateTemporary()
		{
			// JsonParser 自体が捨てられる想定。一回のパースだけなので無駄に参照を持ち続ける懸念をする必要がない。
			// 長めに判定し、JSON 内部の部分文字列のキャッシュも許す。
			return new JsonParser(new StringCache(max_length:32, to_shrink:false));
		}

		//==============================================================================
		// instance members
		//==============================================================================

		private readonly IStringCache m_StringCache;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public JsonParser(IStringCache? stringCache)
		{
			m_StringCache = stringCache ?? new StringCache(max_length:16, to_shrink:true);
		}

		/// <summary>
		/// JSON をパースして JVariant として返す。
		/// <para>
		/// JSON として正しくない文字列が与えられたとき、デフォルトでは Null を示す JVariant を返します。
		/// (null ではなく 「null を示す JVariant」であることに注意してください。)</para>
		/// <para>
		/// 例外を投げるべき場合は第2引数に true を指定してください。</para>
		/// <para>
		/// 多少パースはゆるくなっていて、厳密には JSON ではない文字列もパースします。
		/// ・ オブジェクトのキーが " でくくられていなくとも良い。アルファベットのみの連続はキー名として扱われる。
		/// ・ 数値の解釈が int.TryParse で行われる。無駄な先行 + などは JSON 的には ill-formed だが、パースされる。
		/// ・ Object Array の末尾に , があって良い。
		/// これらが問題になることはないと思われますが、これを期待することは避けてください。</para>
		/// </summary>
		/// <param name="source">パースする文字列</param>
		/// <param name="throws">例外を投げるとき true.</param>
		/// <exception cref="VariantException">throws に true が指定され、パースに失敗したとき。</exception>
		public JVariant Parse(StringView source, bool throws = false)
		{
			var parser_u16 = new Parser.ParserU16(m_StringCache);
			try
			{
				return parser_u16.Parse(source);
			}
			catch (JsonParseException)
			{
				if (throws)
				{
					throw;
				}
				return new JVariant();
			}
		}


		/// <summary>
		/// UTF-8 文字列をパースして JVaraint として返す。
		/// </summary>
		/// <param name="source">パース対象の UTF-8 バイナリ</param>
		/// <param name="throws">失敗時例外を投げるなら true</param>
		/// <exception cref="VariantException">throws に true が指定され、パースに失敗したとき。</exception>
		public JVariant Parse(U8View source, bool throws = false)
		{
			var parser_u8 = new Parser.ParserU8(m_StringCache);
			try
			{
				return parser_u8.Parse(source);
			}
			catch (JsonParseException)
			{
				if (throws)
				{
					throw;
				}
				return new JVariant();
			}
		}


	}
}
