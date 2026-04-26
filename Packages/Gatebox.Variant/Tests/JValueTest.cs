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
	}
}
