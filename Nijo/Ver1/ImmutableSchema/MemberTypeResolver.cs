using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.ImmutableSchema {
    /// <summary>
    /// <see cref="IValueMemberType"/> のインスタンスを解決するクラス。
    /// 単語型や整数型など基本的な属性は既定で用意されているが、
    /// 他のライブラリで独自の属性が定義されることもある。
    /// </summary>
    public sealed class MemberTypeResolver {

        /// <summary>
        /// 既定の <see cref="MemberTypeResolver"/> インスタンスを返します。
        /// 基本的な属性はこの中で既に定義されています。
        /// </summary>
        public static MemberTypeResolver Default() {
            throw new NotImplementedException();
        }

        internal MemberTypeResolver() { }

        /// <summary>
        /// 指定のキーに合致する <see cref="IValueMemberType"/> のインスタンスを返します。
        /// </summary>
        /// <param name="typeKey">属性種類キー</param>
        /// <exception cref="InvalidOperationException">キーに対応する種類が登録されていません。</exception>
        public IValueMemberType Resolve(string typeKey) {
            throw new NotImplementedException();
        }
    }
}
