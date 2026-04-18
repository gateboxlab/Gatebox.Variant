using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEditor.UIElements.ToolbarMenu;

namespace Gatebox.Variant
{
	public struct JArray
	{
		internal static JArray CreateInternal(List<JValue> body) => new JArray(body);



		private List<JValue> m_Body;



		

		public JArray(IEnumerable<JValue> values)
		{
			m_Body = new List<JValue>(values);
		}

		private JArray(List<JValue> values)
		{
			m_Body = values;
		}

		/// <summary>	
		/// 内部データを返す。
		/// <para>
		/// この JArray の内部データを返します。参照をそのまま返すのでこれを編集する場合は注意してください。
		/// JArray は内部情報が null の場合と 0 件の場合があり、表面的にはそれを同等のものとして扱っています。</para>
		/// <para>
		/// このメソッドは内部状態が null の場合、 0 件の情報を生成してそれを返します。(null を返すことはありません) </para>
		/// </summary>
		internal List<JValue> GetBody()
		{
			return EnsureBody();
		}



		// body が null ならば新しいのを作る
		private List<JValue> EnsureBody()
		{

			m_Body ??= new List<JValue>();
			return m_Body;
		}

		public readonly JObject ConvertToObject()
		{
			if (m_Body == null)
			{
				return new JObject();
			}

			JObject ret = JObject.CreateWithCapacity(m_Body.Count + 4);
			for (int i = 0; i < m_Body.Count; i++)
			{
				ret.Set(i.ToString(), m_Body[i]);
			}
			return ret;
		}
	}
}
