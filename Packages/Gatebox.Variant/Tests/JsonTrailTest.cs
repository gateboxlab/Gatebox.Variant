using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Gatebox.Variant.Internal
{
	public class JsonTrailTest
	{
		[Test]
		public void DotOnlySegments()
		{
			var parts = JsonTrail.Parse("");
			Assert.That(parts.Count, Is.EqualTo(1));
			Assert.That(parts[0], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "")));

			parts = JsonTrail.Parse("a");
			Assert.That(parts[0], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "a")));

			parts = JsonTrail.Parse("a.");
			Assert.That(parts.Count, Is.EqualTo(2));
			Assert.That(parts[0], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "a")));
			Assert.That(parts[1], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "")));

			parts = JsonTrail.Parse(".");
			Assert.That(parts.Count, Is.EqualTo(2));
			Assert.That(parts[0], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "")));
			Assert.That(parts[1], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "")));

			parts = JsonTrail.Parse("a.b.c");
			Assert.That(parts.Count, Is.EqualTo(3));
			Assert.That(parts[0], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "a")));
			Assert.That(parts[1], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "b")));
			Assert.That(parts[2], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "c")));
		}

		[Test]
		public void BracketSegments()
		{
			var parts = JsonTrail.Parse("root[items][1].name");

			Assert.That(parts.Count, Is.EqualTo(4));
			Assert.That(parts[0], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "root")));
			Assert.That(parts[1], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferArray, "items")));
			Assert.That(parts[2], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferArray, "1")));
			Assert.That(parts[3], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "name")));
		}

		[Test]
		public void BracketSegmentsSupportEscapesAndAppend()
		{
			var parts = JsonTrail.Parse(@"root[key\.with\]bracket][line\nbreak][+]");

			Assert.That(parts.Count, Is.EqualTo(4));
			Assert.That(parts[0], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferObject, "root")));
			Assert.That(parts[1], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.PrefferArray, "key.with]bracket")));
			Assert.That(parts[2].Kind, Is.EqualTo(JsonTrail.Kind.PrefferArray));
			Assert.That(parts[2].Value.ToString(), Is.EqualTo("line\nbreak"));
			Assert.That(parts[3], Is.EqualTo(new JsonTrail.Part(JsonTrail.Kind.AppendArray, "+")));
		}

		[Test]
		public void ParseForReadReturnsOnlyValues()
		{
			var parts = JsonTrail.ParseForRead(" root . [items] . name ");

			Assert.That(parts.Count, Is.EqualTo(3));
			Assert.That(parts[0].ToString(), Is.EqualTo("root"));
			Assert.That(parts[1].ToString(), Is.EqualTo("items"));
			Assert.That(parts[2].ToString(), Is.EqualTo("name"));
		}

		[Test]
		public void InvalidBracketFormatThrows()
		{
			Assert.That(() => JsonTrail.Parse("root[child"), Throws.TypeOf<InvalidDataException>());
			Assert.That(() => JsonTrail.Parse("root[child]name"), Throws.TypeOf<InvalidDataException>());
		}
	}
}
