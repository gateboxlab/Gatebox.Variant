using System;
using System.Runtime.CompilerServices;
using System.Text;
using Gatebox.Variant.Extensions;

#nullable enable

namespace Gatebox.Variant.Internal
{
	public class TextUtil
	{
		/// <summary>
		/// バックスラッシュのエスケープを行う。
		/// <para>
		/// 対応するエスケープは \\b \\t \\n \\f \\r \\' \\" \\\\</para>
		/// </summary>
		public static string EscapeBackslash(string source)
		{
			if (string.IsNullOrEmpty(source))
			{
				return string.Empty;
			}

			StringBuilder? sb = null;

			for (int i = 0; i < source.Length; i++)
			{
				char ch = source[i];
				char esc = GetEscaped(ch);
				if (esc != '\0')
				{
					sb ??= StringBuilderPool.Rent(source.View(0..i));
					sb.Append('\\');
					sb.Append(esc);
					continue;
				}
				sb?.Append(ch);
			}

			if (sb == null)
			{
				return source;
			}

			return StringBuilderPool.ReturnAndGetString(sb);
		}

		public static string EscapeJsonString(string? source, bool encode)
		{
			if (string.IsNullOrEmpty(source))
			{
				return string.Empty;
			}

			StringBuilder? sb = null;

			for (int i = 0; i < source.Length; i++)
			{
				char ch = source[i];

				if (encode)
				{
					if (ch <= 0x1F || ch >= 0x7F)
					{
						sb ??= StringBuilderPool.Rent(source.View(0..i));
						sb.Append('\\');
						sb.Append('u');
						sb.Append(GetHex((ch >> 12) & 0xF));
						sb.Append(GetHex((ch >> 8) & 0xF));
						sb.Append(GetHex((ch >> 4) & 0xF));
						sb.Append(GetHex(ch & 0xF));
						continue;
					}
				}

				char esc = GetEscaped(ch);
				if (esc != '\0')
				{
					sb ??= StringBuilderPool.Rent(source.View(0..i));
					sb.Append('\\');
					sb.Append(esc);
					continue;
				}
				sb?.Append(ch);
			}

			if (sb == null)
			{
				return source;
			}

			return StringBuilderPool.ReturnAndGetString(sb);

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static char GetHex(int n)
		{
			return ("0123456789ABCDEF")[n];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static char GetEscaped(char ch)
		{
			return ch switch
			{
				'\b' => 'b',
				'\t' => 't',
				'\n' => 'n',
				'\f' => 'f',
				'\r' => 'r',
				'\\' => '\\',
				'\"' => '\"',
				_ => '\0',
			};
		}
	}
}
