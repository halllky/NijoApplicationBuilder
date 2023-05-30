using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.ReactAndWebApi {
    partial class ReactComponent : ITemplate {
        internal static IEnumerable<ReactComponent> All(CodeRenderingContext ctx) {
            return ctx.Schema
                .ToEFCoreGraph()
                .RootEntities()
                .Select(dbEntity => new ReactComponent(dbEntity, ctx));
        }

        internal ReactComponent(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
            _ctx = ctx;
            Aggregate = dbEntity.Item.Aggregate;
            _dbEntity = dbEntity;
            _searchCondition = new SearchCondition(dbEntity);
            _searchResult = new SearchResult(dbEntity);
            _controller = new Controller(dbEntity, ctx);
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<EFCoreEntity> _dbEntity;
        private readonly SearchCondition _searchCondition;
        private readonly SearchResult _searchResult;
        private readonly Controller _controller;

        internal GraphNode<Aggregate> Aggregate { get; }

        public string FileName => $"{Aggregate.Item.DisplayName.ToFileNameSafe()}.tsx";
        internal string ImportName => Path.GetFileNameWithoutExtension(FileName);

        internal string MultiViewComponentName => $"MultiView{Aggregate.Item.DisplayName.ToCSharpSafe()}";
        internal string CreateViewComponentName => $"CreateView{Aggregate.Item.DisplayName.ToCSharpSafe()}";
        internal string SingleViewComponentName => $"SingleView{Aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string UseQueryKey => Aggregate.Item.UniqueId;

        internal string SubDomain => Aggregate.Item.DisplayName.ToCSharpSafe();
        internal string MultiViewUrl => $"/{SubDomain}";
        internal string CreateViewUrl => $"/{SubDomain}/new";
        internal string SingleViewUrl => $"/{SubDomain}/detail";

        private void RenderSearchCondition() {
            foreach (var member in _searchCondition.GetMembers()) {
                var renderer = new SearchConditionRenderer(member);
                foreach (var line in member.Type.UserInterface(renderer)) {
                    WriteLine(line);
                }
            }
        }
        private void RenderCreateViewContents() {
            RenderSingleViewContents();
        }
        private void RenderSingleViewContents() {
            foreach (var member in _searchCondition.GetMembers()) {
                var renderer = new ContentsDetailRenderer(member);
                foreach (var line in member.Type.UserInterface(renderer)) {
                    WriteLine(line);
                }
            }
        }

        /// <summary>
        /// 検索条件
        /// </summary>
        private class SearchConditionRenderer : IGuiForm {
            public SearchConditionRenderer(SearchCondition.Member member) {
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
                        yield return $"<textarea className=\"border\" {{...register('{from}')}}></textarea>";
                        yield return $"〜";
                        yield return $"<textarea className=\"border\" {{...register('{to}')}}></textarea>";

                    } else {
                        yield return $"<input type=\"text\" className=\"border\" {{...register('{from}')}} />";
                        yield return $"〜";
                        yield return $"<input type=\"text\" className=\"border\" {{...register('{to}')}} />";
                    }

                } else {
                    var name = GetRegisterName();
                    if (multiline)
                        yield return $"<textarea className=\"border\" {{...register('{name}')}}></textarea>";
                    else
                        yield return $"<input type=\"text\" className=\"border\" {{...register('{name}')}} />";
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
                    yield return $"    <option selected=\"selected\" value=\"{opt.Key}\">";
                    yield return $"        {opt.Value}";
                    yield return $"    </option>";
                }
                yield return $"</select>";
            }
        }

        /// <summary>
        /// Createビュー兼シングルビュー
        /// </summary>
        private class ContentsDetailRenderer : IGuiForm {

            public ContentsDetailRenderer(SearchCondition.Member member) {
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
            /// Createビュー兼シングルビュー: テキストボックス
            /// </summary>
            public IEnumerable<string> TextBox(bool multiline = false) {
                var name = GetRegisterName();
                if (multiline)
                    yield return $"<textarea className=\"border\" {{...register('{name}')}}></textarea>";
                else
                    yield return $"<input type=\"text\" className=\"border\" {{...register('{name}')}} />";
            }

            /// <summary>
            /// Createビュー兼シングルビュー: トグル
            /// </summary>
            public IEnumerable<string> Toggle() {
                var name = GetRegisterName();
                yield return $"<input type=\"checkbox\" className=\"border\" {{...register('{name}')}} />";
            }

            /// <summary>
            /// Createビュー兼シングルビュー: 選択肢（コード自動生成時に要素が確定しているもの）
            /// </summary>
            public IEnumerable<string> Selection() {
                // TODO: enumの値をスキーマから取得する
                var options = new List<KeyValuePair<string, string>>();
                if (_member.CorrespondingDbMember.RequiredAtDB)
                    options.Add(KeyValuePair.Create("", ""));

                // TODO: RegisterNameを使っていない
                yield return $"<select className=\"border\">";
                foreach (var opt in options) {
                    yield return $"    <option selected=\"selected\" value=\"{opt.Key}\">";
                    yield return $"        {opt.Value}";
                    yield return $"    </option>";
                }
                yield return $"</select>";
            }
        }
    }
}
