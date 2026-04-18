using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;





#nullable enable

using Entity = System.Collections.Generic.KeyValuePair<Gatebox.Variant.StringView, Gatebox.Variant.JValue>;

namespace Gatebox.Variant.Internal
{
	internal class JObjectBody {
		
		
		public JObjectBody(int capacity = 0)
		{
			
		}

		public int Count => 0;


		public ICollection<string> GetKeyCollection()
		{
			return Array.Empty<string>();
		}
		public ICollection<JValue> GetValueCollection()
		{
			return Array.Empty<JValue>();
		}

		public void Clear()
		{

		}

		public bool Remove(StringView key)
		{
			return false;
		}
		public void Add(StringView key, JValue value)
		{

		}

		public bool Contains(KeyValuePair<string, JValue> item)
		{
			return false;
		}
		public bool ContainsKey(StringView key)
		{
			return false;
		}

		public JValue? GetOrDefault( StringView key)
		{
			return null;
		}
		public void CopyTo(KeyValuePair<string, JValue>[] array, int arrayIndex)
		{

		}
		public IEnumerator<KeyValuePair<string, JValue>> GetEnumerator()
		{
			return Enumerable.Empty<KeyValuePair<string, JValue>>().GetEnumerator();
		}

		// ICollection の実装のために仕方なく存在する。
		public bool Remove(KeyValuePair<string, JValue> item)
		{
			// 一回 Key でとってきて value が等しかったら削除する。

		return false;
		}
	}
}
