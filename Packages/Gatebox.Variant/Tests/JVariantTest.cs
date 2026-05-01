using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Gatebox.Variant
{
	public class JVariantTest
	{
		private static JVariant Parse(string json, bool throws = true)
		{
			return new JVariant().Parse(json, throws);
		}

		private static JVariant ParseU8(string json, bool throws = true)
		{
			return new JVariant().Parse(U8View.Create(json), throws);
		}

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

		[Test]
		public void ParseParsesPrimitiveValues()
		{
			Assert.That(Parse("null").IsNull(), Is.True);
			Assert.That(Parse("true").AsBool(), Is.True);
			Assert.That(Parse("false").AsBool(), Is.False);
			Assert.That(Parse("123").AsLong(), Is.EqualTo(123));
			Assert.That(Parse("-1.5").AsDouble(), Is.EqualTo(-1.5));
			Assert.That(double.IsNaN(Parse("NaN").AsDouble()), Is.True);
			Assert.That(Parse("Infinity").AsDouble(), Is.EqualTo(double.PositiveInfinity));
			Assert.That(Parse("-Infinity").AsDouble(), Is.EqualTo(double.NegativeInfinity));
		}

		[Test]
		public void ParseParsesObjectsArraysAndStringEscapes()
		{
			var parsed = Parse("{\"name\":\"Gatebox\",\"values\":[1,true,null],\"message\":\"line\\nquote:\\\" slash:\\/ backslash:\\\\\"}");

			Assert.That(parsed.IsObject(), Is.True);
			Assert.That(parsed["name"].AsString(), Is.EqualTo("Gatebox"));
			Assert.That(parsed["values"].AsArray().Count, Is.EqualTo(3));
			Assert.That(parsed["values"][0].AsInt(), Is.EqualTo(1));
			Assert.That(parsed["values"][1].AsBool(), Is.True);
			Assert.That(parsed["values"][2].IsNull(), Is.True);
			Assert.That(parsed["message"].AsString(), Is.EqualTo("line\nquote:\" slash:/ backslash:\\"));
		}

		[Test]
		public void ParseAllowsDocumentedLeniencies()
		{
			var parsed = Parse("/* leading */ { unquoted_key: +12, list: [1,2,], } // trailing");

			Assert.That(parsed["unquoted_key"].AsInt(), Is.EqualTo(12));
			Assert.That(parsed["list"].AsArray().Count, Is.EqualTo(2));
			Assert.That(parsed["list"][1].AsInt(), Is.EqualTo(2));
		}

		[Test]
		public void ParseReturnsNullVariantOnInvalidJsonWhenThrowsIsFalse()
		{
			var parsed = new JVariant().Parse("{", throws: false);

			Assert.That(parsed.IsNull(), Is.True);
		}

		[Test]
		public void ParseThrowsOnInvalidJsonWhenThrowsIsTrue()
		{
			Assert.That(() => Parse("{"), Throws.TypeOf<JsonParseException>());
			Assert.That(() => Parse("\"\\u12ZZ\""), Throws.TypeOf<JsonParseException>());
		}

		[Test]
		public void ParseAcceptsLeadingBom()
		{
			var parsed = Parse("\uFEFF{\"value\":1}");

			Assert.That(parsed["value"].AsInt(), Is.EqualTo(1));
		}

		[Test]
		public void ParseAcceptsEscapedNullCharacter()
		{
			var parsed = Parse("\"\\u0000\"");

			Assert.That(parsed.IsString(), Is.True);
			Assert.That(parsed.AsString().Length, Is.EqualTo(1));
			Assert.That(parsed.AsString()[0], Is.EqualTo('\0'));
		}

		[Test]
		public void ParseU8ParsesObjectsArraysAndStringEscapes()
		{
			var parsed = ParseU8("{\"name\":\"Gatebox\",\"values\":[1,true,null],\"message\":\"line\\nquote:\\\" slash:\\/ backslash:\\\\\"}");

			Assert.That(parsed.IsObject(), Is.True);
			Assert.That(parsed["name"].AsString(), Is.EqualTo("Gatebox"));
			Assert.That(parsed["values"].AsArray().Count, Is.EqualTo(3));
			Assert.That(parsed["values"][0].AsInt(), Is.EqualTo(1));
			Assert.That(parsed["values"][1].AsBool(), Is.True);
			Assert.That(parsed["values"][2].IsNull(), Is.True);
			Assert.That(parsed["message"].AsString(), Is.EqualTo("line\nquote:\" slash:/ backslash:\\"));
		}

		[Test]
		public void ParseU8ReturnsNullVariantOnInvalidJsonWhenThrowsIsFalse()
		{
			var parsed = ParseU8("{", throws: false);

			Assert.That(parsed.IsNull(), Is.True);
		}

		[Test]
		public void ParseU8ThrowsOnInvalidJsonWhenThrowsIsTrue()
		{
			Assert.That(() => ParseU8("{"), Throws.TypeOf<JsonParseException>());
			Assert.That(() => ParseU8("\"\\u12ZZ\""), Throws.TypeOf<JsonParseException>());
		}

		[Test]
		public void ParseU8AcceptsLeadingBom()
		{
			var parsed = ParseU8("\uFEFF{\"value\":1}");

			Assert.That(parsed["value"].AsInt(), Is.EqualTo(1));
		}

		[Test]
		public void ParseU8AcceptsEscapedNullCharacter()
		{
			var parsed = ParseU8("\"\\u0000\"");

			Assert.That(parsed.IsString(), Is.True);
			Assert.That(parsed.AsString().Length, Is.EqualTo(1));
			Assert.That(parsed.AsString()[0], Is.EqualTo('\0'));
		}
	}
}
