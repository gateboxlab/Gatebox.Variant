using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Gatebox.Variant
{
	public struct JVariant
	{
		public const int DefaultMaxDepth = 64;

		public static implicit operator JVariant(JValue v) => new (v);


		private JValue? m_Value;
		
		public JVariant(JValue value) => m_Value = value;
	
		public JVariant(bool value) => m_Value = value;

		public JVariant(long value)=> m_Value = value;
		public JVariant(double value)=> m_Value = value;
		public JVariant(string value)=> m_Value = value;
		public JVariant(JArray value)=> m_Value = value;
		public JVariant(JObject value)=> m_Value = value;


		/// <summary>
		/// 内部の値の型
		/// </summary>
		public readonly VariantType VariantType => m_Value?.VariantType ?? VariantType.Null;

		public readonly JValue Value => m_Value ?? new JValue();


		public readonly bool IsNull() => VariantType == VariantType.Null;
		public readonly bool IsBoolean() => VariantType == VariantType.Boolean;
		public readonly bool IsNumber() => VariantType == VariantType.Integer || VariantType == VariantType.Float;
		public readonly bool IsString() => VariantType == VariantType.String;
		public readonly bool IsArray() => VariantType == VariantType.Array;
		public readonly bool IsObject() => VariantType == VariantType.Object;
		public readonly bool IsComposite() => IsArray() || IsObject();
		public readonly bool IsPrimitive() => !IsComposite() && !IsNull();
		public readonly JObject AsObject() => m_Value?.AsObject() ?? new JObject();

		internal string Stringify(JsonFormatPolicy? policy)
		{
			throw new NotImplementedException();
		}

		internal U8View ToU8Json(JsonFormatPolicy? policy)
		{
			throw new NotImplementedException();
		}

		internal JVariant Pick(string path)
		{
			throw new NotImplementedException();
		}
	}
}
