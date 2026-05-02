using System;

#nullable enable

namespace Gatebox.Variant
{
	/// <summary>
	/// JVariant の内容物。
	/// <para>
	/// JSON としては整数型は存在せず、Number としてまとめられるべきものですが、
	/// 実装上整数値と少数値は別扱いです。</para>
	/// </summary>
	public enum VariantType
	{
		Null = 0,
		Boolean,
		Integer,
		Float,
		String,
		Array,
		Object,
	}
}
