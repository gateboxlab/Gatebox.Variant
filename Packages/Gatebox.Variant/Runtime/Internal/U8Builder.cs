using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#nullable enable

namespace Gatebox.Variant.Internal
{
	public class U8Builder : IDisposable
	{
		private byte[]? m_Body;
		private int m_Length;


		public void Dispose()
		{
			if (m_Body != null)
			{
				ArrayPool<byte>.Shared.Return(m_Body);
				m_Body = null;
			}
		}

		/// <summary>
		/// 現在保持している内容をそのまま返す。
		/// <para>
		/// 処理中の byte[] の所有権を放棄してそれを返します。
		/// 結果この U8Builder が持つ内容は破棄されます。
		/// </para>
		/// </summary>
		public U8View Detach()
		{
			var ret = new U8View(m_Body, 0, m_Length);
			m_Body = null;
			m_Length = 0;
			return ret;
		}

		public override string ToString()
		{
			if (m_Body == null || m_Length == 0)
			{
				return string.Empty;
			}

			return Encoding.UTF8.GetString(m_Body, 0, m_Length);
		}



		public void Append(U8View view)
		{
			if (view.IsEmpty())
			{
				return;
			}

			EnsureCapacity(m_Length + view.Length);
			Array.Copy(view.Original, view.Begin, m_Body, m_Length, view.Length);
			m_Length += view.Length;
		}

		public void Append(ReadOnlySpan<byte> bin)
		{
			if (bin.Length == 0)
			{
				return;
			}

			EnsureCapacity(m_Length + bin.Length);
			for (int i = 0; i < bin.Length; i++)
			{
				m_Body[m_Length + i] = bin[i];
			}
			m_Length += bin.Length;
		}

		public void Append(char ch)
		{
			if (ch <= 0xff)
			{
				EnsureCapacity(m_Length + 1);
				m_Body[m_Length] = (byte)ch;
				m_Length += 1;
				return;
			}

			// ２バイト符号化
			// 00000yyy-yyxxxxxx => 110yyyyy, 10xxxxxx
			if (ch <= 0x07FF)
			{
				EnsureCapacity(m_Length + 2);
				byte b1 = (byte)(0xC0 | ((ch & 0x07C0) >> 6));
				byte b2 = (byte)(0x80 | (ch & 0x003F));
				m_Body[m_Length + 0] = b1;
				m_Body[m_Length + 1] = b2;
				m_Length += 2;
				return;
			}

			// ３バイト符号化
			// zzzzyyyy-yyxxxxxx => 1110zzzz, 10yyyyyy, 10xxxxxx
			{
				EnsureCapacity(m_Length + 3);

				byte b1 = (byte)(0xE0 | ((ch & 0xF000) >> 12));
				byte b2 = (byte)(0x80 | ((ch & 0x0FC0) >> 6));
				byte b3 = (byte)(0x80 | (ch & 0x003F));
				m_Body[m_Length + 0] = b1;
				m_Body[m_Length + 1] = b2;
				m_Body[m_Length + 2] = b3;
				m_Length += 3;
			}
			return;
		}

		public void Append(string str)
		{
			if (str == null || str.Length == 0)
			{
				return;
			}

			int count = Encoding.UTF8.GetByteCount(str);
			EnsureCapacity(m_Length + count);

			int bytesWritten = Encoding.UTF8.GetBytes(str, 0, str.Length, m_Body, m_Length);
			m_Length += bytesWritten;
		}


		public void Append(long v)
		{
			Span<byte> buf = stackalloc byte[32];
			ReadOnlySpan<byte> span = LongToSpan(v, ref buf);
			Append(span);
		}

		[MemberNotNull(nameof(m_Body))]
		private void EnsureCapacity(int capacity)
		{
			if (capacity <= 0)
			{
				throw new Exception();
			}

			if ((m_Body != null) && (m_Body.Length >= capacity))
			{
				return;
			}

			capacity = NextPow2(capacity);
			var old = m_Body;
			m_Body = ArrayPool<byte>.Shared.Rent(capacity);

			if (old != null && m_Length > 0)
			{
				Array.Copy(old, 0, m_Body, 0, m_Length);
			}
		}

		// n を越える 2 のべき乗を返す。
		private int NextPow2(int n)
		{
			n -= 1;

			// トリッキーだが、1になっている最上位のビットを残して、その下を1で埋めていく
			// 1 になっているビットを隣に書くことで 1 が並ぶので、次は 2ビット一気に、4ビット、8ビット、と行ける。
			n |= n >> 1;
			n |= n >> 2;
			n |= n >> 4;
			n |= n >> 8;
			n |= n >> 16;
			return n + 1;
		}


		private static ReadOnlySpan<byte> LongToSpan(long value, ref Span<byte> buffer)
		{
			int index = buffer.Length - 1;

			if (value == 0)
			{
				buffer[index--] = (byte)'0';
			}
			else
			{
				// 符号を取っておく。
				bool isNegative = value < 0;
				if (isNegative)
				{
					value = -value;
				}

				// 逆順に桁を取得しバッファに格納
				while (value > 0)
				{
					long remainder = value % 10;
					buffer[index--] = (byte)('0' + remainder);
					value /= 10;
				}

				// 負の値の場合、符号を追加
				if (isNegative)
				{
					buffer[index--] = (byte)'-';
				}
			}

			// 結果の範囲を ReadOnlySpan<byte> で返す
			return buffer.Slice(index + 1, buffer.Length - index - 1);
		}
	}
}
