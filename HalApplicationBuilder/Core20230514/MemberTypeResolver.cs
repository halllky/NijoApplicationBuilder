using HalApplicationBuilder.Core20230514.AggregateMembers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    public sealed class MemberTypeResolver {
        public static MemberTypeResolver Default() {
            var resolver = new MemberTypeResolver()
                .Register("id", new Id())
                .Register("word", new Word())
                .Register("sentence", new Sentence());
            return resolver;
        }

        private MemberTypeResolver() { }
        private readonly Dictionary<string, IAggregateMember> _registered = new();

        public MemberTypeResolver Register(string typeName, IAggregateMember member) {
            _registered[typeName] = member;
            return this;
        }
        internal bool TryResolve(string typeName, out IAggregateMember member) {
#pragma warning disable CS8601 // Null 参照代入の可能性があります。
            if (_registered.TryGetValue(typeName, out member)) {
#pragma warning restore CS8601 // Null 参照代入の可能性があります。
                return true;
            } else {
#pragma warning disable CS8625 // null リテラルを null 非許容参照型に変換できません。
                member = default;
#pragma warning restore CS8625 // null リテラルを null 非許容参照型に変換できません。
                return false;
            }
        }
    }
}
