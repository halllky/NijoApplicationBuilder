using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nijo.Models.QueryModelModules {
    /// <summary>
    /// ある集約が他の集約から参照されるときの画面表示用データ
    /// </summary>
    internal static class DisplayDataRef {

        /// <summary>
        /// <see cref="DisplayDataRef"/> に関連するモジュールは、
        /// その集約が他のどの集約からも参照されていない場合はレンダリングしないため、
        /// そのツリー内部で他の集約から参照されているもののみを集めるメソッド。
        /// </summary>
        internal static (Entry[] Entries, RefDisplayDataMemberContainer[] NotEntries) GetReferedMembersRecursively(RootAggregate rootAggregate) {

            // ほかの集約から参照されている集約のエントリーと、その祖先・子孫を再帰的に列挙する。
            var entries = new List<Entry>();
            var notEntries = new Dictionary<(AggregateBase Agg, ISchemaPathNode? Prev), RefDisplayDataMemberContainer>();
            foreach (var agg in rootAggregate.EnumerateThisAndDescendants()) {
                if (!agg.GetRefFroms().Any()) continue;

                var entry = new Entry(agg.AsEntry());
                entries.Add(entry);

                var members = entry
                    .GetMetadataRecursively()
                    .OfType<RefDisplayDataMemberContainer>()
                    .Where(member => member.Aggregate.GetRoot() == rootAggregate);
                foreach (var member in members) {
                    // エントリー以外のクラスは集約とその1個前の集約の組み合わせで一意
                    notEntries[(member.Aggregate, member.Aggregate.PreviousNode)] = member;
                }
            }

            return (entries.ToArray(), notEntries.Values.ToArray());
        }

        #region レンダリング
        /// <summary>
        /// 他の集約から参照されているもののみ再帰的にレンダリングする
        /// </summary>
        internal static string RenderCSharpRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var (entries, notEntries) = GetReferedMembersRecursively(rootAggregate);

            return $$"""
                #region 他の集約から参照されるときの画面表示用オブジェクト
                {{entries.SelectTextTemplate(entry => $$"""
                {{entry.RenderCsClass(ctx)}}

                """)}}
                {{notEntries.SelectTextTemplate(parent => $$"""
                {{parent.RenderCsClass(ctx)}}

                """)}}
                #endregion 他の集約から参照されるときの画面表示用オブジェクト
                """;
        }
        /// <summary>
        /// 他の集約から参照されているもののみ再帰的にレンダリングする
        /// </summary>
        internal static string RenderTypeScriptRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var (entries, _) = GetReferedMembersRecursively(rootAggregate);

            return $$"""
                //#region 他の集約から参照されるときの画面表示用オブジェクト
                {{entries.SelectTextTemplate(entry => $$"""
                {{entry.RenderTypeScriptTypeDef(ctx)}}

                """)}}
                //#endregion 他の集約から参照されるときの画面表示用オブジェクト
                """;
        }

        internal static string RenderTypeScriptFunctionsRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var (entries, _) = GetReferedMembersRecursively(rootAggregate);

            return $$"""
                //#region 他の集約から参照されるときの画面表示用オブジェクトの新規作成関数
                {{entries.SelectTextTemplate(entry => $$"""
                {{entry.RenderTypeScriptObjectCreationFunction(ctx)}}

                """)}}
                //#endregion 他の集約から参照されるときの画面表示用オブジェクトの新規作成関数

                //#region 主キーの抽出
                {{entries.SelectTextTemplate(entry => $$"""
                {{entry.RenderExtractPrimaryKey()}}

                """)}}
                //#endregion 主キーの抽出

                //#region 主キーの設定
                {{entries.SelectTextTemplate(entry => $$"""
                {{entry.RenderAssignPrimaryKey()}}

                """)}}
                //#endregion 主キーの設定
                """;
        }
        #endregion レンダリング


        /// <summary>
        /// C#のクラスが存在するものたち
        /// </summary>
        internal abstract class RefDisplayDataMemberContainer : IInstancePropertyOwnerMetadata {
            private protected RefDisplayDataMemberContainer(AggregateBase aggregate) {
                Aggregate = aggregate;
            }
            internal AggregateBase Aggregate { get; }

            internal abstract string CsClassName { get; }
            /// <summary>
            /// 直近のメンバーを列挙する
            /// </summary>
            internal IEnumerable<IRefDisplayDataMember> GetMembers() {
                var parent = Aggregate.GetParent();
                if (parent != null && Aggregate.PreviousNode != (ISchemaPathNode)parent) {
                    yield return new RefDisplayDataParentMember(parent);
                }
                foreach (var member in Aggregate.GetMembers()) {
                    if (member is ValueMember vm) {
                        yield return new RefDisplayDataValueMember(vm);

                    } else if (member is RefToMember refTo && Aggregate.PreviousNode != (ISchemaPathNode)refTo) {
                        yield return new RefDisplayDataRefToMember(refTo);

                    } else if (member is ChildAggregate child && Aggregate.PreviousNode != (ISchemaPathNode)child) {
                        yield return new RefDisplayDataChildMember(child);

                    } else if (member is ChildrenAggregate children && Aggregate.PreviousNode != (ISchemaPathNode)children) {
                        yield return new RefDisplayDataChildrenMember(children);
                    }
                }
            }

            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
                return GetMembers();
            }

            internal string RenderCsClass(CodeRenderingContext ctx) {
                return $$"""
                    /// <summary>
                    /// {{((AggregateBase)Aggregate.GetEntry()).DisplayName}}が他の集約から外部参照されるときの{{Aggregate.DisplayName}}の型
                    /// </summary>
                    public partial class {{CsClassName}} {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                        {{WithIndent(member.RenderDeclaringCSharp(), "    ")}}
                    """)}}
                    }
                    """;
            }
        }

        /// <summary>
        /// エントリー。エントリーが子孫要素になる場合もある。
        /// </summary>
        internal class Entry : RefDisplayDataMemberContainer {
            internal Entry(AggregateBase aggregate) : base(aggregate) { }

            internal override string CsClassName => $"{base.Aggregate.PhysicalName}RefTarget";
            internal string TsTypeName => $"{base.Aggregate.PhysicalName}RefTarget";

            #region TypeScript側オブジェクト新規作成関数
            public string TsNewObjectFunction => $"createNew{TsTypeName}";

            internal string RenderTypeScriptTypeDef(CodeRenderingContext ctx) {
                return $$"""
                    /** {{((AggregateBase)Aggregate.GetEntry()).DisplayName}}が他の集約から外部参照されるときの{{Aggregate.DisplayName}}の型 */
                    export type {{TsTypeName}} = {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderDeclaringTypeScript(), "  ")}}
                    """)}}
                    }
                    """;
            }
            internal string RenderTypeScriptObjectCreationFunction(CodeRenderingContext ctx) {
                return $$"""
                    /**
                     * {{base.Aggregate.DisplayName}}が他の集約から外部参照されるときのオブジェクトを新規作成します。
                     */
                    export const {{TsNewObjectFunction}} = (): {{TsTypeName}} => ({
                      {{WithIndent(RenderMembersRecursively(this), "  ")}}
                    })
                    """;

                static IEnumerable<string> RenderMembersRecursively(RefDisplayDataMemberContainer obj) {
                    foreach (var member in obj.GetMembers()) {
                        if (member is RefDisplayDataValueMember vm) {
                            yield return $$"""
                                {{member.PhysicalName}}: undefined,
                                """;

                        } else if (member is RefDisplayDataChildrenMember children) {
                            yield return $$"""
                                {{member.PhysicalName}}: [],
                                """;

                        } else if (member is RefDisplayDataMemberContainer container) {
                            yield return $$"""
                                {{member.PhysicalName}}: {
                                  {{WithIndent(RenderMembersRecursively(container), "  ")}}
                                },
                                """;

                        } else {
                            throw new NotImplementedException();
                        }
                    }
                }
            }
            #endregion TypeScript側オブジェクト新規作成関数


            #region 主キーの抽出と設定
            internal string PkExtractFunctionName => $"extract{Aggregate.PhysicalName}RefKeys";
            internal string PkAssignFunctionName => $"assign{Aggregate.PhysicalName}RefKeys";
            internal string RenderExtractPrimaryKey() {
                var keys = Aggregate.GetKeyVMs().ToArray();
                var dataProperties = new Variable("data", this)
                    .Create1To1PropertiesRecursively()
                    .ToDictionary(p => p.Metadata.SchemaPathNode.ToMappingKey());
                return $$"""
                    /**
                     * {{Aggregate.DisplayName}}のオブジェクトから主キーを抽出します。
                     */
                    export const {{PkExtractFunctionName}} = (data: {{TsTypeName}}): [{{keys.Select(k => $"{k.PhysicalName}: {k.Type.TsTypeName} | undefined").Join(", ")}}] => {
                      return [
                    {{keys.SelectTextTemplate(k => $$"""
                        {{dataProperties[k.ToMappingKey()].GetJoinedPathFromInstance(E_CsTs.TypeScript, ".")}},
                    """)}}
                      ]
                    }
                    """;
            }
            internal string RenderAssignPrimaryKey() {
                var keys = Aggregate.GetKeyVMs().ToArray();
                var dataProperties = new Variable("data", this)
                    .Create1To1PropertiesRecursively()
                    .ToDictionary(p => p.Metadata.SchemaPathNode.ToMappingKey());
                return $$"""
                    /**
                     * {{Aggregate.DisplayName}}の主キーを引数のオブジェクトに設定します。
                     */
                    export const {{PkAssignFunctionName}} = (data: {{TsTypeName}}, keys: [{{keys.Select(k => $"{k.PhysicalName}: {k.Type.TsTypeName} | undefined").Join(", ")}}]): void => {
                      if (keys.length !== {{keys.Length}}) {
                        console.error(`主キーの数が一致しません。個数は{{keys.Length}}であるべきところ${keys.length}個です。`);
                        return
                      }
                    {{keys.SelectTextTemplate((k, i) => $$"""
                      {{dataProperties[k.ToMappingKey()].GetJoinedPathFromInstance(E_CsTs.TypeScript, ".")}} = keys[{{i}}]
                    """)}}
                    }
                    """;
            }
            #endregion 主キーの抽出と設定
        }


        #region Entry以外のメンバー
        /// <summary>
        /// <see cref="DisplayDataRef"/>のメンバー
        /// </summary>
        internal interface IRefDisplayDataMember : IInstancePropertyMetadata {
            string PhysicalName { get; }
            string GetTypeName(E_CsTs csts);
            string RenderDeclaringCSharp();
            string RenderDeclaringTypeScript();
        }

        /// <summary>
        /// ValueMember
        /// </summary>
        internal class RefDisplayDataValueMember : IRefDisplayDataMember, IInstanceValuePropertyMetadata {
            internal RefDisplayDataValueMember(ValueMember member) {
                Member = member;
            }
            public ValueMember Member { get; }

            public string PhysicalName => Member.PhysicalName;
            public string DisplayName => Member.DisplayName;
            public string GetTypeName(E_CsTs csts) => csts == E_CsTs.CSharp ? Member.Type.CsDomainTypeName : Member.Type.TsTypeName;

            IValueMemberType IInstanceValuePropertyMetadata.Type => Member.Type;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Member;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;

            string IRefDisplayDataMember.RenderDeclaringCSharp() {
                return $$"""
                    public {{GetTypeName(E_CsTs.CSharp)}}? {{PhysicalName}} { get; set; }
                    """;
            }

            string IRefDisplayDataMember.RenderDeclaringTypeScript() {
                return $$"""
                    {{PhysicalName}}?: {{Member.Type.TsTypeName}}
                    """;
            }
        }

        /// <summary>
        /// Ref
        /// </summary>
        internal class RefDisplayDataRefToMember : Entry, IRefDisplayDataMember, IInstanceStructurePropertyMetadata {
            internal RefDisplayDataRefToMember(RefToMember member) : base(member.RefTo) {
                _member = member;
            }
            private readonly RefToMember _member;

            public string PhysicalName => _member.PhysicalName;
            public string DisplayName => _member.DisplayName;
            public string GetTypeName(E_CsTs csts) => csts == E_CsTs.CSharp ? CsClassName : TsTypeName;

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _member;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;

            string IRefDisplayDataMember.RenderDeclaringCSharp() {
                return $$"""
                    public {{GetTypeName(E_CsTs.CSharp)}} {{PhysicalName}} { get; set; } = new();
                    """;
            }

            string IRefDisplayDataMember.RenderDeclaringTypeScript() {
                var refTo = new Entry(_member.RefTo);

                return $$"""
                    {{PhysicalName}}: {{refTo.TsTypeName}}
                    """;
            }
        }

        /// <summary>
        /// Child
        /// </summary>
        internal class RefDisplayDataChildMember : RefDisplayDataMemberContainer, IRefDisplayDataMember, IInstanceStructurePropertyMetadata {
            internal RefDisplayDataChildMember(ChildAggregate member) : base(member) {
                _member = member;
            }
            private readonly ChildAggregate _member;

            public string PhysicalName => _member.PhysicalName;
            public string DisplayName => _member.DisplayName;
            public string GetTypeName(E_CsTs csts) => csts == E_CsTs.CSharp
                ? $"{_member.PhysicalName}RefTargetVia{_member.PreviousNode!.XElement.Name.LocalName.ToCSharpSafe()}"
                : $"{_member.PhysicalName}RefTargetVia{_member.PreviousNode!.XElement.Name.LocalName.ToCSharpSafe()}";
            internal override string CsClassName => GetTypeName(E_CsTs.CSharp);

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _member;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;

            string IRefDisplayDataMember.RenderDeclaringCSharp() {
                return $$"""
                    public {{GetTypeName(E_CsTs.CSharp)}} {{PhysicalName}} { get; set; } = new();
                    """;
            }

            string IRefDisplayDataMember.RenderDeclaringTypeScript() {
                return $$"""
                    {{PhysicalName}}: {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderDeclaringTypeScript(), "  ")}}
                    """)}}
                    }
                    """;
            }
        }

        /// <summary>
        /// Children
        /// </summary>
        internal class RefDisplayDataChildrenMember : RefDisplayDataMemberContainer, IRefDisplayDataMember, IInstanceStructurePropertyMetadata {
            internal RefDisplayDataChildrenMember(ChildrenAggregate member) : base(member) {
                ChildrenAggregate = member;
            }
            internal ChildrenAggregate ChildrenAggregate { get; }

            public string PhysicalName => ChildrenAggregate.PhysicalName;
            public string DisplayName => ChildrenAggregate.DisplayName;
            public string GetTypeName(E_CsTs csts) => $"{ChildrenAggregate.PhysicalName}RefTargetVia{ChildrenAggregate.PreviousNode!.XElement.Name.LocalName.ToCSharpSafe()}";
            internal override string CsClassName => GetTypeName(E_CsTs.CSharp);

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => ChildrenAggregate;
            bool IInstanceStructurePropertyMetadata.IsArray => true;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;

            string IRefDisplayDataMember.RenderDeclaringCSharp() {
                return $$"""
                    public List<{{GetTypeName(E_CsTs.CSharp)}}> {{PhysicalName}} { get; set; } = [];
                    """;
            }

            string IRefDisplayDataMember.RenderDeclaringTypeScript() {
                return $$"""
                    {{PhysicalName}}: {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderDeclaringTypeScript(), "  ")}}
                    """)}}
                    }[]
                    """;
            }
        }

        /// <summary>
        /// Parent
        /// </summary>
        internal class RefDisplayDataParentMember : RefDisplayDataMemberContainer, IRefDisplayDataMember, IInstanceStructurePropertyMetadata {
            internal RefDisplayDataParentMember(AggregateBase parent) : base(parent) {
                _parent = parent;
            }
            private readonly AggregateBase _parent;

            public string PhysicalName => "Parent";
            public string DisplayName => _parent.DisplayName;
            public string GetTypeName(E_CsTs csts) => $"{_parent.PhysicalName}RefTargetVia{_parent.PreviousNode!.XElement.Name.LocalName.ToCSharpSafe()}";
            internal override string CsClassName => GetTypeName(E_CsTs.CSharp);

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _parent;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;

            string IRefDisplayDataMember.RenderDeclaringCSharp() {
                return $$"""
                    public {{GetTypeName(E_CsTs.CSharp)}} {{PhysicalName}} { get; set; } = new();
                    """;
            }

            string IRefDisplayDataMember.RenderDeclaringTypeScript() {
                return $$"""
                    {{PhysicalName}}: {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderDeclaringTypeScript(), "  ")}}
                    """)}}
                    }
                    """;
            }
        }
        #endregion Entry以外のメンバー
    }
}
