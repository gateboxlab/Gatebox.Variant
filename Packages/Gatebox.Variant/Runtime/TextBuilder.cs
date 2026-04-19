using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace Gatebox.Variant
{
	/// <summary>
	/// StringBuilder をキャッシュして使い回すためのクラス。
	/// <para>
	/// Acquire() でインスタンスを取得し、利用が終わったら Dispose() してください。
	/// 内部の StringBuilder がプールに返却されるため、ヒープの無駄遣いを減らすことができます。</para>
	/// <para>
	/// <see cref="LocalTextBuilder"/> とほぼ同等の機能を提供しますが、
	/// こちらは参照型であるため、これ自体のインスタンスは生成されヒープに上がります。
	/// その分の無駄はありますが、C#としてより自然であることと、同期メソッド内でも利用できるというメリットがあります。
	/// </para>
	/// <para>
	/// 利用箇所が同期のローカルメソッドである場合は <see cref="LocalTextBuilder"/> の利用を検討してください。
	/// ほぼ同等の機能を提供しつつ更に安全、軽量に利用できます。</para>
	/// </summary>
	public class TextBuilder : IDisposable
	{
		//==============================================================================
		// static members
		//==============================================================================

		/// <summary>
		/// インスタンスの取得
		/// <para>
		/// プールから StringBuilder を借りて TextBuilder を生成します。
		/// 利用が終わったら Dispose してください。
		/// </para>
		/// </summary>
		public static TextBuilder Acquire() => new TextBuilder(StringBuilderPool.Rent());


		//==============================================================================
		// instance members
		//==============================================================================

		// コンストラクタ(private)
		// Acquire を利用してください。
		private TextBuilder(StringBuilder body)
		{
			Body = body;
		}

		/// <summary>
		/// StringBuilder 本体
		/// </summary>
		public StringBuilder? Body { get; private set;}

		public int Length => Body!.Length;

		public int Capacity => Body!.Capacity;

		public char this[int index]
		{
			get => Body![index];
			set => Body![index] = value;
		}

		/// <summary>
		/// 破棄。
		/// <para>
		/// プールに StringBuilder を返却します。
		/// 破棄後の各メソッドへのアクセスは NullReferenceException を投げます。
		/// ObjectDisposedException にするべきではありますが、
		/// そもそもありえないことに対する判定を全てに入れるのも冗長なので、NullReferenceException で十分と判断しています。
		/// </para>
		/// </summary>
		public void Dispose()
		{
			if( Body != null )
			{
				StringBuilderPool.Return(Body);
				Body = null;
			}
		}

		public TextBuilder Clear()
		{
			Body!.Clear();
			return this;
		}

		public override string ToString() => Body?.ToString() ?? "";

		public string ToString(int start, int length) => Body!.ToString(start, length);

		public TextBuilder Append(string value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(bool value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(byte value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(sbyte value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(char value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(ushort value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(short value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(int value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(uint value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(long value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(ulong value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(float value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(double value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(object value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(StringView value)
		{
			if (value.IsEmpty())
			{
				return this;
			}
			Body!.Append(value.Original, value.Begin, value.Length);
			return this;
		}
		public TextBuilder Append(char[] value)
		{
			Body!.Append(value);
			return this;
		}
		public TextBuilder Append(char value, int count)
		{
			Body!.Append(value, count);
			return this;
		}
		public TextBuilder Append(char[] value, int start, int count)
		{
			Body!.Append(value, start, count);
			return this;
		}
		public TextBuilder Append(string value, int start, int count)
		{
			Body!.Append(value, start, count);
			return this;
		}
		public TextBuilder AppendLine()
		{
			Body!.AppendLine();
			return this;
		}
		public TextBuilder AppendLine(string value)
		{
			Body!.AppendLine(value);
			return this;
		}

		public TextBuilder Remove(int start, int length)
		{
			Body!.Remove(start, length);
			return this;
		}
		public TextBuilder Replace(char old, char newbie)
		{
			Body!.Replace(old, newbie);
			return this;
		}
		public TextBuilder Replace(char old, char newbie, int start, int count)
		{
			Body!.Replace(old, newbie, start, count);
			return this;
		}
		public TextBuilder Replace(string old, string newbie)
		{
			Body!.Replace(old, newbie);
			return this;
		}
		public TextBuilder Replace(string old, string newbie, int start, int count)
		{
			Body!.Replace(old, newbie, start, count);
			return this;
		}
	}




	/// <summary>
	/// ローカルでのみ利用する TextBuilder。
	/// <para>
	/// ローカルでのみ利用する文字列構築のために StringBuilder のインスタンスを生成するのはヒープの無駄遣いであるため、
	/// StringBuilder をキャッシュして使い回すための構造体です。</para>
	/// <para>
	/// <see cref="TextBuilder"/> とほぼ同等の機能を提供しますが、それとは異なり
	/// ref struct であるため、仕組み上ローカルにしか存在できずヒープに上がることがなく、
	/// より軽量かつ余計な参照を残すことなく安全に利用できます。</para>
	/// <para>
	/// 非同期メソッド内で利用するには LangVersion 13.0 以降が必要です。 
	/// また、その場合も await を越えられないので注意してください。</para>
	/// <para>
	/// 利用条件を満たさない場合は <see cref="TextBuilder"/> を利用してください。
	/// </para>
	/// </summary>
	public readonly ref struct LocalTextBuilder
	{
		//==============================================================================
		// static members
		//==============================================================================

		/// <summary>
		/// インスタンスの取得
		/// <para>
		/// StringBuilderPool から StringBuilder を借りて LocalTextBuilder を作成します。
		/// 使用後は必ず Dispose してください。</para>
		/// </summary>
		public static LocalTextBuilder Acquire() => new LocalTextBuilder(StringBuilderPool.Rent());


		//==============================================================================
		// instance members
		//==============================================================================

		private LocalTextBuilder(StringBuilder body)
		{
			Body = body;
		}

		public StringBuilder Body { get; }

		public int Length => Body.Length;

		public int Capacity => Body.Capacity;

		public char this[int index]
		{
			get => Body[index];
			set => Body[index] = value;
		}


		public readonly LocalTextBuilder Clear()
		{
			Body.Clear();
			return this;
		}

		public override readonly string ToString() => Body.ToString();

		public readonly string ToString(int start, int length) => Body.ToString(start, length);


		public LocalTextBuilder Append(string value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(bool value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(byte value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(sbyte value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(char value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(ushort value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(short value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(int value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(uint value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(long value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(ulong value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(float value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(double value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(object value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(StringView value)
		{
			if (value.IsEmpty())
			{
				return this;
			}
			Body.Append(value.Original, value.Begin, value.Length);
			return this;
		}
		public LocalTextBuilder Append(char[] value)
		{
			Body.Append(value);
			return this;
		}
		public LocalTextBuilder Append(char value, int count)
		{
			Body.Append(value, count);
			return this;
		}
		public LocalTextBuilder Append(char[] value, int start, int count)
		{
			Body.Append(value, start, count);
			return this;
		}
		public LocalTextBuilder Append(string value, int start, int count)
		{
			Body.Append(value, start, count);
			return this;
		}
		public LocalTextBuilder AppendLine()
		{
			Body.AppendLine();
			return this;
		}
		public LocalTextBuilder AppendLine(string value)
		{
			Body.AppendLine(value);
			return this;
		}

		public LocalTextBuilder Remove(int start, int length)
		{
			Body.Remove(start, length);
			return this;
		}
		public LocalTextBuilder Replace(char old, char newbie)
		{
			Body.Replace(old, newbie);
			return this;
		}
		public LocalTextBuilder Replace(char old, char newbie, int start, int count)
		{
			Body.Replace(old, newbie, start, count);
			return this;
		}
		public LocalTextBuilder Replace(string old, string newbie)
		{
			Body.Replace(old, newbie);
			return this;
		}
		public LocalTextBuilder Replace(string old, string newbie, int start, int count)
		{
			Body.Replace(old, newbie, start, count);
			return this;
		}

		public void Dispose()
		{
			StringBuilderPool.Return(Body);
		}
	}

}
