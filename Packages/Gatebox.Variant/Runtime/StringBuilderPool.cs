using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace Gatebox.Variant
{

	/// <summary>
	/// StringBuilder のプール。
	/// <para>
	/// ローカルでしか利用しないような StringBuilder を再利用するためのものです。
	/// </para>
	/// </summary>
	public static class StringBuilderPool
	{
		private static readonly List<StringBuilder> s_List = new();

		private const int MaxBuilders = 16;
		private const int MaxEachCapacity = 32 * 1024;

		/// <summary>
		/// StringBuilder を返す。
		/// <para>
		/// 不要になったら Return してください。
		/// しなくても問題になることはありませんが、Return すると再利用されます。</para>
		/// </summary>
		public static StringBuilder Rent()
		{
			lock (s_List)
			{
				if (s_List.Count == 0)
				{
					return new StringBuilder();
				}

				int last = s_List.Count - 1;
				var r = s_List[last];
				s_List.RemoveAt(last);
				return r;
			}
		}

		public static StringBuilder Rent(StringView initial)
		{
			if (initial.IsEmpty())
			{
				return Rent();
			}

			lock (s_List)
			{
				if (s_List.Count == 0)
				{
					var sb = new StringBuilder(initial.Length);
					sb.Append(initial.AsSpan());
					return sb;
				}

				var r = s_List[0];
				s_List.RemoveAt(0);
				r.Append(initial.AsSpan());
				return r;
			}
		}

		public static void Return(StringBuilder builder)
		{
			builder.Clear();

			if (builder.Capacity > MaxEachCapacity)
			{
				builder.Capacity = MaxEachCapacity;
			}

			lock (s_List)
			{
				if( s_List.Count < MaxBuilders)
				{
					s_List.Add(builder);
				}
			}
		}

		public static string ReturnAndGetString(StringBuilder builder)
		{
			string ret = builder.ToString();
			Return(builder);
			return ret;
		}
	}
}
