using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gatebox.Variant.Internal;

#nullable enable

namespace Gatebox.Variant
{

	public static class JValueExtensions
	{
		public static VariantType GetVariantType(this JValue value) => value?.VariantType ?? VariantType.Null;
		public static bool IsNull(this JValue value) => value.GetVariantType() == VariantType.Null;
		public static bool IsBoolean(this JValue value) => value.GetVariantType() == VariantType.Boolean;
		public static bool IsNumber(this JValue value) => value.GetVariantType() == VariantType.Integer || value.GetVariantType() == VariantType.Float;
		public static bool IsString(this JValue value) => value.GetVariantType() == VariantType.String;
		public static bool IsArray(this JValue value) => value.GetVariantType() == VariantType.Array;
		public static bool IsObject(this JValue value) => value.GetVariantType() == VariantType.Object;
		public static bool IsComposite(this JValue value) => value.IsArray() || value.IsObject();


		public static JObject AsObject(this JValue value)
		{
			if (value == null || value.IsNull())
			{
				return new JObject();
			}
			if (value.IsObject())
			{
				return value.ObjectValue;
			}
			if( value.IsArray())
			{
				return value.ArrayValue.ConvertToObject();
			}

			return new JObject();
		}

		public static JArray AsArray(this JValue value)
		{
			if (value == null || value.IsNull())
			{
				return new JArray();
			}
			if (value.IsArray())
			{
				return value.ArrayValue;
			}
			if (value.IsObject())
			{
				if (value.ObjectValue.TryConvertToArray(out var array))
				{
					return array;
				}
			}
			return new JArray();
		}
	}






	/// <summary>
	/// JSON の 値。
	/// <para>
	/// 参照型です。
	/// </para>
	/// </summary>
	public class JValue
	{

		//==============================================================================
		// operators
		//==============================================================================

		public static implicit operator JValue(bool v) => new JValue(v);
		public static implicit operator JValue(long v)=> new JValue(v);
		public static implicit operator JValue(double v)=> new JValue(v);
		public static implicit operator JValue(string v)=> new JValue(v);
		public static implicit operator JValue(JArray v)=> new JValue(v);
		public static implicit operator JValue(JObject v)=> new JValue(v);



		//==============================================================================
		// static members
		//==============================================================================



		//==============================================================================
		// instance members
		//==============================================================================

		private VariantType m_Type;
		private long m_IntValue;
		private double m_FloatValue;
		private object? m_RefValue;



		public JValue()
		{
			m_Type = VariantType.Null;
		}

		public JValue(bool b)
		{
			m_Type = VariantType.Boolean;
			m_IntValue = b ? 1 : 0;
		}

		public JValue(long i)
		{
			m_Type = VariantType.Integer;
			m_IntValue = i;
		}
		public JValue(double d)
		{
			m_Type = VariantType.Float;
			m_FloatValue = d;
		}
		public JValue(string s)
		{
			m_Type = VariantType.String;
			m_RefValue = s;
		}

		public JValue(JArray a)
		{
			m_Type = VariantType.Array;
			m_RefValue = a;
		}

		public JValue(JObject o)
		{
			m_Type = VariantType.Object;
			m_RefValue = o;
		}

		/// <summary>
		/// シャロウコピーによるコンストラクタ。
		/// <para>
		/// C# のオブジェクトとしては別物になりますが、
		/// 内部に配列もしくはオブジェクトが入っていた場合は同じものを指すことになります。
		/// null が与えられた場合は Null を指す JValue として初期化されます。</para>
		/// </summary>
		public JValue(JValue source)
		{
			if (source == null)
			{
				m_Type = VariantType.Null;
				return;
			}
			m_Type = source.m_Type;
			m_IntValue = source.m_IntValue;
			m_FloatValue = source.m_FloatValue;
			m_RefValue = source.m_RefValue;
		}


		/// <summary>
		/// 保持している値の種類。
		/// <para>
		/// - <seealso cref="JValueExtensions.GetVariantType(JValue)">GetVariantType()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsNull(JValue)">IsNull()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsBoolean(JValue)">IsBoolean()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsNumber(JValue)">IsNumber()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsString(JValue)">IsString()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsArray(JValue)">IsArray()</seealso><br/>
		/// - <seealso cref="JValueExtensions.IsObject(JValue)">IsObject()</seealso><br/>
		/// </para>
		/// </summary>
		public VariantType VariantType => m_Type;


		public JObject ObjectValue
		{
			get
			{
				if (m_Type == VariantType.Object)
				{
					return JObject.CreateInternal((JObjectBody)m_RefValue!);
				}

				if (m_Type == VariantType.Null)
				{
					return new JObject();
				}

				throw new VariantException($"JValue does not contain an object. Actual type: {m_Type}");				
			}
		}

		public JArray ArrayValue
		{
			get
			{
				if (m_Type == VariantType.Array)
				{
					return JArray.CreateInternal((List<JValue>)m_RefValue!);
				}
				if (m_Type == VariantType.Null)
				{
					return new JArray();
				}
				throw new VariantException($"JValue does not contain an array. Actual type: {m_Type}");
			}

		}



		/// <summary>代入。</summary>
		public void Assign(bool v)
		{
			m_Type = VariantType.Boolean;
			m_IntValue = v ? 1 : 0;
			m_RefValue = null;
		}

		/// <summary>代入。</summary>
		public void Assign(long v)
		{
			m_Type = VariantType.Integer;
			m_IntValue = v;
			m_RefValue = null;
		}
		/// <summary>代入。</summary>
		public void Assign(double v) {
			m_Type = VariantType.Float;
			m_FloatValue = v;
			m_RefValue = null;
		}
		/// <summary>代入。</summary>
		public void Assign(string v)
		{
			m_Type = VariantType.String;
			m_RefValue = v;
		}
		/// <summary>代入。</summary>
		public void Assign(JArray v)
		{
			m_Type = VariantType.Array;
			m_RefValue = v.GetBody();
		}
		/// <summary>代入。</summary>
		public void Assign(JObject v)
		{
			m_Type = VariantType.Object;
			m_RefValue = v.GetBody();
		}

		/// <summary>代入。</summary>
		public void Assign(JVariant v)
		{
			Assign(v.Value);
		}

		/// <summary>代入。</summary>
		public void Assign(JValue v)
		{
			if (v == null)
			{
				m_Type = VariantType.Null;
				m_RefValue = null;
				return;
			}
			m_Type = v.m_Type;
			m_IntValue = v.m_IntValue;
			m_FloatValue = v.m_FloatValue;
			m_RefValue = v.m_RefValue;
		}
	}
}
