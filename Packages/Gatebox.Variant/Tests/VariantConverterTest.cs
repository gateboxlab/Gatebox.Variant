using System;
using System.Collections.Generic;
using System.Text;
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
	}
}
