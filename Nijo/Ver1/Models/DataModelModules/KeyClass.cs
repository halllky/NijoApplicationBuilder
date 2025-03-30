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

        /// <summary>
        /// 子孫のキークラス定義も含めて全部レンダリング
        /// </summary>
        internal string RenderClassDeclaringRecursively(CodeRenderingContext ctx) {
            if (_aggregate is not RootAggregate) throw new InvalidOperationException();

            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} のキー
                /// </summary>
                public sealed class {{ClassName}} {
                    // TODO ver.1
                }
                """;
        }
    }


    #region メンバー
    internal abstract class KeyClassMember {
        internal abstract string PhysicalName { get; }
        internal abstract string CsType { get; }
    }
    /// <summary>
    /// キー情報の値メンバー
    /// </summary>
    internal class KeyClassValueMember : KeyClassMember {
        internal KeyClassValueMember(ValueMember vm) {
            _vm = vm;
        }
        private readonly ValueMember _vm;

        internal override string PhysicalName => _vm.PhysicalName;
        internal override string CsType => _vm.Type.CsDomainTypeName;
    }
    /// <summary>
    /// キー情報の中に出てくる他の集約のキー
    /// </summary>
    internal class KeyClassRefMember : KeyClassMember {
        internal KeyClassRefMember(RefToMember refTo) {
            _refTo = refTo;
            _refToKey = new KeyClassEntry(refTo.RefTo);
        }
        private readonly RefToMember _refTo;
        private readonly KeyClassEntry _refToKey;

        internal override string PhysicalName => _refTo.PhysicalName;
        internal override string CsType => _refToKey.ClassName;
    }
    /// <summary>
    /// 子孫のキー情報の中に出てくる親集約のキー。
    /// </summary>
    internal class KeyClassParentMember : KeyClassMember {
        internal KeyClassParentMember(AggregateBase parent) {
            _parent = parent;
            //_keyClassEntry = keyClassEntry;
        }
        private readonly AggregateBase _parent;
        //private readonly AggregateBase _keyClassEntry;

        internal override string PhysicalName => "Parent";

        internal override string CsType => $"{_parent}KeyAsNotEntry";
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
        private string? _csTypeCache;
    }
    #endregion メンバー
}
