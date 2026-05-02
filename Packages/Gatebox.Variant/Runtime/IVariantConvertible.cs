using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Gatebox.Variant
{
	public interface IVariantConvertible
	{
		public JVariant AsVariant();
	}
}
