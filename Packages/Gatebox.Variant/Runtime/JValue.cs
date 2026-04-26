using System;
using System.Collections.Generic;
using Gatebox.Variant.Extensions;
using Gatebox.Variant.Internal;

#nullable enable

using SystemDebug = System.Diagnostics.Debug;

namespace Gatebox.Variant
{

	public static class JValueExtensions
	{
		/// <summary>
		/// 内部の値の型を返す。
		/// </summary>
		public static VariantType GetVariantType(this JValue? value) => value?.VariantType ?? VariantType.Null;

		public static bool IsNull(this JValue? value) => value.GetVariantType() == VariantType.Null;
		public static bool IsBoolean(this JValue? value) => value.GetVariantType() == VariantType.Boolean;
		public static bool IsNumber(this JValue? value) => value.GetVariantType() == VariantType.Integer || value.GetVariantType() == VariantType.Float;
		public static bool IsString(this JValue? value) => value.GetVariantType() == VariantType.String;
		public static bool IsArray(this JValue? value) => value.GetVariantType() == VariantType.Array;
		public static bool IsObject(this JValue? value) => value.GetVariantType() == VariantType.Object;
		
		
		/// <summary>
		/// 配列かオブジェクトのとき true.
		/// </summary>
		public static bool IsComposite(this JValue? value) => value.IsArray() || value.IsObject();

		/// <summary>
		/// 「空である」時 true を返す。
		/// <para>
		/// 内部の値の型によって「空である」の意味は異なります。
		/// <list type="bullet">
		/// <item>Null: 常に true</item>
		/// <item>Boolean: false のとき</item>
		/// <item>Number: 0 のとき</item>
		/// <item>String: 空文字のとき</item>
		/// <item>Array: 要素がないとき</item>
		/// <item>Object: プロパティがないとき</item>
		/// </list>
		/// </para>
		/// </summary>
		public static bool IsEmpty(this JValue? value)
		{
			return value?.IsEmpty ?? true;
		}

		public static bool AsBool(this JValue v)
		{
			return v?.BoolValue ?? false;
		}
		public static int AsInt(this JValue v)
		{
			return v?.IntValue ?? 0;
		}
		public static long AsLong(this JValue v)
		{
			return v?.LongValue ?? 0;
		}
		public static float AsFloat(this JValue v)
		{
			return v?.FloatValue ?? 0.0f;
		}
		public static double AsDouble(this JValue v)
		{
			return v?.DoubleValue ?? 0.0;
		}
		public static string AsString(this JValue v)
		{
			return v?.StringValue ?? string.Empty;
		}

		/// <summary>
		/// オブジェクトとしての値を返す。
		/// <para>
		/// 内部の値がオブジェクトである場合はそのまま返します。
		/// 内部の値が配列である場合はオブジェクトに変換して返します。
		/// 内部の値が null またはその他の型である場合は空のオブジェクトを返します。
		/// </para>
		/// </summary>
		public static JObject AsObject(this JValue value)
		{
			if (value == null || value.IsNull())
			{
				return new JObject();
			}
			if (value.IsObject())
			{
				return value.ObjectValue;
			}
			if (value.IsArray())
			{
				return value.ArrayValue.ConvertToObject();
			}

			return new JObject();
		}

		/// <summary>
		/// 配列としての値を返す。
		/// <para>
		/// 内部の値が配列である場合はそのまま返します。
		/// 内部の値がオブジェクトである場合は、そのキーがすべて int として解釈可能なときはそれぞれの要素を各 index に詰めた配列を返します。
		/// int として解釈できないキーがあるときは空の配列を返します。
		/// 内部の値が null またはその他の型である場合は空の配列を返します。
		/// </para>
		/// </summary>
		public static JArray AsArray(this JValue value)
		{
			if (value == null || value.IsNull())
			{
				return new JArray();
			}
			if (value.IsArray())
			{
				return value.ArrayValue;
			}
			if (value.IsObject())
			{
				if (value.ObjectValue.TryConvertToArray(out var array))
				{
					return array;
				}
			}
			return new JArray();
		}
	}






	/// <summary>
	/// JSON の 値。
	/// <para>
	/// このクラスは可変かつ参照型です。
	/// 「Javascript の Null を示す JValue のインスタンス」と「C# の null 」が別のものとして存在すること、
	/// 一般的な参照型のように C# の変数をによって情報が共有されることがあること、
	/// その内容は可変であるため、参照を介して別の箇所が変更されてしまうことなどに注意してください。</para>
	/// <para>
	/// JValue は各種の型から暗黙に変換できるようになっています。
	/// この挙動は場合によって意図しない変換が起こりうるため、引数などでは JVariant を利用することを想定しています。</para>
	/// </summary>
	public class JValue : IEquatable<JValue>
	{

		//==============================================================================
		// operators
		//==============================================================================

		// 暗黙変換
		public static implicit operator JValue(bool v) => new JValue(v);
		public static implicit operator JValue(long v) => new JValue(v);
		public static implicit operator JValue(double v) => new JValue(v);
		public static implicit operator JValue(string v) => new JValue(v);
		public static implicit operator JValue(JArray v) => new JValue(v);
		public static implicit operator JValue(JObject v) => new JValue(v);
		public static implicit operator JValue(JVariant v) => new JValue(v.Value);


		/// <summary>
		/// bool への変換
		/// <para>
		/// この変換は条件式として bool が要求される文脈で利用されるものです。
		/// 内容として bool を持つときの値は　BoolValue を利用してください。</para>
		/// <para>
		/// 変換は <see cref="IsEmpty"/> が利用されます。（BoolValue とは異なる値を返します）
		/// </para>
		/// </summary>
		public static bool operator true(JValue? v)
		{
			return !v.IsEmpty();
		}

		/// <summary>
		/// bool への変換
		/// <para>
		/// この変換は条件式として bool が要求される文脈で利用されるものです。
		/// 内容として bool を持つときの値は　BoolValue を利用してください。</para>
		/// <para>
		/// 変換は <see cref="IsEmpty"/> が利用されます。（BoolValue とは異なる値を返します）
		/// </para>
		/// </summary>
		public static bool operator false(JValue? v)
		{
			return v.IsEmpty();
		}

		/// <summary>
		/// 否定
		/// <para>
		/// operator false() と同じです。</para>
		/// </summary>
		public static bool operator !(JValue? v)
		{
			return v.IsEmpty();
		}

		/// <summary>
		/// 同値性比較
		/// <para>
		/// 内部がオブジェクトもしくは配列の場合は、参照している内部オブジェクト同じものであるかどうかを返します。
		/// 内容が同じであることを比較する場合は 
		/// <see cref="EquivalentTo(JValue?, int, int)">EquivalentTo()</see> を利用してください。</para>
		/// </summary>
		public static bool operator ==(JValue? a, JValue? b)
		{
			if (a is null || b is null)
			{
				return (a is null) && (b is null);
			}
			return a.Equals(b);
		}

		/// <summary>
		/// 非同値性比較
		/// <para>
		/// !( a==b )
		/// </para>
		/// </summary>
		public static bool operator !=(JValue? a, JValue? b)
		{
			return !(a == b);
		}

		//==============================================================================
		// instance members
		//==============================================================================

		private VariantType m_Type;
		private long m_IntValue;
		private double m_FloatValue;
		private object? m_RefValue;



		public JValue()
		{
			m_Type = VariantType.Null;
		}

		public JValue(bool b)
		{
			m_Type = VariantType.Boolean;
			m_IntValue = b ? 1 : 0;
		}

		public JValue(long i)
		{
			m_Type = VariantType.Integer;
			m_IntValue = i;
		}
		public JValue(double d)
		{
			m_Type = VariantType.Float;
			m_FloatValue = d;
		}
		public JValue(string s)
		{
			m_Type = VariantType.String;
			m_RefValue = s;
		}

		public JValue(JArray a)
		{
			m_Type = VariantType.Array;
			m_RefValue = a.GetBody();
		}

		public JValue(JObject o)
		{
			m_Type = VariantType.Object;
			m_RefValue = o.GetBody();
		}

		public JValue(IVariantConvertible? v)
		{
			if (v == null)
			{
				m_Type = VariantType.Null;
				return;
			}
			Assign(v.AsVariant());
		}

		/// <summary>
		/// シャロウコピーによるコンストラクタ。
		/// <para>
		/// C# のオブジェクトとしては別物になりますが、
		/// 内部に配列もしくはオブジェクトが入っていた場合は同じものを指すことになります。
		/// null が与えられた場合は Null を指す JValue として初期化されます。</para>
		/// </summary>
		public JValue(JValue source)
		{
			if (source == null)
			{
				m_Type = VariantType.Null;
				return;
			}
			m_Type = source.m_Type;
			m_IntValue = source.m_IntValue;
			m_FloatValue = source.m_FloatValue;
			m_RefValue = source.m_RefValue;
		}


		/// <summary>
		/// 保持している値の種類。
		/// <para>
		/// - <seealso cref="JValueExtensions.GetVariantType(JValue)">GetVariantType()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsNull(JValue)">IsNull()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsBoolean(JValue)">IsBoolean()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsNumber(JValue)">IsNumber()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsString(JValue)">IsString()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsArray(JValue)">IsArray()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsObject(JValue)">IsObject()</seealso><br/>
		/// </para>
		/// </summary>
		public VariantType VariantType => m_Type;


		/// <summary>
		/// 内容が Empty(空) であるかどうかを返す。
		/// <para>
		/// Empty の意味は内容の型によって異なり、以下のとおりです。
		/// <list>
		/// <item>Null     ⇒ true.</item>
		/// <item>Boolean  ⇒ false であるか</item>
		/// <item>Integer  ⇒ 0 であるか</item>
		/// <item>Float    ⇒ 0.0 であるか</item>
		/// <item>String   ⇒ 長さ 0 であるか</item>
		/// <item>Array    ⇒ 要素数が 0 であるか</item>
		/// <item>Object   ⇒ 要素数が 0 であるか</item>
		/// </list>
		/// </para>
		/// <para>
		/// 拡張メソッド <see cref="JValueExtensions.IsEmpty(JValue)">IsEmpty()</see> を利用してください。 
		/// </para>
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				switch (m_Type)
				{
					case VariantType.Null: return true;
					case VariantType.Boolean: return m_IntValue == 0;
					case VariantType.Integer: return m_IntValue == 0;
					case VariantType.Float: return m_FloatValue == 0.0;
					case VariantType.String: return String.IsNullOrEmpty(m_RefValue as string);
					case VariantType.Array: return GetArrayBody()!.Count == 0;
					case VariantType.Object: return GetObjectBody()!.Count == 0;
				}
				SystemDebug.Assert(false);
				return true;
			}
		}

		/// <summary>
		/// 要素数。
		/// <para>
		/// VariantType が Array, Object のときはその要素数を、
		/// Null のときは 0 を、
		/// String のときは文字数を、
		/// それ以外のときは 1 を返します。</para>
		/// </summary>
		public int Count
		{
			get
			{
				if (m_Type == VariantType.Array)
				{
					return GetArrayBody()!.Count;
				}
				if (m_Type == VariantType.Object)
				{
					return GetObjectBody()!.Count;
				}
				if (m_Type == VariantType.Null)
				{
					return 0;
				}
				if (m_Type == VariantType.String)
				{
					return StringValue.Length;
				}
				return 1;
			}
		}

		/// <summary>
		/// bool 値。bool 以外を持っている場合はそれなりに変換しますが、それに依存しないようにしてください。
		/// <para>
		/// bool 以外を持っていた場合は以下の値を返します。
		/// Null    ⇒ false
		/// Integer ⇒ 0 以外のとき true
		/// Float   ⇒ 0.0 と等しくないとき true
		/// String  ⇒ 数値として解釈可能であればそれが 0 以外のとき true. 数値ではないときは "true" と Case Insensitive に比較した結果
		/// Array   ⇒ 要素数が 0 ではないとき true
		/// Object  ⇒ 要素数が 0 ではないとき true</para>
		/// <seealso cref="JVariantExtensions.AsBool(JVariant)"/>
		/// </summary>
		public bool BoolValue
		{
			get
			{
				switch (m_Type)
				{
					case VariantType.Boolean: return m_IntValue != 0;
					case VariantType.Null: return false;
					case VariantType.Integer: return m_IntValue != 0;
					case VariantType.Float: return m_FloatValue != 0.0;
					case VariantType.String:

						StringView s = StringValue.View();
						if (s.EqualsIgnoreCase("true"))
						{
							return true;
						}
						if (s.TryParseInt(out int x))
						{
							return x != 0;
						}
						return false;
					case VariantType.Array: return GetArrayBody()!.Count != 0;
					case VariantType.Object: return GetObjectBody()!.Count != 0;
				}
				SystemDebug.Assert(false);
				return false;
			}
		}

		/// <summary>
		/// long 値。数値以外を持っている場合はそれなりに変換しますが、それに依存しないようにしてください。
		/// <para>
		/// 整数以外を持っていた場合は以下の値を返します。
		/// Null   ⇒ 0
		/// Bool   ⇒ true は 1, false は 0 
		/// Float  ⇒ int キャストした結果
		/// String ⇒ 数値として解釈可能なときはその数値、でなければ 0.
		/// Array  ⇒ 要素数
		/// Object ⇒ 要素数</para>
		/// <seealso cref="JValueExtensions.AsLong(JVariant)"/>
		/// </summary>
		public long LongValue
		{
			get
			{
				switch (m_Type)
				{
					case VariantType.Integer: return m_IntValue;
					case VariantType.Null: return 0;
					case VariantType.Boolean: return m_IntValue;
					case VariantType.Float: return (long)m_FloatValue;
					case VariantType.String:
						if (long.TryParse(m_RefValue as string, out long ret))
						{
							return ret;
						}
						return 0;

					case VariantType.Array:
						return GetArrayBody()!.Count;
					case VariantType.Object:
						return GetObjectBody()!.Count;
				}

				SystemDebug.Assert(false);
				return 0;
			}
		}


		/// <summary>
		/// int 値。数値以外を持っている場合はそれなりに変換しますが、それに依存しないようにしてください。
		/// <para>
		/// 整数以外を持っていた場合は以下の値を返します。
		/// Null   ⇒ 0
		/// Bool   ⇒ true は 1, false は 0 
		/// Float  ⇒ int キャストした結果
		/// String ⇒ 数値として解釈可能なときはその数値、でなければ 0.
		/// Array  ⇒ 要素数
		/// Object ⇒ 要素数</para>
		/// <seealso cref="JValueExtensions.AsInt(JVariant)"/>
		/// </summary>
		public int IntValue => (int)this.LongValue;

		/// <summary>
		/// double 値。数値以外を持っている場合はそれなりに変換しますが、それに依存しないようにしてください。
		/// <para>
		/// 少数以外を持っていた場合は以下の値を返します。
		/// Null    ⇒ 0.0
		/// Bool    ⇒ true は 1.0, false は 0.0
		/// Integer ⇒ そのまま
		/// String  ⇒ 数値として解釈可能なときはその数値、でなければ 0.
		/// Array   ⇒ 要素数
		/// Object  ⇒ 要素数</para>
		/// <seealso cref="JValueExtensions.AsDouble(JValue)"/>
		/// </summary>
		public double DoubleValue
		{
			get
			{
				switch (m_Type)
				{
					case VariantType.Float: return m_FloatValue;
					case VariantType.Null: return 0.0;
					case VariantType.Boolean: return m_IntValue;
					case VariantType.Integer: return m_IntValue;
					case VariantType.String:
						if (double.TryParse(m_RefValue as string, out double ret))
						{
							return ret;
						}
						if (m_RefValue as string == "NaN")
						{
							return double.NaN;
						}
						if (m_RefValue as string == "infinity")
						{
							return double.PositiveInfinity;
						}
						if (m_RefValue as string == "negative infinity")
						{
							return double.NegativeInfinity;
						}
						return 0;

					case VariantType.Array:
						return GetArrayBody()!.Count;
					case VariantType.Object:
						return GetObjectBody()!.Count;
				}

				SystemDebug.Assert(false);
				return 0;
			}
		}

		/// <summary>
		/// float 値。数値以外を持っている場合はそれなりに変換しますが、それに依存しないようにしてください。
		/// <para>
		/// 少数以外を持っていた場合は以下の値を返します。
		/// Null    ⇒ 0.0
		/// Bool    ⇒ true は 1.0, false は 0.0
		/// Integer ⇒ そのまま
		/// String  ⇒ 数値として解釈可能なときはその数値、でなければ 0.
		/// Array   ⇒ 要素数
		/// Object  ⇒ 要素数</para>
		/// <seealso cref="JValueExtensions.AsFloat(JValue)"/>
		/// </summary>
		public float FloatValue => (float)this.DoubleValue;

		/// <summary>
		/// 文字列表現を返す。
		/// <para>ToString() と同じです。
		/// string 以外を持っているときはなんとなく内容を表す文字列を返します。
		/// string以外を持っているときにここから返却される文字列に依存しないようにしてください。</para>
		/// </summary>
		public string StringValue => m_Type == VariantType.String ? (m_RefValue as string ?? string.Empty) : ToString();

		/// <summary>
		/// 配列としての値。
		/// <para>
		/// 内部が配列の場合はそれを返します。
		/// Null の場合は空の配列、
		/// Object ですべてのキーが int として解釈可能な場合はそれぞれの要素を各 index に詰めた配列。
		/// それ以外では VariantException を投げます。</para>
		/// <para>
		/// 配列の場合は内部の配列オブジェクトそのものを返します。それ以外では新しい配列を生成して返します。
		/// </para>
		/// <seealso cref="JValueExtensions.AsArray(JValue)"/>
		/// </summary>
		public JArray ArrayValue
		{
			get
			{
				if (m_Type == VariantType.Array)
				{
					return JArray.CreateInternal((List<JValue>)m_RefValue!);
				}
				if (m_Type == VariantType.Null)
				{
					return new JArray();
				}
				if (m_Type == VariantType.Object)
				{
					JObject obj = JObject.CreateInternal(GetObjectBody());
					if( obj.TryConvertToArray(out JArray array))
					{
						return array;
					}
				}
				throw new VariantException($"JValue does not contain an array. Actual type: {m_Type}");
			}
		}

		/// <summary>
		/// オブジェクトとしての値。
		/// <para>
		/// 内部がオブジェクトの場合はそれを返します。
		/// Null の場合は空のオブジェクト、
		/// Array の場合は配列をオブジェクトに変換したものを返します。
		/// それ以外では VariantException を投げます。</para>
		/// <para>
		/// オブジェクトの場合は内部のオブジェクトそのものを返します。それ以外では新しいオブジェクトを生成して返します。</para>
		/// </summary>
		public JObject ObjectValue
		{
			get
			{
				if (m_Type == VariantType.Object)
				{
					return JObject.CreateInternal((JObjectBody)m_RefValue!);
				}

				if (m_Type == VariantType.Null)
				{
					return new JObject();
				}

				if (m_Type == VariantType.Array)
				{
					var array = JArray.CreateInternal(GetArrayBody());
					return array.ConvertToObject();
				}

				throw new VariantException($"JValue does not contain an object. Actual type: {m_Type}");
			}
		}


		/// <summary>
		/// 同一性比較
		/// <para>
		/// 配列やオブジェクトを持っている場合の同一性は、
		/// 同じオブジェクトを参照しているときのみ true になります。
		/// 内容を検証するわけではないので注意してください。
		/// </para>
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj is JVariant other)
			{
				return this.Equals(other.Value);
			}
			if (obj is JValue value)
			{
				return this.Equals(value);
			}
			return false;
		}

		/// <summary>
		/// 同一性比較
		/// <para>
		/// 配列やオブジェクトを持っている場合の同一性は、
		/// 同じオブジェクトを参照しているときのみ true になります。
		/// 内容を検証するわけではないので注意してください。
		/// </para>
		/// </summary>
		public bool Equals(JValue? other)
		{
			if (other is null)
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			var vt = m_Type;

			// 型が違う
			if (vt != other.m_Type)
			{
				return false;
			}

			if (vt == VariantType.Null)
			{
				return true;
			}
			if (vt == VariantType.Boolean)
			{
				return ((m_IntValue != 0) == (other.m_IntValue != 0));
			}
			if (vt == VariantType.Integer)
			{
				return (m_IntValue == other.m_IntValue);
			}
			if (vt == VariantType.Float)
			{
				return (m_FloatValue == other.m_FloatValue);
			}
			if (vt == VariantType.String)
			{
				return StringValue.Equals(other.StringValue);
			}
			if (vt == VariantType.Array || vt == VariantType.Object)
			{
				return object.ReferenceEquals(m_RefValue, other.m_RefValue);
			}

			SystemDebug.Assert(false);
			return false;
		}


		/// <summary>
		/// ハッシュコード
		/// <para>
		/// それなりに仕様に則ったハッシュコードを返しますが、
		/// JVariant をハッシュのキーにするようなことは避けてください。
		/// </para>
		/// </summary>
		public override int GetHashCode()
		{
			var vt = m_Type;
			int ret = (int)vt * 419;

			if (vt == VariantType.Null)
			{
				return ret;
			}
			if (vt == VariantType.Integer || vt == VariantType.Boolean)
			{
				return ret + m_IntValue.GetHashCode();
			}
			if (vt == VariantType.Float)
			{
				return ret + m_FloatValue.GetHashCode();
			}
			if (vt == VariantType.String)
			{
				return ret + this.StringValue.GetHashCode();
			}

			return ret + m_RefValue!.GetHashCode();
		}

		/// <summary>
		/// 文字列化
		/// <para>
		/// string の場合は設定されている string そのもの。
		/// その他はなんとなく内容を表す文字列を返します。JSON表現ではないので注意してください。</para>
		/// <para>
		/// null のとき "null" ではなく空の文字列を返します。</para>
		/// </summary>
		public override string ToString()
		{
			switch (m_Type)
			{
				case VariantType.Null: return "";
				case VariantType.Boolean: return IntValue != 0 ? "true" : "false";
				case VariantType.Integer: return LongValue.ToString();
				case VariantType.Float: return DoubleValue.ToString();
				case VariantType.String: return StringValue;
			}
			if (m_Type == VariantType.Array)
			{
				return ArrayValue.ToString();
			}

			if (m_Type == VariantType.Object)
			{
				return ObjectValue.ToString();
			}

			SystemDebug.Assert(false);
			return "";
		}

		/// <summary>
		/// クリア
		/// <para>
		/// Null を示すようになります。</para>
		/// </summary>
		public void Clear()
		{
			m_Type = VariantType.Null;
			m_IntValue = 0;
			m_FloatValue = 0;
			m_RefValue = null;
		}

		/// <summary>代入。</summary>
		public void Assign(bool v)
		{
			m_Type = VariantType.Boolean;
			m_IntValue = v ? 1 : 0;
			m_RefValue = null;
		}

		/// <summary>代入。</summary>
		public void Assign(long v)
		{
			m_Type = VariantType.Integer;
			m_IntValue = v;
			m_RefValue = null;
		}
		/// <summary>代入。</summary>
		public void Assign(double v)
		{
			m_Type = VariantType.Float;
			m_FloatValue = v;
			m_RefValue = null;
		}
		/// <summary>代入。</summary>
		public void Assign(string? v)
		{
			m_Type = VariantType.String;
			m_RefValue = v ?? "";
		}
		/// <summary>代入。</summary>
		public void Assign(JArray v)
		{
			m_Type = VariantType.Array;
			m_RefValue = v.GetBody();
		}
		/// <summary>代入。</summary>
		public void Assign(JObject v)
		{
			m_Type = VariantType.Object;
			m_RefValue = v.GetBody();
		}

		/// <summary>代入。</summary>
		public void Assign(JVariant v)
		{
			Assign(v.Value);
		}

		/// <summary>代入。</summary>
		public void Assign(JValue v)
		{
			if (v == null)
			{
				m_Type = VariantType.Null;
				m_RefValue = null;
				return;
			}
			m_Type = v.m_Type;
			m_IntValue = v.m_IntValue;
			m_FloatValue = v.m_FloatValue;
			m_RefValue = v.m_RefValue;
		}

		/// <summary>
		/// 配列としての要素の追加。
		/// <para>
		/// 自身が配列であれば、そのまま自分に要素を追加します。
		/// それ以外の場合、 Null であれば空の配列を生成してから要素を追加、
		/// Object であれば、各キーが int として解釈可能であることを確認してそれによって自身を配列に変換してから要素を追加します。
		/// それ以外の場合は VariantException を投げます。 </para>
		/// </summary>
		public void Add(JValue v)
		{
			this.SwitchToArray().Add(v);
		}

		/// <summary>
		/// 配列としての要素設定。
		/// <para>
		/// 指定インデックスの値を設定します。
		/// 自身が配列であればそのまま設定、
		/// null であれば空の配列を生成してから設定、
		/// Object であれば、各キーが int として解釈可能であることを確認してそれによって自身を配列に変換してから設定します。
		/// それ以外の場合は VariantException を投げます。
		/// </para>
		public void Set(int index, JValue v)
		{
			EnsureArrayItem(index).Assign(v);
		}

		/// <summary>
		/// 配列としての値取得
		/// <para>
		/// 指定インデックスの値を取得します。</para>
		/// <para>
		/// 自身が配列であればそのまま取得、
		/// Object であればインデックスを文字列解釈して値を取得します。</para>
		/// <para>
		/// それ以外は Null を示す JValue を返します。</para>
		/// <para>
		/// ないのと null が入っていることの区別はこのメソッドではできません。
		/// </para>
		/// </summary>
		public JVariant Get(int index)
		{
			if (m_Type == VariantType.Null)
			{
				return new JValue();
			}

			if (m_Type == VariantType.Array)
			{
				return ArrayValue.Get(index);
			}
			if (m_Type == VariantType.Object)
			{
				return ObjectValue.Get(index.ToString());
			}

			return new JValue();
		}

		/// <summary>
		/// 配列としてのインデクサ
		/// <para>
		/// index で示される要素を取得設定します。
		/// 自分自身がオブジェクトの場合は index は文字列として解釈されます。
		/// null の場合は自分を空の配列に変換してからあつかいます。
		/// それ以外の場合は Variant Exception を投げます。</para>
		/// <para>
		/// 取得操作であっても内容が変更されることに注意してください。
		/// （自分自身が配列でなければ配列に変換される、指定された要素がなければ確保する）
		/// その挙動が望ましくない場合は Get() を利用してください。</para>
		/// </summary>
		public JVariant this[int index]
		{
			get => EnsureArrayItem(index);
			set => EnsureArrayItem(index).Assign(value);
		}

		/// <summary>
		/// オブジェクト としての値の設定。
		/// <para>
		/// 自身が null であれば空のオブジェクトを生成してから設定。
		/// 配列であれば、key が int として解釈可能であればそれをインデックスとして配列に設定。そうでない場合は自分をオブジェクトに変換してから設定。
		/// オブジェクトであればそのまま設定。
		/// それ以外の場合は VariantException を投げます。</para>
		/// </summary>
		public void Set(StringView key, JValue v)
		{
			EnsureObjectItem(key).Assign(v);
		}

		/// <summary>
		/// オブジェクトとしての値の取得。
		/// <para>
		/// 自身がオブジェクトであればそのまま取得。
		/// 配列であれば、key が int として解釈可能であればそれをインデックスとして配列から取得。
		/// それ以外の場合は Null を示す JValue を返します。</para>
		/// <para>
		/// ないのと null が入っていることの区別はこのメソッドではできません。
		/// </para>
		/// </summary>
		public JVariant Get(StringView key)
		{
			if (m_Type == VariantType.Object)
			{
				return ObjectValue.Get(key);
			}
			if (m_Type == VariantType.Array)
			{
				if (key.TryParseInt(out int index))
				{
					return ArrayValue.Get(index);
				}
			}
			return new JValue();
		}

		/// <summary>
		/// オブジェクトとしてのインデクサ
		/// <para>
		/// key で示される要素を取得設定します。
		/// 自分自身が配列の場合は key は数値として解釈されます。</para>
		/// <para>
		/// 自身が null であれば自分を空のオブジェクトに変換してからあつかいます。
		/// <para>
		/// 配列の場合は、まず key を数値として解釈して配列にアクセスを試みます。
		/// 数値として解釈できない場合は自分をオブジェクトに変換してからアクセスします。</para>
		/// <para>
		/// 配列でもオブジェクトでも null でもない場合は VariantException を投げます。</para>
		/// <para>
		/// 取得操作であっても内容が変更されることに注意してください。
		/// （自分自身がオブジェクトでなければオブジェクトに変換される、指定された要素がなければ確保する）
		/// その挙動が望ましくない場合は Get() を利用してください。</para>
		/// </summary>
		public JVariant this[StringView key]
		{
			get => EnsureObjectItem(key);
			set => EnsureObjectItem(key).Assign(value);
		}

		/// <summary>
		/// 指定したキーが、現在のコレクション内に存在するかどうかを判断する。
		/// <para>
		/// オブジェクト型の場合はキーの存在を、配列型の場合はキーを文字列として解釈してインデックスが有効かどうかを判定します。
		/// その他の型では false を返します。</para>
		/// </summary>
		public bool ContainsKey(StringView key)
		{
			if (m_Type == VariantType.Object)
			{
				return ObjectValue.ContainsKey(key);
			}
			if (m_Type == VariantType.Array)
			{
				if (key.TryParseInt(out int index))
				{
					return index >= 0 && index < ArrayValue.Count;
				}
			}
			return false;
		}


		/// <summary>
		/// JSON 文字列化。
		/// </summary>
		/// <param name="policy">フォーマット指定、省略した場合は改行なし。</param>
		public string Stringify(JsonFormatPolicy? policy = null)
		{
			policy ??= JsonFormatPolicy.OneLiner;

			return ToJSON(policy);
		}

		/// <summary>
		/// JSON 化。
		/// <para>
		/// 内容の JSON 表現を返します。</para>
		/// <para>
		/// 配列やオブジェクトは内部に配列やオブジェクトを持ち、更に参照経由で内容を共有することができるため、
		/// 親子関係は循環していることがあります。
		/// この実装ではそれらを検出することはできないため階層の深さを見ています。
		/// 64(JsonFormatPolicy.MaxDepth) 以上ネストしたデータは変換できません。
		/// <para>
		/// 以下の状況で <see cref="VaraintException"/> を投げます。
		/// <list type="bullet">
		///   <item>ネストが <see cref="JsonFormatPolicy.MaxDepth"/> を越えている。</item>
		///   <item><see cref="JsonFormatPolicy.SpecialFloatPolicy"/> に Throw が指定され、Number 値に NaN, Infinity 等が含まれる。</item>
		/// </list>
		/// </para>
		/// </summary>
		/// <param name="policy">フォーマット指定、省略した場合はそれなりに改行するポリシー</param>
		public string ToJSON(JsonFormatPolicy? policy = null)
		{
			policy ??= JsonFormatPolicy.Mixed;

			// そのまま文字列になるもの
			switch (m_Type)
			{
				case VariantType.Null: return "null";
				case VariantType.Boolean: return m_IntValue != 0 ? "true" : "false";
				case VariantType.Integer: return m_IntValue.ToString();
			}

			var context = StringifyContext.ForString(policy);
			try
			{
				ConvertToJSON(ref context);
				return context.StringResult();
			}
			finally
			{
				context.Dispose();
			}
		}

		/// <summary>
		/// UTF-8 バイナリによる JSON 化。
		/// </summary>
		public U8View ToU8JSON(JsonFormatPolicy? policy = null)
		{
			policy ??= JsonFormatPolicy.Mixed;

			// そのまま文字列になるもの
			switch (m_Type)
			{
				case VariantType.Null: return Literal.Null.U8;
				case VariantType.Boolean: return m_IntValue != 0 ? Literal.True.U8 : Literal.False.U8;
			}

			var context = StringifyContext.ForU8(policy);
			try
			{
				ConvertToJSON(ref context);
				return context.U8Result();
			}
			finally
			{
				context.Dispose();
			}
			
		}

		internal void ConvertToJSON(ref StringifyContext context)
		{
			var buffer = context.GetBuffer();
			switch (m_Type)
			{
				case VariantType.Null:
					buffer.Append(Literal.Null);
					break;
				case VariantType.Boolean:
					buffer.Append(m_IntValue != 0 ? Literal.True : Literal.False);
					break;
				case VariantType.Integer:
					buffer.Append(m_IntValue);
					break;
				case VariantType.Float:
					AppendFloat(ref context, m_FloatValue);
					break;
				case VariantType.String:
					AppendString(ref context, m_RefValue as string);
					break;
				case VariantType.Array:
					JArray.CreateInternal(GetArrayBody()).ConvertToJSON(ref context);
					break;
				case VariantType.Object:
					JObject.CreateInternal(GetObjectBody()).ConvertToJSON(ref context);
					break;
			}
		}

		// 浮動小数の JSON 化。NaN, Infinity 等の特殊な値の扱いは context.Policy.SpecialFloatPolicy に従います。
		private static void AppendFloat(ref StringifyContext context, double v)
		{
			var floatPolicy = context.Policy.SpecialFloatPolicy;
			var buffer = context.GetBuffer();

			void AppendSpecialFloat(Literal literal)
			{
				if (floatPolicy == SpecialFloatPolicy.Throw)
				{
					throw new JsonFormatException($"{literal.U16} is not allowed.");
				}
				if (floatPolicy == SpecialFloatPolicy.AsJsLiteral)
				{
					buffer.Append(literal);
					return;
				}
				if (floatPolicy == SpecialFloatPolicy.AsString)
				{
					buffer.Append('"');
					buffer.Append(literal);
					buffer.Append('"');
					return;
				}
			}

			if (double.IsNaN(v))
			{
				AppendSpecialFloat(Literal.NaN);
				return;
			}
			if (double.IsPositiveInfinity(v))
			{
				AppendSpecialFloat(Literal.Infinity);
				return;
			}
			if (double.IsNegativeInfinity(v))
			{
				AppendSpecialFloat(Literal.NegativeInfinity);
				return;
			}

			buffer.Append(v);
		}

		private static void AppendString(ref StringifyContext context, string? v)
		{
			string escaped = TextUtil.EscapeJsonString(v, context.Policy.EscapeMultiBytes);
			var buffer = context.GetBuffer();

			buffer.Append('"');
			buffer.Append(escaped);
			buffer.Append('"');
		}

		// Object として key の要素を確保して返す。
		// 配列の場合は key が int 解釈可能であればそちらを優先して確保して返す。
		// そうでない場合は自分をオブジェクトに変換してから確保して返す。
		// 自分がオブジェクトであればそのまま確保して返す。
		// 自分が null であれば自分をオブジェクトに変換してから確保して返す。
		// それ以外の場合は VariantException を投げる。
		private JValue EnsureObjectItem(StringView key)
		{
			if (m_Type == VariantType.Null)
			{
				SwitchToObject();
			}

			if (m_Type == VariantType.Array)
			{
				if (key.TryParseInt(out int index))
				{
					return EnsureArrayItem(index);
				}
				SwitchToObject();
			}

			if (m_Type == VariantType.Object)
			{
				return ObjectValue[key];
			}

			throw new VariantException($"JValue does not contain an object. Actual type: {m_Type}");
		}

		private JValue EnsureArrayItem(int index)
		{
			if (m_Type == VariantType.Null)
			{
				SwitchToArray();
			}
			if (m_Type == VariantType.Array)
			{
				return ArrayValue[index];
			}
			if (m_Type == VariantType.Object)
			{
				return ObjectValue[index.ToString()];
			}
			throw new VariantException($"JValue does not contain an array or object. Actual type: {m_Type}");
		}


		private JObject SwitchToObject()
		{
			if (m_Type == VariantType.Null || m_Type == VariantType.Array)
			{
				this.Assign(ObjectValue);
			}
			if (m_Type == VariantType.Object)
			{
				return JObject.CreateInternal((JObjectBody)m_RefValue!);
			}
			throw new VariantException($"JValue does not contain an object. Actual type: {m_Type}");
		}

		// 自分自身を Array に変換
		// VariantException を投げることがあります。
		private JArray SwitchToArray()
		{
			if (m_Type != VariantType.Array)
			{
				this.Assign(ArrayValue);
			}
			return JArray.CreateInternal(GetArrayBody());
		}



		internal string GetSimpleString()
		{
			switch (m_Type)
			{
				case VariantType.Null: return "null";
				case VariantType.Boolean: return IntValue != 0 ? "true" : "false";
				case VariantType.Integer: return m_IntValue.ToString();
				case VariantType.Float: return m_FloatValue.ToString();
				case VariantType.String: return "\"" + (m_RefValue as string) + "\"";
				case VariantType.Array: return "<array>";
				case VariantType.Object: return "<object>";
			}
			return "";
		}

		// 配列としての Body を返す。配列ではないときは null を返すので注意。
		private List<JValue>? GetArrayBody()
		{
			if (m_Type == VariantType.Array)
			{
				return m_RefValue as List<JValue>;
			}
			return null;
		}

		// 配列としての Body を返す。配列ではないときは null を返すので注意。
		private JObjectBody? GetObjectBody()
		{
			if (m_Type == VariantType.Object)
			{
				return m_RefValue as JObjectBody;
			}
			return null;
		}
	}
}
