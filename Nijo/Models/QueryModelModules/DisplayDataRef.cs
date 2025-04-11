using Nijo.CodeGenerating;
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
        internal static (Entry[] Entries, RefDisplayDataParentMember[] ParentMembers) GetReferedMembersRecursively(RootAggregate rootAggregate) {
            var tree = rootAggregate
                .EnumerateThisAndDescendants()
                .ToArray();

            // エントリー。ほかの集約から参照されている場合のみレンダリングする
            var entries = tree
                .Where(agg => agg.GetRefFroms().Any())
                .Select(agg => new Entry(agg))
                .ToArray();

            // エントリーに含まれる集約の祖先はAsParentをレンダリングする
            var parentMembers = tree
                .Where(agg => entries.Any(entry => agg.IsAncestorOf(entry.Aggregate)))
                .Select(agg => new RefDisplayDataParentMember(agg))
                .ToArray();

            return (entries, parentMembers);
        }

        #region レンダリング
        /// <summary>
        /// 他の集約から参照されているもののみ再帰的にレンダリングする
        /// </summary>
        internal static string RenderCSharpRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var (entries, parentMembers) = GetReferedMembersRecursively(rootAggregate);

            return $$"""
                #region 他の集約から参照されるときの画面表示用オブジェクト
                {{entries.SelectTextTemplate(entry => $$"""
                {{entry.RenderCsClass(ctx)}}

                """)}}
                {{parentMembers.SelectTextTemplate(parent => $$"""
                {{parent.RenderCsClass()}}

                """)}}
                #endregion 他の集約から参照されるときの画面表示用オブジェクト
                """;
        }
        /// <summary>
        /// 他の集約から参照されているもののみ再帰的にレンダリングする
        /// </summary>
        internal static string RenderTypeScriptRecursively(RootAggregate rootAggregate) {
            var refDisp = new Entry(rootAggregate);

            return $$"""
                /**
                 * {{rootAggregate.DisplayName}}が他の集約から外部参照されるときの型
                 */
                export type {{refDisp.TsTypeName}} = {
                  // TODO ver.1
                }
                """;
        }

        internal static string RenderTypeScriptObjectCreationFunctionRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var (entries, _) = GetReferedMembersRecursively(rootAggregate);

            return $$"""
                #region 他の集約から参照されるときの画面表示用オブジェクトの新規作成関数
                {{entries.SelectTextTemplate(entry => $$"""
                {{entry.RenderTypeScriptObjectCreationFunction(ctx)}}

                """)}}
                #endregion 他の集約から参照されるときの画面表示用オブジェクトの新規作成関数
                """;
        }
        #endregion レンダリング


        /// <summary>
        /// C#のクラスが存在するものたち
        /// </summary>
        internal abstract class RefDisplayDataMemberContainer {
            private protected RefDisplayDataMemberContainer(AggregateBase aggregate) {
                _aggregate = aggregate;
            }
            private protected AggregateBase _aggregate;

            internal abstract string CsClassName { get; }

            /// <summary>
            /// 直近の子を列挙する
            /// </summary>
            internal IEnumerable<IRefDisplayDataMember> GetMembers() {
                var parent = _aggregate.GetParent();
                if (parent != null) {
                    yield return new RefDisplayDataParentMember(parent);
                }
                foreach (var member in _aggregate.GetMembers()) {
                    if (member is ValueMember vm) {
                        yield return new RefDisplayDataValueMember(vm);

                    } else if (member is RefToMember refTo) {
                        yield return new RefDisplayDataRefToMember(refTo);

                    } else if (member is ChildAggreagte child) {
                        yield return new RefDisplayDataChildMember(child);

                    } else if (member is ChildrenAggreagte children) {
                        yield return new RefDisplayDataChildrenMember(children);
                    }
                }
            }
        }

        /// <summary>
        /// エントリー。エントリーが子孫要素になる場合もある。
        /// </summary>
        internal class Entry : RefDisplayDataMemberContainer {
            internal Entry(AggregateBase aggregate) : base(aggregate) { }

            internal AggregateBase Aggregate => _aggregate;
            internal override string CsClassName => $"{_aggregate.PhysicalName}RefTarget";
            internal string TsTypeName => $"{_aggregate.PhysicalName}RefTarget";

            internal string RenderCsClass(CodeRenderingContext ctx) {
                return $$"""
                    /// <summary>
                    /// {{_aggregate.DisplayName}}が他の集約から外部参照されるときの型
                    /// </summary>
                    public partial class {{CsClassName}} {
                        // TODO ver.1
                    }
                    """;
            }

            #region TypeScript側オブジェクト新規作成関数
            public string TsNewObjectFunction => $"createNew{TsTypeName}";
            internal string RenderTypeScriptObjectCreationFunction(CodeRenderingContext ctx) {
                return $$"""
                    /**
                     * {{_aggregate.DisplayName}}が他の集約から外部参照されるときのオブジェクトを新規作成します。
                     */
                    export const {{TsNewObjectFunction}} = (): {{TsTypeName}} => ({
                      // TODO ver.1
                    })
                    """;
            }
            #endregion TypeScript側オブジェクト新規作成関数
        }


        #region 子孫メンバー
        /// <summary>
        /// <see cref="DisplayDataRef"/>のメンバー
        /// </summary>
        internal interface IRefDisplayDataMember {
            string PhysicalName { get; }
            string DisplayName { get; }
            string CsType { get; }
        }

        /// <summary>
        /// ValueMember
        /// </summary>
        internal class RefDisplayDataValueMember : IRefDisplayDataMember {
            internal RefDisplayDataValueMember(ValueMember member) {
                Member = member;
            }
            public ValueMember Member { get; }

            public string PhysicalName => Member.PhysicalName;
            public string DisplayName => Member.DisplayName;
            public string CsType => Member.Type.CsDomainTypeName;
        }

        /// <summary>
        /// Ref
        /// </summary>
        internal class RefDisplayDataRefToMember : Entry, IRefDisplayDataMember {
            internal RefDisplayDataRefToMember(RefToMember member) : base(member.RefTo) {
                _member = member;
            }
            private readonly RefToMember _member;

            public string PhysicalName => _member.PhysicalName;
            public string DisplayName => _member.DisplayName;
            public string CsType => CsClassName;
        }

        /// <summary>
        /// Child
        /// </summary>
        internal class RefDisplayDataChildMember : RefDisplayDataMemberContainer, IRefDisplayDataMember {
            internal RefDisplayDataChildMember(ChildAggreagte member) : base(member) {
                _member = member;
            }
            private readonly ChildAggreagte _member;

            public string PhysicalName => _member.PhysicalName;
            public string DisplayName => _member.DisplayName;
            public string CsType => $"{_member.PhysicalName}RefTargetAsNotEntry";
            internal override string CsClassName => CsType;
        }

        /// <summary>
        /// Children
        /// </summary>
        internal class RefDisplayDataChildrenMember : RefDisplayDataMemberContainer, IRefDisplayDataMember {
            internal RefDisplayDataChildrenMember(ChildrenAggreagte member) : base(member) {
                _member = member;
            }
            private readonly ChildrenAggreagte _member;

            public string PhysicalName => _member.PhysicalName;
            public string DisplayName => _member.DisplayName;
            public string CsType => $"{_member.PhysicalName}RefTargetAsNotEntry";
            internal override string CsClassName => CsType;
        }

        /// <summary>
        /// Parent
        /// </summary>
        internal class RefDisplayDataParentMember : RefDisplayDataMemberContainer, IRefDisplayDataMember {
            internal RefDisplayDataParentMember(AggregateBase parent) : base(parent) {
                _parent = parent;
            }
            private readonly AggregateBase _parent;

            public string PhysicalName => "Parent";
            public string DisplayName => _parent.DisplayName;
            public string CsType => $"{_parent.PhysicalName}RefTargetAsParent";
            internal override string CsClassName => CsType;

            internal string RenderCsClass() {
                return $$"""
                    public partial class {{CsClassName}} {
                        // TODO ver.1
                    }
                    """;
            }
        }
        #endregion 子孫メンバー
    }
}
