using System;
using System.Text;

#nullable enable

namespace Gatebox.Variant.Internal
{
	internal readonly struct Literal
	{
		public static readonly Literal Null = new("null");
		public static readonly Literal True = new("true");
		public static readonly Literal False = new("false");
		public static readonly Literal NaN = new("NaN");
		public static readonly Literal Infinity = new("Infinity");
		public static readonly Literal NegativeInfinity = new("-Infinity");


		public readonly string U16 { get; }
		public readonly U8View U8 { get; }

		public Literal(string s)
		{
			U16 = s;
			U8 = Encoding.UTF8.GetBytes(s);
		}
	}
}
