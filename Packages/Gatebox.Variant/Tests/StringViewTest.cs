using NUnit.Framework;

using Gatebox.Variant.Extensions;

namespace Gatebox.Variant
{
	public class StringViewTest
	{
		[Test]
		public void ParseTest()
		{
			StringView v1 = new StringView();
			Assert.That(v1.ParseInt(), Is.EqualTo(0));

			v1 = new StringView("X11", 1, 2);
			Assert.That(v1.ParseInt(), Is.EqualTo(1));

			Assert.That(("-1").View().ParseInt(), Is.EqualTo(-1));
			Assert.That(("-1X").View().ParseInt(), Is.EqualTo(-1));
			Assert.That(("+1X").View().ParseInt(), Is.EqualTo(+1));
			Assert.That(("123,456").View().ParseInt(), Is.EqualTo(123));
			Assert.That(("123,456").View().ParseInt(), Is.EqualTo(123));
			Assert.That(("-2147483648").View().ParseInt(), Is.EqualTo(-2147483648));
			Assert.That(("2147483647").View().ParseInt(), Is.EqualTo(+2147483647));
			Assert.That(("-2147483649").View().ParseInt(), Is.EqualTo(-214748364));
			Assert.That(("+21474836490").View().ParseInt(), Is.EqualTo(+214748364));

			int i1 = 0;
			Assert.That(("-1").View().TryParseInt(out i1), Is.True);
			Assert.That(("").View().TryParseInt(out i1), Is.False);
			Assert.That(("-").View().TryParseInt(out i1), Is.False);
			Assert.That(("123,456").View().TryParseInt(out i1), Is.False);
			Assert.That(("-2147483648").View().TryParseInt(out i1), Is.True);
			Assert.That(("2147483647").View().TryParseInt(out i1), Is.True);
			Assert.That(("-2147483649").View().TryParseInt(out i1), Is.False);
			Assert.That(("+21474836490").View().TryParseInt(out i1), Is.False);

			Assert.That(("-1").View().ParseLong(), Is.EqualTo(-1));
			Assert.That(("-1X").View().ParseLong(), Is.EqualTo(-1));
			Assert.That(("+1X").View().ParseLong(), Is.EqualTo(+1));
			Assert.That(("123,456").View().ParseLong(), Is.EqualTo(123));
			Assert.That(("123,456").View().ParseLong(), Is.EqualTo(123));
			Assert.That(("-2147483648").View().ParseLong(), Is.EqualTo(-2147483648));
			Assert.That(("2147483647").View().ParseLong(), Is.EqualTo(+2147483647));
			Assert.That(("-9223372036854775808").View().ParseLong(), Is.EqualTo(-9223372036854775808));
			Assert.That(("+9223372036854775807").View().ParseLong(), Is.EqualTo(+9223372036854775807));

			long l1 = 0;
			Assert.That(("-1").View().TryParseLong(out l1), Is.True);
			Assert.That(("").View().TryParseLong(out l1), Is.False);
			Assert.That(("-").View().TryParseLong(out l1), Is.False);
			Assert.That(("123,456").View().TryParseLong(out l1), Is.False);
			Assert.That(("-2147483648").View().TryParseLong(out l1), Is.True);
			Assert.That(("2147483647").View().TryParseLong(out l1), Is.True);
			Assert.That(("-9223372036854775808").View().TryParseLong(out l1), Is.True);
			Assert.That(("9223372036854775807").View().TryParseLong(out l1), Is.True);
			Assert.That(("-9223372036854775809").View().TryParseLong(out l1), Is.False);
			Assert.That(("+92233720368547758079").View().TryParseLong(out l1), Is.False);


		}

		[Test]
		public void ConstructorAndToStringTest()
		{
			var view = new StringView("abc");
			Assert.That(view.Length, Is.EqualTo(3));
			Assert.That(view.ToString(), Is.EqualTo("abc"));

			var part = new StringView("xyz123", 3, 6);
			Assert.That(part.Length, Is.EqualTo(3));
			Assert.That(part.ToString(), Is.EqualTo("123"));

			var empty = new StringView(null);
			Assert.That(empty.IsEmpty(), Is.True);
			Assert.That(empty.ToString(), Is.EqualTo(string.Empty));
		}

		[Test]
		public void IndexerAndSliceTest()
		{
			var view = new StringView("abcdef");
			Assert.That(view[0], Is.EqualTo('a'));
			Assert.That(view[5], Is.EqualTo('f'));
			Assert.That(view[6], Is.EqualTo('\0'));

			var slice = view.Slice(1, 4);
			Assert.That(slice.ToString(), Is.EqualTo("bcd"));

			var sub = view.SubView(2, 2);
			Assert.That(sub.ToString(), Is.EqualTo("cd"));
		}

		[Test]
		public void CompareAndEqualsTest()
		{
			var a = new StringView("ABC");
			var b = new StringView("ABC");
			var c = new StringView("ABD");

			Assert.That(a == b, Is.True);
			Assert.That(a != c, Is.True);
			Assert.That(a.CompareTo(c), Is.LessThan(0));
			Assert.That(c.CompareTo(a), Is.GreaterThan(0));
			Assert.That(a.EqualsIgnoreCase(new StringView("abc")), Is.True);
		}

		[Test]
		public void StartsEndsWithTest()
		{
			var view = new StringView("HelloWorld");
			Assert.That(view.StartsWith("Hello"), Is.True);
			Assert.That(view.StartsWith("World"), Is.False);
			Assert.That(view.EndsWith("World"), Is.True);
			Assert.That(view.EndsWith("Hello"), Is.False);
			Assert.That(view.StartsWithIgnoreCase("heLLo"), Is.True);
		}

		[Test]
		public void TrimAndBlankTest()
		{
			var view = new StringView("  abc \t");
			Assert.That(view.TrimStart().ToString(), Is.EqualTo("abc \t"));
			Assert.That(view.TrimEnd().ToString(), Is.EqualTo("  abc"));
			Assert.That(view.Trim().ToString(), Is.EqualTo("abc"));

			var blank = new StringView(" \t\n");
			Assert.That(blank.IsBlank(), Is.True);
			Assert.That(blank.HasContent(), Is.True);
		}

		[Test]
		public void SplitAndDivideTest()
		{
			var view = new StringView("a,b,,c");
			var parts = view.Split(',');
			Assert.That(parts.Count, Is.EqualTo(4));
			Assert.That(parts[0].ToString(), Is.EqualTo("a"));
			Assert.That(parts[1].ToString(), Is.EqualTo("b"));
			Assert.That(parts[2].ToString(), Is.EqualTo(string.Empty));
			Assert.That(parts[3].ToString(), Is.EqualTo("c"));

			var filtered = view.Split(',', true);
			Assert.That(filtered.Count, Is.EqualTo(3));
			Assert.That(filtered[2].ToString(), Is.EqualTo("c"));

			var divided = view.Divide(',');
			Assert.That(divided.Head.ToString(), Is.EqualTo("a"));
			Assert.That(divided.Tail.ToString(), Is.EqualTo("b,,c"));
		}

		[Test]
		public void SplitTest()
		{
			StringView v1 = new StringView();
			Assert.That(v1.Split(' ').Count, Is.EqualTo(1) );

			v1 = new StringView("X X", 1, 2);
			var sp = v1.Split(' ');
			Assert.That(sp.Count, Is.EqualTo(2) );
			Assert.That(sp[0].ToString(), Is.EqualTo(string.Empty));
			Assert.That(v1.Split(' ', true).Count, Is.EqualTo(0));

			v1 = new StringView("#1,2.3-4+5#", 1, 10);
			var r = v1.Split(new char[] { ',', '.', '-', '+' });
			Assert.That(r.Count, Is.EqualTo(5));
			Assert.That(r[0].ToString(), Is.EqualTo("1"));
			Assert.That(r[4].ToString(), Is.EqualTo("5"));

			var view = new StringView("a,b,c,");
			var parts = view.Split(',');
			Assert.That(parts.Count, Is.EqualTo(4));
		}

		[Test]
		public void CountAndIsAlphaNumericTest()
		{
			var view = new StringView("a1b2c3");
			Assert.That(view.Count('1'), Is.EqualTo(1));
			Assert.That(view.IsAlphaNumeric(), Is.True);
			Assert.That(new StringView("a-1").IsAlphaNumeric(), Is.False);
		}
	}
}
