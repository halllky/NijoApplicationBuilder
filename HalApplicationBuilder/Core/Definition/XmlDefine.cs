using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Xml.Linq;

namespace HalApplicationBuilder.Core.Definition {
    internal class XmlDefine : IAggregateDefine {
        internal static IEnumerable<IAggregateDefine> EnumerateAggregateDefines(Config config, XDocument xDocument) {
            if (xDocument.Root == null) throw new FormatException($"集約定義のXMLの形式が不正です。");
            foreach (var element in xDocument.Root.Elements()) {
                if (element.Name.LocalName == Config.XML_CONFIG_SECTION_NAME) continue;
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
            var found = EnumerateAggregateDefines(_config, _xDocument)
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

                var isPrimary = innerElement.Attribute("key") != null;
                var isInstanceName = innerElement.Attribute("name") != null;
                var isNullable = innerElement.Attribute("nullable") != null;

                var type = innerElement.Attribute("type");
                if (type != null) {
                    var schalarType = MemberImpl.SchalarValue.TryParseTypeName(type.Value.Trim().ToLower());
                    if (schalarType == null) throw new InvalidOperationException($"{displayName}のtype属性の値'{type.Value}'が不正です。");
                    yield return new MemberImpl.SchalarValue(_config, displayName, isPrimary, isInstanceName, owner, schalarType, isNullable);

                } else {
                    var isRef = innerElement.Attribute("refTo") != null;
                    var isChildren = innerElement.Attribute("multiple") != null;
                    var isVariation = innerElement.Attribute("variation") != null;
                    if ((isRef && isChildren) || (isRef && isVariation) || (isChildren && isVariation)) {
                        throw new InvalidOperationException($"isRef属性,multiple属性,variation属性を同時に指定することはできません({displayName})。");

                    } else if (isRef) {
                        var to = innerElement.Attribute("refTo")?.Value;
                        if (string.IsNullOrWhiteSpace(to)) throw new FormatException($"type属性がrefの'{displayName}'にはto属性が必須です。");
                        var getRefTarget = () => GetAggregateByUniquePath(to);
                        yield return new MemberImpl.Reference(_config, displayName, isPrimary, isInstanceName, isNullable, owner, getRefTarget);

                    } else if (isChildren) {
                        var children = new XmlDefine(_config, innerElement, _xDocument);
                        yield return new MemberImpl.Children(_config, displayName, isPrimary, isInstanceName, owner, children);

                    } else if (isVariation) {
                        var variations = innerElement.Elements().Select((el, index) => {
                            var key = index + 1;
                            var value = (IAggregateDefine)new XmlDefine(_config, el, _xDocument);
                            return KeyValuePair.Create(key, value);
                        });
                        yield return new MemberImpl.Variation(_config, displayName, isPrimary, isInstanceName, owner, variations);

                    } else {
                        var child = new XmlDefine(_config, innerElement, _xDocument);
                        yield return new MemberImpl.Child(_config, displayName, isPrimary, isInstanceName, owner, child);
                    }
                }
            }
        }
    }
}

