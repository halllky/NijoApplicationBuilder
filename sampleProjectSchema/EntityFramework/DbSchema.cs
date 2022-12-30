using System;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.EntityFramework {
    public class DbSchema {
        internal DbSchema() { }

        public haldoc.Core.ProjectContext ProjectContext { get; init; }

        private List<DbTable> _tables;
        public IEnumerable<DbTable> Tables {
            get {
                if (_tables == null) {
                    _tables = ProjectContext
                        .EnumerateAllAggregates()
                        .Select(aggregate => new DbTable {
                            Aggregate = aggregate,
                            Schema = this,
                        })
                        .ToList();
                }
                return _tables;
            }
        }

        public DbTable FindTable(haldoc.Core.Aggregate aggregate) {
            return Tables.Single(table => table.Aggregate == aggregate);
        }
    }
}
