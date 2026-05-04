using System;
using System.Collections.Generic;
using System.Text;

namespace Gatebox.Variant.Internal
{


	public class DynamicConvertTrait<T> : ConvertTrait<T>
	{
		public DynamicConvertTrait() 
		{ 
		
		
		}

		public override JVariant CreateVariant(T v)
		{
			throw new NotImplementedException();
		}

		public override T ConvertVariant(JVariant variant)
		{
			throw new NotImplementedException();
		}
	}
}
