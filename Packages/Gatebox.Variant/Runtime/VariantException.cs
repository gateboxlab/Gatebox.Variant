using System;

#nullable enable

namespace Gatebox.Variant
{

	/// <summary>
	/// JVariant 関連の例外
	/// </summary>
	public class VariantException : Exception
	{
		public VariantException()
		{
		}

		public VariantException(string message) : base(message)
		{
		}

		public VariantException(string message, Exception ex) : base(message, ex)
		{
		}
	}

	/// <summary>
	/// JVariant と他の型との変換に失敗したことを示す例外。
	/// <para>
	/// 変換 API では、入力値が対象型と整合しないなど「想定内の変換失敗」をこの例外で表現します。</para>
	/// <para>
	/// <see cref="JVariant.As{T}(bool)"/> は throws が false のとき、この例外を補足して default を返します。</para>
	/// </summary>
	public class VariantConvertException : VariantException
	{
		public VariantConvertException()
		{
		}

		public VariantConvertException(string message) : base(message)
		{
		}

		public VariantConvertException(string message, Exception ex) : base(message, ex)
		{
		}
	}

	public class JsonFormatException : VariantException
	{
		public JsonFormatException(string message) : base(message) { }
		public JsonFormatException(string message, Exception ex) : base(message, ex) { }
	}

	public class JsonParseException : VariantException
	{
		public JsonParseException(string message) : base(message) { }
		public JsonParseException(string message, Exception ex) : base(message, ex) { }
	}
}
