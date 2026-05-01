using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace Gatebox.Variant.Parser
{
	/// <summary>
	/// 位置。
	/// <para>変更可能な struct なので注意</para>
	/// </summary>
	internal struct Position
	{
		public static Position Invalid
		{
			get
			{
				var r = new Position();
				r.m_Offset = -1;
				return r;
			}
		}

		private int m_Offset;
		private int m_Line;
		private int m_Column;

		public readonly int Index => m_Offset;
		public readonly int Line => m_Line;

		public readonly bool IsInvalid()
		{
			return m_Offset < 0;
		}

		public int Increment()
		{
			m_Offset += 1;
			m_Column += 1;
			return m_Offset;
		}

		public int Return()
		{
			m_Offset += 1;
			m_Line += 1;
			m_Column = 0;
			return m_Offset;
		}

		public readonly override string ToString()
		{
			return $"{m_Line + 1}:{m_Column}";
		}
	}
}
