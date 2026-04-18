using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatebox.Variant
{
	/// <summary>
	/// JVariant 関連の例外
	/// </summary>
	public class VariantException : Exception
	{
		public VariantException()
		{
		}

		public VariantException(string message) : base(message)
		{
		}

		public VariantException(string message, Exception ex) : base(message, ex)
		{
		}
	}
}
