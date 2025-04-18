using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        internal class KeyClassEntry : IKeyClassMember, IKeyClassStructure, SaveCommand.ISaveCommandMember, IInstanceStructurePropertyMetadata {
            internal KeyClassEntry(AggregateBase aggregate) {
                _aggregate = aggregate;
            }
            private readonly AggregateBase _aggregate;

            internal string ClassName => $"{_aggregate.PhysicalName}Key";
            string IKeyClassMember.CsType => ClassName;

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

            /// <summary>
            /// 子孫のキークラス定義も含めて全部レンダリング
            /// </summary>
            internal static string RenderClassDeclaringRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {

                var tree = rootAggregate
                    .AsEntry()
                    .EnumerateThisAndDescendants()
                    .ToArray();

                // キーのエントリー。ほかの集約から参照されている場合のみレンダリングする
                var entries = tree
                    .Where(agg => agg.GetRefFroms().Any())
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
                        public required {{m.CsType}}? {{m.PhysicalName}} { get; set; }
                    """)}}
                    }
                    """;
            }

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _aggregate;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;
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
            string CsType { get; }
        }
        /// <summary>
        /// キー情報の値メンバー
        /// </summary>
        internal class KeyClassValueMember : SaveCommand.SaveCommandValueMember, IKeyClassMember {
            internal KeyClassValueMember(ValueMember vm) : base(vm) { }

            public string CsType => Member.Type.CsDomainTypeName;

            ISchemaPathNode SaveCommand.ISaveCommandMember.Member => Member;
            string SaveCommand.ISaveCommandMember.CsCreateType => CsType;
            string SaveCommand.ISaveCommandMember.CsUpdateType => CsType;
            string SaveCommand.ISaveCommandMember.CsDeleteType => CsType;
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
            public string CsType => MemberKeyClassEntry.ClassName;

            ISchemaPathNode SaveCommand.ISaveCommandMember.Member => Member;
            string SaveCommand.ISaveCommandMember.CsCreateType => CsType;
            string SaveCommand.ISaveCommandMember.CsUpdateType => CsType;
            string SaveCommand.ISaveCommandMember.CsDeleteType => CsType;

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

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Member;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetOwnMembers();
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
