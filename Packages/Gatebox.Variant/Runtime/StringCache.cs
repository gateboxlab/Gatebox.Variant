using System;
using System.Collections.Generic;

#nullable enable

namespace Gatebox.Variant
{
	/// <summary>
	/// 短い文字列が何度もできないようにするためのもの。
	/// <para>
	/// 同じような JSON を何度もパースする場合、キー文字列を細切れに何度も生成しないようにするためのものです。
	/// </para>
	/// </summary>
	public interface IStringCache
	{
		/// <summary>
		/// StringView から string を取得する。
		/// <para>
		/// キャッシュされていればそれを、
		/// キャッシュされていない場合は StringView を string 化したものを（キャッシュして）返します。</para>
		/// </summary>
		public string GetString(StringView view);

		/// <summary>
		/// StringView に対する string を設定する。
		/// <para>
		/// GetString でもキャッシュは行われますが、これを使用して明示的にキャッシュに追加することができます。
		/// これはエスケープ処理等のため、元の文字列と違う文字列をキャッシュしたい場合に使用します。 </para>
		/// </summary>
		public void SetString(StringView view, string value);

		/// <summary>
		/// StringView から string を取得する。ない場合は null.
		/// </summary>
		public string? TryGetString(StringView view);

		/// <summary>
		/// UTF-8 の並びから string を返す。
		/// <para>
		/// キャッシュされていればそれを、
		/// キャッシュされていない場合は UTF-8 の並びを string 化したものを（キャッシュして）返します。
		/// </para>
		/// </summary>
		public string GetString(U8View view);

		/// <summary>
		/// UTF-8 の並び に対する string を設定する。
		/// <para>
		/// GetString でもキャッシュは行われますが、これを使用して明示的にキャッシュに追加することができます。
		/// エスケープ処理等のため、元の文字列と違う文字列をキャッシュしたい場合に使用します。
		/// </para>
		/// </summary>
		public void SetString(U8View view, string value);

		/// <summary>
		/// UTF-8 の並びから string を返す。ない場合は null.
		/// </summary>
		public string? TryGetString(U8View view);
	}



	/// <summary>
	/// IStringCache の実装。
	/// <para>
	/// マルチスレッドでの利用は想定していません。必要な場合はロック等で保護してください。
	/// </para>
	/// </summary>
	public class StringCache : IStringCache
	{
		private readonly int m_MaxLength;
		private readonly bool m_IsToShrink;
		private Dictionary<StringView, string>? m_StringMap;
		private Dictionary<U8View, string>? m_U8Map;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="max_length">キャッシュする文字列の最大長</param>
		/// <param name="to_shrink">キー側文字列を縮小保持するかどうか。trueにするとキー文字列はソースの文字列内の部分文字列のままキャッシュのキーになることがあります。</param>
		public StringCache(int max_length, bool to_shrink = true)
		{
			m_MaxLength = max_length;
			m_IsToShrink = to_shrink;
		}

		public string GetString(StringView view)
		{
			if (view.IsEmpty())
			{
				return string.Empty;
			}

			// 長過ぎる
			if (view.Length > m_MaxLength)
			{
				return view.ToString();
			}

			m_StringMap ??= new();

			// あればそれを返す。
			if (m_StringMap.TryGetValue(view, out string ret))
			{
				return ret;
			}

			// 部分文字列を作って、view 自身もそれを参照するようにする
			ret = view.ToString();
			view = ret;

			// キャッシュしつつ返す。
			m_StringMap[view] = ret;
			return ret;
		}

		public string GetString(U8View view)
		{
			if (view.IsEmpty())
			{
				return string.Empty;
			}

			// 長すぎるのはキャッシュしない。
			if (view.Length > m_MaxLength)
			{
				return view.ToString();
			}

			m_U8Map ??= new();

			//  キャッシュされていればそれを返す。
			if (m_U8Map.TryGetValue(view, out var result))
			{
				return result;
			}

			if (m_IsToShrink)
			{
				byte[] bytes = new byte[view.Length];
				Array.Copy(view.Original, view.Begin, bytes, 0, view.Length);
				view = new U8View(bytes, 0, view.Length);
			}

			// キャッシュにないので追加しつつ返す。
			var str = view.ToString();
			m_U8Map.Add(view, str);

			return str;
		}

		public void SetString(StringView view, string value)
		{
			// 長過ぎる
			if (view.Length > m_MaxLength)
			{
				return;
			}

			if (m_IsToShrink)
			{
				view = view.Shrink();
			}

			m_StringMap ??= new();
			m_StringMap[view] = value;
		}

		public void SetString(U8View view, string value)
		{
			// 長過ぎる
			if (view.Length > m_MaxLength)
			{
				return;
			}

			if (m_IsToShrink)
			{
				byte[] bytes = new byte[view.Length];
				Array.Copy(view.Original, view.Begin, bytes, 0, view.Length);
				view = new U8View(bytes, 0, view.Length);
			}

			m_U8Map ??= new();
			m_U8Map[view] = value;
		}

		public string? TryGetString(StringView view)
		{
			return m_StringMap?.GetValueOrDefault(view) ?? null;
		}
		public string? TryGetString(U8View view)
		{
			return m_U8Map?.GetValueOrDefault(view) ?? null;
		}

	}
}
