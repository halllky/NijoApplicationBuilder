using Nijo.Core.AggregateMemberTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.DataPatternsClass {
    /// <summary>
    /// 200_XMLファイルでなくC#で機械的に掛け合わせたパターン
    /// </summary>
    internal class _200_XMLファイルでなくC_で機械的に掛け合わせたパターン : DataPattern {
        public _200_XMLファイルでなくC_で機械的に掛け合わせたパターン() {
        }

        protected override string PatternName => "200_XMLファイルでなくC#で機械的に掛け合わせたパターン";

        protected override string GetNijoXmlContents() {
            var builder = new CombinationPatternBuilder<XElement>(random => new XElement($"集約{random.NextInt64()}"));

            var rootAggregates = builder
                // ルート集約の型
                .Pattern((el, random) => {
                    el.SetAttributeValue("is", "write-model-2");
                }, (el, random) => {
                    el.SetAttributeValue("is", "read-model-2");
                }, (el, random) => {
                    el.SetAttributeValue("is", "write-model-2 generate-default-read-model");
                })
                // DB名の有無
                .Pattern((el, random) => {
                    el.SetAttributeValue("DbName", $"TABLE{random.NextInt64()}");
                }, (el, random) => {
                    // DB名をつけない
                })
                // スカラメンバー列挙
                .Pattern((el, random) => {
                    var resolver = Core.MemberTypeResolver.Default();
                    foreach (var (key, memberType) in resolver.EnumerateAll()) {
                        var id = new XElement($"項目{random.NextInt64()}");
                        var isKey = memberType is Uuid || memberType is Integer || memberType is YearMonth || memberType is YearMonthDay;
                        id.SetAttributeValue("is", $"{key} {(isKey ? "key" : "")}");
                        el.Add(id);
                    }
                })
                .Build();

            var rootNode = new XElement("自動テストで作成されたプロジェクト");
            foreach (var rootAggregate in rootAggregates) {
                rootNode.Add(rootAggregate);
            }
            var doc = new XDocument();
            doc.Add(rootNode);
            return doc.ToString();
        }
    }
}
