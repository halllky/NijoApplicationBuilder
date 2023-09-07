using HalApplicationBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class AggregateInstanceKeyNamePair : ITemplate {
        internal AggregateInstanceKeyNamePair(Config config) {
            _namespace = config.RootNamespace;
        }
        private readonly string _namespace;

        internal const string CLASSNAME = "AggregateInstanceKeyNamePair";
        internal const string KEY = "Key";
        internal const string NAME = "DisplayName";

        internal const string TS_DEF = "{ key: string, name: string }";
        internal const string JSON_KEY = "key";
        internal const string JSON_NAME = "name";

        public string FileName => "AggregateInstanceKeyNamePair.cs";
    }
}
