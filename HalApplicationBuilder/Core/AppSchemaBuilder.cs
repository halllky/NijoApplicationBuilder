using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static HalApplicationBuilder.CodeRendering.Searching.SearchFeature;

namespace HalApplicationBuilder.Core {
    internal class AppSchemaBuilder {

        #region XML
        internal static bool AddXml(AppSchemaBuilder builder, XDocument xDocument, out ICollection<string> errors) {
            if (xDocument.Root == null) throw new FormatException($"Xml doesn't have contents.");

            var errorList = new List<string>();

            void Handle(ParsedXElement el, AggregatePath? parent) {
                const string VARIATION_KEY = "variation-key";
                var errorListLocal = new HashSet<string>();

                // パス組み立て
                AggregatePath aggregatePath;
                if (parent == null) {
                    if (!AggregatePath.TryCreate(new[] { el.Source.Name.LocalName }, out aggregatePath, out var err)) errorListLocal.Add(err);
                } else {
                    if (!parent.TryCreateChild(el.Source.Name.LocalName, out aggregatePath, out var err)) errorListLocal.Add(err);
                }

                // バリデーション
                var members = el.Source
                    .Elements()
                    .Select(inner => Attributes.Parse(inner, errorListLocal))
                    .ToArray();

                if (el.ElementType == E_XElementType.VariationContainer) {
                    var duplicates = el.Source
                        .Elements()
                        .Select(e => e.Attribute(VARIATION_KEY)?.Value)
                        .Where(str => str != null)
                        .Select(str => int.TryParse(str, out var i) ? i : (int?)null)
                        .GroupBy(i => i)
                        .Where(group => group.Key != null && group.Count() >= 2);
                    foreach (var group in duplicates) {
                        errorListLocal.Add($"Value of '{VARIATION_KEY}' of child of '{el.Source.Name.LocalName}' duplicates: {group.Key}");
                    }
                    foreach (var innerElement in el.Source.Elements()) {
                        if (innerElement.Attribute(VARIATION_KEY) == null) {
                            errorListLocal.Add($"Aggregate define '{innerElement.Name}' must have '{VARIATION_KEY}' attribute.");
                            continue;
                        }
                    }
                }

                errorList.AddRange(errorListLocal);

                // 登録
                var schalarMembers = members
                    .Where(member => member.ElementType == E_XElementType.Schalar)
                    .Select(member => new SchalarMemberDef {
                        Name = member.Source.Name.LocalName,
                        Type = member.AggregateMemberTypeName,
                        IsPrimary = member.IsKey,
                        IsInstanceName = member.IsName,
                        Optional = !member.IsRequired,
                    });

                if (el.ElementType == E_XElementType.RootAggregate) {
                    builder.AddAggregate(new AggregateDef {
                        FullPath = aggregatePath,
                        Members = schalarMembers.ToList(),
                    });
                } else if (el.ElementType == E_XElementType.ChildAggregate) {
                    if (el.IsMultipleChildAggregate) {
                        builder.AddChildrenAggregate(new ChildrenDef {
                            Name = el.Source.Name.LocalName,
                            Members = schalarMembers.ToList(),
                            OwnerFullPath = parent!.Value,
                        });
                    } else {
                        builder.AddChildAggregate(new ChildDef {
                            Name = el.Source.Name.LocalName,
                            Members = schalarMembers.ToList(),
                            OwnerFullPath = parent!.Value,
                        });
                    }
                } else if (el.ElementType == E_XElementType.VariationValue) {
                    builder.AddVariationAggregate(new VariationDef {
                        Name = el.Source.Name.LocalName,
                        Members = schalarMembers.ToList(),
                        VariationContainer = el.Source.Parent?.Name.LocalName ?? string.Empty,
                        VariationSwitch = el.Source.Attribute(VARIATION_KEY)?.Value ?? string.Empty,
                        OwnerFullPath = parent!.Value,
                        IsPrimary = el.IsKey,
                        IsInstanceName = el.IsName,
                        Optional = !el.IsRequired,
                    });
                }

                var refMembers = members.Where(m => m.ElementType == E_XElementType.Ref);
                foreach (var member in refMembers) {
                    builder.AddReference(new ReferenceDef {
                        Name = member.Source.Name.LocalName,
                        OwnerFullPath = aggregatePath.Value,
                        IsPrimary = member.IsKey,
                        IsInstanceName = member.IsName,
                        IsRequired = member.IsRequired,
                        TargetFullPath = member.RefTargetName,
                    });
                }

                // 再帰
                var descendants = members
                    .Where(member => member.ElementType == E_XElementType.ChildAggregate
                                  || member.ElementType == E_XElementType.VariationValue);
                foreach (var item in descendants) {
                    Handle(item, aggregatePath);
                }
            }

            builder.SetApplicationName(xDocument.Root.Name.LocalName);

            foreach (var xElement in xDocument.Root.Elements()) {
                if (xElement.Name.LocalName == Config.XML_CONFIG_SECTION_NAME) continue;
                var parsed = Attributes.Parse(xElement, errorList);
                Handle(parsed, parent: null);
            }
            errors = errorList;
            return errors.Count == 0;
        }

        private class ParsedXElement {
            public required XElement Source { get; init; }
            public required E_XElementType ElementType { get; init; }
            public required bool IsMultipleChildAggregate { get; init; }
            public required bool IsKey { get; init; }
            public required bool IsName { get; init; }
            public required bool IsRequired { get; init; }
            public required string AggregateMemberTypeName { get; init; }

            public required string RefTargetName { get; init; }
        }
        private enum E_XElementType {
            RootAggregate,
            ChildAggregate,
            VariationContainer,
            VariationValue,
            Schalar,
            Ref,
        }

        /// <summary>
        /// is="" で複数の値を指定したときに設定が競合したりidを自動的に主キーと推測したりする仕様が複雑なのでそれを簡略化するための仕組み
        /// </summary>
        private abstract class Attributes {
            public static ParsedXElement Parse(XElement element, ICollection<string> errors) {

                // stringの辞書に変換
                var isAttribute = element.Attribute("is")?.Value ?? string.Empty;
                var splitted = isAttribute.Split(' ', '　');
                var keyValues = new Dictionary<string, string>();
                foreach (var item in splitted) {
                    var separated = item.Split(':');
                    if (separated.Length >= 3) {
                        errors.Add($"'{element.Name}' の '{item}' が':'を複数含んでいます。");
                        continue;
                    }
                    var key = separated[0];
                    var value = separated.Length >= 2 ? separated[1] : string.Empty;
                    keyValues.Add(key, value);
                }

                // 各値のハンドラを決定
                var attributeTypes = Enumerate().ToDictionary(attr => attr.GetType().GetCustomAttribute<IsAttribute>()!.Value);
                var handlers = new HashSet<Attributes>();
                foreach (var kv in keyValues) {
                    if (!attributeTypes.TryGetValue(kv.Key.ToLower(), out var handler)) {
                        errors.Add($"'{element.Name}' の '{kv.Key}' は認識できない属性です。");
                        continue;
                    }
                    if (handlers.Contains(handler)) {
                        errors.Add($"'{element.Name}' に '{kv.Key}' が複数指定されています。");
                        continue;
                    }
                    handler.Value = kv.Value;
                    handlers.Add(handler);
                }

                // 各指定間で矛盾がないかを調べて返す
                bool specified;
                var elementType = Parse(handlers.Select(h => h.ElementType), out specified, err => errors.Add($"'{element.Name.LocalName}' 種別の指定でエラー: {err} ('{isAttribute}')"));
                if (!specified) errors.Add("の種別が不明です。");

                var multiple = Parse(handlers.Select(h => h.IsMultipleChildAggregate), out specified, err => errors.Add($"'{element.Name.LocalName}' エラー: {err} ('{isAttribute}')"));
                if (!specified) multiple = false;

                var isKey = Parse(handlers.Select(h => h.IsKey), out specified, err => errors.Add($"'{element.Name.LocalName}' キーか否かの指定でエラー: {err} ('{isAttribute}')"));
                if (!specified) isKey = false;

                var isName = Parse(handlers.Select(h => h.IsName), out specified, err => errors.Add($"'{element.Name.LocalName}' 表示名称か否かの指定でエラー: {err} ('{isAttribute}')"));
                if (!specified) isName = false;

                var isRequired = Parse(handlers.Select(h => h.IsRequired), out specified, err => errors.Add($"'{element.Name.LocalName}' 必須か否かの指定でエラー: {err} ('{isAttribute}')"));
                if (!specified) isRequired = false;

                var memberTypeName = Parse(handlers.Select(h => h.AggregateMemberTypeName), out specified, err => errors.Add($"'{element.Name.LocalName}' 型名の指定でエラー: {err} ('{isAttribute}')")) ?? string.Empty;
                if (!specified) memberTypeName = string.Empty;

                return new ParsedXElement {
                    Source = element,
                    ElementType = elementType,
                    IsKey = isKey,
                    IsName = isName,
                    IsRequired = isRequired,
                    AggregateMemberTypeName = memberTypeName,
                    IsMultipleChildAggregate = multiple,
                    RefTargetName = elementType == E_XElementType.Ref
                        ? handlers.OfType<RefToAttr>().First().Value
                        : string.Empty,
                };
            }
            /// <summary>
            /// 各指定値の優先順位を考慮して値を決定する。
            /// </summary>
            private static T? Parse<T>(IEnumerable<(T, E_Priority)?> values, out bool specified, Action<string> onError) {
                var notNullValues = values
                    .Where(x => x != null)
                    .Cast<(T, E_Priority)>()
                    .ToArray();

                var force = notNullValues
                    .Where(x => x.Item2 == E_Priority.Force)
                    .GroupBy(x => x.Item1)
                    .ToArray();
                if (force.Length >= 2) {
                    onError("矛盾する値が指定されています。");
                    specified = true;
                    return default;
                } else if (force.Length == 1) {
                    specified = true;
                    return force.Single().Key;
                }

                var ifNotSpecified = notNullValues
                    .Where(x => x.Item2 == E_Priority.IfNotSpecified)
                    .ToArray();
                if (ifNotSpecified.Length >= 1) {
                    specified = true;
                    return ifNotSpecified.First().Item1;
                }

                specified = false;
                return default;
            }

            public enum E_Priority {
                Force,
                IfNotSpecified,
            }

            protected virtual ValueTuple<E_XElementType, E_Priority>? ElementType => null;
            protected virtual (bool, E_Priority)? IsMultipleChildAggregate => null;
            protected virtual (bool, E_Priority)? IsKey => null;
            protected virtual (bool, E_Priority)? IsName => null;
            protected virtual (bool, E_Priority)? IsRequired => null;
            protected virtual (string, E_Priority)? AggregateMemberTypeName => null;
            protected string Value { get; set; } = string.Empty;

            #region 新しい属性があればここに追加
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            private sealed class IsAttribute : Attribute {
                public IsAttribute(string value) {
                    Value = value;
                }
                public string Value { get; }
            }
            private static IEnumerable<Attributes> Enumerate() {
                yield return new MasterDataAttr();
                yield return new ObjectAttr();
                yield return new ArrayAttr();
                yield return new VariationAttr();
                yield return new RefToAttr();
                yield return new KeyAttr();
                yield return new NameAttr();
                yield return new IdAttr();
                yield return new WordAttr();
                yield return new SentenceAttr();
            }

            [Is("master-data")]
            private class MasterDataAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.RootAggregate, E_Priority.Force);
            }
            [Is("object")]
            private class ObjectAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.ChildAggregate, E_Priority.Force);
                protected override (bool, E_Priority)? IsMultipleChildAggregate => (false, E_Priority.Force);
            }
            [Is("array")]
            private class ArrayAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.ChildAggregate, E_Priority.Force);
                protected override (bool, E_Priority)? IsMultipleChildAggregate => (true, E_Priority.Force);
            }
            [Is("variation")]
            private class VariationAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.VariationContainer, E_Priority.Force);
            }
            [Is("ref-to")]
            private class RefToAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Ref, E_Priority.Force);
            }

            [Is("key")]
            private class KeyAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.IfNotSpecified);
                protected override (bool, E_Priority)? IsKey => (true, E_Priority.Force);
                protected override (bool, E_Priority)? IsRequired => (true, E_Priority.Force);
            }
            [Is("name")]
            private class NameAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.IfNotSpecified);
                protected override (bool, E_Priority)? IsName => (true, E_Priority.Force);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_WORD, E_Priority.IfNotSpecified);
            }

            [Is("id")]
            private class IdAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.Force);
                protected override (bool, E_Priority)? IsKey => (true, E_Priority.IfNotSpecified);
                protected override (bool, E_Priority)? IsRequired => (true, E_Priority.IfNotSpecified);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_ID, E_Priority.Force);
            }
            [Is("word")]
            private class WordAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.Force);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_WORD, E_Priority.Force);
            }
            [Is("sentence")]
            private class SentenceAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.Force);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_SENTENCE, E_Priority.Force);
            }
            #endregion 新しい属性があればここに追加
        }
        #endregion XML

        private string? _applicationName;
        private readonly List<AggregateDef> _aggregateDefs = new List<AggregateDef>();
        private readonly List<ChildDef> _childDefs = new List<ChildDef>();
        private readonly List<VariationDef> _variationDefs = new List<VariationDef>();
        private readonly List<ChildrenDef> _childrenDefs = new List<ChildrenDef>();
        private readonly List<ReferenceDef> _referencesDefs = new List<ReferenceDef>();

        internal AppSchemaBuilder SetApplicationName(string value) {
            _applicationName = value;
            return this;
        }
        internal AppSchemaBuilder AddAggregate(AggregateDef aggregateDef) {
            _aggregateDefs.Add(aggregateDef);
            return this;
        }
        internal AppSchemaBuilder AddChildAggregate(ChildDef childDef) {
            _childDefs.Add(childDef);
            return this;
        }
        internal AppSchemaBuilder AddChildrenAggregate(ChildrenDef childrenDef) {
            _childrenDefs.Add(childrenDef);
            return this;
        }
        internal AppSchemaBuilder AddVariationAggregate(VariationDef variationDef) {
            _variationDefs.Add(variationDef);
            return this;
        }
        internal AppSchemaBuilder AddReference(ReferenceDef referenceDef) {
            _referencesDefs.Add(referenceDef);
            return this;
        }

        internal bool TryBuild(out AppSchema appSchema, out ICollection<string> errors, MemberTypeResolver? memberTypeResolver = null) {

            var aggregateDefs = _aggregateDefs
                .Select(def => new {
                    FullPath = def.FullPath.Value,
                    def.Members,
                })
                .Concat(_childrenDefs.Select(def => new {
                    FullPath = $"{def.OwnerFullPath}/{def.Name}",
                    def.Members,
                }))
                .Concat(_childDefs.Select(def => new {
                    FullPath = $"{def.OwnerFullPath}/{def.Name}",
                    def.Members,
                }))
                .Concat(_variationDefs.Select(def => new {
                    FullPath = $"{def.OwnerFullPath}/{def.Name}",
                    def.Members,
                }));

            var relationDefs = _childrenDefs
                .Select(def => new {
                    Initial = def.OwnerFullPath,
                    Terminal = $"{def.OwnerFullPath}/{def.Name}",
                    RelationName = def.Name,
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD },
                        { DirectedEdgeExtensions.REL_ATTR_MULTIPLE, true },
                    },
                })
                .Concat(_childDefs.Select(def => new {
                    Initial = def.OwnerFullPath,
                    Terminal = $"{def.OwnerFullPath}/{def.Name}",
                    RelationName = def.Name,
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD },
                    },
                }))
                .Concat(_variationDefs.Select(def => new {
                    Initial = def.OwnerFullPath,
                    Terminal = $"{def.OwnerFullPath}/{def.Name}",
                    RelationName = def.Name,
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD },
                        { DirectedEdgeExtensions.REL_ATTR_VARIATIONSWITCH, def.VariationSwitch },
                        { DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, def.VariationContainer },
                        { DirectedEdgeExtensions.REL_ATTR_IS_PRIMARY, def.IsPrimary },
                        { DirectedEdgeExtensions.REL_ATTR_IS_INSTANCE_NAME, def.IsInstanceName },
                        { DirectedEdgeExtensions.REL_ATTR_IS_REQUIRED, !def.Optional },
                    },
                }))
                .Concat(_referencesDefs.Select(def => new {
                    Initial = def.OwnerFullPath,
                    Terminal = def.TargetFullPath,
                    RelationName = def.Name,
                    Attributes = new Dictionary<string, object> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_REFERENCE },
                        { DirectedEdgeExtensions.REL_ATTR_IS_PRIMARY, def.IsPrimary },
                        { DirectedEdgeExtensions.REL_ATTR_IS_INSTANCE_NAME, def.IsInstanceName },
                        { DirectedEdgeExtensions.REL_ATTR_IS_REQUIRED, def.IsRequired },
                    },
                }));

            // ---------------------------------------------------------
            // バリデーション

            errors = new HashSet<string>();
            memberTypeResolver ??= MemberTypeResolver.Default();

            if (string.IsNullOrWhiteSpace(_applicationName)) {
                errors.Add($"アプリケーション名が指定されていません。");
            }

            var aggregates = new Dictionary<AggregatePath, Aggregate>();
            var aggregateMembers = new HashSet<AggregateMemberNode>();
            var edgesFromAggToAgg = new List<GraphEdgeInfo>();
            var edgesFromAggToMember = new List<GraphEdgeInfo>();
            foreach (var aggregate in aggregateDefs) {
                var successToParse = true;

                // バリデーションおよびグラフ構成要素の作成: 集約ID
                if (!AggregatePath.TryParse(aggregate.FullPath, out var id, out var error)) {
                    errors.Add(error);
                    successToParse = false;
                } else if (aggregates.ContainsKey(id)) {
                    errors.Add($"ID '{id}' が重複しています。");
                    successToParse = false;
                }

                // バリデーションおよびグラフ構成要素の作成: 集約メンバー
                var aggregateId = new NodeId(id.Value);
                foreach (var member in aggregate.Members) {
                    if (!memberTypeResolver.TryResolve(member.Type, out var memberType)) {
                        errors.Add($"'{member.Name}' のタイプ '{member.Type}' が不正です。");
                        successToParse = false;
                        continue;
                    }
                    var memberId = new NodeId($"{id.Value}/{member.Name}");
                    aggregateMembers.Add(new AggregateMemberNode {
                        Id = memberId,
                        Name = member.Name,
                        Type = memberType,
                        IsPrimary = member.IsPrimary,
                        IsInstanceName = member.IsInstanceName,
                        Optional = member.Optional,
                    });
                    edgesFromAggToMember.Add(new GraphEdgeInfo {
                        Initial = aggregateId,
                        Terminal = memberId,
                        RelationName = member.Name,
                        Attributes = new Dictionary<string, object> {
                            { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_HAVING },
                        },
                    });
                }

                if (successToParse) {
                    aggregates.Add(id, new Aggregate(id));
                }
            }

            foreach (var relation in relationDefs) {
                var successToParse = true;

                // バリデーションおよびグラフ構成要素の作成: リレーションの集約ID
                if (!AggregatePath.TryParse(relation.Initial, out var initial, out var error1)) {
                    errors.Add(error1);
                    successToParse = false;
                } else if (!aggregates.ContainsKey(initial)) {
                    errors.Add($"ID '{relation.Initial}' と対応する定義がありません。");
                    successToParse = false;
                }
                if (!AggregatePath.TryParse(relation.Terminal, out var terminal, out var error2)) {
                    errors.Add(error2);
                    successToParse = false;
                } else if (!aggregates.ContainsKey(terminal)) {
                    errors.Add($"ID '{relation.Terminal}' と対応する定義がありません。");
                    successToParse = false;
                }

                if (successToParse) {
                    edgesFromAggToAgg.Add(new GraphEdgeInfo {
                        Initial = new NodeId(initial.Value),
                        Terminal = new NodeId(terminal.Value),
                        RelationName = relation.RelationName,
                        Attributes = relation.Attributes,
                    });
                }
            }

            // ---------------------------------------------------------
            // 基盤機能
            var halappEntities = new List<IGraphNode>();
            var halappEnums = new List<EnumDefinition>();

            // 基盤機能: バッチ処理
            halappEntities.Add(CodeRendering.BackgroundService.BackgroundTaskEntity.CreateEntity());
            halappEnums.Add(CodeRendering.BackgroundService.BackgroundTaskEntity.CreateBackgroundTaskStateEnum());

            // ---------------------------------------------------------
            // グラフを作成して返す
            var nodes = aggregates.Values
                .Cast<IGraphNode>()
                .Concat(aggregateMembers)
                .Concat(halappEntities);
            var edges = edgesFromAggToAgg
                .Concat(edgesFromAggToMember);
            if (!DirectedGraph.TryCreate(nodes, edges, out var graph, out var errors1)) {
                foreach (var err in errors1) errors.Add(err);
            }

            appSchema = errors.Any()
                ? AppSchema.Empty()
                : new AppSchema(_applicationName!, graph, halappEnums);
            return !errors.Any();
        }

        internal class AggregateDef {
            internal AggregatePath FullPath { get; set; } = AggregatePath.Empty;
            internal IList<SchalarMemberDef> Members { get; set; } = new List<SchalarMemberDef>();
        }
        internal class SchalarMemberDef {
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public bool IsPrimary { get; set; }
            public bool IsInstanceName { get; set; }
            public bool Optional { get; set; }
        }
        internal class ChildDef {
            public string Name { get; set; } = "";
            public string OwnerFullPath { get; set; } = "";
            internal IList<SchalarMemberDef> Members { get; set; } = new List<SchalarMemberDef>();
        }
        internal class VariationDef {
            public string Name { get; set; } = "";
            public string OwnerFullPath { get; set; } = "";
            internal IList<SchalarMemberDef> Members { get; set; } = new List<SchalarMemberDef>();
            public string VariationContainer { get; set; } = "";
            public string VariationSwitch { get; set; } = "";
            public bool IsPrimary { get; set; }
            public bool IsInstanceName { get; set; }
            public bool Optional { get; set; }
        }
        internal class ChildrenDef {
            public string Name { get; set; } = "";
            public string OwnerFullPath { get; set; } = "";
            internal IList<SchalarMemberDef> Members { get; set; } = new List<SchalarMemberDef>();
        }
        internal class ReferenceDef {
            public string Name { get; set; } = "";
            public string OwnerFullPath { get; set; } = "";
            public string TargetFullPath { get; set; } = "";
            public bool IsPrimary { get; set; }
            public bool IsInstanceName { get; set; }
            public bool IsRequired { get; set; }
        }
    }

    internal static class DirectedEdgeExtensions {
        internal const string REL_ATTR_RELATION_TYPE = "relationType";
        internal const string REL_ATTRVALUE_HAVING = "having";
        internal const string REL_ATTRVALUE_PARENT_CHILD = "child";
        internal const string REL_ATTRVALUE_REFERENCE = "reference";
        internal const string REL_ATTRVALUE_AGG_2_ETT = "aggregate-dbentity";
        internal const string REL_ATTRVALUE_AGG_2_INS = "aggregate-instance";

        internal const string REL_ATTR_MULTIPLE = "multiple";
        internal const string REL_ATTR_VARIATIONGROUPNAME = "variation-group-name";
        internal const string REL_ATTR_VARIATIONSWITCH = "switch";
        internal const string REL_ATTR_IS_PRIMARY = "is-primary";
        internal const string REL_ATTR_IS_INSTANCE_NAME = "is-instance-name";
        internal const string REL_ATTR_IS_REQUIRED = "is-required";

        // ----------------------------- GraphNode extensions -----------------------------

        internal static bool IsRoot(this GraphNode graphNode) {
            return !graphNode.In.Any(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                             && (string)type == REL_ATTRVALUE_PARENT_CHILD);
        }
        internal static GraphNode<T> GetRoot<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.EnumerateAncestorsAndThis().First();
        }

        internal static GraphEdge? GetParent(this GraphNode graphNode) {
            return graphNode.In.SingleOrDefault(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                                                     && (string)type == REL_ATTRVALUE_PARENT_CHILD);
        }
        internal static GraphEdge<T>? GetParent<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return ((GraphNode)graphNode).GetParent()?.As<T>();
        }

        /// <summary>
        /// 祖先を列挙する。ルート要素が先。
        /// </summary>
        internal static IEnumerable<GraphNode<T>> EnumerateAncestorsAndThis<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            foreach (var ancestor in graphNode.EnumerateAncestors()) {
                yield return ancestor.Initial;
            }
            yield return graphNode;
        }
        /// <summary>
        /// 祖先を列挙する。ルート要素が先。
        /// </summary>
        internal static IEnumerable<GraphEdge<T>> EnumerateAncestors<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            var stack = new Stack<GraphEdge<T>>();
            GraphEdge<T>? edge = graphNode.GetParent();
            while (edge != null) {
                stack.Push(edge);
                edge = edge.Initial.GetParent();
            }
            while (stack.Count > 0) {
                yield return stack.Pop();
            }
        }

        internal static IEnumerable<GraphNode<T>> EnumerateDescendants<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            static IEnumerable<GraphNode<T>> GetDescencantsRecursively(GraphNode<T> node) {
                var children = node.GetChildEdges()
                    .Concat(node.GetVariationGroups().SelectMany(group => group.VariationAggregates.Values))
                    .Concat(node.GetChildrenEdges());
                foreach (var edge in children) {
                    yield return edge.Terminal;
                    foreach (var descendant in GetDescencantsRecursively(edge.Terminal)) {
                        yield return descendant;
                    }
                }
            }

            foreach (var desc in GetDescencantsRecursively(graphNode)) {
                yield return desc;
            }
        }
        internal static IEnumerable<GraphNode<T>> EnumerateThisAndDescendants<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            yield return graphNode;
            foreach (var desc in graphNode.EnumerateDescendants()) {
                yield return desc;
            }
        }

        internal static bool IsChildrenMember(this GraphNode graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && parent.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && parent.Attributes.ContainsKey(REL_ATTR_MULTIPLE);
        }
        internal static bool IsChildMember(this GraphNode graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && parent.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && !parent.Attributes.ContainsKey(REL_ATTR_MULTIPLE)
                && !parent.Attributes.ContainsKey(REL_ATTR_VARIATIONGROUPNAME);
        }
        internal static bool IsVariationMember(this GraphNode graphNode) {
            var parent = graphNode.GetParent();
            return parent != null
                && parent.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_PARENT_CHILD
                && !parent.Attributes.ContainsKey(REL_ATTR_MULTIPLE)
                && parent.Attributes.ContainsKey(REL_ATTR_VARIATIONGROUPNAME);
        }

        internal static IEnumerable<GraphNode<AggregateMemberNode>> GetMemberNodes(this GraphNode<Aggregate> aggregate) {
            return aggregate.Out
                .Where(edge => (string)edge.Attributes[REL_ATTR_RELATION_TYPE] == REL_ATTRVALUE_HAVING)
                .Select(edge => edge.Terminal.As<AggregateMemberNode>());
        }
        internal static IEnumerable<GraphEdge<T>> GetChildrenEdges<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_PARENT_CHILD
                            && edge.Attributes.ContainsKey(REL_ATTR_MULTIPLE))
                .Select(edge => edge.As<T>());
        }
        internal static IEnumerable<GraphEdge<T>> GetChildEdges<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_PARENT_CHILD
                            && !edge.Attributes.ContainsKey(REL_ATTR_MULTIPLE)
                            && !edge.Attributes.ContainsKey(REL_ATTR_VARIATIONGROUPNAME))
                .Select(edge => edge.As<T>());
        }
        internal static IEnumerable<GraphEdge<T>> GetRefEdge<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_REFERENCE)
                .Select(edge => edge.As<T>());
        }
        internal static IEnumerable<GraphEdge<T>> GetReferedEdges<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.In
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_REFERENCE)
                .Select(edge => edge.As<T>());
        }

        internal static IEnumerable<VariationGroup<T>> GetVariationGroups<T>(this GraphNode<T> graphNode) where T : IGraphNode {
            return graphNode.Out
                .Where(edge => edge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                            && (string)type == REL_ATTRVALUE_PARENT_CHILD
                            && !edge.Attributes.ContainsKey(REL_ATTR_MULTIPLE)
                            && edge.Attributes.ContainsKey(REL_ATTR_VARIATIONGROUPNAME))
                .GroupBy(edge => (string)edge.Attributes[REL_ATTR_VARIATIONGROUPNAME])
                .Select(group => new VariationGroup<T> {
                    GroupName = group.Key,
                    VariationAggregates = group.ToDictionary(
                        edge => (string)edge.Attributes[REL_ATTR_VARIATIONSWITCH],
                        edge => edge.As<T>()),
                });
        }

        // ----------------------------- GraphEdge extensions -----------------------------

        internal static bool IsPrimary(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_PRIMARY, out var bln) && (bool)bln;
        }
        internal static bool IsInstanceName(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_INSTANCE_NAME, out var bln) && (bool)bln;
        }
        internal static bool IsRequired(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_REQUIRED, out var bln) && (bool)bln;
        }
        internal static bool IsRef(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type == REL_ATTRVALUE_REFERENCE;
        }
    }

    internal class VariationGroup<T> where T : IGraphNode {
        internal GraphNode<T> Owner => VariationAggregates.First().Value.Initial.As<T>();
        internal required string GroupName { get; init; }
        internal required IReadOnlyDictionary<string, GraphEdge<T>> VariationAggregates { get; init; }
        internal bool IsPrimary => VariationAggregates.First().Value.IsPrimary();
        internal bool IsInstanceName => VariationAggregates.First().Value.IsInstanceName();
        internal bool RequiredAtDB => VariationAggregates.First().Value.IsRequired();
    }
}
