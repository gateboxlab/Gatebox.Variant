using System;


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
