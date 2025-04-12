using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.DataModelModules;
using Nijo.Util.DotnetEx;
using Nijo.ValueMemberTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Models.DataModelModules {
    /// <summary>
    /// データモデルの登録更新処理の引数
    /// </summary>
    internal class SaveCommand {

        internal SaveCommand(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string CsClassNameCreate => $"{_aggregate.PhysicalName}CreateCommand";
        internal string CsClassNameUpdate => $"{_aggregate.PhysicalName}UpdateCommand";
        internal string CsClassNameDelete => $"{_aggregate.PhysicalName}DeleteCommand";

        internal const string VERSION = "Version";
        internal const string TO_DBENTITY = "ToDbEntity";
        internal const string FROM_DBENTITY = "FromDbEntity";

        /// <summary>
        /// 新規・更新・削除のすべてのコマンドを、子孫要素の分も含めて再帰的にレンダリングします。
        /// </summary>
        internal string RenderAll(CodeRenderingContext ctx) {
            if (_aggregate is not RootAggregate) throw new InvalidOperationException();

            var descendants = _aggregate.EnumerateDescendants().Select(agg => new SaveCommand(agg));
            var tree = new[] { this }.Concat(descendants).ToArray();

            return $$"""
                #region 新規登録時引数
                {{tree.SelectTextTemplate(agg => $$"""
                {{agg.RenderCreateCommandDeclaring(ctx)}}
                """)}}
                #endregion 新規登録時引数


                #region 更新時引数
                {{tree.SelectTextTemplate(agg => $$"""
                {{agg.RenderUpdateCommandDeclaring(ctx)}}
                """)}}
                #endregion 更新時引数


                #region 物理削除時引数
                {{tree.SelectTextTemplate(agg => $$"""
                {{agg.RenderDeleteCommandDeclaring(ctx)}}
                """)}}
                #endregion 物理削除時引数
                """;
        }


        #region CREATE
        internal IEnumerable<ISaveCommandMember> GetCreateCommandMembers() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is ValueMember vm) {
                    // 新規登録時に自動採番されるものは新規登録メンバー中に含めない
                    if (vm.Type is SequenceMember) continue;

                    yield return new SaveCommandValueMember(vm);

                } else if (member is RefToMember rm) {
                    yield return new SaveCommandRefMember(rm);

                } else if (member is ChildAggreagte child) {
                    yield return new SaveCommandChildMember(child);

                } else if (member is ChildrenAggreagte children) {
                    yield return new SaveCommandChildrenMember(children);
                }
            }
        }
        private string RenderCreateCommandDeclaring(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の新規登録コマンド引数
                /// </summary>
                public partial class {{CsClassNameCreate}} {
                {{GetCreateCommandMembers().SelectTextTemplate(m => $$"""
                    /// <summary>{{m.DisplayName}}</summary>
                    public required {{m.CsCreateType}}? {{m.PhysicalName}} { get; set; }
                """)}}
                {{If(_aggregate is RootAggregate, () => $$"""

                    {{WithIndent(RenderToDbEntity(isCreate: true), "    ")}}
                """)}}
                }
                """;
        }
        #endregion CREATE


        #region UPDATE
        internal IEnumerable<ISaveCommandMember> GetUpdateCommandMembers() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is ValueMember vm) {
                    yield return new SaveCommandValueMember(vm);

                } else if (member is RefToMember rm) {
                    yield return new SaveCommandRefMember(rm);

                } else if (member is ChildAggreagte child) {
                    yield return new SaveCommandChildMember(child);

                } else if (member is ChildrenAggreagte children) {
                    yield return new SaveCommandChildrenMember(children);
                }
            }
        }
        private string RenderUpdateCommandDeclaring(CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の更新コマンド引数
                /// </summary>
                public partial class {{CsClassNameUpdate}} {
                {{GetUpdateCommandMembers().SelectTextTemplate(m => $$"""
                    /// <summary>{{m.DisplayName}}</summary>
                    public required {{m.CsUpdateType}}? {{m.PhysicalName}} { get; set; }
                """)}}
                {{If(_aggregate is RootAggregate, () => $$"""
                    /// <summary>楽観排他制御用のバージョン</summary>
                    public required int? {{VERSION}} { get; set; }

                    {{WithIndent(RenderToDbEntity(isCreate: false), "    ")}}

                    /// <summary>
                    /// Entity Framework Core のエンティティからこのクラスのインスタンスを作成します。
                    /// </summary>
                    public static {{CsClassNameUpdate}} {{FROM_DBENTITY}}({{efCoreEntity.CsClassName}} dbEntity) {
                        throw new NotImplementedException(); // TODO ver.1
                    }
                """)}}
                }
                """;
        }
        #endregion UPDATE


        #region DELETE
        private IEnumerable<ISaveCommandMember> GetDeleteCommandMembers() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is ValueMember vm) {
                    if (!vm.IsKey) continue;

                    yield return new SaveCommandValueMember(vm);

                } else if (member is RefToMember rm) {
                    if (!rm.IsKey) continue;

                    yield return new SaveCommandRefMember(rm);
                }
            }
        }
        private string RenderDeleteCommandDeclaring(CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} の物理削除コマンド引数。キーとバージョンのみを持つ。
                /// </summary>
                public partial class {{CsClassNameDelete}} {
                {{GetDeleteCommandMembers().SelectTextTemplate(m => $$"""
                    /// <summary>{{m.DisplayName}}</summary>
                    public required {{m.CsDeleteType}}? {{m.PhysicalName}} { get; set; }
                """)}}
                {{If(_aggregate is RootAggregate, () => $$"""
                    /// <summary>楽観排他制御用のバージョン</summary>
                    public required int? {{VERSION}} { get; set; }
                """)}}
                }
                """;
        }
        #endregion DELETE


        #region ValueMember再帰列挙
        /// <summary>
        /// ValueMemberのみの再帰列挙
        /// <list type="bullet">
        /// <item>親、参照先のValueMember: 含めます。</item>
        /// <item>Child, ChildrenのValueMember: 含めません。</item>
        /// </list>
        /// </summary>
        internal IEnumerable<SaveCommandValueMember> GetCreateCommandValueMembersRecursively() {
            foreach (var member in GetCreateCommandMembers()) {
                if (member is SaveCommandValueMember vm) {
                    yield return vm;

                } else if (member is KeyClass.IKeyClassStructure keyClass) {
                    foreach (var vm2 in keyClass.GetValueMembersRecursively()) {
                        yield return vm2;
                    }
                }
            }
        }
        /// <inheritdoc cref="GetCreateCommandValueMembersRecursively"/>
        internal IEnumerable<SaveCommandValueMember> GetUpdateCommandValueMembersRecursively() {
            foreach (var member in GetUpdateCommandMembers()) {
                if (member is SaveCommandValueMember vm) {
                    yield return vm;

                } else if (member is KeyClass.IKeyClassStructure keyClass) {
                    foreach (var vm2 in keyClass.GetValueMembersRecursively()) {
                        yield return vm2;
                    }
                }
            }
        }
        /// <inheritdoc cref="GetCreateCommandValueMembersRecursively"/>
        internal IEnumerable<SaveCommandValueMember> GetDeleteCommandValueMembersRecursively() {
            foreach (var member in GetDeleteCommandMembers()) {
                if (member is SaveCommandValueMember vm) {
                    yield return vm;

                } else if (member is KeyClass.IKeyClassStructure keyClass) {
                    foreach (var vm2 in keyClass.GetValueMembersRecursively()) {
                        yield return vm2;
                    }
                }
            }
        }
        #endregion ValueMember再帰列挙


        #region メンバー
        internal interface ISaveCommandMember {
            ISchemaPathNode Member { get; }
            string PhysicalName { get; }
            string DisplayName { get; }
            string CsCreateType { get; }
            string CsUpdateType { get; }
            string CsDeleteType { get; }
        }
        /// <summary>
        /// 更新処理引数クラスの値メンバー
        /// </summary>
        internal class SaveCommandValueMember : ISaveCommandMember {
            internal SaveCommandValueMember(ValueMember vm) {
                Member = vm;
            }
            internal ValueMember Member { get; }
            ISchemaPathNode ISaveCommandMember.Member => Member;

            public string PhysicalName => Member.PhysicalName;
            public string DisplayName => Member.DisplayName;
            public string CsCreateType => Member.Type.CsDomainTypeName;
            public string CsUpdateType => Member.Type.CsDomainTypeName;
            public string CsDeleteType => Member.Type.CsDomainTypeName;
        }
        /// <summary>
        /// 更新処理引数クラスの参照先キー項目
        /// </summary>
        internal class SaveCommandRefMember : KeyClass.KeyClassEntry {
            internal SaveCommandRefMember(RefToMember refTo) : base(refTo.RefTo) {
                Member = refTo;
            }
            internal RefToMember Member { get; }
        }
        /// <summary>
        /// 更新処理引数クラスの子メンバー
        /// </summary>
        internal class SaveCommandChildMember : SaveCommand, ISaveCommandMember {
            internal SaveCommandChildMember(ChildAggreagte child) : base(child) { }

            ISchemaPathNode ISaveCommandMember.Member => (IAggregateMember)_aggregate;
            public string PhysicalName => _aggregate.PhysicalName;
            public string DisplayName => _aggregate.DisplayName;
            public string CsCreateType => new SaveCommand(_aggregate).CsClassNameCreate;
            public string CsUpdateType => new SaveCommand(_aggregate).CsClassNameUpdate;
            public string CsDeleteType => new SaveCommand(_aggregate).CsClassNameDelete;
        }
        /// <summary>
        /// 更新処理引数クラスの子コレクションメンバー
        /// </summary>
        internal class SaveCommandChildrenMember : SaveCommand, ISaveCommandMember {
            internal SaveCommandChildrenMember(ChildrenAggreagte children) : base(children) { }

            ISchemaPathNode ISaveCommandMember.Member => (IAggregateMember)_aggregate;
            public string PhysicalName => _aggregate.PhysicalName;
            public string DisplayName => _aggregate.DisplayName;
            public string CsCreateType => $"List<{new SaveCommand(_aggregate).CsClassNameCreate}>";
            public string CsUpdateType => $"List<{new SaveCommand(_aggregate).CsClassNameUpdate}>";
            public string CsDeleteType => $"List<{new SaveCommand(_aggregate).CsClassNameDelete}>";
        }
        #endregion メンバー


        private string RenderToDbEntity(bool isCreate) {
            var efCoreEntity = new EFCoreEntity(_aggregate);

            return $$"""
                /// <summary>
                /// このインスタンスを Entity Framework Core のエンティティに変換します。
                /// </summary>
                public {{efCoreEntity.CsClassName}} {{TO_DBENTITY}}() {
                    return new {{efCoreEntity.CsClassName}} {
                        {{WithIndent(RenderToDbEntityBody(efCoreEntity, this, "this", new Dictionary<XElement, string>()), "        ")}}
                    };
                }
                """;

            IEnumerable<string> RenderToDbEntityBody(
                EFCoreEntity entity,
                SaveCommand source,
                string rightInstanceName,
                IReadOnlyDictionary<XElement, string> ancestorsKeys) {

                // 右辺
                var rightMembers = new Dictionary<XElement, string>();
                var ancestorsAndThisKeys = new Dictionary<XElement, string>(ancestorsKeys);

                var rightSourceMembers = isCreate
                    ? source.GetCreateCommandMembers()
                    : source.GetUpdateCommandMembers();
                foreach (var member in rightSourceMembers) {
                    var path = member.Member
                        .GetPathFromEntry()
                        .SinceNearestChildren()
                        .AsSaveCommand();
                    var joined = $"{(member.Member is ValueMember vm3 ? vm3.Type.RenderCastToPrimitiveType() : "")}{rightInstanceName}.{path.Join("?.")}";

                    // 右辺の候補に追加
                    rightMembers.Add(member.Member.XElement, joined);

                    // キー項目の場合は子孫のレンダリングのために祖先メンバーリストにも追加
                    if (member is SaveCommandValueMember vm && vm.Member.IsKey) {
                        if (ancestorsAndThisKeys.ContainsKey(member.Member.XElement)) continue;

                        ancestorsAndThisKeys.Add(member.Member.XElement, joined);

                    } else if (member is SaveCommandRefMember rm && rm.Member.IsKey) {
                        foreach (var vm2 in rm.GetValueMembersRecursively()) {
                            if (ancestorsAndThisKeys.ContainsKey(vm2.Member.XElement)) continue;

                            var path2 = vm2.Member
                                .GetPathFromEntry()
                                .SinceNearestChildren()
                                .AsSaveCommand();
                            var joined2 = $"{vm2.Member.Type.RenderCastToPrimitiveType()}{rightInstanceName}.{path2.Join("?.")}";
                            ancestorsAndThisKeys.Add(vm2.Member.XElement, joined2);
                        }
                    }
                }

                // 自身のカラム、外部参照のキー、親のキー
                foreach (var col in entity.GetColumns()) {
                    if (col is EFCoreEntity.OwnColumnMember ownCol) {
                        // シーケンス項目など登録と共に採番されるものはnullになる可能性がある
                        var right = rightMembers.GetValueOrDefault(ownCol.Member.XElement);
                        yield return $$"""
                            {{col.PhysicalName}} = {{right ?? "null"}},
                            """;

                    } else if (col is EFCoreEntity.ParentKeyMember parentKeyCol) {
                        // 親のキーメンバーの場合。
                        // シーケンス項目など登録と共に採番されるものはnullになる可能性がある
                        var right = ancestorsKeys.GetValueOrDefault(parentKeyCol.Member.XElement);
                        yield return $$"""
                            {{col.PhysicalName}} = {{right ?? "null"}},
                            """;

                    } else if (col is EFCoreEntity.RefKeyMember refKeyCol) {
                        // 参照先のキーメンバーの場合
                        var right = ancestorsKeys.GetValueOrDefault(refKeyCol.Member.XElement)
                            ?? throw new InvalidOperationException($"{refKeyCol.RefEntry}の右辺が見つからない。XElement: {refKeyCol.Member.XElement}");

                        yield return $$"""
                            {{col.PhysicalName}} = {{right}},
                            """;
                    }
                }

                // 子
                var childAndChildren = entity
                    .GetNavigationProperties()
                    .OfType<EFCoreEntity.NavigationOfParentChild>()
                    .Where(nav => nav.Principal.ThisSide == entity.Aggregate);

                foreach (var nav in childAndChildren) {
                    if (nav.Relevant.ThisSide is ChildAggreagte child) {
                        // Child
                        var childEntity = new EFCoreEntity(nav.Relevant.ThisSide);
                        var childSaveCommand = new SaveCommandChildMember(child);

                        yield return $$"""
                            {{nav.Principal.OtherSidePhysicalName}} = new() {
                                {{WithIndent(RenderToDbEntityBody(childEntity, childSaveCommand, rightInstanceName, ancestorsAndThisKeys), "    ")}}
                            },
                            """;

                    } else if (nav.Relevant.ThisSide is ChildrenAggreagte children) {
                        var childrenEntity = new EFCoreEntity(nav.Relevant.ThisSide);
                        var childrenSaveCommand = new SaveCommandChildrenMember(children);
                        var x = children.GetLoopVarName();
                        var arrayPath = children
                            .GetPathFromEntry()
                            .SinceNearestChildren()
                            .AsSaveCommand();

                        yield return $$"""
                            {{nav.Principal.OtherSidePhysicalName}} = {{rightInstanceName}}.{{arrayPath.Join("?.")}}?.Select({{x}} => new {{childrenEntity.CsClassName}} {
                                {{WithIndent(RenderToDbEntityBody(childrenEntity, childrenSaveCommand, x, ancestorsAndThisKeys), "    ")}}
                            }).ToHashSet() ?? [],
                            """;

                    } else {
                        throw new NotImplementedException();
                    }
                }

                // バージョン
                if (entity.Aggregate is RootAggregate) {
                    yield return $$"""
                        {{VERSION}} = 0,
                        """;
                }
            }
        }
    }
}

namespace Nijo.CodeGenerating {
    partial class SchemaPathNodeExtensions {

        /// <summary>
        /// <see cref="GetPathFromEntry(ISchemaPathNode)"/> の結果を <see cref="SaveCommand"/> のルールに沿ったパスとして返す
        /// </summary>
        public static IEnumerable<string> AsSaveCommand(this IEnumerable<ISchemaPathNode> path) {
            var isOutOfEntryTree = false;

            foreach (var node in path) {
                if (node.PreviousNode == null) continue; // パスの一番最初（エントリー）はスキップ
                if (node.PreviousNode is RefToMember) continue; // refの1つ次の要素の名前はrefで列挙済みのためスキップ

                // 外部参照のプロパティを辿るパス
                if (node is RefToMember refTo) {
                    var previous = (AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("reftoの前は必ず参照元集約か参照先集約になるのでありえない");

                    // 参照元から参照先へ辿るパス
                    if (previous == refTo.Owner) {
                        if (!isOutOfEntryTree) {
                            // エントリーの集約内部から外に出る瞬間の場合
                            var member = new SaveCommand.SaveCommandRefMember(refTo);
                            yield return member.PhysicalName;

                            isOutOfEntryTree = true;
                            continue;

                        } else {
                            // 参照先のキーの中でさらに他の集約への参照が発生した場合
                            var key = new KeyClass.KeyClassRefMember(refTo);
                            yield return key.PhysicalName;
                            continue;
                        }
                    }
                    // 参照先から参照元へ辿るパス
                    if (previous == refTo.RefTo) {
                        throw new InvalidOperationException("更新処理引数クラスでは参照先から参照元へ辿ることはできない");
                    }
                    throw new InvalidOperationException("reftoの前は必ず参照元集約か参照先集約になるのでありえない");
                }

                // 親子間のナビゲーションプロパティを辿るパス
                if (node is AggregateBase curr && node.PreviousNode is AggregateBase prev) {

                    // 子から親へ辿るパス
                    if (curr.IsParentOf(prev)) {

                        // エントリーの集約内部では子から親へ辿るパターンは無い
                        if (!isOutOfEntryTree) throw new InvalidOperationException("エントリーの集約内部では子から親へ辿るパターンは無い");

                        var parentMember = new KeyClass.KeyClassParentMember(curr);
                        yield return parentMember.PhysicalName;
                        continue;
                    }
                    // 親から子へ辿るパス
                    if (curr.IsChildOf(prev)) {

                        // 参照先のキーの中では親から子へ辿るパターンは無い
                        if (isOutOfEntryTree) throw new InvalidOperationException("参照先のキーの中では親から子へ辿るパターンは無い");

                        var childMemberPhysicalName = curr switch {
                            ChildAggreagte child => new SaveCommand.SaveCommandChildMember(child).PhysicalName,
                            ChildrenAggreagte children => new SaveCommand.SaveCommandChildrenMember(children).PhysicalName,
                            _ => throw new InvalidOperationException("ありえない"),
                        };
                        yield return childMemberPhysicalName;
                        continue;
                    }
                    throw new InvalidOperationException("必ず 親→子, 子→親 のどちらかになるのでありえない");
                }

                // 末端のメンバー
                if (node is not ValueMember vm) throw new InvalidOperationException("この分岐まで来るケースは値の場合しか無いのでありえない");

                var valueMember = new SaveCommand.SaveCommandValueMember(vm);
                yield return valueMember.PhysicalName;
            }
        }

    }
}
