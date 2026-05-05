using System;
using System.Collections.Generic;
using System.Linq;
using Gatebox.Variant.Internal;

#nullable enable

namespace Gatebox.Variant
{


	/// <summary>
	/// JSON の値を表す構造体。
	/// <para>
	/// 内部に <see cref="JValue"/> を持つ構造体です。
	/// <see cref="JValue"/> は javascript の変数に相当するクラスであり、JVariant はそれをラップする構造体です。</para>
	/// <para>
	/// この構造体は(<see cref="JObject"/>, <see cref="JArray"/>) と異なり、
	/// readonly struct であり、直接内容を変更するメソッドを持ちません。
	/// JValue に対する変更を想定していない View としてのインターフェースを持ちます。
	/// （同じ JValue への参照を持つのであって、参照されている JValue は変更可能であるため意味的には必ずしも不変ではないことに注意してください。）
	/// </para>
	/// </summary>
	public readonly struct JVariant: IEquatable<JVariant>
	{
		//==============================================================================
		// static members
		//==============================================================================
		public const int DefaultMaxDepth = 64;


		/// <summary>
		/// Null を指す JVariant.
		/// </summary>
		public static readonly JVariant Null = new JVariant();

		/// <summary>
		/// Json 文字列をパースして JVariant を返す。
		/// <para>
		/// 多少パースはゆるくなっていて、厳密には JSON ではない文字列もパースします。
		/// ・ オブジェクトのキーが " でくくられていなくとも良い。アルファベットのみの連続はキー名として扱われる。
		/// ・ 数値の解釈が int.TryParse で行われる。無駄な先行 + などは JSON 的には ill-formed だが、パースされる。
		/// ・ Object Array の末尾に , があって良い。
		/// ・ /* */ // 形式のコメントがあってもよい。
		/// これらが問題になることはないと思われますが、これを期待することは避けてください。</para>
		/// <para>
		/// デフォルトではパースに失敗した場合は Null を示す JVariant を返します。
		/// 失敗を例外としたい場合は throws に true を指定してください。<see cref="VariantException"/> を投げます。</para>
		/// </summary>
		/// <param name="source">パースする JSON 文字列</param>
		/// <param name="throws">パースエラー時に例外を投げるかどうか</param>
		public JVariant Parse( StringView source, bool throws = false)
		{
			return JsonParser.CreateTemporary().Parse(source, throws);
		}

		/// <summary>
		/// JTF-8 JSON 文字列をパースして JVariant を返す。
		/// <see cref="Parse(StringView, bool)">StringView を受けるバージョン</see>を参照してください。
		/// </summary>
		public JVariant Parse(U8View source, bool throws = false)
		{
			return JsonParser.CreateTemporary().Parse(source, throws);
		}

		/// <summary>
		/// 任意型からの生成
		/// </summary>
		public static JVariant Create()
		{
			return new JVariant();
		}

		/// <summary>
		/// 任意型からの生成
		/// </summary>
		public static JVariant Create<T>(T t)
		{
			if (t is null)
			{
				return new JVariant();
			}

			var v = VariantConverter.CreateVariantFixed(t);
			if (v is not null)
			{
				return v;
			}

			var context = ConvertContext.Acquire();
			try
			{
				return context.Converter.CreateVariantFrom(t, typeof(T));
			}
			finally
			{
				context.Release();
			}
		}

		/// <summary>
		/// 任意型からの生成。
		/// <para>
		/// Create との違いは実行時の型を利用して変換することです。
		/// </para>
		/// </summary>
		public static JVariant CreateDynamic(object? t)
		{
			if (t is null)
			{
				return new JVariant();
			}
			var v = VariantConverter.CreateVariantFixed(t);
			if (v is not null)
			{
				return v;
			}

			var context = ConvertContext.Acquire();
			try
			{
				return context.Converter.CreateVariantFrom(t, t.GetType());
			}
			finally
			{
				context.Release();
			}
		}

		/// <summary>
		/// JSON Pointer のパース
		/// <para>
		/// 先頭は / で始まっていなければいけないものとします。
		/// (JSON Pointer 的には空文字列が許される（自分自身）らしいですが、それはないものとします。)
		/// 以後 / で区切り ~0 => ~, ~1 => / によるエスケープを処理してリストで返します。
		/// </para>
		/// </summary>
		public static IList<StringView> ParseJsonPointer(StringView pointer)
		{
			if (pointer.IsBlank())
			{
				return new List<StringView>();
			}

			if (pointer[0] != '/')
			{
				throw new ArgumentException("JSON Pointer must start with '/'", nameof(pointer));
			}

			// "/" はルートを指すので空リストを返す
			if (pointer.Length == 1)
			{
				return new List<StringView>();
			}

			// StringView として Split. 頭の / があるので最初はスキップ
			var segments = pointer.Split('/').Skip(1).ToList();

			// 個々のパートはひとつづつ見ていくしかない。
			for (int i = 0; i < segments.Count; i++)
			{
				var segment = segments[i];

				// チルダを探す、ない場合はそのままでよい。
				int tilde = segment.Find('~');
				if (tilde < 0)
				{
					continue;
				}

				// RFC 6901: "~1" => "/", "~0" => "~" (and reject invalid "~" usage)
				var s = segment.ToString();
				for (int j = 0; j < s.Length; j++)
				{
					if (s[j] != '~')
					{
						continue;
					}

					if (j + 1 >= s.Length)
					{
						throw new ArgumentException("Invalid escape sequence in JSON Pointer.", nameof(pointer));
					}

					char next = s[j + 1];
					if (next != '0' && next != '1')
					{
						throw new ArgumentException("Invalid escape sequence in JSON Pointer.", nameof(pointer));
					}

					j++; // skip the escaped char
				}

				segments[i] = s.Replace("~1", "/").Replace("~0", "~");
			}

			return segments;
		}

		//==============================================================================
		// operators
		//==============================================================================

		public static implicit operator JVariant(JValue v) => new (v);
		public static implicit operator JVariant(JObject v) => new(v);
		public static implicit operator JVariant(JArray v) => new(v);

		/// <summary>
		/// bool への変換
		/// <para>
		/// この変換は条件式として bool が要求される文脈で利用されるものです。
		/// 内容として bool を持つときの値は　BoolValue を利用してください。</para>
		/// <para>
		/// 変換は IsEmpty が利用されます。（BoolValue とは異なる値を返します）
		/// </para>
		/// </summary>
		public static bool operator true(JVariant v)
		{
			return !v.IsEmpty();
		}
		public static bool operator false(JVariant v)
		{
			return v.IsEmpty();
		}

		/// <summary>
		/// 否定
		/// <para>
		/// operator false() と同じです。</para>
		/// </summary>
		public static bool operator !(JVariant v)
		{
			return v.IsEmpty();
		}

		/// <summary>
		/// 同値性比較
		/// <para>
		/// 内部がオブジェクトもしくは配列の場合は、参照している内部オブジェクト同じものであるかどうかを返します。
		/// 内容が同じであることを比較する場合は 
		/// <see cref="EquivalentTo(JVariant, int, int)">EquivalentTo()</see> を利用してください。</para>
		/// </summary>
		public static bool operator ==(JVariant a, JVariant b)
		{
			return a.Equals(b);
		}

		/// <summary>
		/// 非同値性比較
		/// <para>
		/// !( a==b )
		/// </para>
		/// </summary>
		public static bool operator !=(JVariant a, JVariant b)
		{
			return !(a == b);
		}

		//==============================================================================
		// instance members
		//==============================================================================

		private readonly JValue? m_Value;

		// Unity(C#9.0) のため、デフォルトコンストラクタを持つことができない。
		// public JVariant() => m_Value = null;

		/// <summary>
		/// コピーによるコンストラクタ
		/// <para>
		/// 同じ JValue を参照します。
		/// ディープコピーが必要な場合は Duplicate() を利用してください。</para>
		/// </summary>
		public JVariant(JVariant value) => m_Value = value.m_Value;

		public JVariant(bool value) => m_Value = value;
		public JVariant(long value) => m_Value = value;
		public JVariant(double value) => m_Value = value;
		public JVariant(string value) => m_Value = value;
		public JVariant(JArray value) => m_Value = value;
		public JVariant(JObject value) => m_Value = value;

		/// <summary>
		/// JValue を直接指定するコンストラクタ
		/// <para>
		/// 基本的に JValue は null 非許容ですが、
		/// 実行時の挙動としてはこのコンストラクタは null を受け入れます。</para>
		/// <para>
		/// Null を指す JValue と null では意味的には同一視されるように設計していますが、
		/// 厳密にはこのコンストラクタに null を渡すと内部の参照も null になり、参照先の JValue が共有されるものであるか否かが異なります。
		/// （JVariant a があり、JVariant b = new(a.Value!) としたとき、a と b は同じ JValue を参照することが期待されるが null の時はそうではない。</para>
		/// <para>
		/// 現実的にこれが問題になることはないと思われますが、null を渡すことは避けるべきです</para>
		/// </summary>
		public JVariant(JValue value) => m_Value = value;


		/// <summary>
		/// 内部の値の型
		/// </summary>
		public readonly VariantType VariantType => m_Value?.VariantType ?? VariantType.Null;

		/// <summary>
		/// 保持する値
		/// <para>
		/// null を返却しうるので注意してください。</para>
		/// <para>
		/// 厳密にいうと内部の参照が null であることと Null を指す JValue を参照していることは異なり、このプロパティはそれを示します。
		/// JVariant 全体としてはその二者をできるだけ同一視するように設計していますが、
		/// このプロパティに関しては null を返すことがあることに注意してください。
		/// </para>
		/// </summary>
		public readonly JValue? Value => m_Value;

		/// <summary>
		/// 要素数。
		/// <para>
		/// VariantType が Array, Object のときはその要素数を、
		/// Null のときは 0 を、
		/// string のときは文字数を、
		/// それ以外のときは 1 を返します。</para>
		/// </summary>
		public int Count => m_Value?.Count ?? 0;
		


		public VariantType GetVariantType() => m_Value?.VariantType ?? VariantType.Null;

		public readonly bool IsNull() => VariantType == VariantType.Null;
		public readonly bool IsBoolean() => VariantType == VariantType.Boolean;
		public readonly bool IsNumber() => VariantType == VariantType.Integer || VariantType == VariantType.Float;
		public readonly bool IsString() => VariantType == VariantType.String;
		public readonly bool IsArray() => VariantType == VariantType.Array;
		public readonly bool IsObject() => VariantType == VariantType.Object;
		public readonly bool IsComposite() => IsArray() || IsObject();
		public readonly bool IsPrimitive() => !IsComposite() && !IsNull();
	

		/// <summary>
		/// bool 値であることを期待し、それを返す。
		/// <para>
		/// 内部の値が bool でない場合はVariantException を投げます。</para>
		/// <seealso cref="AsBool()"/>
		/// </summary>
		public bool RequireBool()
		{
			if (IsBoolean())
			{
				return m_Value!.BoolValue;
			}
			throw new VariantException($"Value is not a boolean: {this}");
		}

		/// <summary>
		/// Number 値であることを期待し、それを long として返す。
		/// <seealso cref="AsInt()"/>
		/// <seealso cref="AsLong()"/>
		/// </summary>
		public long RequireInteger()
		{
			if (IsNumber())
			{
				return m_Value!.LongValue;
			}
			throw new VariantException($"Value is not a number: {this}");
		}

		/// <summary>
		/// Number 値であることを期待し、それを double として返す。
		/// <seealso cref="AsFloat()"/>
		/// <seealso cref="AsDouble()"/>
		/// </summary>
		public double RequireFloat()
		{
			if (IsNumber())
			{
				return m_Value!.DoubleValue;
			}
			throw new VariantException($"Value is not a number: {this}");
		}

		/// <summary>
		/// 文字列値であることを期待し、それを string として返す。
		/// <see cref="AsString()"/>
		/// </summary>
		public string RequireString()
		{
			if (IsString())
			{
				return m_Value!.StringValue;
			}
			throw new VariantException($"Value is not a string: {this}");
		}

		/// <summary>
		/// Object 値であることを期待し、それを JObject として返す。
		/// <seealso cref="AsObject()"/>
		/// </summary>
		public JObject RequireObject()
		{
			if (IsObject())
			{
				return m_Value!.ObjectValue;
			}
			throw new VariantException($"Value is not an object: {this}");
		}

		/// <summary>
		/// Array 値であることを期待し、それを JArray として返す。
		/// <seealso cref="AsArray()"/>
		/// </summary>
		public JArray RequireArray()
		{
			if (IsArray())
			{
				return m_Value!.ArrayValue;
			}
			throw new VariantException($"Value is not an array: {this}");
		}



		/// <summary>
		/// 任意型への変換
		/// <para>
		/// 任意の具体型 T に変換して返します。変換できない場合は VariantException を投げます。</para>
		/// <para>
		/// JVariant は null を持つことがあるので、 null を返すことが正解であることもあるのですが、
		/// このメソッドは null 非許容であり、参照型に対して null を返すことはありません。VariantException を投げます。
		/// （null を返すことがあるのは T に Null 許容の値型を指定した場合のみ）</para>
		/// <para>
		/// T は 具体型である必要があります。抽象クラスやインターフェースは指定できません。
		/// （意味的にはできてもいいはずですが、実装上の対応していません。IList で十分としても List を指定してください。）</para>
		/// </summary>
		public readonly T Require<T>()
		{
			var context = ConvertContext.Acquire();
			try
			{
				return context.Converter.ConvertToStrict<T>(this);
			}
			finally
			{
				context.Release();
			}
		}


		/// <summary>
		/// 任意型への変換
		/// <para>
		/// JVariant を任意の具体型 T に変換して返します。</para>
		/// <para>
		/// null 許容型でない T に対しても null を返すことがあります。
		/// (null を返すことが正解であることがある)</para>
		/// <para>
		/// T は 具体型である必要があります。抽象クラスやインターフェースは指定できません。
		/// （意味的にはできてもいいはずですが、実装上の対応していません。IList で十分としても List を指定してください。）</para>
		/// <para>
		/// 変換方法がわからないとき、デフォルトでは null もしくは T のデフォルト値を返します。
		/// 例外を投げるべき時は throws に true を指定してください。</para>
		/// </summary>
		public readonly T? As<T>(bool throws = false) 
		{
			// null だったら default.
			if (IsNull())
			{
				return default;
			}

			try
			{
				// 定型変換
				if (VariantConverter.ConvertVariantFixed(this, typeof(T), out object? x))
				{
					return (T?)x;
				}

				// 明確に変換不能な型
				if (VariantConverter.IsUnsupported(typeof(T)))
				{
					throw new VariantConvertException($"Conversion to type {typeof(T)} is not supported.");
				}

				var context = ConvertContext.Acquire();
				try
				{
					return context.Converter.ConvetrtVariantTo<T>(this);
				}
				finally
				{
					context.Release();
				}
			}
			catch (VariantConvertException) when (!throws)
			{
				return default;
			}
		}
		


		/// <summary>
		/// bool としての値。bool 以外を持っている場合はそれなりに変換します。
		/// <para>
		/// bool 以外を持っていた場合は以下の値を返します。
		/// Null    ⇒ false
		/// Integer ⇒ 0 以外のとき true
		/// Float   ⇒ 0.0 と等しくないとき true
		/// String  ⇒ 数値として解釈可能であればそれが 0 以外のとき true. 数値ではないときは "true" と Case Insensitive に比較した結果
		/// Array   ⇒ 要素数が 0 ではないとき true
		/// Object  ⇒ 要素数が 0 ではないとき true</para>
		/// </summary>
		public readonly  bool AsBool() => m_Value.AsBool();

		/// <summary>
		/// int としての値。Number 以外を持っている場合はそれなりに変換します。
		/// </summary>
		public readonly int AsInt() => m_Value.AsInt();

		/// <summary>
		/// long としての値。Number 以外を持っている場合はそれなりに変換します。
		/// </summary>
		public readonly long AsLong() => m_Value.AsLong();

		/// <summary>
		/// float としての値。Number 以外を持っている場合はそれなりに変換します。
		/// </summary>
		public readonly float AsFloat() => m_Value.AsFloat();

		/// <summary>
		/// double としての値。Number 以外を持っている場合はそれなりに変換します。
		/// </summary>
		public readonly double AsDouble() => m_Value.AsDouble();

		/// <summary>
		/// string としての値。String 以外を持っている場合はそれなりに変換します。
		/// <para>
		/// なんとなく内容を表す文字列を返します。JSON表現ではないので注意してください。
		/// ToString() と異なり、Null のときは空の文字列を返します。</para>
		/// </summary>
		public readonly string AsString()
		{
			if( IsNull() )
			{
				return string.Empty;
			}
			return m_Value.AsString();
		}

		/// <summary>
		/// Object としての値。Object 以外を持っている場合はそれなりに変換します。
		/// <para>
		/// 配列の場合インデックスをそれぞれ文字列にして Object に変換します。
		/// それ以外の型の場合からの JObject を返します。
		/// </para>
		/// </summary>
		/// <returns></returns>
		public readonly JObject AsObject() => m_Value.AsObject();

		/// <summary>
		/// Array としての値。Array 以外を持っている場合はそれなりに変換するか、空の配列を返します。
		/// <para>
		/// 変換可能なのはキーとして int に変換可能な文字列を持つ Object のみです。
		/// それ以外の型の場合は空の配列を返します。
		/// </para>
		/// </summary>
		public readonly JArray AsArray() => m_Value.AsArray();


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
		/// </summary>
		public readonly bool IsEmpty() => m_Value == null || m_Value.IsEmpty;



		/// <summary>
		/// オブジェクトとしての要素の取得。
		/// <para>
		/// 指定された key の要素を返します。格納されている値が配列である場合は key は数値として解釈されます。
		/// 配列でもオブジェクトでもない場合は空の JVariant を返します。</para>
		/// <para>
		/// この操作によって内容が変更されることはありません。</para>
		/// </summary>
		public readonly JVariant Get( StringView key)
		{
			if (IsObject())
			{
				return m_Value!.ObjectValue.Get(key);
			}
			if (IsArray())
			{
				if (int.TryParse(key, out int index))
				{
					return m_Value!.ArrayValue.Get(index);
				}
			}
			return new JVariant();
		}

		/// <summary>
		/// 配列としての要素の取得。
		/// <para>
		/// 指定インデックスの要素を返します。格納されている値がオブジェクトである場合は key は文字列として解釈されます。
		/// 存在しない場合は 空の JVariant を返します。</para>
		/// </summary>
		public readonly JVariant Get(int index)
		{
			if (IsArray())
			{
				return m_Value!.ArrayValue.Get(index);
			}
			if( IsObject() )
			{
				return m_Value!.ObjectValue.Get(index.ToString());
			}
			return new JVariant();
		}

		/// <summary>
		/// インデクサー。Get と同じです。
		/// <para>
		/// この挙動は <see cref="JValue" /> <see cref="JObject"/> <see cref="JArray"/> 
		/// のどれとも異なります。インデクサによって内部が変更されることはありません。
		/// </para>
		/// </summary>
		public JVariant this[StringView key] => Get(key);
		public JVariant this[int index] => Get(index);


		/// <summary>
		/// 指定されたキーの要素が存在するか返す。
		/// <para>
		/// 内容がオブジェクトの場合は JObject.ContainsKey() と同等です。
		/// 配列の場合は引数が数値として解釈可能であればそれと要素数を比較します。
		/// それ以外の場合は false を返します。
		/// </para>
		/// </summary>
		public bool ContainsKey(string x)
		{
			if (IsObject())
			{
				return m_Value!.ObjectValue.ContainsKey(x);
			}
			if (IsArray())
			{
				if (int.TryParse(x, out int index))
				{
					return (index >= 0) && (index < this.Count);
				}
			}
			return false;
		}





		/// <summary>
		/// 同一性比較
		/// <para>
		/// 全く同じ JValue を参照しているかどうかを比較します。
		/// 内容を検証するわけではないので注意してください。
		/// （同じ Object を参照する JValue 同士も違うとみなされます。）
		/// </para>
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj is JVariant other)
			{
				return this.Equals(other);
			}
			return false;
		}

		/// <summary>
		/// 同一性比較
		/// <para>
		/// 全く同じ JValue を参照しているかどうかを比較します。
		/// 内容を検証するわけではないので注意してください。
		/// </para>
		/// </summary>
		public bool Equals(JVariant other)
		{
			return this.m_Value == other.m_Value;
		}

		/// <summary>
		/// 内容の同値性比較
		/// </summary>
		public bool EquivalentTo(JVariant other, int maxDepth = DefaultMaxDepth, int depth = 0)
		{
			if (ReferenceEquals(this.m_Value, other.m_Value))
			{
				return true;
			}
			if (this.VariantType != other.VariantType)
			{
				return false;
			}
			if( this.IsNull() )
			{
				return other.IsNull();
			}
			return m_Value!.EquivalentTo(other.m_Value!, maxDepth, depth);
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
			return m_Value?.GetHashCode() ?? 0;
		}

		/// <summary>
		/// 文字列化
		/// <para>
		/// string の場合は設定されている string そのもの。
		/// その他はなんとなく内容を表す文字列を返します。JSON表現ではないので注意してください。</para>
		/// <para>
		/// null のとき "null" という文字列を返します。</para>
		/// <seealso cref="ToJSON(JsonFormatPolicy)"/>
		/// </summary>
		public override string ToString()
		{
			return m_Value?.ToString() ?? "null";
		}

		/// <summary>
		/// 値を返す。
		/// <para>
		/// 内部の JValue が null の場合は、Null を指す新しい JValue を作って返します。</para>
		/// </summary>
		public readonly JValue GetValue()
		{
			return m_Value ?? new JValue();
		}

		/// <summary>
		/// JSON 文字列化。
		/// <para>
		/// デフォルトではワンライナーで出力されます。</para>
		/// </summary>
		/// <seealso cref="JValue.ToJson(JsonFormatPolicy)"/>
		public string Stringify(JsonFormatPolicy? policy)
		{
			if (IsNull())
			{
				return "null";
			}
			return m_Value!.Stringify(policy);
		}

		/// <summary>
		/// UTF-8 JSON 文字列化。
		/// <para>
		/// 引数 policy を省略した場合は それなりに改行するモードで出力されます。</para>
		/// </summary>
		/// <seealso cref="JValue.ToJson(JsonFormatPolicy)"/>
		public U8View ToU8Json(JsonFormatPolicy? policy)
		{
			if( IsNull() )
			{
				return Literal.Null.U8;
			}
			return m_Value!.ToU8Json(policy);
		}

		/// <summary>
		/// Json 文字列化。
		/// <para>
		/// 引数 policy を省略した場合は それなりに改行するモードで出力されます。</para>
		/// </summary>
		/// <seealso cref="JValue.ToJson(JsonFormatPolicy)"/>
		public string ToJson(JsonFormatPolicy? policy = null)
		{
			if (IsNull())
			{
				return "null";
			}
			return m_Value!.ToJson(policy);
		}

		/// <summary>
		/// 子要素の参照。
		/// <para>
		/// 自分の配下の要素を文字列で指定して参照します。
		/// <para>
		/// 引数が '/' で開始する場合は 
		/// JSON Pointer(RFC 6901) として解釈され、自分の配下の JSON Pointer で示される要素を返します。</para>
		/// <para>
		/// '/' 以外から開始する場合は . 区切りでそれぞれの個所ををキーとみなすような解釈となります。
		/// (正確な仕様は <see cref="JsonTrail"/> を参照してください。</para>
		/// <para>
		///	<code>
		///	value = JObject.Create();
		/// value["1"] = JObject.Create();
		///	value["1"]["1"] = JObject.Create();
		///	value["1"]["1"]["1"] = "Item 1-1-1";
		/// 
		///	value.Pick( "1.1.1" );  // -> "Item 1-1-1"
		/// </code>
		/// たどることができるのはオブジェクトもしくは配列のみです。
		/// 途中でそれ以外の要素になった場合は Null を示す JVariant を返すします。
		/// どこまで辿れたかを結果から知ることはできません。</para>
		/// </para>
		/// </summary>
		/// <param name="path">JSON Pointer もしくは . 区切りでオブジェクト内の位置を表した文字列。</param>
		public readonly JVariant Pick(StringView path)
		{
			if (path.IsBlank())
			{
				return new JVariant();
			}

			IList<StringView> parts = (path[0] == '/')
				? ParseJsonPointer(path)
				: JsonTrail.ParseForRead(path);

			JVariant v = this;

			for (int i = 0; i < parts.Count; i++)
			{
				var key = parts[i].Trim();

				if (v.IsObject())
				{
					v = v.AsObject().Get(key);
					continue;
				}
				if (v.IsArray() && int.TryParse(key, out int index))
				{
					v = v.AsArray().Get(index);
					continue;
				}
				return new JVariant();
			}
			return v;
		}
	}
}
