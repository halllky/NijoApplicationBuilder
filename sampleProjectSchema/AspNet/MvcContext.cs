using System;
using System.Collections.Generic;
using System.Linq;
using haldoc.Core;

namespace haldoc.AspNet {
    public class MvcContext {
        internal MvcContext() { }

        public ProjectContext ProjectContext { get; init; }

        private List<Instance> _instanceModels;
        public IEnumerable<Instance> InstanceModels {
            get {
                if (_instanceModels == null) {
                    _instanceModels = ProjectContext
                        .EnumerateAllAggregates()
                        .Select(aggregate => new Instance {
                            Aggregate = aggregate,
                            MvcContext = this,
                        })
                        .ToList();
                }
                return _instanceModels;
            }
        }

        internal Instance FindModel(Aggregate aggregate) {
            return InstanceModels.Single(instance => instance.Aggregate == aggregate);
        }
    }
}
