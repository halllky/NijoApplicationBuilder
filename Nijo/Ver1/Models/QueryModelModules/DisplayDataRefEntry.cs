using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;

namespace Nijo.Ver1.Models.QueryModelModules {
    public class DisplayDataRefEntry {
        public DisplayDataRefEntry(AggregateBase aggregate) {
            _aggregate = aggregate;
        }

        private readonly AggregateBase _aggregate;

        public virtual string CsClassName => $"{_aggregate.PhysicalName}RefTarget";
        public virtual string TsTypeName => $"{_aggregate.PhysicalName}RefTarget";

        public IEnumerable<IMember> GetMembers() {
            var parent = _aggregate.GetParent();
            if (parent != null) {
                yield return new Descendant(parent, this, _aggregate);
            }
            throw new NotImplementedException();
        }

        internal string RenderCsClass(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
        internal string RenderTsType(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }

        #region TypeScript側オブジェクト新規作成関数
        public string TsNewObjectFunction => $"createNew{TsTypeName}";
        internal string RenderTypeScriptObjectCreationFunction(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
        #endregion TypeScript側オブジェクト新規作成関数


        #region QueryModelの通常のSearchResultからの変換
        internal string RenderConvertingFromSearchResult(string instance) {
            throw new NotImplementedException();
        }
        #endregion QueryModelの通常のSearchResultからの変換


        #region 子孫メンバー
        /// <summary>
        /// <see cref="DisplayDataRefEntry"/>のメンバー
        /// </summary>
        public interface IMember {
            string MemberName { get; }
        }

        /// <summary>
        /// <see cref="DisplayDataRefEntry"/>のメンバーのうち親集約または子集約または参照先
        /// </summary>
        private class Descendant : DisplayDataRefEntry, IMember {
            public Descendant(AggregateBase aggregate, DisplayDataRefEntry previous, AggregateBase refEntry) : base(aggregate) {
                _previous = previous;
                _refEntry = refEntry;
            }

            private readonly DisplayDataRefEntry _previous;
            private readonly AggregateBase _refEntry;

            public string MemberName => _previous._aggregate.GetParent() == _aggregate
                ? "Parent"
                : ((IRelationalMember)_aggregate).RelationPhysicalName;
            public override string CsClassName => $"{_refEntry.PhysicalName}RefTarget_{GetPathFromRefEntry(this).Join("の")}";
            public override string TsTypeName => $"{_refEntry.PhysicalName}RefTarget_{GetPathFromRefEntry(this).Join("の")}";
        }
        #endregion 子孫メンバー


        /// <summary>
        /// エントリーから引数の集約までのパスをRefTargetのルールに従って返す
        /// </summary>
        internal static IEnumerable<string> GetPathFromEntry(AggregateBase aggregate) {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Refエントリーから引数の集約までのパス
        /// </summary>
        internal static IEnumerable<string> GetPathFromRefEntry(DisplayDataRefEntry refDisplayData) {
            throw new NotImplementedException();
        }
    }
}
