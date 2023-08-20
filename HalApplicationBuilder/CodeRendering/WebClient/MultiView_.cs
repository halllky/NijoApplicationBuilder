using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class MultiView : ITemplate {
        internal MultiView(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _ctx = ctx;
            _aggregate = aggregate;

            var dbEntity = aggregate.GetDbEntity().AsEntry();
            _searchCondition = new SearchCondition(dbEntity);
            _searchResult = new SearchResult(dbEntity);

            var memberNames = _searchCondition.GetMembers().Select(m => m.Name);
            PropNameWidth = AggregateInstanceFormBody.GetPropNameFlexBasis(memberNames);
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly SearchCondition _searchCondition;
        private readonly SearchResult _searchResult;

        public string FileName => "list.tsx";
        internal string Url => $"/{_aggregate.Item.UniqueId}";
        internal string Route => $"/{_aggregate.Item.UniqueId}";

        private string UseQueryKey => _aggregate.Item.UniqueId;

        private string GetCreateViewUrl() => new CreateView(_aggregate, _ctx).Url;
        private string GetSingleViewUrl() => new SingleView(_aggregate, _ctx).Url;
        private string GetSearchCommandApi() => new AggFile.Controller(_aggregate).SearchCommandApi;

        private string PropNameWidth { get; }

        private IEnumerable<string> RenderForm(SearchCondition.Member member) {
            var renderer = new FormRenderer(member);
            foreach (var line in member.Type.RenderUI(renderer)) yield return line;
        }

        private class FormRenderer : IGuiFormRenderer {
            public FormRenderer(SearchCondition.Member member) {
                _member = member;
            }
            private readonly SearchCondition.Member _member;

            private string GetRegisterName(string? inner = null) {
                var list = _member.Owner.EFCoreEntity
                    .PathFromEntry()
                    .Select(path => path.RelationName)
                    .ToList();
                list.Add(_member.Name);
                if (!string.IsNullOrEmpty(inner)) list.Add(inner);
                return list.Join(".");
            }

            /// <summary>
            /// 検索条件: テキストボックス
            /// </summary>
            public IEnumerable<string> TextBox(bool multiline = false) {
                if (_member.Type.SearchBehavior == SearchBehavior.Range) {
                    var from = GetRegisterName(Util.FromTo.FROM);
                    var to = GetRegisterName(Util.FromTo.TO);
                    if (multiline) {
                        yield return $"<textarea className=\"border w-40\" {{...register('{from}')}}></textarea>";
                        yield return $"〜";
                        yield return $"<textarea className=\"border w-40\" {{...register('{to}')}}></textarea>";

                    } else {
                        yield return $"<input type=\"text\" className=\"border w-40\" {{...register('{from}')}} />";
                        yield return $"〜";
                        yield return $"<input type=\"text\" className=\"border w-40\" {{...register('{to}')}} />";
                    }

                } else {
                    var name = GetRegisterName();
                    if (multiline)
                        yield return $"<textarea className=\"border w-80\" {{...register('{name}')}}></textarea>";
                    else
                        yield return $"<input type=\"text\" className=\"border w-80\" {{...register('{name}')}} />";
                }
            }
            /// <summary>
            /// 検索条件: トグル
            /// </summary>
            public IEnumerable<string> Toggle() {
                // TODO: "true only", "false only", "all" の3種類のラジオボタン
                yield break;
            }
            /// <summary>
            /// 検索条件: 選択肢（コード自動生成時に要素が確定しているもの）
            /// </summary>
            public IEnumerable<string> Selection() {
                // TODO: enumの値をスキーマから取得する
                var options = new List<KeyValuePair<string, string>>();
                options.Add(KeyValuePair.Create("", ""));

                // TODO: RegisterNameを使っていない
                yield return $"<select className=\"border\">";
                foreach (var opt in options) {
                    yield return $"  <option selected value=\"{opt.Key}\">";
                    yield return $"    {opt.Value}";
                    yield return $"  </option>";
                }
                yield return $"</select>";
            }
        }
    }
}
