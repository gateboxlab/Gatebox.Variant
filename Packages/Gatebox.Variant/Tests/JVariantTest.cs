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

			// string コンストラクタに null を渡すと空文字列扱い。
			string nullString = null;
			v = new JVariant(nullString);
			Assert.That(v.IsString(), Is.True);
			Assert.That(v.AsString(), Is.EqualTo(string.Empty));

			v = new JVariant(new JArray());
			Assert.That(v.IsArray(), Is.True);

			v = new JVariant(new JObject());
			Assert.That(v.IsObject(), Is.True);
		}

		[Test]
		public void ToJsonUsesMixedFormattingByDefaultForObject()
		{
			var obj = new JObject
			{
				["array"] = new JArray { 1, 2 },
			};
			var variant = obj.AsVariant();

			Assert.That(variant.ToJson(), Is.EqualTo("{\n  \"array\": [1, 2]\n}"));
			Assert.That(obj.ToJson(), Is.EqualTo(variant.ToJson()));
		}

		[Test]
		public void ToJsonUsesMixedFormattingByDefaultForArray()
		{
			var array = new JArray
			{
				new JObject
				{
					["value"] = 1,
				},
			};
			var variant = array.AsVariant();

			// ToJson の Policy のデフォルトは Mixed. 改行あり。
			Assert.That(variant.ToJson(), Is.EqualTo("[\n  {\"value\": 1}\n]"));
			Assert.That(array.ToJson(), Is.EqualTo(variant.ToJson()));
		}
	}
}
