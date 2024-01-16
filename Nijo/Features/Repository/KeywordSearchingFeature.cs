using Nijo.Architecture.WebServer;
using Nijo.Architecture.Utility;
using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using Nijo.Util.CodeGenerating;
using Nijo.Architecture;

namespace Nijo.Features.Repository {
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

        private Architecture.WebClient.Controller GetController() {
            var root = _aggregate.GetRoot();
            return new Architecture.WebClient.Controller(root.Item);
        }

        internal string AppServiceMeghodName => $"SearchByKeyword{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        private const int LIST_BY_KEYWORD_MAX = 100;

        internal string RenderController() {
            var controller = GetController();
            return $$"""
                [HttpGet("{{GetActionName()}}")]
                public virtual IActionResult SearchByKeyword{{_aggregate.Item.UniqueId}}([FromQuery] string? keyword) {
                    var items = _applicationService.{{AppServiceMeghodName}}(keyword);
                    return this.JsonContent(items);
                }
                """;
        }

        internal string RenderAppSrvMethod() {
            var appSrv = new ApplicationService();
            const string LIKE = "like";

            var keyName = new RefTargetKeyName(_aggregate);
            var filterColumns = _aggregate
                .GetKeys()
                .Union(_aggregate.GetNames())
                .OfType<AggregateMember.ValueMember>()
                .Select(m => m.Declared.GetFullPath(_aggregate).Join(".") + (m.CSharpTypeName == "string" ? "" : ".ToString()"));
            var orderColumn = _aggregate
                .AsEntry() // OrderByはSelect後のオブジェクトを子集約起点で辿るので
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Select(m => m.Declared.GetFullPath().Join("."))
                .First();

            string RenderKeyNameConvertingRecursively(GraphNode<Aggregate> agg) {
                var keyNameClass = new RefTargetKeyName(agg);
                return keyNameClass
                    .GetOwnMembers()
                    .Where(m => m.Owner == agg)
                    .SelectTextTemplate(m => m is AggregateMember.ValueMember vm ? $$"""
                        {{m.MemberName}} = e.{{vm.GetDbColumn().GetFullPath(_aggregate.As<IEFCoreEntity>()).Join(".")}},
                        """ : $$"""
                        {{m.MemberName}} = new {{new RefTargetKeyName(((AggregateMember.RelationMember)m).MemberAggregate).CSharpClassName}}() {
                            {{WithIndent(RenderKeyNameConvertingRecursively(((AggregateMember.RelationMember)m).MemberAggregate), "    ")}}
                        },
                        """);
            }

            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}をキーワードで検索します。
                /// </summary>
                public virtual IEnumerable<{{keyName.CSharpClassName}}> {{AppServiceMeghodName}}(string? keyword) {
                    var query = (IQueryable<{{_aggregate.Item.EFCoreEntityClassName}}>){{appSrv.DbContext}}.{{_aggregate.Item.DbSetName}};

                    if (!string.IsNullOrWhiteSpace(keyword)) {
                        var {{LIKE}} = $"%{keyword.Trim().Replace("%", "\\%")}%";
                        query = query.Where(item => {{WithIndent(filterColumns.SelectTextTemplate(path => $"EF.Functions.Like(item.{path}, {LIKE})"), "                                 || ")}});
                    }

                    var results = query
                        .Select(e => new {{keyName.CSharpClassName}} {
                            {{WithIndent(RenderKeyNameConvertingRecursively(_aggregate.AsEntry()), "            ")}}
                        })
                        .OrderBy(m => m.{{orderColumn}})
                        .Take({{LIST_BY_KEYWORD_MAX + 1}});

                    return results.AsEnumerable();
                }
                """;
        }
    }
}
