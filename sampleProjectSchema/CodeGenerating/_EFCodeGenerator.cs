using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.CodeGenerating {
    partial class EFCodeGenerator {
        public EFCodeGenerator(Schema.ApplicationSchema schema) {
            entities = schema.GetEFCoreEntities();
        }
        public EFCodeGenerator(Core.ProjectContext context) {
            var aggregates = context.BuildAll();
            entities = aggregates
                .Union(aggregates.SelectMany(e => e.GetDescendantAggregates()))
                .Select(e => e.ToEFCoreEntity());
        }

        private readonly IEnumerable<Schema.EntityDef> entities;

        public IEnumerable<Schema.EntityDef> GetEntityDefs() {
            return entities;
        }
    }
}
