using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    /// <summary>
    /// 値オブジェクトメンバー。モデル定義は <see cref="Models.ValueObjectModel"/> で行っている。
    /// </summary>
    internal class ValueObjectMember : StringMemberType {
        public ValueObjectMember(string className, string primitiveType, E_SearchBehavior searchBehavior) {
            _className = className;
            _primitiveType = primitiveType;
            _searchBehavior = searchBehavior;
        }
        private readonly string _className;
        private readonly string _primitiveType;
        private readonly E_SearchBehavior _searchBehavior;

        protected override E_SearchBehavior SearchBehavior => _searchBehavior;

        public override string GetCSharpTypeName() {
            return _className;
        }

        // 検索の挙動定義。ほぼ基底クラス側のそれと同じだが、プリミティブ型への明示的なキャストが必要な点が異なる。
        private protected override string RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject) {
            var pathFromSearchCondition = searchConditionObject == E_SearchConditionObject.SearchCondition
                ? member.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
                : member.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.CSharp);
            var fullpathNullable = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{searchCondition}.{pathFromSearchCondition.Join(".")}";
            var whereFullpath = searchQueryObject == E_SearchQueryObject.SearchResult
                ? member.GetFullPathAsSearchResult(E_CsTs.CSharp, out var isArray)
                : member.GetFullPathAsDbEntity(E_CsTs.CSharp, out isArray);

            return $$"""
                if (!string.IsNullOrWhiteSpace({{fullpathNullable}})) {
                {{If(SearchBehavior == E_SearchBehavior.PartialMatch, () => $$"""
                    var escaped = "%" + {{fullpathNotNull}}.Trim().Replace("\\", "\\\\").Replace("%", "\\%") + "%";

                """).ElseIf(SearchBehavior == E_SearchBehavior.ForwardMatch, () => $$"""
                    var escaped = {{fullpathNotNull}}.Trim().Replace("\\", "\\\\").Replace("%", "\\%") + "%";

                """).ElseIf(SearchBehavior == E_SearchBehavior.BackwardMatch, () => $$"""
                    var escaped = "%" + {{fullpathNotNull}}.Trim().Replace("\\", "\\\\").Replace("%", "\\%");

                """).Else(() => $$"""
                    var escaped = {{fullpathNotNull}}.Trim().Replace("\\", "\\\\").Replace("%", "\\%");

                """)}}
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => EF.Functions.Like(({{_primitiveType}})y.{{member.MemberName}}, escaped, "\\")));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => EF.Functions.Like(({{_primitiveType}})x.{{whereFullpath.Join(".")}}, escaped, "\\"));
                """)}}
                }
                """;
        }


        public override string GetUiDisplayName() {
            return _className;
        }
        public override string GetHelpText() => $$"""
            {{_className}}。
            """;
        internal string RenderDummyDataValue(Random random) {
            return $"'{random.Next(0, 999999999):000000000}'";
        }
    }
}
