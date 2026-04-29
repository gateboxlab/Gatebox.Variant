using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace Gatebox.Variant
{
	public class JValueTest
	{
		[Test]
		public void TestImplicitCast()
		{
			// 各種プリミティブから作れる。
			// 条件式は IsEmpty で判別される。( null でないことがわかってるなら isEmpty を使ったほうがいいと思うけど)
			JValue v = null;

			if (v)
			{
				Assert.Fail();
			}

			v = new JVariant();
			if (v)
			{
				Assert.Fail();
			}

			v = false;
			if (v)
			{
				Assert.Fail();
			}

			v = 0;
			if (v)
			{
				Assert.Fail();
			}

			v = "";
			if (v)
			{
				Assert.Fail();
			}

			v = new JObject();
			if (v)
			{
				Assert.Fail();
			}

			v = new JArray();
			if (v)
			{
				Assert.Fail();
			}

			v = true;
			if (!v)
			{
				Assert.Fail();
			}

			v = 1;
			if (!v)
			{
				Assert.Fail();
			}

			v = "a";
			if (!v)
			{
				Assert.Fail();
			}

			JObject obj = new JObject() { ["x"] = 1 };
			v = obj;
			if (!v)
			{
				Assert.Fail();
			}

			JArray array = new JArray() { 1 };
			v = array;
			if (!v)
			{
				Assert.Fail();
			}

		}


		[Test]
		public void StringValueAndToStringReturnStoredString()
		{
			var value = new JValue("hello");

			Assert.That(value.StringValue, Is.EqualTo("hello"));
			Assert.That(value.ToString(), Is.EqualTo("hello"));
			Assert.That(value.Count, Is.EqualTo(5));
		}

		[Test]
		public void ImplicitConversionFromJVariantCopiesVariantValue()
		{
			JVariant variant = new JVariant("hello");

			JValue value = variant;

			Assert.That(value.IsString(), Is.True);
			Assert.That(value.StringValue, Is.EqualTo("hello"));
		}

		[Test]
		public void EqualsObjectComparesJVariantAndJValue()
		{
			var value = new JValue("hello");
			object variant = new JVariant("hello");
			object sameValue = new JValue("hello");

			Assert.That(value.Equals(variant), Is.True);
			Assert.That(value.Equals(sameValue), Is.True);
		}

		[Test]
		public void FloatingPointJsonUsesInvariantCulture()
		{
			var originalCulture = Thread.CurrentThread.CurrentCulture;
			var originalUICulture = Thread.CurrentThread.CurrentUICulture;

			try
			{
				var culture = CultureInfo.GetCultureInfo("fr-FR");
				Thread.CurrentThread.CurrentCulture = culture;
				Thread.CurrentThread.CurrentUICulture = culture;

				var value = new JValue(1.25);

				Assert.That(value.ToJson(), Is.EqualTo("1.25"));
				Assert.That(value.ToU8Json().ToString(), Is.EqualTo("1.25"));
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = originalCulture;
				Thread.CurrentThread.CurrentUICulture = originalUICulture;
			}
		}
	}
}
