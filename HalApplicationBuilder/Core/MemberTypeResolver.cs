using HalApplicationBuilder.Core.AggregateMemberTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    public sealed class MemberTypeResolver {
        public static MemberTypeResolver Default() {
            var resolver = new MemberTypeResolver()
                .Register(TYPE_ID, new Id())
                .Register(TYPE_UUID, new Uuid())
                .Register(TYPE_WORD, new Word())
                .Register(TYPE_SENTENCE, new Sentence())
                .Register(TYPE_INT, new Integer());
            return resolver;
        }
        internal const string TYPE_ID = "id";
        internal const string TYPE_UUID = "uuid";
        internal const string TYPE_WORD = "word";
        internal const string TYPE_SENTENCE = "sentence";
        internal const string TYPE_INT = "int";

        private MemberTypeResolver() { }
        private readonly Dictionary<string, IAggregateMemberType> _registered = new();

        public MemberTypeResolver Register(string typeName, IAggregateMemberType member) {
            _registered[typeName] = member;
            return this;
        }
        internal bool TryResolve(string typeName, out IAggregateMemberType member) {
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
