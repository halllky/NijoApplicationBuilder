using Newtonsoft.Json.Linq;
using Nijo.Core.AggregateMemberTypes;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Core {

    public sealed class AppSchemaBuilder {

        internal AppSchemaBuilder() {
        }

        private string? _applicationName;
        private readonly Dictionary<TreePath, AggregateBuildOption> _aggregates = new();
        private readonly Dictionary<TreePath, AggregateMemberBuildOption> _aggregateMembers = new();
        private readonly Dictionary<string, IEnumerable<EnumValueOption>> _enums = new();

        // メンバーの順番をAddされた順番にするための仕組み
        private int _addedOrderCount = 0;
        private readonly Dictionary<TreePath, int> _addedOrder = new();

        public AppSchemaBuilder SetApplicationName(string value) {
            _applicationName = value;
            return this;
        }
        public AppSchemaBuilder AddAggregate(IEnumerable<string> path, AggregateBuildOption? options = null) {
            var treePath = new TreePath(path.ToArray());
            _aggregates[treePath] = options ?? new();

            if (!_addedOrder.ContainsKey(treePath)) {
                _addedOrder[treePath] = _addedOrderCount;
                _addedOrderCount++;
            }
            return this;
        }
        public AppSchemaBuilder AddAggregateMember(IEnumerable<string> path, AggregateMemberBuildOption? options = null) {
            var treePath = new TreePath(path.ToArray());
            _aggregateMembers[treePath] = options ?? new();

            if (!_addedOrder.ContainsKey(treePath)) {
                _addedOrder[treePath] = _addedOrderCount;
                _addedOrderCount++;
            }
            return this;
        }
        public AppSchemaBuilder AddEnum(string name, IEnumerable<EnumValueOption> values) {
            _enums[name] = values;
            return this;
        }

        internal bool TryBuild(out AppSchema appSchema, out ICollection<string> errors, MemberTypeResolver? memberTypeResolver = null) {

            var aggregateDefs = _aggregates
                .Select(aggregate => new {
                    TreePath = aggregate.Key,
                    Options = aggregate.Value,
                    Members = _aggregateMembers
                        .Where(member => member.Key.Parent == aggregate.Key)
                        .Select(member => new {
                            TreePath = member.Key,
                            Name = member.Key.BaseName,
                            Options = member.Value,
                            Order = _addedOrder[member.Key],
                        })
                        .ToArray(),
                    Order = _addedOrder[aggregate.Key],
                })
                .ToArray();

            var parentAndChild = aggregateDefs
                .Where(aggregate => !aggregate.TreePath.IsRoot)
                .Select(aggregate => new {
                    Initial = aggregate.TreePath.Parent,
                    Terminal = aggregate.TreePath,
                    RelationName = aggregate.TreePath.BaseName,
                    Attributes = new Dictionary<string, object?> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD },
                        { DirectedEdgeExtensions.REL_ATTR_MULTIPLE, aggregate.Options.IsArray == true },
                        { DirectedEdgeExtensions.REL_ATTR_VARIATIONSWITCH, aggregate.Options.IsVariationGroupMember?.Key ?? string.Empty },
                        { DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, aggregate.Options.IsVariationGroupMember?.GroupName ?? string.Empty },
                        { DirectedEdgeExtensions.REL_ATTR_IS_PRIMARY, aggregate.Options.IsPrimary == true },
                        { DirectedEdgeExtensions.REL_ATTR_INVISIBLE_IN_GUI, aggregate.Options.InvisibleInGui == true },
                        { DirectedEdgeExtensions.REL_ATTR_MEMBER_ORDER, aggregate.Order },
                    },
                });
            var refs = aggregateDefs
                .SelectMany(aggregate => aggregate.Members, (aggregate, member) => new { aggregate, member })
                .Where(x => x.member.Options.IsReferenceTo != null)
                .Select(x => new {
                    Initial = x.aggregate.TreePath,
                    Terminal = TreePath.FromString(x.member.Options.IsReferenceTo!),
                    RelationName = x.member.Name,
                    Attributes = new Dictionary<string, object?> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_REFERENCE },
                        { DirectedEdgeExtensions.REL_ATTR_IS_PRIMARY, x.member.Options.IsPrimary == true },
                        { DirectedEdgeExtensions.REL_ATTR_IS_INSTANCE_NAME, x.member.Options.IsDisplayName == true },
                        { DirectedEdgeExtensions.REL_ATTR_IS_NAME_LIKE, x.member.Options.IsNameLike == true },
                        { DirectedEdgeExtensions.REL_ATTR_IS_REQUIRED, x.member.Options.IsRequired == true },
                        { DirectedEdgeExtensions.REL_ATTR_INVISIBLE_IN_GUI, x.member.Options.InvisibleInGui == true },
                        { DirectedEdgeExtensions.REL_ATTR_SINGLEVIEW_CUSTOM_UI_COMPONENT_NAME, x.member.Options.SingleViewCustomUiComponentName },
                        { DirectedEdgeExtensions.REL_ATTR_SEARCHCONDITION_CUSTOM_UI_COMPONENT_NAME, x.member.Options.SearchConditionCustomUiComponentName },
                        { DirectedEdgeExtensions.REL_ATTR_MEMBER_ORDER, x.member.Order },
                    },
                });
            var readModelDependency = aggregateDefs
                .Where(aggregate => aggregate.Options.Handler == NijoCodeGenerator.Models.ReadModel.Key)
                .SelectMany(
                    aggregate => aggregate.Options.DependsOn,
                    (aggregate, dependent) => new { aggregate, dependent })
                .Select(x => new {
                    Initial = x.aggregate.TreePath,
                    Terminal = TreePath.FromString(x.dependent),
                    RelationName = "Depends On",
                    Attributes = new Dictionary<string, object?> {
                        { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_DEPENDSON },
                    },
                });

            var relationDefs = parentAndChild
                .Concat(refs)
                .Concat(readModelDependency);

            // ---------------------------------------------------------
            // バリデーションおよびドメインクラスへの変換

            errors = new HashSet<string>();
            memberTypeResolver ??= MemberTypeResolver.Default();

            if (string.IsNullOrWhiteSpace(_applicationName)) {
                errors.Add($"アプリケーション名が指定されていません。");
            }

            // enumの組み立て（スキーマ定義で指定されているenum）
            var builtEnums = new List<EnumDefinition>();
            foreach (var @enum in _enums) {
                var items = new List<EnumDefinition.Item>();
                var unusedInt = 0;
                var usedInt = @enum.Value
                    .Where(v => v.Value.HasValue)
                    .Select(v => v.Value)
                    .Cast<int>()
                    .ToHashSet();
                foreach (var item in @enum.Value) {
                    if (item.Value.HasValue) {
                        items.Add(new EnumDefinition.Item {
                            PhysicalName = item.Name,
                            Value = item.Value.Value,
                        });
                    } else {
                        // 値が未指定の場合、自動的に使われていない整数値を使う
                        while (usedInt.Contains(unusedInt)) unusedInt++;
                        usedInt.Add(unusedInt);
                        items.Add(new EnumDefinition.Item {
                            PhysicalName = item.Name,
                            Value = unusedInt,
                        });
                    }
                }

                if (EnumDefinition.TryCreate(@enum.Key, items, out var created, out var enumCreateErrors)) {
                    builtEnums.Add(created);
                    memberTypeResolver.Register(created.Name, new EnumList(created));
                } else {
                    foreach (var err in enumCreateErrors) errors.Add(err);
                }
            }

            // enumの組み立て（Variation）
            var variationGroups = relationDefs
                .GroupBy(relation => relation.Attributes.TryGetValue(DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, out var groupName)
                    ? (string)groupName!
                    : string.Empty)
                .Where(group => group.Key != string.Empty);
            foreach (var variationGroup in variationGroups) {
                var enumValues = new List<EnumDefinition.Item>();
                foreach (var relation in variationGroup) {
                    var strValue = (string)relation.Attributes[DirectedEdgeExtensions.REL_ATTR_VARIATIONSWITCH]!;
                    if (!int.TryParse(strValue, out var intValue)) {
                        errors.Add($"Variationのキー '{strValue}' が整数ではありません。");
                        continue;
                    }
                    enumValues.Add(new EnumDefinition.Item {
                        Value = intValue,
                        PhysicalName = relation.RelationName,
                        DisplayName = relation.RelationName,
                    });
                }

                if (!EnumDefinition.TryCreate($"E_{variationGroup.Key}", enumValues, out var enumDef, out errors)) {
                    continue;
                }
                builtEnums.Add(enumDef);
            }

            // GraphNodeの組み立て
            var aggregates = new Dictionary<NodeId, Aggregate>();
            var aggregateMembers = new HashSet<AggregateMemberNode>();
            var edgesFromAggToAgg = new List<GraphEdgeInfo>();
            var edgesFromAggToMember = new List<GraphEdgeInfo>();
            foreach (var aggregateDef in aggregateDefs) {
                var successToParse = true;

                // バリデーションおよびグラフ構成要素の作成: 集約ID
                var aggregateId = aggregateDef.TreePath.ToGraphNodeId();
                if (aggregates.ContainsKey(aggregateId)) {
                    errors.Add($"ID '{aggregateDef.TreePath}' が重複しています。");
                    successToParse = false;
                }

                // バリデーションおよびグラフ構成要素の作成: 集約メンバー
                var hasNameMember = false;
                foreach (var member in aggregateDef.Members) {
                    if (member.Options.IsDisplayName == true) hasNameMember = true;

                    // refはリレーションの方で作成する
                    if (member.Options.IsReferenceTo != null) continue;

                    if (member.Options.MemberType == null) {
                        errors.Add($"'{member.Name}' のタイプが指定されていません。");
                        successToParse = false;
                        continue;
                    }
                    if (!memberTypeResolver.TryResolve(member.Options.MemberType, out var memberType)) {
                        errors.Add($"'{member.Name}' のタイプ '{member.Options.MemberType}' が不正です。");
                        successToParse = false;
                        continue;
                    }
                    var memberId = member.TreePath.ToGraphNodeId();
                    aggregateMembers.Add(new AggregateMemberNode {
                        Id = memberId,
                        MemberName = member.Name,
                        MemberType = memberType,
                        IsKey = member.Options.IsPrimary == true,
                        IsDisplayName = member.Options.IsDisplayName == true,
                        IsNameLike = member.Options.IsNameLike == true,
                        IsRequired = member.Options.IsRequired == true,
                        InvisibleInGui = member.Options.InvisibleInGui == true,
                        SingleViewCustomUiComponentName = member.Options.SingleViewCustomUiComponentName,
                        SearchConditionCustomUiComponentName = member.Options.SearchConditionCustomUiComponentName,
                        UiWidth = member.Options.UiWidthRem,
                    });
                    edgesFromAggToMember.Add(new GraphEdgeInfo {
                        Initial = aggregateId,
                        Terminal = memberId,
                        RelationName = member.Name,
                        Attributes = new Dictionary<string, object?> {
                            { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_HAVING },
                            { DirectedEdgeExtensions.REL_ATTR_MEMBER_ORDER, member.Order },
                        },
                    });
                }

                if (successToParse) {
                    var displayName = aggregateDef.TreePath.BaseName;
                    var aggregate = new Aggregate(aggregateId, displayName, !hasNameMember, aggregateDef.Options);
                    aggregates.Add(aggregateId, aggregate);
                }
            }

            // GraphEdgeの組み立て
            foreach (var relation in relationDefs) {
                var successToParse = true;
                var initial = relation.Initial.ToGraphNodeId();
                var terminal = relation.Terminal.ToGraphNodeId();

                // バリデーションおよびグラフ構成要素の作成: リレーションの集約ID
                if (!aggregates.ContainsKey(initial)) {
                    errors.Add($"ID '{relation.Initial}' と対応する定義がありません。");
                    successToParse = false;
                }
                if (!aggregates.ContainsKey(terminal)) {
                    errors.Add($"ID '{relation.Terminal}' と対応する定義がありません。");
                    successToParse = false;
                }

                if (successToParse) {
                    edgesFromAggToAgg.Add(new GraphEdgeInfo {
                        Initial = initial,
                        Terminal = terminal,
                        RelationName = relation.RelationName,
                        Attributes = relation.Attributes,
                    });
                }
            }

            // ---------------------------------------------------------
            // グラフを作成して返す
            var nodes = aggregates.Values
                .Cast<IGraphNode>()
                .Concat(aggregateMembers);
            var edges = edgesFromAggToAgg
                .Concat(edgesFromAggToMember);
            if (!DirectedGraph.TryCreate(nodes, edges, out var graph, out var errors1)) {
                foreach (var err in errors1) errors.Add(err);
            }
            var enums = builtEnums
                .ToArray();

            appSchema = errors.Any()
                ? AppSchema.Empty()
                : new AppSchema(_applicationName!, graph, enums);
            return !errors.Any();
        }
    }

    // TODO: それぞれのオプションの説明を書く
    public sealed class AggregateBuildOption {
        public bool? IsPrimary { get; set; }
        public bool? IsArray { get; set; }
        public GroupOption? IsVariationGroupMember { get; set; }
        public bool? InvisibleInGui { get; set; }
        public string? Handler { get; set; }
        public List<string> DependsOn { get; } = new();
        /// <summary>
        /// <see cref="Models.ReadModel2"/> において、この集約の登録・更新・削除のタイミングが親集約と別々かどうかを表す。
        /// 別々な場合、この集約のオブジェクトを画面上で追加したり削除したりすることができたり、
        /// 親の内容が画面上で変更されていてもこの集約に変更がなければ更新がかからなかったりする。
        /// </summary>
        public bool HasLifeCycle { get; set; }
        /// <summary>
        /// 画面から編集できる集約かそうでないかを表します。
        /// </summary>
        public bool IsReadOnlyAggregate { get; set; }
        /// <summary>
        /// この集約が <see cref="Models.WriteModel2"/> の場合に
        /// 既定の <see cref="Models.ReadModel2"/> を生成するかどうか。
        /// DBのデータ型と画面のデータ型が完全一致する場の使用を想定している。
        /// </summary>
        public bool GenerateDefaultReadModel { get; set; }

        /// <summary>
        /// <see cref="Models.CommandModel"/> 用のステップ属性。その入力項目がウィザード形式の入力画面の何番目かを表す。
        /// ルート集約の直下の <see cref="AggregateMember.Child"/> でのみ指定可能。
        /// ステップありなしは混在不可能（ステップをつけるなら全てのChildをステップにする必要がある）。
        /// </summary>
        public int? Step { get; set; }

        public sealed class GroupOption {
            public required string GroupName { get; init; }
            public required string Key { get; init; }
        }
    }
    // TODO: それぞれのオプションの説明を書く
    public sealed class AggregateMemberBuildOption {
        public string? MemberType { get; set; }
        public bool? IsPrimary { get; set; }
        public bool? IsDisplayName { get; set; }
        public bool? IsNameLike { get; set; }
        public bool? IsRequired { get; set; }
        public string? IsReferenceTo { get; set; }
        public bool? InvisibleInGui { get; set; }
        /// <summary>生成後のソースで外から注入して、中で React context 経由で参照する詳細画面用コンポーネント。ValueMemberまたはRefでのみ使用</summary>
        public string? SingleViewCustomUiComponentName { get; set; }
        /// <summary>生成後のソースで外から注入して、中で React context 経由で参照する詳細画面用コンポーネント。ValueMemberまたはRefでのみ使用</summary>
        public string? SearchConditionCustomUiComponentName { get; set; }
        /// <summary>テキストボックスの横幅。文字列型と数値型のValueMemberでのみ有効</summary>
        public TextBoxWidth? UiWidthRem { get; set; }
    }
    public sealed class EnumValueOption {
        public string Name { get; set; } = string.Empty;
        public int? Value { get; set; }
    }
    public sealed class TextBoxWidth {
        public required E_ZenHan ZenHan { get; init; }
        public required int CharCount { get; set; }

        public string GetCssValue() {
            // 確認に用いたフォントは font-family:"Cascadia Mono", "BIZ UDGothic"。
            // 0.5m は テキストボックス内のパディング。
            var width = ZenHan == E_ZenHan.Zenkaku
                ? 0.5m + (CharCount * 1.050m)
                : 0.5m + (CharCount * 0.615m);

            // 小数第三位以下は誤差として小数第二位で切り上げる
            var ceiled = Math.Ceiling(width * 100) / 100;

            return $"{ceiled}rem";
        }

        public enum E_ZenHan {
            Zenkaku,
            Hankaku,
        }
    }


    internal static class DirectedEdgeExtensions {
        internal const string REL_ATTR_RELATION_TYPE = "relationType";
        internal const string REL_ATTRVALUE_HAVING = "having";
        internal const string REL_ATTRVALUE_PARENT_CHILD = "child";
        internal const string REL_ATTRVALUE_REFERENCE = "reference";
        internal const string REL_ATTRVALUE_DEPENDSON = "depends-on";

        internal const string REL_ATTR_MULTIPLE = "multiple";
        internal const string REL_ATTR_VARIATIONGROUPNAME = "variation-group-name";
        internal const string REL_ATTR_VARIATIONSWITCH = "switch";
        internal const string REL_ATTR_IS_PRIMARY = "is-primary";
        internal const string REL_ATTR_IS_INSTANCE_NAME = "is-instance-name";
        internal const string REL_ATTR_IS_NAME_LIKE = "is-name-like";
        internal const string REL_ATTR_IS_REQUIRED = "is-required";
        internal const string REL_ATTR_INVISIBLE_IN_GUI = "invisible-in-gui";
        internal const string REL_ATTR_SINGLEVIEW_CUSTOM_UI_COMPONENT_NAME = "singleview-custom-ui-component-name";
        internal const string REL_ATTR_SEARCHCONDITION_CUSTOM_UI_COMPONENT_NAME = "searchcondition-custom-ui-component-name";
        internal const string REL_ATTR_MEMBER_ORDER = "relation-aggregate-order";


        // ----------------------------- GraphEdge extensions -----------------------------

        internal static bool IsPrimary(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_PRIMARY, out var bln) && (bool)bln!;
        }
        internal static bool IsInstanceName(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_INSTANCE_NAME, out var bln) && (bool)bln!;
        }
        internal static bool IsNameLike(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_NAME_LIKE, out var bln) && (bool)bln!;
        }
        internal static bool IsRequired(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_IS_REQUIRED, out var bln) && (bool)bln!;
        }
        internal static bool InvisibleInGui(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_INVISIBLE_IN_GUI, out var bln) && (bool)bln!;
        }
        internal static bool IsRef(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type! == REL_ATTRVALUE_REFERENCE;
        }
        internal static bool IsParentChild(this GraphEdge graphEdge) {
            return graphEdge.Attributes.TryGetValue(REL_ATTR_RELATION_TYPE, out var type)
                && (string)type! == REL_ATTRVALUE_PARENT_CHILD;
        }

        internal static int GetMemberOrder(this GraphEdge graphEdge) {
            return (int)graphEdge.Attributes[REL_ATTR_MEMBER_ORDER]!;
        }
    }

    internal class VariationGroup<T> where T : IGraphNode {
        internal GraphNode<T> Owner => VariationAggregates.First().Value.Initial.As<T>();
        internal required string GroupName { get; init; }
        internal required IReadOnlyDictionary<string, GraphEdge<T>> VariationAggregates { get; init; }
        internal required int MemberOrder { get; init; }
        internal bool IsPrimary => VariationAggregates.First().Value.IsPrimary();
        internal bool IsInstanceName => VariationAggregates.First().Value.IsInstanceName();
        internal bool IsNameLike => VariationAggregates.First().Value.IsNameLike();
        internal bool RequiredAtDB => VariationAggregates.First().Value.IsRequired();
        internal bool InvisibleInGui => VariationAggregates.First().Value.InvisibleInGui();

        internal string CsEnumType => $"E_{GroupName.ToCSharpSafe()}";
    }
}
