using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace haldoc.Schema {
    public abstract class ApplicationSchema {

        public abstract string ApplicationName { get; }
        protected abstract IEnumerable<Type> RegisterAggregates();

        private HashSet<Type> _cache;
        public IReadOnlySet<Type> CachedTypes {
            get {
                if (_cache == null) _cache = RegisterAggregates().ToHashSet();
                return _cache;
            }
        }

        /// <summary>プロトタイプのためDBは適当な配列で代用している</summary>
        public HashSet<object> DB { get; }

        public ApplicationSchema(HashSet<object> db = null) {
            DB = db ?? new();
        }
    }
}
