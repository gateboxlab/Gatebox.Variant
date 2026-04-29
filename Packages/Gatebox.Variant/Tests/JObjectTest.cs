using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;


namespace Gatebox.Variant
{
#pragma warning disable CS8887 // 割り当てられていないローカル変数の使用
	public class JObjectTest
	{


		[Test]
		public void ConstructTest()
		{
			// 値型なので new しなくても空の状態で存在して使える
			JObject obj;

			Assert.That(obj.Count, Is.EqualTo(0));
			Assert.That(obj.IsEmpty());

			// 初期化子が効く
			obj = new JObject
			{
				{"bool", false },
				{"int", 1 },
				{"float", 1 },
				{"string", "" },
				{"array", new JArray{ 1,2,3 } },
			};
			Assert.That(obj.Count, Is.EqualTo(5));
			Assert.That(obj["bool"].BoolValue, Is.False);
			Assert.That(obj["int"].IntValue, Is.EqualTo(1));
			Assert.That(obj["string"].StringValue, Is.EqualTo(""));
			Assert.That(obj["array"].ArrayValue.Count, Is.EqualTo(3));

			// この形式も可能
			obj = new JObject
			{
				["1"] = 1,
			};
			Assert.That(obj.Count, Is.EqualTo(1));
			Assert.That(obj["1"].IntValue, Is.EqualTo(1));
		}

		[Test]
		public void AddTest()
		{

			JObject obj = new JObject();
			obj.Add("bool", true);
			obj.Add("int", 1);
			obj.Add("float", 1.0);
			obj.Add("string", "str");
			obj.Add("array", new JArray() { 1, 2, 3 });
			obj.Add("object", new JObject() { ["x"] = 72 });

			Assert.That(obj["bool"].BoolValue, Is.EqualTo(true));
			Assert.That(obj["int"].IntValue, Is.EqualTo(1));
			Assert.That(obj["float"].FloatValue, Is.EqualTo(1.0));
			Assert.That(obj["string"].StringValue, Is.EqualTo("str"));
			Assert.That(obj["array"].ArrayValue.Count, Is.EqualTo(3));
			Assert.That(obj["object"]["x"].IntValue, Is.EqualTo(72));

			// 同じものを追加すると例外になる、これは IDictionary の要求
			try
			{
				obj.Add("string", "anoter string.");
				Assert.Fail();
			}
			catch (ArgumentException) { }

			// Set() はできる
			obj.Set("bool", false);
			obj.Set("int", 2);
			obj.Set("float", 2.0);
			obj.Set("string", "string");
			obj.Set("array", new JArray() { 1, 2, 3, 4 });
			obj.Set("object", new JObject() { ["x"] = 9393 });

			Assert.That(obj["bool"].BoolValue, Is.EqualTo(false));
			Assert.That(obj["int"].IntValue, Is.EqualTo(2));
			Assert.That(obj["float"].FloatValue, Is.EqualTo(2.0));
			Assert.That(obj["string"].StringValue, Is.EqualTo("string"));
			Assert.That(obj["array"].ArrayValue.Count, Is.EqualTo(4));
			Assert.That(obj["object"]["x"].IntValue, Is.EqualTo(9393));

			// 新しいところにも Set はできる
			obj.Set("new_field", 12345);
			Assert.That(obj["new_field"].IntValue, Is.EqualTo(12345));
		}

	}
#pragma warning restore CS8887
}
