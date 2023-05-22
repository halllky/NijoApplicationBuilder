using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class EFCoreEntity : IGraphNode {
        internal EFCoreEntity(AppSchema appSchema, Aggregate aggregate) {
            _appSchema = appSchema;
            Source = aggregate;
        }

        private readonly AppSchema _appSchema;
        public NodeId Id => Source.Id;
        internal Aggregate Source { get; }

        internal string ClassName => Source.DisplayName.ToCSharpSafe();


        internal IEnumerable<Member> GetMembers() {
            // 自身のテーブルで保持するカラム
            foreach (var member in Source.Members) {
                yield return new Member {
                    Owner = this,
                    PropertyName = member.Name,
                    CorrespondingAggregateMember = member,
                    CSharpTypeName = member.Type.GetCSharpTypeName(),
                    Initializer = "default",
                    RequiredAtDB = member.IsPrimary, // TODO XMLでrequired属性を定義できるようにする
                    Virtual = false,
                };
            }
        }


        internal class Member : ValueObject {
            internal required EFCoreEntity Owner { get; init; }
            internal required Aggregate.Member CorrespondingAggregateMember { get; init; }
            internal bool Virtual { get; init; }
            internal required string CSharpTypeName { get; init; }
            internal required string PropertyName { get; init; }
            internal string? Initializer { get; init; }
            internal bool RequiredAtDB { get; init; }

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return PropertyName;
            }
        }
    }
}
