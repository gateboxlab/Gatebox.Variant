using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

#nullable enable

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
			string source = "{\"name\":\"Gatebox\",\"values\":[1,true,null],\"message\":\"line\\nquote:\\\" slash:\\/ backslash:\\\\\"}";
			var parsed = Parse(source);

			
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


		

		[Test]
		public void AsFixedType() 
		{
			var values = new JObject()
			{
				["number"] = 123,
				["float"] = 1.5,
				["string"] = "abc",
				["boolean"] = true,
				["null"] = null!,
				
				["array"] = new JArray { 1, 2, 3 },
				["object"] = new JObject { ["key"] = "value" },
				["array_like_object"] = new JObject { ["0"] = "zero", ["1"] = "one" },
			};

			var number = values["number"].AsVariant();
			var floating = values["float"].AsVariant();
			var text = values["string"].AsVariant();
			var boolean = values["boolean"].AsVariant();
			var nil = values["null"].AsVariant();
			var array = values["array"].AsVariant();
			var obj = values["object"].AsVariant();
			var arrayLikeObject = values["array_like_object"].AsVariant();

			Assert.That(number.As<int>(), Is.EqualTo(123));
			Assert.That(number.As<long>(), Is.EqualTo(123L));
			Assert.That(number.As<short>(), Is.EqualTo((short)123));
			Assert.That(number.As<sbyte>(), Is.EqualTo((sbyte)123));
			Assert.That(number.As<uint>(), Is.EqualTo(123u));
			Assert.That(number.As<ushort>(), Is.EqualTo((ushort)123));
			Assert.That(number.As<byte>(), Is.EqualTo((byte)123));
			Assert.That(number.As<ulong>(), Is.EqualTo(123ul));
			Assert.That(number.As<float>(), Is.EqualTo(123.0f));
			Assert.That(number.As<double>(), Is.EqualTo(123.0));
			Assert.That(number.As<char>(), Is.EqualTo((char)123));

			Assert.That(floating.As<float>(), Is.EqualTo(1.5f));
			Assert.That(floating.As<double>(), Is.EqualTo(1.5));
			Assert.That(floating.As<ulong>(), Is.EqualTo(1ul));

			Assert.That(text.As<string>(), Is.EqualTo("abc"));
			Assert.That(boolean.As<bool>(), Is.True);

			Assert.That(nil.As<string>(), Is.Null);
			Assert.That(nil.As<int>(), Is.EqualTo(0));
			Assert.That(nil.As<bool>(), Is.False);
			Assert.That(nil.As<JValue>(), Is.Null);
			Assert.That(nil.As<JArray>().Count, Is.EqualTo(0));
			Assert.That(nil.As<JObject>().Count, Is.EqualTo(0));

			var asArray = array.As<JArray>();
			var asObject = obj.As<JObject>();
			var arrayAsObject = array.As<JObject>();
			var objectAsArray = arrayLikeObject.As<JArray>();

			Assert.That(asArray.Count, Is.EqualTo(3));
			Assert.That(asArray[2].AsInt(), Is.EqualTo(3));
			Assert.That(asObject["key"].AsString(), Is.EqualTo("value"));
			Assert.That(arrayAsObject["0"].AsString(), Is.EqualTo("1"));
			Assert.That(objectAsArray.Count, Is.EqualTo(2));
			Assert.That(objectAsArray[1].AsString(), Is.EqualTo("one"));

			Assert.That(number.As<JVariant>(), Is.EqualTo(number));
			Assert.That(number.As<JValue>(), Is.SameAs(values["number"]));
		}


		private enum SampleEnum
		{
			Value1 = 0,
			Value2 = 1,
			Value3 = 2,
		}

		private enum SampleLongEnum : long
		{
			Large = 5000000000L,
		}

		private enum SampleULongEnum : ulong
		{
			Large = 5000000000UL,
		}

		private class SampleClass : IVariantConvertible
		{
			public int Value { get; }
			public SampleClass(int value)
			{
				Value = value;
			}
			public SampleClass(JVariant variant)
			{
				Value = variant.AsInt();
			}
			public JVariant AsVariant()
			{
				return new JVariant(Value);
			}
		}

		[Test]
		public void AsBuildInType()
		{
			var x = new JVariant();
			int? nullable_int = x.As<int?>();
			Assert.That(nullable_int.HasValue, Is.False);

			x = new JArray { 1, 2, 3 }.AsVariant();
			var array = x.As<int[]>()!;
			Assert.That(array.Length, Is.EqualTo(3));

			var list = x.As<List<int>>()!;
			Assert.That(list.Count, Is.EqualTo(3));

			x = new JObject { ["key"] = "value" }.AsVariant();
			var dict = x.As<Dictionary<string, string>>()!;
			Assert.That(dict.Count, Is.EqualTo(1));
			Assert.That(dict["key"], Is.EqualTo("value"));

			x = new JVariant(2);
			var enumValue = x.As<SampleEnum>();
			Assert.That(enumValue, Is.EqualTo(SampleEnum.Value3));

			x = new JVariant("Value1");
			enumValue = x.As<SampleEnum>();
			Assert.That(enumValue, Is.EqualTo(SampleEnum.Value1));

			x = new JVariant(123);
			var sample = x.As<SampleClass>()!;
			Assert.That(sample.Value, Is.EqualTo(123));
		}

		[Test]
		public void AsEnumSupportsLargeUnderlyingValues()
		{
			var x = new JVariant(5000000000L);

			Assert.That(x.As<SampleLongEnum>(), Is.EqualTo(SampleLongEnum.Large));
			Assert.That(x.As<SampleULongEnum>(), Is.EqualTo(SampleULongEnum.Large));
		}

		[Test]
		public void AsEnumRejectsNonIntegralFloatingPointValues()
		{
			var x = new JVariant(1.5);

			Assert.That(x.As<SampleEnum>(), Is.EqualTo(default(SampleEnum)));
			Assert.That(() => x.As<SampleEnum>(throws: true), Throws.TypeOf<VariantConvertException>());
		}

		[Test]
		public void AsReturnsDefaultOnVariantConvertExceptionUnlessThrowsIsTrue()
		{
			var x = new JVariant("abc");

			Assert.That(x.As<SampleEnum>(), Is.EqualTo(default(SampleEnum)));
			Assert.That(() => x.As<SampleEnum>(throws: true), Throws.TypeOf<VariantConvertException>());
		}

		[Test]
		public void PickReadsNestedObjectWithDotTrail()
		{
			var x = Parse("{\"user\":{\"profile\":{\"name\":\"Gatebox\"}}}");

			Assert.That(x.Pick("user.profile.name").AsString(), Is.EqualTo("Gatebox"));
		}

		[Test]
		public void PickReadsNestedArrayWithDotTrail()
		{
			var x = Parse("{\"items\":[{\"name\":\"first\"},{\"name\":\"second\"}]}");

			Assert.That(x.Pick("items.0.name").AsString(), Is.EqualTo("first"));
			Assert.That(x.Pick("items.1.name").AsString(), Is.EqualTo("second"));
		}

		[Test]
		public void PickReadsBracketTrailKeysLiterally()
		{
			var x = Parse("{\"root\":{\"key.with.dot\":{\"inner]key\":\"value\"}}}");

			Assert.That(x.Pick(@"root[key.with.dot][inner\]key]").AsString(), Is.EqualTo("value"));
		}

		[Test]
		public void PickReadsJsonPointer()
		{
			var x = Parse("{\"a/b\":{\"c~d\":[10,20]}}");

			Assert.That(x.Pick("/a~1b/c~0d/1").AsInt(), Is.EqualTo(20));
		}

		[Test]
		public void PickReturnsNullVariantWhenPathCannotBeResolved()
		{
			var x = Parse("{\"items\":[1]}");

			Assert.That(x.Pick("").IsNull(), Is.True);
			Assert.That(x.Pick("items.name").IsNull(), Is.True);
			Assert.That(x.Pick("items.10").IsNull(), Is.True);
			Assert.That(x.Pick("items.one").IsNull(), Is.True);
		}

	}
}
