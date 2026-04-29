using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Gatebox.Variant
{
	public class JVariantTest
	{
		// コンストラクタ
		[Test]
		public void TestConstruct()
		{
			JVariant v;

			v = new JVariant();
			Assert.That(v.IsNull(), Is.True);

			v = new JVariant(1);
			Assert.That(v.IsNumber(), Is.True);
			Assert.That(v.AsInt(), Is.EqualTo(1));

			v = new JVariant(1.0);
			Assert.That(v.IsNumber(), Is.True);
			Assert.That(v.AsFloat(), Is.EqualTo(1.0));

			v = new JVariant(true);
			Assert.That(v.IsBoolean(), Is.True);
			Assert.That(v.AsBool(), Is.True);

			v = new JVariant("string");
			Assert.That(v.IsString(), Is.True);
			Assert.That(v.AsString(), Is.EqualTo("string"));

			// これは null になるのでちょっと注意
			string nullString = null;
			v = new JVariant(nullString);
			Assert.That(v.IsNull(), Is.True);

			// Null に対する AsString は空文字列
			Assert.That(v.AsString(), Is.EqualTo(string.Empty));

			v = new JVariant(new JArray());
			Assert.That(v.IsArray(), Is.True);

			v = new JVariant(new JObject());
			Assert.That(v.IsObject(), Is.True);
		}
	}
}
