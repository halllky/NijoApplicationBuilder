using System;
namespace haldoc.Runtime {
    public class SingleViewModel {
        public Guid AggregateId { get; set; }
        public object Instance { get; set; }
    }
    public class SingleViewModel<T> : SingleViewModel {
        public new T Instance {
            get => (T)base.Instance;
            set => base.Instance = value;
        }
    }
}
