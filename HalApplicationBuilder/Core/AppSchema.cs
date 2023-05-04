using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class AppSchema {
        /// <summary>
        /// 集約定義XMLからスキーマを構築します。
        /// </summary>
        internal static AppSchema FromXml(string xmlContent) {
            return new AppSchema(config => {
                return Core.Definition.XmlDefine
                   .Create(config, xmlContent)
                   .Select(def => new RootAggregate(config, def));
            });
        }

        private AppSchema(Func<Config, IEnumerable<RootAggregate>> rootAggregatesBuilder) {
            _rootAggregatesBuilder = rootAggregatesBuilder;
        }
        private readonly Func<Config, IEnumerable<RootAggregate>> _rootAggregatesBuilder;
        internal IEnumerable<RootAggregate> GetRootAggregates(Config config) {
            return _rootAggregatesBuilder.Invoke(config);
        }

        internal bool IsValid(TextWriter? log = null) {
            var errors = new List<string>();

            return errors.Count == 0;
        }
    }
}
