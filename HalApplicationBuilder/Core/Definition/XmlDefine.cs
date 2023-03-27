using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Xml.Linq;

namespace HalApplicationBuilder.Core.Definition {
    internal class XmlDefine : IAggregateDefine {
        internal static IEnumerable<IAggregateDefine> Create(Config config, string xml) {
            var xDocument = XDocument.Parse(xml);
            return Create(config, xDocument);
        }
        internal static IEnumerable<IAggregateDefine> Create(Config config, XDocument xDocument) {
            if (xDocument.Root == null) throw new FormatException($"集約定義のXMLの形式が不正です。");
            foreach (var element in xDocument.Root.Elements()) {
                yield return new XmlDefine(config, element, xDocument);
            }
        }

        private XmlDefine(Config config, XElement aggregateElement, XDocument xDocument) {
            _config = config;
            _aggregateElement = aggregateElement;
            _xDocument = xDocument;
        }

        private readonly Config _config;
        private readonly XElement _aggregateElement;
        private readonly XDocument _xDocument;

        public string DisplayName => _aggregateElement.Name.LocalName;

        private Aggregate GetAggregateByUniquePath(string uniquePath) {
            var found = Create(_config, _xDocument)
                .Select(def => new RootAggregate(_config, def))
                .SelectMany(root => root.GetDescendantsAndSelf())
                .SingleOrDefault(aggregate => aggregate.GetUniquePath() == uniquePath);
            if (found == null)
                throw new InvalidOperationException($"'{uniquePath}' の集約が見つかりません。");
            return found;
        }

        public IEnumerable<AggregateMember> GetMembers(Aggregate owner) {
            foreach (var innerElement in _aggregateElement.Elements()) {
                var displayName = innerElement.Name.LocalName;

                var key = innerElement.Attribute("Key")?.Value.Trim().ToLower();
                var @null = innerElement.Attribute("Null")?.Value.Trim().ToLower();
                if (key != null && key != "true" && key != "false")
                    throw new InvalidOperationException($"{displayName} のKey属性の値が不正です。trueまたはfalseのいずれかを指定してください。");
                if (@null != null && @null != "true" && @null != "false")
                    throw new InvalidOperationException($"{displayName} のNull属性の値が不正です。trueまたはfalseのいずれかを指定してください。");

                var isPrimary = key == "true";
                var isNullable = @null == "true";

                var kind = innerElement.Attribute("Kind")?.Value.Trim().ToLower();
                if (string.IsNullOrWhiteSpace(kind))
                    throw new InvalidOperationException($"{displayName} のKind属性が未設定です。");

                var schalarType = MemberImpl.SchalarValue.TryParseTypeName(kind);
                if (schalarType != null) {
                    yield return new MemberImpl.SchalarValue(_config, displayName, isPrimary, owner, schalarType, isNullable);

                } else if (kind == "ref") {
                    var to = innerElement.Attribute("To")?.Value;
                    if (string.IsNullOrWhiteSpace(to)) throw new FormatException($"Kind属性がrefの'{displayName}'にはTo属性が必須です。");
                    var getRefTarget = () => GetAggregateByUniquePath(to);
                    yield return new MemberImpl.Reference(_config, displayName, isPrimary, isNullable, owner, getRefTarget);

                } else if (kind == "child") {
                    var child = new XmlDefine(_config, innerElement, _xDocument);
                    yield return new MemberImpl.Child(_config, displayName, isPrimary, owner, child);

                } else if (kind == "children") {
                    var children = new XmlDefine(_config, innerElement, _xDocument);
                    yield return new MemberImpl.Children(_config, displayName, isPrimary, owner, children);

                } else if (kind == "variation") {
                    var variations = innerElement.Elements().ToDictionary(
                        el => int.Parse(el.Attribute("Key")?.Value ?? ""),
                        el => (IAggregateDefine)new XmlDefine(_config, el, _xDocument));
                    yield return new MemberImpl.Variation(_config, displayName, isPrimary, owner, variations);

                } else {
                    throw new InvalidOperationException($"{displayName} のKind属性が不正です: '{kind}'");
                }
            }
        }
    }
}

