using Nijo.Parts.WebServer;
using Nijo.Parts.Utility;
using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;
using Nijo.Parts;

namespace Nijo.Features.Storing {
    internal class KeywordSearchingFeature {
        internal KeywordSearchingFeature(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        private string GetActionName() {
            return _aggregate.IsRoot()
                ? $"list-by-keyword"
                : $"list-by-keyword-{_aggregate.Item.UniqueId}";
        }
        internal string GetUri() {
            var controller = GetController();
            return $"/{controller.SubDomain}/{GetActionName()}";
        }

        private Controller GetController() {
            var root = _aggregate.GetRoot();
            return new Controller(root.Item);
        }

        internal string AppServiceMeghodName => $"SearchByKeyword{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        private const int LIST_BY_KEYWORD_MAX = 100;

        internal string RenderController() {
            var controller = GetController();
            return $$"""
                /// <summary>
                /// 既存の{{_aggregate.Item.DisplayName}}をキーワードで一覧検索する Web API
                /// </summary>
                [HttpGet("{{GetActionName()}}")]
                public virtual IActionResult SearchByKeyword{{_aggregate.Item.UniqueId}}([FromQuery] string? keyword) {
                    var items = _applicationService.{{AppServiceMeghodName}}(keyword);
                    return this.JsonContent(items);
                }
                """;
        }

        internal string RenderAppSrvMethod() {
            var appSrv = new ApplicationService();
            var returnType = new DataClassForDisplayRefTarget(_aggregate);
            const string LIKE = "like";

            // キーワード検索は子孫集約のDbEntityが基点になる
            var entry = _aggregate.AsEntry();

            var keyName = new DataClassForSaveRefTarget(_aggregate);
            var filterColumns = entry
                .GetKeys()
                .Union(entry.GetNames())
                .OfType<AggregateMember.ValueMember>()
                .Select(m => m.Declared.GetFullPath().Join(".") + (m.CSharpTypeName == "string" ? "" : ".ToString()"));
            var orderColumn = entry
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Select(m => m.Declared.GetFullPath().Join("."))
                .First();

            string RenderKeyNameConvertingRecursively(GraphNode<Aggregate> agg) {
                var keyNameClass = new DataClassForSaveRefTarget(agg);
                return keyNameClass
                    .GetOwnMembers()
                    .Where(m => m.Owner == agg)
                    .SelectTextTemplate(m => m is AggregateMember.ValueMember vm ? $$"""
                        {{m.MemberName}} = e.{{vm.GetFullPath().Join(".")}},
                        """ : $$"""
                        {{m.MemberName}} = new {{new DataClassForSaveRefTarget(((AggregateMember.RelationMember)m).MemberAggregate).CSharpClassName}}() {
                            {{WithIndent(RenderKeyNameConvertingRecursively(((AggregateMember.RelationMember)m).MemberAggregate), "    ")}}
                        },
                        """);
            }

            var names = entry
                .GetNames()
                .OfType<AggregateMember.ValueMember>()
                .ToArray();

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}をキーワードで検索します。
                /// </summary>
                public virtual IEnumerable<{{returnType.CsClassName}}> {{AppServiceMeghodName}}(string? keyword) {
                    var query = (IQueryable<{{_aggregate.Item.EFCoreEntityClassName}}>){{appSrv.DbContext}}.{{_aggregate.Item.DbSetName}};

                    if (!string.IsNullOrWhiteSpace(keyword)) {
                        var {{LIKE}} = $"%{keyword.Trim().Replace("%", "\\%")}%";
                        query = query.Where(item => {{WithIndent(filterColumns.SelectTextTemplate(path => $"EF.Functions.Like(item.{path}, {LIKE})"), "                                 || ")}});
                    }

                    var results = query
                        .OrderBy(m => m.{{orderColumn}})
                        .Take({{LIST_BY_KEYWORD_MAX + 1}})
                        .AsEnumerable()
                        .Select(entity => {{returnType.CsClassName}}.{{DataClassForDisplayRefTarget.FROM_DBENTITY}}(entity));

                    return results;
                }
                """;
        }
    }
}
