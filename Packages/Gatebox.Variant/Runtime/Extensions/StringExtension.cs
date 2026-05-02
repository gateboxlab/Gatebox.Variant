using System;

#nullable enable

namespace Gatebox.Variant.Extensions
{
	public static class StringExtension
	{
		/// <summary>
		/// string 全体を示す StringView を返す。
		/// </summary>
		public static StringView View(this string s)
		{
			return new StringView(s);
		}

		/// <summary>
		/// string から Range を指定してその範囲を示す StringView を返す
		/// </summary>
		public static StringView View(this string s, Range range)
		{
			return new StringView(s, range);
		}

	}
}
