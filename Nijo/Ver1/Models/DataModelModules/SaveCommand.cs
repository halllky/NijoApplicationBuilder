using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.ValueMemberTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.DataModelModules {
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
                    yield return new SaveCommandDescendantMember(child);

                } else if (member is ChildrenAggreagte children) {
                    yield return new SaveCommandDescendantMember(children);
                }
            }
        }
        private string RenderCreateCommandDeclaring(CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(_aggregate);

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

                    /// <summary>
                    /// このインスタンスを Entity Framework Core のエンティティに変換します。
                    /// </summary>
                    public {{efCoreEntity.CsClassName}} {{TO_DBENTITY}}() {
                        throw new NotImplementedException(); // TODO ver.1
                    }
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
                    yield return new SaveCommandDescendantMember(child);

                } else if (member is ChildrenAggreagte children) {
                    yield return new SaveCommandDescendantMember(children);
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

                    /// <summary>
                    /// このインスタンスを Entity Framework Core のエンティティに変換します。
                    /// </summary>
                    public {{efCoreEntity.CsClassName}} {{TO_DBENTITY}}() {
                        throw new NotImplementedException(); // TODO ver.1
                    }

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


        #region メンバー
        internal interface ISaveCommandMember {
            IAggregateMember Member { get; }
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
            IAggregateMember ISaveCommandMember.Member => Member;

            public string PhysicalName => Member.PhysicalName;
            public string DisplayName => Member.DisplayName;
            public string CsCreateType => Member.Type.CsDomainTypeName;
            public string CsUpdateType => Member.Type.CsDomainTypeName;
            public string CsDeleteType => Member.Type.CsDomainTypeName;
        }
        /// <summary>
        /// 更新処理引数クラスの参照先キー項目
        /// </summary>
        internal class SaveCommandRefMember : ISaveCommandMember {
            internal SaveCommandRefMember(RefToMember refTo) {
                Member = refTo;
                _refToKey = new KeyClass.KeyClassEntry(refTo.RefTo);
            }
            internal RefToMember Member { get; }
            IAggregateMember ISaveCommandMember.Member => Member;
            private readonly KeyClass.KeyClassEntry _refToKey;

            public string PhysicalName => Member.PhysicalName;
            public string DisplayName => Member.DisplayName;
            public string CsCreateType => _refToKey.ClassName;
            public string CsUpdateType => _refToKey.ClassName;
            public string CsDeleteType => _refToKey.ClassName;
        }
        /// <summary>
        /// 更新処理引数クラスの子孫メンバー
        /// </summary>
        internal class SaveCommandDescendantMember : SaveCommand, ISaveCommandMember {
            internal SaveCommandDescendantMember(ChildAggreagte child) : base(child) { }
            internal SaveCommandDescendantMember(ChildrenAggreagte children) : base(children) { }

            IAggregateMember ISaveCommandMember.Member => (IAggregateMember)_aggregate;
            public string PhysicalName => _aggregate.PhysicalName;
            public string DisplayName => _aggregate.DisplayName;
            public string CsCreateType => _aggregate is ChildrenAggreagte ? $"List<{new SaveCommand(_aggregate).CsClassNameCreate}>" : new SaveCommand(_aggregate).CsClassNameCreate;
            public string CsUpdateType => _aggregate is ChildrenAggreagte ? $"List<{new SaveCommand(_aggregate).CsClassNameUpdate}>" : new SaveCommand(_aggregate).CsClassNameUpdate;
            public string CsDeleteType => _aggregate is ChildrenAggreagte ? $"List<{new SaveCommand(_aggregate).CsClassNameDelete}>" : new SaveCommand(_aggregate).CsClassNameDelete;
        }
        #endregion メンバー
    }
}

namespace Nijo.Ver1.CodeGenerating {
    using Nijo.Ver1.Models.DataModelModules;

    partial class SchemaPathNodeExtensions {

        /// <summary>
        /// <see cref="GetPathFromEntry(ISchemaPathNode)"/> の結果を <see cref="SaveCommand"/> のルールに沿ったパスとして返す
        /// </summary>
        public static IEnumerable<string> AsSaveCommand(this IEnumerable<ISchemaPathNode> path) {
            var isOutOfEntryTree = false;

            foreach (var node in path) {
                if (node is RootAggregate && node.PreviousNode == null) continue; // パスの一番最初（エントリー）はスキップ
                if (node.PreviousNode is RefToMember) continue; // refの1つ次の要素の名前はrefで列挙済みのためスキップ

                // 外部参照のナビゲーションプロパティを辿るパス
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

                        var childMember = curr switch {
                            ChildAggreagte child => new SaveCommand.SaveCommandDescendantMember(child),
                            ChildrenAggreagte children => new SaveCommand.SaveCommandDescendantMember(children),
                            _ => throw new InvalidOperationException("ありえない"),
                        };
                        yield return childMember.PhysicalName;
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
