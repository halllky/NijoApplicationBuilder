using System;
namespace haldoc.Runtime {
    public class FromTo<T> {
        public T From { get; }
        public T To { get; }
    }

    public class FromTo : FromTo<object> { }
}
