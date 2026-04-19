
using System.ComponentModel;


// UNITY_2021_2_OR_NEWER は C#9.0 が使える Unity のバージョン。
// これに意味があるわけではなく、Unity であるかどうかを分岐したいだけなのだが、それに相当するシンボルは存在しない。
// コードとして C#9.0 移行を前提としているため、UNITY_2021_2_OR_NEWER を Unity であるかどうかの分岐に使うことにする。


// 2021.2 以前はそのそもこのコードは動かない。
// (この 5.3 っていうのも意味がないのだが、一番古いっぽいのでこれを使う。)
#if UNITY_5_3_OR_NEWER && !UNITY_2021_2_OR_NEWER
#error "Unity 2021.2 以降を使用してください。"
#endif


namespace System.Runtime.CompilerServices
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1
	// コンパイラは C#9.0 に対応しているが、ライブラリが対応していない。
	// init-only プロパティを使うために、IsExternalInit を定義する。
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static class IsExternalInit
	{
	}
#endif
}


// Nullable 関連属性。

namespace System.Diagnostics.CodeAnalysis
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1


	/// <summary>
	/// メソッド実行後、戻り値が true もしくは false の時、特定のメンバーが null でないことを表明する属性。
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	internal sealed class MemberNotNullWhenAttribute : Attribute
	{
		public string[] Members { get; }
		public MemberNotNullWhenAttribute(bool returnValue, params string[] members) { Members = members; }
	}

	/// <summary>
	/// メソッド実行後、特定のメンバーが null でないことを表明する属性。
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	internal sealed class MemberNotNullAttribute : Attribute
	{
		public string[] Members { get; }
		public MemberNotNullAttribute(params string[] members) { Members = members; }
	}

	// ありそうなやつ、必要に応じて追加していく。
	// NotNullWhenAttribute 
	// MaybeNullAttribute
	// MemberNotNullWhenAttribute
	// DisallowNullAttribute
	// AllowNullAttribute


#endif
}
