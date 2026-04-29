using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace Gatebox.Variant
{
	public class JValueTest
	{
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
