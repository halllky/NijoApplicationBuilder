using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.CodeGenerating {
    partial class EFCodeGenerator {
        public EFCodeGenerator(Core.ProjectContext context) {
            var aggregates = context.BuildAll();
            entities = aggregates
                .Union(aggregates.SelectMany(e => e.GetDescendantAggregates()))
                .Select(e => e.ToEFCoreEntity());
        }

        private readonly IEnumerable<Core.Dto.EntityDef> entities;

        public IEnumerable<Core.Dto.EntityDef> GetEntityDefs() {
            return entities;
        }
    }
}
