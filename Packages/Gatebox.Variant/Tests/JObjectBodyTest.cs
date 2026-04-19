using System;
using System.Collections.Generic;
using System.Linq;
using Gatebox.Variant.Internal;
using NUnit.Framework;

namespace Gatebox.Variant
{
	public class JObjectBodyTest
	{
		[Test]
		public void AddKeepsKeysSortedForSmallCollection()
		{
			var body = new JObjectBody();
			var a = new JValue("a-value");
			var b = new JValue("b-value");
			var c = new JValue("c-value");

			body.Add("b", b);
			body.Add("a", a);
			body.Add("c", c);

			Assert.That(body.Count, Is.EqualTo(3));
			Assert.That(body.GetKeyAt(0), Is.EqualTo("a"));
			Assert.That(body.GetKeyAt(1), Is.EqualTo("b"));
			Assert.That(body.GetKeyAt(2), Is.EqualTo("c"));
			Assert.That(body.Find("a"), Is.EqualTo(0));
			Assert.That(body.Find("b"), Is.EqualTo(1));
			Assert.That(body.Find("c"), Is.EqualTo(2));
			Assert.That(body["a"], Is.SameAs(a));
			Assert.That(body["b"], Is.SameAs(b));
			Assert.That(body["c"], Is.SameAs(c));
		}

		[Test]
		public void AddRejectsDuplicateKey()
		{
			var body = new JObjectBody();
			body.Add("key", new JValue(1L));

			Assert.That(() => body.Add("key", new JValue(2L)), Throws.ArgumentException);
			Assert.That(body.Count, Is.EqualTo(1));
		}

		[Test]
		public void IndexerReplacesExistingValueAndInsertsMissingValue()
		{
			var body = new JObjectBody();
			var first = new JValue(1L);
			var replacement = new JValue(2L);
			var inserted = new JValue(3L);

			body["b"] = first;
			body["b"] = replacement;
			body["a"] = inserted;

			Assert.That(body.Count, Is.EqualTo(2));
			Assert.That(body.GetKeyAt(0), Is.EqualTo("a"));
			Assert.That(body.GetKeyAt(1), Is.EqualTo("b"));
			Assert.That(body["a"], Is.SameAs(inserted));
			Assert.That(body["b"], Is.SameAs(replacement));
		}

		[Test]
		public void RemoveDeletesKeyAndCompactsBody()
		{
			var body = new JObjectBody();
			var a = new JValue("a-value");
			var b = new JValue("b-value");
			var c = new JValue("c-value");

			body.Add("a", a);
			body.Add("b", b);
			body.Add("c", c);

			Assert.That(body.Remove("b"), Is.True);
			Assert.That(body.Remove("missing"), Is.False);
			Assert.That(body.Count, Is.EqualTo(2));
			Assert.That(body.ContainsKey("b"), Is.False);
			Assert.That(body.GetKeyAt(0), Is.EqualTo("a"));
			Assert.That(body.GetKeyAt(1), Is.EqualTo("c"));
			Assert.That(body["a"], Is.SameAs(a));
			Assert.That(body["c"], Is.SameAs(c));
		}

		[Test]
		public void TryGetValueAndGetOrDefaultReturnStoredValue()
		{
			var body = new JObjectBody();
			var value = new JValue("value");
			body.Add("key", value);

			Assert.That(body.TryGetValue("key", out var found), Is.True);
			Assert.That(found, Is.SameAs(value));
			Assert.That(body.TryGetValue("missing", out var missing), Is.False);
			Assert.That(missing, Is.Null);
			Assert.That(body.GetOrDefault("key"), Is.SameAs(value));
			Assert.That(body.GetOrDefault("missing"), Is.Null);
		}

		[Test]
		public void AssignFromDictionarySortsKeys()
		{
			var b = new JValue("b-value");
			var a = new JValue("a-value");
			var source = new Dictionary<string, JValue>
			{
				{ "b", b },
				{ "a", a },
			};

			var body = new JObjectBody();
			body.Assign(source);

			Assert.That(body.Count, Is.EqualTo(2));
			Assert.That(body.GetKeyAt(0), Is.EqualTo("a"));
			Assert.That(body.GetKeyAt(1), Is.EqualTo("b"));
			Assert.That(body["a"], Is.SameAs(a));
			Assert.That(body["b"], Is.SameAs(b));
		}

		[Test]
		public void KeysAndValuesEnumerateStoredEntriesInKeyOrder()
		{
			var body = new JObjectBody();
			var a = new JValue("a-value");
			var b = new JValue("b-value");

			body.Add("b", b);
			body.Add("a", a);

			Assert.That(body.Keys.ToArray(), Is.EqualTo(new[] { "a", "b" }));
			Assert.That(body.Values.ToArray(), Is.EqualTo(new[] { a, b }));
		}
	}
}
