using System;


#nullable enable

namespace Gatebox.Variant
{
	/// <summary>
	/// JVaraint との互換性を持つ型
	/// <para>
	/// このインターフェイスを実装することで、型は JVariant との相互変換が可能になります。
	/// AsVariant() を実装し、 JVariant を受けるコンストラクタを定義してください。</para>
	/// </summary>
	public interface IVariantConvertible
	{
		public JVariant AsVariant();
	}
}
