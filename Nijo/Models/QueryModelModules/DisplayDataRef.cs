using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.Common;
using Nijo.Parts.CSharp;
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
        internal static (Entry[] Entries, DisplayDataRefBase[] NotEntries) GetReferedMembersRecursively(RootAggregate rootAggregate) {

            // ほかの集約から参照されている集約のエントリーと、その祖先・子孫を再帰的に列挙する。
            var entries = new List<Entry>();
            var notEntries = new Dictionary<(AggregateBase Agg, ISchemaPathNode? Prev), DisplayDataRefBase>();
            foreach (var agg in rootAggregate.EnumerateThisAndDescendants()) {
                if (!agg.GetRefFroms().Any()) continue;

                var entry = new Entry(agg.AsEntry());
                entries.Add(entry);

                var members = entry
                    .GetMetadataRecursively()
                    .OfType<DisplayDataRefBase>()
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
                """;
        }
        #endregion レンダリング


        /// <summary>
        /// エントリー、Child, Children, Parent の基底クラス
        /// </summary>
        internal abstract class DisplayDataRefBase : IPresentationLayerStructure, IInstancePropertyOwnerMetadata {
            internal DisplayDataRefBase(AggregateBase aggregate) { Aggregate = aggregate; }

            internal AggregateBase Aggregate { get; private set; }

            public abstract string CsClassName { get; }
            public abstract string TsTypeName { get; }

            IEnumerable<IInstancePropertyMetadata> IPresentationLayerStructure.GetMembers() => GetMembers();
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();
            public IEnumerable<IPresentationLayerStructure.IMember> GetMembers() {
                var parent = Aggregate.GetParent();
                if (parent != null && Aggregate.PreviousNode != (ISchemaPathNode)parent) {
                    yield return new RefDisplayDataParentMember(parent);
                }
                foreach (var member in Aggregate.GetMembers()) {
                    if (member is ValueMember vm) {

                        // 検索条件にのみ存在するメンバー
                        if (vm.OnlySearchCondition) continue;

                        // 汎用参照テーブルのハードコードされる項目
                        if (vm.IsHardCodedPrimaryKey) continue;

                        yield return new RefDisplayDataValueMember(vm);

                    } else if (member is RefToMember refTo && Aggregate.PreviousNode != (ISchemaPathNode)refTo) {
                        yield return new RefDisplayDataRefToMember(refTo);

                    } else if (member is ChildAggregate child && Aggregate.PreviousNode != (ISchemaPathNode)child) {
                        yield return new RefDisplayDataChildMember(child);

                    } else if (member is ChildrenAggregate children && Aggregate.PreviousNode != (ISchemaPathNode)children) {

                        // 参照先のChildrenを生成すると検索処理のSQLが複雑になりすぎて発行できないことがあるので
                        // オプションで明示的に生成するよう指定が無い限りは生成されない
                        if (!Aggregate.SchemaParseContext.ProjectOptions.GenerateRefToChildrenDisplayData) continue;

                        yield return new RefDisplayDataChildrenMember(children);
                    }
                }
            }

            internal string RenderCsClass(CodeRenderingContext ctx) {
                return $$"""
                    /// <summary>
                    /// {{((AggregateBase)Aggregate.GetEntry()).DisplayName}}が他の集約から外部参照されるときの{{Aggregate.DisplayName}}の型
                    /// </summary>
                    {{NijoAttr.RenderAttributeValues(ctx, Aggregate)}}
                    public partial class {{CsClassName}} {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                        {{WithIndent(member.RenderDeclaringCSharp(ctx))}}
                    """)}}
                    }
                    """;
            }
        }

        /// <summary>
        /// エントリー。エントリーが子孫要素になる場合もある。
        /// </summary>
        internal class Entry : DisplayDataRefBase, ICreatablePresentationLayerStructure {
            internal Entry(AggregateBase aggregate) : base(aggregate) { }

            public override string CsClassName => $"{base.Aggregate.PhysicalName}RefTarget";
            public override string TsTypeName => $"{base.Aggregate.PhysicalName}RefTarget";
            string IPresentationLayerStructure.CsClassName => CsClassName;

            #region TypeScript側オブジェクト新規作成関数
            public string TsNewObjectFunction => $"createNew{TsTypeName}";

            internal string RenderTypeScriptTypeDef(CodeRenderingContext ctx) {
                return $$"""
                    /**
                     * {{((AggregateBase)Aggregate.GetEntry()).DisplayName}}が他の集約から外部参照されるときの{{Aggregate.DisplayName}}の型
                     */
                    export type {{TsTypeName}} = {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderDeclaringTypeScript())}}
                    """)}}
                    }
                    """;
            }
            internal string RenderTypeScriptObjectCreationFunction(CodeRenderingContext ctx) {
                return $$"""
                    /** {{CsClassName}}を新規作成します。 */
                    export const {{TsNewObjectFunction}} = (): {{TsTypeName}} => ({{RenderTsNewObjectFunctionBody()}})
                    """;
            }
            public string RenderTsNewObjectFunctionBody() {
                return $$"""
                    {
                      {{WithIndent(RenderMembersRecursively(this))}}
                    }
                    """;
                static IEnumerable<string> RenderMembersRecursively(DisplayDataRefBase obj) {
                    foreach (var member in obj.GetMembers()) {
                        if (member is RefDisplayDataValueMember vm) {
                            yield return $$"""
                                {{member.PhysicalName}}: undefined,
                                """;

                        } else if (member is RefDisplayDataChildrenMember children) {
                            yield return $$"""
                                {{member.PhysicalName}}: [],
                                """;

                        } else if (member is DisplayDataRefBase container) {
                            yield return $$"""
                                {{member.PhysicalName}}: {
                                  {{WithIndent(RenderMembersRecursively(container))}}
                                },
                                """;

                        } else {
                            throw new NotImplementedException();
                        }
                    }
                }
            }
            #endregion TypeScript側オブジェクト新規作成関数
        }


        #region Entry以外のメンバー

        /// <summary>
        /// ValueMember
        /// </summary>
        internal class RefDisplayDataValueMember : IPresentationLayerStructure.IMember, IInstanceValuePropertyMetadata {
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

            string IPresentationLayerStructure.IMember.RenderDeclaringCSharp(CodeRenderingContext ctx) {
                return $$"""
                    {{NijoAttr.RenderAttributeValues(ctx, Member)}}
                    public {{GetTypeName(E_CsTs.CSharp)}}? {{PhysicalName}} { get; set; }
                    """;
            }

            string IPresentationLayerStructure.IMember.RenderDeclaringTypeScript() {
                return $$"""
                    {{PhysicalName}}?: {{Member.Type.TsTypeName}}
                    """;
            }
        }

        /// <summary>
        /// Ref
        /// </summary>
        internal class RefDisplayDataRefToMember : Entry, IPresentationLayerStructure.IMember, IInstanceStructurePropertyMetadata {
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

            string IPresentationLayerStructure.IMember.RenderDeclaringCSharp(CodeRenderingContext ctx) {
                return $$"""
                    {{NijoAttr.RenderAttributeValues(ctx, _member)}}
                    public {{GetTypeName(E_CsTs.CSharp)}} {{PhysicalName}} { get; set; } = new();
                    """;
            }

            string IPresentationLayerStructure.IMember.RenderDeclaringTypeScript() {
                var refTo = new Entry(_member.RefTo);

                return $$"""
                    {{PhysicalName}}: {{refTo.TsTypeName}}
                    """;
            }
        }

        /// <summary>
        /// Child
        /// </summary>
        internal class RefDisplayDataChildMember : DisplayDataRefBase, IPresentationLayerStructure.IMember, IInstanceStructurePropertyMetadata {
            internal RefDisplayDataChildMember(ChildAggregate member) : base(member) {
                _member = member;
            }
            private readonly ChildAggregate _member;

            public string PhysicalName => _member.PhysicalName;
            public string DisplayName => _member.DisplayName;
            public string GetTypeName(E_CsTs csts) => csts == E_CsTs.CSharp
                ? $"{_member.PhysicalName}RefTargetVia{_member.PreviousNode!.XElement.Name.LocalName.ToCSharpSafe()}"
                : $"{_member.PhysicalName}RefTargetVia{_member.PreviousNode!.XElement.Name.LocalName.ToCSharpSafe()}";
            public override string CsClassName => GetTypeName(E_CsTs.CSharp);
            public override string TsTypeName => throw new InvalidOperationException("このオブジェクトはTSの名前つきの型ではない");

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _member;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;

            string IPresentationLayerStructure.IMember.RenderDeclaringCSharp(CodeRenderingContext ctx) {
                return $$"""
                    {{NijoAttr.RenderAttributeValues(ctx, _member)}}
                    public {{GetTypeName(E_CsTs.CSharp)}} {{PhysicalName}} { get; set; } = new();
                    """;
            }

            string IPresentationLayerStructure.IMember.RenderDeclaringTypeScript() {
                return $$"""
                    {{PhysicalName}}: {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderDeclaringTypeScript())}}
                    """)}}
                    }
                    """;
            }
        }

        /// <summary>
        /// Children。
        /// 参照先のChildrenを生成すると検索処理のSQLが複雑になりすぎて発行できないことがあるので
        /// オプションで明示的に生成するよう指定が無い限りは生成されない
        /// </summary>
        internal class RefDisplayDataChildrenMember : DisplayDataRefBase, IPresentationLayerStructure.IMember, IInstanceStructurePropertyMetadata {
            internal RefDisplayDataChildrenMember(ChildrenAggregate member) : base(member) {
                ChildrenAggregate = member;
            }
            internal ChildrenAggregate ChildrenAggregate { get; }

            public string PhysicalName => ChildrenAggregate.PhysicalName;
            public string DisplayName => ChildrenAggregate.DisplayName;
            public string GetTypeName(E_CsTs csts) => $"{ChildrenAggregate.PhysicalName}RefTargetVia{ChildrenAggregate.PreviousNode!.XElement.Name.LocalName.ToCSharpSafe()}";
            public override string CsClassName => GetTypeName(E_CsTs.CSharp);
            public override string TsTypeName => throw new InvalidOperationException("このオブジェクトはTSの名前つきの型ではない");

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => ChildrenAggregate;
            bool IInstanceStructurePropertyMetadata.IsArray => true;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;

            string IPresentationLayerStructure.IMember.RenderDeclaringCSharp(CodeRenderingContext ctx) {
                return $$"""
                    {{NijoAttr.RenderAttributeValues(ctx, ChildrenAggregate)}}
                    public List<{{GetTypeName(E_CsTs.CSharp)}}> {{PhysicalName}} { get; set; } = [];
                    """;
            }

            string IPresentationLayerStructure.IMember.RenderDeclaringTypeScript() {
                return $$"""
                    {{PhysicalName}}: {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderDeclaringTypeScript())}}
                    """)}}
                    }[]
                    """;
            }
        }

        /// <summary>
        /// Parent
        /// </summary>
        internal class RefDisplayDataParentMember : DisplayDataRefBase, IPresentationLayerStructure.IMember, IInstanceStructurePropertyMetadata {
            internal RefDisplayDataParentMember(AggregateBase parent) : base(parent) {
                _parent = parent;
            }
            private readonly AggregateBase _parent;

            public string PhysicalName => "Parent";
            public string DisplayName => _parent.DisplayName;
            public string GetTypeName(E_CsTs csts) => $"{_parent.PhysicalName}RefTargetVia{_parent.PreviousNode!.XElement.Name.LocalName.ToCSharpSafe()}";
            public override string CsClassName => GetTypeName(E_CsTs.CSharp);
            public override string TsTypeName => throw new InvalidOperationException("このオブジェクトはTSの名前つきの型ではない");

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _parent;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => PhysicalName;

            string IPresentationLayerStructure.IMember.RenderDeclaringCSharp(CodeRenderingContext ctx) {
                return $$"""
                    {{NijoAttr.RenderAttributeValues(ctx, _parent)}}
                    public {{GetTypeName(E_CsTs.CSharp)}} {{PhysicalName}} { get; set; } = new();
                    """;
            }

            string IPresentationLayerStructure.IMember.RenderDeclaringTypeScript() {
                return $$"""
                    {{PhysicalName}}: {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderDeclaringTypeScript())}}
                    """)}}
                    }
                    """;
            }
        }
        #endregion Entry以外のメンバー
    }
}
