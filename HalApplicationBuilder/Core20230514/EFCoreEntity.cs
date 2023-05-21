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


        internal IEnumerable<PropertyDefinition> GetMembers() {
            foreach (var member in Source.Members) {
                yield return member.ToPropertyDefinition();
            }
        }
    }
}
