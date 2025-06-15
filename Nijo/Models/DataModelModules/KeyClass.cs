using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Nijo.Models.DataModelModules {

    /// <summary>
    /// 集約のキー部分のみの情報
    /// </summary>
    internal static class KeyClass {

        /// <summary>
        /// キーのエントリー。子孫集約になることもある。
        /// <see cref="IKeyClassMember"/> インターフェースを備えている理由は、
        /// エントリーが子孫かつこのクラスがその子孫の親の場合、このクラスは子孫のキーのメンバーになりうるため。
        /// </summary>
        internal class KeyClassEntry : IKeyClassMember, IKeyClassStructure, IInstanceStructurePropertyMetadata {
            internal KeyClassEntry(AggregateBase aggregate) {
                _aggregate = aggregate;
            }
            private readonly AggregateBase _aggregate;

            internal string ClassName => $"{_aggregate.PhysicalName}Key";
            string IKeyClassMember.GetTypeName(E_CsTs csts) => ClassName;

            bool SaveCommand.ISaveCommandMember.IsKey => true;
            ISchemaPathNode SaveCommand.ISaveCommandMember.Member => _aggregate;
            public virtual string PhysicalName => _aggregate.PhysicalName;
            public virtual string DisplayName => _aggregate.DisplayName;
            public string CsCreateType => ClassName;
            public string CsUpdateType => ClassName;
            public string CsDeleteType => ClassName;

            public IEnumerable<IKeyClassMember> GetOwnMembers() {
                var p = _aggregate.GetParent();
                if (p != null) {
                    yield return new KeyClassEntry(p);
                }

                foreach (var m in _aggregate.GetMembers()) {
                    if (m is ValueMember vm && vm.IsKey) {
                        yield return new KeyClassValueMember(vm);

                    } else if (m is RefToMember rm && rm.IsKey) {
                        yield return new KeyClassRefMember(rm);
                    }
                }
            }

            string SaveCommand.ISaveCommandMember.RenderDeclaring() {
                return $$"""
                    /// <summary>{{DisplayName}}</summary>
                    public required {{ClassName}}? {{PhysicalName}} { get; set; }
                    """;
            }

            /// <summary>
            /// 子孫のキークラス定義も含めて全部レンダリング
            /// </summary>
            internal static string RenderClassDeclaringRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {

                var tree = rootAggregate
                    .AsEntry()
                    .EnumerateThisAndDescendants()
                    .ToArray();

                // キーのエントリー。以下いずれかの場合のみレンダリングする
                // - その集約がほかの集約から参照されている場合
                // - その集約の子がほかの集約から参照されている場合
                var entries = tree
                    .Where(agg => agg.EnumerateThisAndDescendants().Any(a => a.GetRefFroms().Any()))
                    .Select(agg => new KeyClassEntry(agg))
                    .ToArray();

                return $$"""
                    #region キー項目のみのオブジェクト
                    {{entries.SelectTextTemplate(entry => $$"""
                    {{entry.RenderDeclaring()}}

                    """)}}
                    #endregion キー項目のみのオブジェクト
                    """;
            }

            protected virtual string RenderDeclaring() {
                return $$"""
                    /// <summary>
                    /// {{_aggregate.DisplayName}} のキー
                    /// </summary>
                    public partial class {{ClassName}} {
                    {{GetOwnMembers().SelectTextTemplate(m => $$"""
                        /// <summary>{{m.DisplayName}}</summary>
                        public required {{m.GetTypeName(E_CsTs.CSharp)}}? {{m.PhysicalName}} { get; set; }
                    """)}}

                        {{WithIndent(RenderCovertFromCreateCommand(), "    ")}}
                    }
                    """;
            }

            #region FromCreateCommand
            internal const string FROM_SAVE_COMMAND = "FromCreateCommand";
            /// <summary>
            /// ルート集約の <see cref="SaveCommand"/> から、このクラスのインスタンス1個または複数個を作成するメソッド。
            /// このクラスがChildren、または祖先にChildrenが含まれる場合は戻り値が複数になる。
            /// ダミーデータの生成に使用。
            /// </summary>
            private string RenderCovertFromCreateCommand() {
                var root = _aggregate.GetRoot();
                var rootCreateCommandMetadata = new SaveCommand(root, SaveCommand.E_Type.Create);
                var arg = new Variable("createCommand", rootCreateCommandMetadata);

                // ------------------------------------------
                // 右辺の変数に使われる変数を定義する。右辺は集約ルートが起点になる。
                var thisCreateCommandInstance = (IInstancePropertyOwner?)null;
                var thisCreateCommandInstanceOwnerArray = (IInstancePropertyOwner?)null;
                var rightInstances = CollectInstancesRecursively(arg).ToDictionary(kv => kv.Key, kv => kv.Value);

                IEnumerable<KeyValuePair<SchemaNodeIdentity, string>> CollectInstancesRecursively(IInstancePropertyOwner currentInstance, IInstancePropertyOwner? ownerArray = null) {
                    var currentSaveCommand = (SaveCommand)currentInstance.Metadata;

                    // ValueMember(Ref先のValueMember含む)
                    var valueMembers = currentInstance
                        .Create1To1PropertiesRecursively()
                        .Where(p => p.Metadata is SaveCommand.SaveCommandValueMember member && member.IsKey);
                    foreach (var member in valueMembers) {
                        yield return KeyValuePair.Create(
                            member.Metadata.SchemaPathNode.ToMappingKey(),
                            member.GetJoinedPathFromInstance(E_CsTs.CSharp, "?."));
                    }

                    // 左辺の集約が表れたら終了（左辺の子孫の集約はそれと対応する右辺の変数を定義しなくてもよい）
                    if (currentSaveCommand.Aggregate == _aggregate) {
                        thisCreateCommandInstance = currentInstance;
                        thisCreateCommandInstanceOwnerArray = ownerArray;
                        yield break;
                    }

                    // Child, Children に対して再帰処理
                    foreach (var member in currentSaveCommand.GetMembers()) {
                        if (member is SaveCommand.SaveCommandChildMember child) {
                            var childProperty = currentInstance.CreateProperty(child);
                            foreach (var desc in CollectInstancesRecursively(childProperty)) {
                                yield return desc;
                            }

                        } else if (member is SaveCommand.SaveCommandChildrenMember children) {
                            var childProperty = currentInstance.CreateProperty(children);
                            var loopVar = new Variable(children.Aggregate.GetLoopVarName(), children);
                            foreach (var desc in CollectInstancesRecursively(loopVar, childProperty)) {
                                yield return desc;
                            }
                        }
                    }
                }

                // ------------------------------------------
                var ancestors = _aggregate.EnumerateThisAndAncestors().ToArray();
                var isReturnArray = ancestors.Any(agg => agg is ChildrenAggregate);
                var returnType = isReturnArray ? $"IEnumerable<{ClassName}>" : $"{ClassName}";

                return $$"""
                    /// <summary>
                    /// <see cref="{{rootCreateCommandMetadata.CsClassNameCreate}}"/> を <see cref="{{ClassName}}"> のインスタンス{{(isReturnArray ? "複数個" : "")}}に変換します。
                    /// </summary>
                    public static {{returnType}} {{FROM_SAVE_COMMAND}}({{rootCreateCommandMetadata.CsClassNameCreate}} {{arg.Name}}) {
                        {{WithIndent(RenderReturnOrForEach(arg, 0, false), "    ")}}
                    }
                    """;

                // 引数の集約からスタートして子孫を辿りChildrenが登場するまでをレンダリングする。
                // 戻り値のKeyClassの祖先にChildrenが2回以上表れる場合、
                // 戻り値のKeyClassのメンバーの一部はそれぞれのChildrenのループ変数から取得する必要があるので、
                // foreachでレンダリングする。
                string RenderReturnOrForEach(IInstancePropertyOwner currentInstance, int indexInAncestorArray, bool isInForEach) {
                    var currentSaveCommand = (SaveCommand)currentInstance.Metadata;

                    // 戻り値の集約まで辿りついたので、SaveCommandをKeyClassに変換してreturnする
                    if (currentSaveCommand.Aggregate == _aggregate) {
                        var @return = isInForEach ? "yield return" : "return";

                        return $$"""
                            {{@return}} new {{ClassName}} {
                                {{WithIndent(RenderReturnBodyRecursively(this), "    ")}}
                            };
                            """;
                    }

                    var next = ancestors[indexInAncestorArray + 1];

                    // Child
                    if (next is ChildAggregate child) {
                        var childMetadata = new SaveCommand.SaveCommandChildMember(child, SaveCommand.E_Type.Create);
                        var childProperty = currentInstance.CreateProperty(childMetadata);

                        return $$"""
                            {{RenderReturnOrForEach(childProperty, indexInAncestorArray + 1, isInForEach)}}
                            """;
                    }

                    // Children
                    if (next is ChildrenAggregate children) {
                        var childMetadata = new SaveCommand.SaveCommandChildrenMember(children, SaveCommand.E_Type.Create);
                        var childProperty = currentInstance.CreateProperty(childMetadata);
                        var loopVar = new Variable(children.GetLoopVarName(), childMetadata);

                        return $$"""
                            foreach (var {{loopVar.Name}} in {{childProperty.GetJoinedPathFromInstance(E_CsTs.CSharp)}}) {
                                {{WithIndent(RenderReturnOrForEach(childProperty, indexInAncestorArray + 1, true), "    ")}}
                            }
                            """;
                    }

                    throw new NotImplementedException("不明なパターン");
                }

                // return new xxxxKeyClass { ... }; の中身のレンダリング
                IEnumerable<string> RenderReturnBodyRecursively(KeyClassEntry keyClass) {
                    foreach (var member in keyClass.GetOwnMembers()) {
                        if (member is KeyClassValueMember vm) {
                            var path = rightInstances[vm.Member.ToMappingKey()];
                            yield return $$"""
                                {{member.PhysicalName}} = {{path}},
                                """;

                        } else if (member is KeyClassRefMember rm) {
                            yield return $$"""
                                {{member.PhysicalName}} = new() {
                                    {{WithIndent(RenderReturnBodyRecursively(rm.MemberKeyClassEntry), "    ")}}
                                },
                                """;

                        } else if (member is KeyClassEntry pm) {
                            yield return $$"""
                                {{member.PhysicalName}} = new() {
                                    {{WithIndent(RenderReturnBodyRecursively(pm), "    ")}}
                                },
                                """;

                        } else {
                            throw new NotImplementedException();
                        }
                    }
                }
            }
            #endregion FromSaveCommand

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _aggregate;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstanceStructurePropertyMetadata.GetTypeName(E_CsTs csts) => ClassName;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetOwnMembers();
        }


        #region メンバー
        /// <summary>
        /// KeyClassのエントリー、Ref の2種類
        /// </summary>
        internal interface IKeyClassStructure : IInstancePropertyOwnerMetadata {
            IEnumerable<IKeyClassMember> GetOwnMembers();
        }
        internal interface IKeyClassMember : SaveCommand.ISaveCommandMember {
            string GetTypeName(E_CsTs csts);
        }
        /// <summary>
        /// キー情報の値メンバー
        /// </summary>
        internal class KeyClassValueMember : SaveCommand.SaveCommandValueMember, IKeyClassMember {
            internal KeyClassValueMember(ValueMember vm) : base(vm) { }

            public string GetTypeName(E_CsTs csts) => Member.Type.CsDomainTypeName;

            ISchemaPathNode SaveCommand.ISaveCommandMember.Member => Member;
            string SaveCommand.ISaveCommandMember.CsCreateType => GetTypeName(E_CsTs.CSharp);
            string SaveCommand.ISaveCommandMember.CsUpdateType => GetTypeName(E_CsTs.CSharp);
            string SaveCommand.ISaveCommandMember.CsDeleteType => GetTypeName(E_CsTs.CSharp);
        }
        /// <summary>
        /// キー情報の中に出てくる他の集約のキー
        /// </summary>
        internal class KeyClassRefMember : IKeyClassStructure, IKeyClassMember, IInstanceStructurePropertyMetadata {
            internal KeyClassRefMember(RefToMember refTo) {
                Member = refTo;
                MemberKeyClassEntry = new KeyClassEntry(refTo.RefTo);
            }
            internal RefToMember Member { get; }
            internal KeyClassEntry MemberKeyClassEntry { get; }

            public string PhysicalName => Member.PhysicalName;
            public string DisplayName => Member.DisplayName;
            public string GetTypeName(E_CsTs csts) => MemberKeyClassEntry.ClassName;

            ISchemaPathNode SaveCommand.ISaveCommandMember.Member => Member;
            string SaveCommand.ISaveCommandMember.CsCreateType => GetTypeName(E_CsTs.CSharp);
            string SaveCommand.ISaveCommandMember.CsUpdateType => GetTypeName(E_CsTs.CSharp);
            string SaveCommand.ISaveCommandMember.CsDeleteType => GetTypeName(E_CsTs.CSharp);

            public IEnumerable<IKeyClassMember> GetOwnMembers() {
                var p = Member.RefTo.GetParent();
                if (p != null) {
                    yield return new KeyClassEntry(p);
                }

                foreach (var m in Member.RefTo.GetMembers()) {
                    if (m is ValueMember vm && vm.IsKey) {
                        yield return new KeyClassValueMember(vm);

                    } else if (m is RefToMember rm && rm.IsKey) {
                        yield return new KeyClassRefMember(rm);
                    }
                }
            }

            bool SaveCommand.ISaveCommandMember.IsKey => true;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Member;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetOwnMembers();

            string SaveCommand.ISaveCommandMember.RenderDeclaring() {
                return $$"""
                    /// <summary>{{DisplayName}}</summary>
                    public required {{GetTypeName(E_CsTs.CSharp)}} {{PhysicalName}} { get; set; }
                    """;
            }
        }
        #endregion メンバー
    }
}

namespace Nijo.CodeGenerating {
    using Nijo.Models.DataModelModules;

    partial class SchemaPathNodeExtensions {
        internal static IEnumerable<SaveCommand.SaveCommandValueMember> GetValueMembersRecursively(this KeyClass.IKeyClassStructure keyClass) {
            foreach (var member in keyClass.GetOwnMembers()) {
                if (member is SaveCommand.SaveCommandValueMember vm) {
                    yield return vm;

                } else if (member is KeyClass.IKeyClassStructure structure) {
                    foreach (var vm2 in structure.GetValueMembersRecursively()) {
                        yield return vm2;
                    }
                }
            }
        }
    }
}
