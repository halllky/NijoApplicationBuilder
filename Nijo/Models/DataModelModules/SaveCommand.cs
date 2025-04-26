using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
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
    internal class SaveCommand : IInstancePropertyOwnerMetadata {

        internal SaveCommand(AggregateBase aggregate, E_Type type) {
            Aggregate = aggregate;
            Type = type;
        }
        internal AggregateBase Aggregate { get; }

        internal enum E_Type { Create, Update, Delete }
        internal E_Type Type { get; }

        internal string CsClassName => Type switch {
            E_Type.Create => $"{Aggregate.PhysicalName}CreateCommand",
            E_Type.Update => $"{Aggregate.PhysicalName}UpdateCommand",
            E_Type.Delete => $"{Aggregate.PhysicalName}DeleteCommand",
            _ => throw new InvalidOperationException(),
        };
        internal string CsClassNameCreate => CsClassName;
        internal string CsClassNameUpdate => CsClassName;
        internal string CsClassNameDelete => CsClassName;

        internal const string VERSION = "Version";
        internal const string TO_DBENTITY = "ToDbEntity";
        internal const string FROM_DBENTITY = "FromDbEntity";

        /// <summary>
        /// 新規・更新・削除のすべてのコマンドを、子孫要素の分も含めて再帰的にレンダリングします。
        /// </summary>
        internal static string RenderAll(RootAggregate rootAggregate, CodeRenderingContext ctx) {

            var tree = rootAggregate.EnumerateThisAndDescendants().ToArray();

            return $$"""
                #region 新規登録時引数
                {{tree.SelectTextTemplate(agg => $$"""
                {{new SaveCommand(agg, E_Type.Create).RenderCreateCommandDeclaring(ctx)}}
                """)}}
                #endregion 新規登録時引数


                #region 更新時引数
                {{tree.SelectTextTemplate(agg => $$"""
                {{new SaveCommand(agg, E_Type.Update).RenderUpdateCommandDeclaring(ctx)}}
                """)}}
                #endregion 更新時引数


                #region 物理削除時引数
                {{tree.SelectTextTemplate(agg => $$"""
                {{new SaveCommand(agg, E_Type.Delete).RenderDeleteCommandDeclaring(ctx)}}
                """)}}
                #endregion 物理削除時引数
                """;
        }

        /// <summary>
        /// メンバーを列挙する。ValueMember, Ref, Child, Children すべて含む
        /// </summary>
        internal IEnumerable<ISaveCommandMember> GetMembers() {
            return Type switch {
                E_Type.Create => GetCreateCommandMembers(),
                E_Type.Update => GetUpdateCommandMembers(),
                E_Type.Delete => GetDeleteCommandMembers(),
                _ => throw new NotImplementedException(),
            };
        }
        IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();


        #region CREATE
        internal IEnumerable<ISaveCommandMember> GetCreateCommandMembers() {
            foreach (var member in Aggregate.GetMembers()) {
                if (member is ValueMember vm) {
                    // 新規登録時に自動採番されるものは新規登録メンバー中に含めない
                    if (vm.Type is SequenceMember) continue;

                    yield return new SaveCommandValueMember(vm);

                } else if (member is RefToMember rm) {
                    yield return new SaveCommandRefMember(rm);

                } else if (member is ChildAggregate child) {
                    yield return new SaveCommandChildMember(child, Type);

                } else if (member is ChildrenAggregate children) {
                    yield return new SaveCommandChildrenMember(children, Type);
                }
            }
        }
        private string RenderCreateCommandDeclaring(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{Aggregate.DisplayName}} の新規登録コマンド引数
                /// </summary>
                public partial class {{CsClassNameCreate}} {
                {{GetCreateCommandMembers().SelectTextTemplate(m => $$"""
                    {{WithIndent(m.RenderDeclaring(), "    ")}}
                """)}}
                {{If(Aggregate is RootAggregate, () => $$"""

                    {{WithIndent(RenderToDbEntity(isCreate: true), "    ")}}
                """)}}
                }
                """;
        }
        #endregion CREATE


        #region UPDATE
        internal IEnumerable<ISaveCommandMember> GetUpdateCommandMembers() {
            foreach (var member in Aggregate.GetMembers()) {
                if (member is ValueMember vm) {
                    yield return new SaveCommandValueMember(vm);

                } else if (member is RefToMember rm) {
                    yield return new SaveCommandRefMember(rm);

                } else if (member is ChildAggregate child) {
                    yield return new SaveCommandChildMember(child, Type);

                } else if (member is ChildrenAggregate children) {
                    yield return new SaveCommandChildrenMember(children, Type);
                }
            }
        }
        private string RenderUpdateCommandDeclaring(CodeRenderingContext ctx) {
            var efCoreEntity = new EFCoreEntity(Aggregate);

            return $$"""
                /// <summary>
                /// {{Aggregate.DisplayName}} の更新コマンド引数
                /// </summary>
                public partial class {{CsClassNameUpdate}} {
                {{GetUpdateCommandMembers().SelectTextTemplate(m => $$"""
                    {{WithIndent(m.RenderDeclaring(), "    ")}}
                """)}}
                {{If(Aggregate is RootAggregate, () => $$"""
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
            foreach (var member in Aggregate.GetMembers()) {
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
            var efCoreEntity = new EFCoreEntity(Aggregate);

            return $$"""
                /// <summary>
                /// {{Aggregate.DisplayName}} の物理削除コマンド引数。キーとバージョンのみを持つ。
                /// </summary>
                public partial class {{CsClassNameDelete}} {
                {{GetDeleteCommandMembers().SelectTextTemplate(m => $$"""
                    {{WithIndent(m.RenderDeclaring(), "    ")}}
                """)}}
                {{If(Aggregate is RootAggregate, () => $$"""
                    /// <summary>楽観排他制御用のバージョン</summary>
                    public required int? {{VERSION}} { get; set; }
                """)}}
                }
                """;
        }
        #endregion DELETE


        #region メンバー
        internal interface ISaveCommandMember : IInstancePropertyMetadata {
            bool IsKey { get; }
            ISchemaPathNode Member { get; }
            string PhysicalName { get; }
            string CsCreateType { get; }
            string CsUpdateType { get; }
            string CsDeleteType { get; }

            string RenderDeclaring();
        }
        /// <summary>
        /// 更新処理引数クラスの値メンバー
        /// </summary>
        internal class SaveCommandValueMember : ISaveCommandMember, IInstanceValuePropertyMetadata {
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

            public bool IsKey => Member.IsKey;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Member;
            IValueMemberType IInstanceValuePropertyMetadata.Type => Member.Type;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;

            string ISaveCommandMember.RenderDeclaring() {
                return $$"""
                    /// <summary>{{DisplayName}}</summary>
                    public required {{Member.Type.CsDomainTypeName}}? {{PhysicalName}} { get; set; }
                    """;
            }
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
        internal class SaveCommandChildMember : SaveCommand, ISaveCommandMember, IInstanceStructurePropertyMetadata {
            internal SaveCommandChildMember(ChildAggregate child, E_Type type) : base(child, type) { }

            ISchemaPathNode ISaveCommandMember.Member => (IAggregateMember)Aggregate;
            public string PhysicalName => Aggregate.PhysicalName;
            public string DisplayName => Aggregate.DisplayName;
            public string CsCreateType => new SaveCommand(Aggregate, SaveCommand.E_Type.Create).CsClassNameCreate;
            public string CsUpdateType => new SaveCommand(Aggregate, SaveCommand.E_Type.Update).CsClassNameUpdate;
            public string CsDeleteType => new SaveCommand(Aggregate, SaveCommand.E_Type.Delete).CsClassNameDelete;

            bool ISaveCommandMember.IsKey => false;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Aggregate;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;

            string ISaveCommandMember.RenderDeclaring() {
                var className = Type switch {
                    E_Type.Create => CsCreateType,
                    E_Type.Update => CsUpdateType,
                    E_Type.Delete => CsDeleteType,
                    _ => throw new NotImplementedException(),
                };
                return $$"""
                    /// <summary>{{DisplayName}}</summary>
                    public required {{className}} {{PhysicalName}} { get; set; }
                    """;
            }
        }
        /// <summary>
        /// 更新処理引数クラスの子コレクションメンバー
        /// </summary>
        internal class SaveCommandChildrenMember : SaveCommand, ISaveCommandMember, IInstanceStructurePropertyMetadata {
            internal SaveCommandChildrenMember(ChildrenAggregate children, E_Type type) : base(children, type) { }

            internal new ChildrenAggregate Aggregate => (ChildrenAggregate)base.Aggregate;
            ISchemaPathNode ISaveCommandMember.Member => (IAggregateMember)base.Aggregate;
            public string PhysicalName => base.Aggregate.PhysicalName;
            public string DisplayName => base.Aggregate.DisplayName;
            public string CsCreateType => $"List<{new SaveCommand(base.Aggregate, E_Type.Create).CsClassName}>";
            public string CsUpdateType => $"List<{new SaveCommand(base.Aggregate, E_Type.Update).CsClassName}>";
            public string CsDeleteType => $"List<{new SaveCommand(base.Aggregate, E_Type.Delete).CsClassName}>";

            bool ISaveCommandMember.IsKey => false;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => base.Aggregate;
            bool IInstanceStructurePropertyMetadata.IsArray => true;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;

            string ISaveCommandMember.RenderDeclaring() {
                var className = Type switch {
                    E_Type.Create => CsCreateType,
                    E_Type.Update => CsUpdateType,
                    E_Type.Delete => CsDeleteType,
                    _ => throw new NotImplementedException(),
                };
                return $$"""
                    /// <summary>{{DisplayName}}</summary>
                    public required {{className}} {{PhysicalName}} { get; set; }
                    """;
            }
        }
        #endregion メンバー

        private string RenderToDbEntity(bool isCreate) {

            // 右辺の定義
            var rootInstance = new Variable("this", this);
            var rightDictOfRootInstance = rootInstance
                .Create1To1PropertiesRecursively()
                .ToDictionary(x => x.Metadata.SchemaPathNode.ToMappingKey());

            var efCoreEntity = new EFCoreEntity(Aggregate);

            return $$"""
                /// <summary>
                /// このインスタンスを Entity Framework Core のエンティティに変換します。
                /// </summary>
                public {{efCoreEntity.CsClassName}} {{TO_DBENTITY}}() {
                    return new {{efCoreEntity.CsClassName}} {
                        {{WithIndent(RenderToDbEntityBody(efCoreEntity, rootInstance, rightDictOfRootInstance), "        ")}}
                        {{EFCoreEntity.VERSION}} = {{(isCreate ? "0" : $"{rootInstance.Name}.{VERSION}")}},
                    };
                }
                """;

            IEnumerable<string> RenderToDbEntityBody(EFCoreEntity left, IInstancePropertyOwner right, IReadOnlyDictionary<SchemaNodeIdentity, IInstanceProperty> rigthMembers) {

                // 自身のカラム、外部参照のキー、親のキー
                foreach (var col in left.GetColumns()) {
                    // シーケンス項目など登録と共に採番されるものはnullになる可能性がある
                    var sourcePath = rigthMembers.TryGetValue(col.Member.ToMappingKey(), out var source)
                        ? $"{source.Root.Name}.{source.GetPathFromInstance().Select(p => p.Metadata.PropertyName).Join("?.")}"
                        : "null";
                    yield return $$"""
                        {{col.PhysicalName}} = {{col.Member.Type.RenderCastToPrimitiveType()}}{{sourcePath}},
                        """;
                }

                // 子
                var childAndChildren = left
                    .GetNavigationProperties()
                    .OfType<EFCoreEntity.NavigationOfParentChild>()
                    .Where(nav => nav.Principal.ThisSide == left.Aggregate);
                foreach (var nav in childAndChildren) {
                    if (nav.Relevant.ThisSide is ChildAggregate child) {
                        // Child
                        var childEntity = new EFCoreEntity(nav.Relevant.ThisSide);

                        yield return $$"""
                            {{nav.Principal.OtherSidePhysicalName}} = new() {
                                {{WithIndent(RenderToDbEntityBody(childEntity, right, rigthMembers), "    ")}}
                            },
                            """;

                    } else if (nav.Relevant.ThisSide is ChildrenAggregate children) {
                        var childrenEntity = new EFCoreEntity(nav.Relevant.ThisSide);
                        var arrayPath = rigthMembers.TryGetValue(children.ToMappingKey(), out var source)
                            ? $"{source.Root.Name}.{source.GetPathFromInstance().Select(p => p.Metadata.PropertyName).Join("?.")}"
                            : throw new InvalidOperationException($"右辺にChildrenのXElementが無い: {children}");

                        // 辞書に、ラムダ式内部で右辺に使用できるプロパティを加える
                        var dict2 = new Dictionary<SchemaNodeIdentity, IInstanceProperty>(rigthMembers);
                        var saveCommand = new SaveCommandChildrenMember(children, Type);
                        var loopVar = new Variable(children.GetLoopVarName(), saveCommand);
                        foreach (var descendant in loopVar.Create1To1PropertiesRecursively()) {
                            dict2.Add(descendant.Metadata.SchemaPathNode.ToMappingKey(), descendant);
                        }

                        yield return $$"""
                            {{nav.Principal.OtherSidePhysicalName}} = {{arrayPath}}?.Select({{loopVar.Name}} => new {{childrenEntity.CsClassName}} {
                                {{WithIndent(RenderToDbEntityBody(childrenEntity, loopVar, dict2), "    ")}}
                            }).ToHashSet() ?? [],
                            """;

                    } else {
                        throw new NotImplementedException();
                    }
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

                        var parentMember = new KeyClass.KeyClassEntry(curr);
                        yield return parentMember.PhysicalName;
                        continue;
                    }
                    // 親から子へ辿るパス
                    if (curr.IsChildOf(prev)) {

                        // 参照先のキーの中では親から子へ辿るパターンは無い
                        if (isOutOfEntryTree) throw new InvalidOperationException("参照先のキーの中では親から子へ辿るパターンは無い");

                        var childMemberPhysicalName = curr switch {
                            ChildAggregate child => new SaveCommand.SaveCommandChildMember(child, SaveCommand.E_Type.Create).PhysicalName, // CUD全部名前同じなのでCにしている
                            ChildrenAggregate children => new SaveCommand.SaveCommandChildrenMember(children, SaveCommand.E_Type.Create).PhysicalName, // CUD全部名前同じなのでCにしている
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
