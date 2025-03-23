using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.Parts.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.Common {
    internal class ComplexPost {
        internal const string REQUEST_CS = "ComplexPostRequest";
        internal const string REQUEST_TS = "ComplexPostRequest";
        internal const string OPTIONS_CS = "ComplexPostOptions";
        internal const string OPTIONS_TS = "ComplexPostOptions";
        internal const string RESULT_CS = "ComplexPostResult";
        internal const string RESULT_TS = "ComplexPostResult";

        internal static SourceFile RenderCSharp(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "ComplexPost.cs",
                Contents = $$"""
                    using Microsoft.AspNetCore.Mvc.ModelBinding;
                    using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
                    using System;
                    using System.Text.Json.Serialization;

                    namespace {{ctx.Config.RootNamespace}};

                    public class {{REQUEST_CS}}<T> {
                        [JsonPropertyName("data")]
                        public T Data { get; set; }
                        [JsonPropertyName("{{OPTIONS_TS}}")]
                        public {{OPTIONS_CS}} Options { get; set; } = new();
                    }

                    public class {{OPTIONS_CS}} {
                        [JsonPropertyName("ignoreConfirm")]
                        public bool IgnoreConfirm { get; set; }
                    }

                    public class {{RESULT_CS}} : IActionResult {
                        public {{RESULT_CS}}({{PresentationContext.RESULT}} presentationContextResult) {
                            _presentationContextResult = presentationContextResult;
                            _returnValue = null;
                        }
                        public {{RESULT_CS}}({{PresentationContext.RESULT}} presentationContextResult, object returnValue) {
                            _presentationContextResult = presentationContextResult;
                            _returnValue = returnValue;
                        }
                        private readonly {{PresentationContext.RESULT}} _presentationContextResult;
                        private readonly object? _returnValue;
                    }


                    {{RenderModelBinder()}}
                    """,
            };
        }

        internal static SourceFile RenderTypeScript(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "complex-post.ts",
                Contents = $$"""
                    export type {{REQUEST_TS}}<T> = {
                      options: {{OPTIONS_TS}}
                      data: T
                    }

                    export type {{OPTIONS_TS}} = {
                      ignoreConfirm: boolean
                    }
                    """,
            };
        }


        #region ASP.NET Core のバインディング設定
        private const string MODEL_BINDER_BASE = "ComplexPostModelBinderBase";
        private const string PARSE_AS_COMPLEX_POST_REQUEST = "ParseAsComplexPostRequest";

        internal static void RegisterWebapiConfiguration(IMultiAggregateSourceFileManager ctx) {
            ctx.Use<ApplicationConfigure>()
                .AddControllers(option => $$"""
                    {{option}}.ModelBinderProviders.Add(new {{MODEL_BINDER_BASE}}(this));
                    """)
                .AddWebapiMethod($$"""
                    /// <summary>
                    /// HTTPリクエストの内容を読み取り{{REQUEST_CS}}型として返す。
                    /// 具体的な処理はアプリケーション毎に異なるため抽象クラスとしている。
                    /// </summary>
                    /// <param name="bindingContext">ASP.NET Core が提供するモデルバインディングの仕組み</param>
                    /// <typeparam name="TParameter">パラメータのデータ型</typeparam>
                    /// <returns>{{REQUEST_CS}}のインスタンス</returns>
                    public abstract {{REQUEST_CS}}<TParameter> {{PARSE_AS_COMPLEX_POST_REQUEST}}<TParameter>(ModelBindingContext bindingContext);
                    """);
        }
        private static string RenderModelBinder() {
            return $$"""
                /// <summary>
                /// ASP.NET Core が <see cref="{{REQUEST_CS}}"/> 型のHTTPリクエストを解釈する処理の実装。
                /// </summary>
                public abstract class {{MODEL_BINDER_BASE}} : IModelBinderProvider {
                    {{MODEL_BINDER_BASE}}({{ApplicationConfigure.ABSTRACT_CLASS_WEBAPI}} configure) {
                        _configure = configure;
                    }
                    private readonly {{ApplicationConfigure.ABSTRACT_CLASS_WEBAPI}} _configure;

                    /// <summary>
                    /// ControllerのActionのパラメータが、このクラスで処理すべき型か否かを判定し、
                    /// 処理対象であれば（つまりこの場合だと{{REQUEST_CS}}型であれば）そのインスタンスを返す。
                    /// 処理対象でなければnullを返す。
                    /// </summary>
                    public IModelBinder? GetBinder(ModelBinderProviderContext context) {
                        if (!context.Metadata.ModelType.IsGenericType) return null;

                        var genericType = context.Metadata.ModelType.GetGenericTypeDefinition();
                        if (genericType != typeof({{REQUEST_CS}}<>)) return null;

                        var paramType = context.Metadata.ModelType.GetGenericArguments()[0];
                        var binderType = typeof(ModelBinderImpl<>).MakeGenericType(paramType);
                        var binder = Activator.CreateInstance(binderType, [_configure]);
                        return binder;
                    }

                    private class ModelBinderImpl<TParameter> : IModelBinder {
                        public ModelBinderImpl({{ApplicationConfigure.ABSTRACT_CLASS_WEBAPI}} configure) {
                            _configure = configure;
                        }
                        private readonly {{ApplicationConfigure.ABSTRACT_CLASS_WEBAPI}} _configure;

                        public Task BindModelAsync(ModelBindingContext bindingContext) {
                            bindingContext.Result = ModelBindingResult.Success(_configure.{{PARSE_AS_COMPLEX_POST_REQUEST}}<TParameter>(bindingContext));
                            return Task.CompletedTask;
                        }
                    }
                }
                """;
        }
        #endregion ASP.NET Core のバインディング設定
    }
}
