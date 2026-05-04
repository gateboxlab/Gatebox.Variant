using System;
using System.Collections.Generic;
using System.Text;

namespace Gatebox.Variant.Internal
{
	public static class TypeExtensions
	{
		/// <summary>
		/// その型のインスタンスが存在しうるかどうか。
		/// <para>
		/// 抽象クラスでなく、インターフェースでなく、オープンジェネリック型でないとき true.</para>
		/// </summary>
		public static bool IsConcrete(this Type type)
		{
			return !type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition;
		}

		/// <summary>
		/// パラメータなしのコンストラクタでその型を生成できるかどうか。
		/// </summary>
		public static bool IsDefaultConstructible(this Type type)
		{
			return type.IsConcrete() && type.GetConstructor(Type.EmptyTypes) != null;
		}
	}
}
