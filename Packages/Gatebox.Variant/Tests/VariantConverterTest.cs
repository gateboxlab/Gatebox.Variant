using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Gatebox.Variant.Test;
using NUnit.Framework;


namespace Gatebox.Variant
{
	namespace Test
	{
		public class DateTimeConvertTrait : ConvertTrait<DateTime>
		{
			public override DateTime ConvertVariant(JVariant variant)
			{
				if (variant.IsString())
				{
					return DateTime.Parse(variant.AsString());
				}
				throw new VariantConvertException($"Unable to convert {variant.GetType().Name} to DateTime.");
			}
			public override JVariant CreateVariant(DateTime v)
			{
				return new JValue(v.ToString("o"));
			}
		}

		public class CustomArrayTrait<T> : ConvertTrait<List<T>>
		{
			public override List<T> ConvertVariant(JVariant variant)
			{
				return new List<T>();
			}

			public override JVariant CreateVariant(List<T> v)
			{
				return new JArray();
			}
		}

		public class AlternativeDateTimeConvertTrait : ConvertTrait<DateTime>
		{
			public override DateTime ConvertVariant(JVariant variant)
			{
				return DateTime.UnixEpoch;
			}

			public override JVariant CreateVariant(DateTime v)
			{
				return new JValue("alt");
			}
		}

		public class DynamicBase
		{
			public int BaseValue { get; set; }
			public string Shadowed { get; set; } = "base";
		}

		public class DynamicNested
		{
			public string Label { get; set; } = "";
		}

		public class DynamicSample : DynamicBase
		{
			public new string Shadowed { get; set; } = "";

			[JsonPropertyName("field_value")]
			public int FieldValue;

			[JsonPropertyName("renamed_value")]
			public string RenamedValue { get; set; } = "";

			[JsonIgnore]
			public string IgnoredValue { get; set; } = "";

			public DynamicNested Child { get; set; } = new DynamicNested();

			public string GetterOnly => "getter";
		}
	}



	public class VariantConverterTest
	{

		[Test]
		public void CustomConverter()
		{
			var converter = new VariantConverter();
			converter.RegisterTraitDefinition<DateTime, Test.DateTimeConvertTrait>();
			converter.RegisterTraitDefinition(typeof(List<>), typeof(Test.CustomArrayTrait<>));

			var trait = converter.GetTrait(typeof(DateTime));
			Assert.That(trait, Is.InstanceOf<Test.DateTimeConvertTrait>());

			trait = converter.GetTrait(typeof(List<int>));
			Assert.That(trait, Is.InstanceOf<Test.CustomArrayTrait<int>>());
		}

		[Test]
		public void RegisterTraitDefinition_RequiresOverwriteToReplace()
		{
			var converter = new VariantConverter();
			converter.RegisterTraitDefinition<DateTime, Test.DateTimeConvertTrait>();

			Assert.Throws<VariantException>(() =>
				converter.RegisterTraitDefinition<DateTime, Test.AlternativeDateTimeConvertTrait>());

			converter.RegisterTraitDefinition<DateTime, Test.AlternativeDateTimeConvertTrait>(overwrite: true);

			var trait = converter.GetTrait(typeof(DateTime));
			Assert.That(trait, Is.InstanceOf<Test.AlternativeDateTimeConvertTrait>());
		}

		[Test]
		public void RegisterTraitDefinition_RejectsMismatchedTargetType()
		{
			var converter = new VariantConverter();

			Assert.Throws<VariantException>(() =>
				converter.RegisterTraitDefinition(typeof(int), typeof(Test.DateTimeConvertTrait)));

			Assert.Throws<VariantException>(() =>
				converter.RegisterTraitDefinition(typeof(List<>), typeof(Test.DateTimeConvertTrait)));
		}

		[Test]
		public void DynamicConvertTrait_CreateVariant_UsesPublicMembersAndJsonAttributes()
		{
			var value = new Test.DynamicSample
			{
				BaseValue = 10,
				Shadowed = "derived",
				FieldValue = 20,
				RenamedValue = "renamed",
				IgnoredValue = "ignored",
				Child = new Test.DynamicNested { Label = "child" }
			};

			var variant = JVariant.Create(value);

			Assert.That(variant.IsObject(), Is.True);
			Assert.That(variant.Get("BaseValue").AsInt(), Is.EqualTo(10));
			Assert.That(variant.Get("Shadowed").AsString(), Is.EqualTo("derived"));
			Assert.That(variant.Get("field_value").AsInt(), Is.EqualTo(20));
			Assert.That(variant.Get("renamed_value").AsString(), Is.EqualTo("renamed"));
			Assert.That(variant.Get("Child").Get("Label").AsString(), Is.EqualTo("child"));
			Assert.That(variant.ContainsKey("IgnoredValue"), Is.False);
			Assert.That(variant.ContainsKey("GetterOnly"), Is.False);
			Assert.That(variant.ContainsKey("RenamedValue"), Is.False);
		}

		[Test]
		public void DynamicConvertTrait_ConvertVariant_CreatesInstanceAndSetsMembers()
		{
			var child = new JObject();
			child.Add("Label", new JVariant("child").GetValue());

			var obj = new JObject();
			obj.Add("BaseValue", new JVariant(10).GetValue());
			obj.Add("Shadowed", new JVariant("derived").GetValue());
			obj.Add("field_value", new JVariant(20).GetValue());
			obj.Add("renamed_value", new JVariant("renamed").GetValue());
			obj.Add("IgnoredValue", new JVariant("ignored").GetValue());
			obj.Add("Child", child.AsVariant().GetValue());

			var value = obj.AsVariant().Require<Test.DynamicSample>();

			Assert.That(value.BaseValue, Is.EqualTo(10));
			Assert.That(value.Shadowed, Is.EqualTo("derived"));
			Assert.That(value.FieldValue, Is.EqualTo(20));
			Assert.That(value.RenamedValue, Is.EqualTo("renamed"));
			Assert.That(value.IgnoredValue, Is.EqualTo(""));
			Assert.That(value.Child.Label, Is.EqualTo("child"));
		}
	}
}
