using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class AppSchema {
        /// <summary>
        /// 集約定義XMLからスキーマを構築します。
        /// </summary>
        internal static AppSchema FromXml(string xmlContent) {
            return new AppSchema(config => Core.Definition.XmlDefine
                .Create(config, xmlContent)
                .Select(def => new RootAggregate(config, def)));
        }
        /// <summary>
        /// アセンブリ内で定義されている集約からスキーマを構築します。
        /// </summary>
        /// <param name="assembly">アセンブリ</param>
        /// <param name="namespace">特定の名前空間以下の型のみを参照したい場合は指定してください。</param>
        internal static AppSchema FromAssembly(Assembly assembly, string? @namespace = null) {
            return new AppSchema(config => {
                var types = assembly
                    .GetTypes()
                    .Where(type => type.GetCustomAttribute<AggregateAttribute>() != null);
                if (!string.IsNullOrWhiteSpace(@namespace)) {
                    types = types.Where(type => type.Namespace?.StartsWith(@namespace) == true);
                }
                return types.Select(t => new RootAggregate(config, new Core.Definition.ReflectionDefine(config, t, types)));
            });
        }
        /// <summary>
        /// 集約ルートを表す型の一覧からスキーマを構築します。
        /// </summary>
        /// <param name="rootAggregateTypes">集約ルート</param>
        internal static AppSchema FromReflection(IEnumerable<Type> rootAggregateTypes) {
            return new AppSchema(config => {
                return rootAggregateTypes.Select(t => {
                    var def = new Core.Definition.ReflectionDefine(config, t, rootAggregateTypes);
                    return new RootAggregate(config, def);
                });
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

            // TOOD 集約名重複チェック
            // TODO refの参照先の集約が実際に存在するかチェック

            return errors.Count == 0;
        }
    }
}
