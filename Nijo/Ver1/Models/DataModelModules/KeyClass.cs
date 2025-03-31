using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nijo.Ver1.Models.DataModelModules;

/// <summary>
/// 集約のキー部分のみの情報
/// </summary>
internal static class KeyClass {

    /// <summary>
    /// キーのエントリー。子孫集約になることもある。
    /// </summary>
    internal class KeyClassEntry {
        internal KeyClassEntry(AggregateBase aggregate) {
            _aggregate = aggregate.AsEntry();
        }
        private readonly AggregateBase _aggregate;

        internal string ClassName => $"{_aggregate.PhysicalName}Key";

        private IEnumerable<IKeyClassMember> GetMembers() {
            var p = _aggregate.GetParent();
            if (p != null) {
                yield return new KeyClassParentMember(p);
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
                .EnumerateThisAndDescendants()
                .ToArray();

            // キーのエントリー。ほかの集約から参照されている場合のみレンダリングする
            var entries = tree
                .Where(agg => agg.GetRefFroms().Any())
                .Select(agg => new KeyClassEntry(agg))
                .ToArray();

            // entriesに含まれる集約の祖先はAsParentをレンダリングする
            var parentMembers = tree
                .Where(agg => entries.Any(entry => agg.IsAncestorOf(entry._aggregate)))
                .Select(agg => new KeyClassParentMember(agg))
                .ToArray();

            return $$"""
                #region キー項目のみのオブジェクト
                {{entries.SelectTextTemplate(entry => $$"""
                {{entry.RenderDeclaring()}}

                """)}}
                {{parentMembers.SelectTextTemplate(parent => $$"""
                {{parent.RenderDeclaring()}}

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
                {{GetMembers().SelectTextTemplate(m => $$"""
                    /// <summary>{{m.DisplayName}}</summary>
                    public required {{m.CsType}}? {{m.PhysicalName}} { get; set; }
                """)}}
                }
                """;
        }
    }


    #region メンバー
    internal interface IKeyClassMember {
        string PhysicalName { get; }
        string DisplayName { get; }
        string CsType { get; }
    }
    /// <summary>
    /// キー情報の値メンバー
    /// </summary>
    internal class KeyClassValueMember : IKeyClassMember {
        internal KeyClassValueMember(ValueMember vm) {
            _vm = vm;
        }
        private readonly ValueMember _vm;

        public string PhysicalName => _vm.PhysicalName;
        public string DisplayName => _vm.DisplayName;
        public string CsType => _vm.Type.CsDomainTypeName;
    }
    /// <summary>
    /// キー情報の中に出てくる他の集約のキー
    /// </summary>
    internal class KeyClassRefMember : IKeyClassMember {
        internal KeyClassRefMember(RefToMember refTo) {
            _refTo = refTo;
            _refToKey = new KeyClassEntry(refTo.RefTo);
        }
        private readonly RefToMember _refTo;
        private readonly KeyClassEntry _refToKey;

        public string PhysicalName => _refTo.PhysicalName;
        public string DisplayName => _refTo.DisplayName;
        public string CsType => _refToKey.ClassName;
    }
    /// <summary>
    /// 子孫のキー情報の中に出てくる親集約のキー。
    /// </summary>
    internal class KeyClassParentMember : KeyClassEntry, IKeyClassMember {
        internal KeyClassParentMember(AggregateBase parent) : base(parent) {
            _parent = parent;
            //_keyClassEntry = keyClassEntry;
        }
        private readonly AggregateBase _parent;
        //private readonly AggregateBase _keyClassEntry;

        public string PhysicalName => "Parent";
        public string DisplayName => _parent.DisplayName;

        public string CsType => $"{_parent}KeyAsNotEntry";
        //internal override string CsType {
        //    get {
        //        if (_csTypeCache == null) {
        //            var list = new List<string> {
        //                $"{_keyClassEntry.PhysicalName}Key"
        //            };
        //            foreach (var node in _parent.GetFullPath()) {
        //                if (node is AggregateBase curr
        //                 && node.PreviousNode is AggregateBase prev
        //                 && curr.IsParentOf(prev)) {
        //                    list.Add("Parent");

        //                } else {
        //                    list.Add(node.XElement.Name.LocalName);
        //                }
        //            }
        //            _csTypeCache = list.Join("の");
        //        }
        //        return _csTypeCache;
        //    }
        //}
        //private string? _csTypeCache;
    }
    #endregion メンバー
}
