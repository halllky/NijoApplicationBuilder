using Nijo.Core;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 詳細画面。新規モード・閲覧モード・編集モードの3種類をもつ。
    /// </summary>
    internal class SingleView : IReactPage {
        /// <summary>
        /// 新規モード・閲覧モード・編集モードのうちいずれか
        /// </summary>
        internal enum E_Type {
            New,
            ReadOnly,
            Edit,
        }

        internal SingleView(GraphNode<Aggregate> agg, E_Type type) {
            _aggregate = agg;
            _type = type;
        }
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly E_Type _type;

        public string Url {
            get {
                if (_type == E_Type.New) {
                    return $"/{_aggregate.Item.UniqueId}/new";

                } else {
                    // React Router は全角文字非対応なので key0, key1, ... をURLに使う
                    var urlKeys = _aggregate
                        .GetKeys()
                        .OfType<AggregateMember.ValueMember>()
                        .Select((_, i) => $":key{i}");

                    if (_type == E_Type.ReadOnly) {
                        return $"/{_aggregate.Item.UniqueId}/detail/{urlKeys.Join("/")}";

                    } else if (_type == E_Type.Edit) {
                        return $"/{_aggregate.Item.UniqueId}/edit/{urlKeys.Join("/")}";
                    } else {
                        throw new InvalidOperationException($"SingleViewの種類が不正: {_aggregate.Item}");
                    }
                }
            }
        }
        public string DirNameInPageDir => _aggregate.Item.DisplayName.ToFileNameSafe();
        public string ComponentPhysicalName => _type switch {
            E_Type.New => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}CreateView",
            E_Type.ReadOnly => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}DetailView",
            E_Type.Edit => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}EditView",
            _ => throw new NotImplementedException(),
        };
        public bool ShowMenu => false;
        public string? LabelInMenu => null;

        public SourceFile GetSourceFile() => new SourceFile {
            FileName = _type switch {
                E_Type.New => "new.tsx",
                E_Type.ReadOnly => "detail.tsx",
                E_Type.Edit => "edit.tsx",
                _ => throw new NotImplementedException(),
            },
            RenderContent = ctx => {
                return $$"""
                    export default function () {
                      return (
                        <div>
                          TODO #35 SingleView({{_type}})
                        </div>
                      )
                    }
                    """;
            },
        };

        public string GetUrlFnName => _type switch {
            E_Type.New => $"get{_aggregate.Item.PhysicalName}CreateViewUrl",
            _ => $"get{_aggregate.Item.PhysicalName}SingleViewUrl",
        };
        internal string RenderGetUrlFn(CodeRenderingContext context) {
            if (_type == E_Type.New) {
                return $$"""
                    export const {{GetUrlFnName}} = () => {
                      return `{{Url}}`
                    }
                    """;
            } else {
                var readView = new SingleView(_aggregate, E_Type.ReadOnly);
                var editView = new SingleView(_aggregate, E_Type.Edit);
                var dataClass = new DataClassForDisplay(_aggregate);
                var keys = _aggregate
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .Select(vm => new {
                        vm.MemberName,
                        Path = vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript),
                    })
                    .ToArray();
                return $$"""
                    export const {{GetUrlFnName}} = (obj: Types.{{dataClass.TsTypeName}}, to: 'readonly' | 'edit') => {
                    {{keys.SelectTextTemplate((k, i) => $$"""
                      const key{{i}} = obj.{{k.Path.Join("?.")}}
                    """)}}
                    {{keys.SelectTextTemplate((k, i) => $$"""
                      if (key{{i}} === undefined) throw new Error('{{k.MemberName}}が指定されていません。')
                    """)}}

                      return to === 'readonly'
                        ? `{{readView.Url}}/{{keys.Select((_, i) => $"/${{window.encodeURI(`${{key{i}}}`)}}").Join("")}}`
                        : `{{editView.Url}}/{{keys.Select((_, i) => $"/${{window.encodeURI(`${{key{i}}}`)}}").Join("")}}`
                    }
                    """;

            }
        }
    }
}
