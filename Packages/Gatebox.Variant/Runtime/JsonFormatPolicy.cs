using System;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Gatebox.Variant
{
	/// <summary>
	/// JSON への変換時の改行指定
	/// </summary>
	public enum ReturnPolicy
	{
		/// <summary>常に改行する</summary>
		Every,

		/// <summary>空配列空オブジェクト以外は改行する。</summary>
		ExceptEmpty,

		/// <summary>それなりにシンプルな配列、オブジェクトは改行しない</summary>
		Simple,

		/// <summary>すべて一行で出力する</summary>
		Never,
	};


	/// <summary>
	/// NaN, Infinity の扱い
	/// </summary>
	public enum SpecialFloatPolicy
	{
		/// <summary>NaN, Infinity があったら文字列で出力する。JSON としては正しい形式ですが、値の型は失われます。</summary>
		AsString,

		/// <summary>NaN, Infinity があったら JavaScript のリテラルとして出力する。JSONとして正しくないものになりますが、JavaScript 含む多くの環境で有効であり、値の型は失われません。</summary>
		AsJsLiteral,

		/// <summary>NaN, Infinity があったら例外を投げる</summary>
		Throw,
	};



	/// <summary>
	/// JSON への変換のフォーマット指定。
	/// <para>
	/// いつ改行するか、インデントは何で行うかを持ちます。</para>
	/// </summary>
	public class JsonFormatPolicy
	{
		//==============================================================================
		// static members
		//==============================================================================

		/// <summary>一行出力</summary>
		public static readonly JsonFormatPolicy OneLiner = new JsonFormatPolicy(ReturnPolicy.Never);

		/// <summary>シンプルな内容は一行で、インデントは空白２つ</summary>
		public static readonly JsonFormatPolicy Mixed = new JsonFormatPolicy(ReturnPolicy.Simple, "  ");

		/// <summary>空配列空オブジェクトは改行しない、インデントは空白２つ</summary>
		public static readonly JsonFormatPolicy Pretty = new JsonFormatPolicy(ReturnPolicy.ExceptEmpty, "  ");


		//==============================================================================
		// instance members
		//==============================================================================

		private string m_Indent = "";
		private byte[] m_IndentU8 = Array.Empty<byte>();


		/// <summary>
		/// コンストラクタ
		/// </summary>
		public JsonFormatPolicy()
		{

		}


		/// <summary>
		/// コンストラクタ
		/// </summary>
		public JsonFormatPolicy(
			ReturnPolicy p,
			string indent = "",
			SpecialFloatPolicy floatPolicy = SpecialFloatPolicy.AsString,
			bool escapeUnicode = false,
			int maxDepth = JVariant.DefaultMaxDepth)
		{
			Indent = indent ?? string.Empty;
			ReturnPolicy = p;
			SpecialFloatPolicy = floatPolicy;
			EscapeMultiBytes = escapeUnicode;
			MaxDepth = maxDepth;
		}

		/// <summary>
		/// インデント文字列
		/// <para>
		/// シングルバイト文字のみで構成されている必要があります。</para>
		/// </summary>
		public string Indent
		{
			get => m_Indent;

			[MemberNotNull(nameof(m_Indent), nameof(m_IndentU8))]
			init
			{
				m_Indent = value ?? "";
				m_IndentU8 = ToAscii(m_Indent);
			}
		}

		/// <summary>
		/// byte 配列としてのインデント文字列
		/// <para>
		/// 設定は <see cref="Indent"/> を通して行います。</para>
		/// </summary>
		public byte[] IndentU8
		{
			get { return m_IndentU8; }
		}

		/// <summary>
		/// 改行指定
		/// </summary>
		public ReturnPolicy ReturnPolicy { get; init; } = ReturnPolicy.Never;

		/// <summary>
		/// Nan, Infinity の扱い
		/// </summary>
		public SpecialFloatPolicy SpecialFloatPolicy { get; init; } = SpecialFloatPolicy.AsJsLiteral;


		/// <summary>
		/// マルチバイト文字列をユニコードエスケープするかどうか
		/// </summary>
		public bool EscapeMultiBytes { get; init; } = false;


		/// <summary>
		/// 最大深度
		/// <para>
		/// 簡易的な親子間の循環参照の検出のための値です。深さがこの値を超えると例外を投げます。
		/// 実際にここまで深い JSON が必要な場合はこの値を変更してください。</para>
		/// </summary>
		public int MaxDepth { get; private set; } = JVariant.DefaultMaxDepth;


		private static byte[] ToAscii(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return Array.Empty<byte>();
			}

			var bytes = new byte[s.Length];
			for (int i = 0; i < s.Length; i++)
			{
				if (s[i] > 0xFF)
				{
					throw new ArgumentException($"Indent string must be ASCII. char '{s[i]}' at position {i} is not ASCII.");
				}
				bytes[i] = (byte)s[i];
			}
			return bytes;
		}
	}
}
