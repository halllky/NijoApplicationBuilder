using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.CodeGenerating {
    partial class EFCodeGenerator {
        public EFCodeGenerator(Schema.ApplicationSchema schema) {
            _schema = schema;
        }

        private readonly Schema.ApplicationSchema _schema;

        public IEnumerable<Schema.EntityDef> GetEntityDefs() {
            return _schema.GetEFCoreEntities();
        }
    }
}
