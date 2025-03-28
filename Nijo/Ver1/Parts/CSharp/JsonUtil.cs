using Nijo.Ver1.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.CSharp {
    /// <summary>
    /// C#側の既定のJSONシリアライズ設定に関する各種処理
    /// </summary>
    internal class JsonUtil : IMultiAggregateSourceFile {

        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }

        void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
            ctx.CoreLibrary(dir => {
                dir.Directory("Util", utilDir => {
                    utilDir.Generate(Render(ctx));
                });
            });
        }

        internal SourceFile Render(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "JsonUtil.cs",
                Contents = $$"""
                    using System.Text.Json;
                    using System.Text.Json.Nodes;
                    using System.Text.Json.Serialization;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// JSONシリアライズ設定に関する各種処理
                    /// </summary>
                    public static partial class JsonUtil {

                        /// <summary>
                        /// 既定のJSONシリアライズ設定を返します。
                        /// </summary>
                        public static JsonSerializerOptions GetDefaultJsonSerializerOptions() {
                            var option = new JsonSerializerOptions();

                            // 日本語文字などがUnicode変換されるのを避ける
                            option.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

                            // 値がnullの場合はレンダリングしない
                            option.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

                            // ValueObjectのJSONシリアライズ登録
                            // TODO ver.1

                            return option;
                        }

                        /// <summary>
                        /// 既定のJSONシリアライズ設定でこのインスタンスをJSON文字列に変換します。
                        /// </summary>
                        public static string ToJson<T>(this T obj, bool writeIndented = false) {
                            var options = GetDefaultJsonSerializerOptions();
                            options.WriteIndented = writeIndented;
                            return JsonSerializer.Serialize(obj, options);
                        }
                    }
                    """,
            };
        }
    }
}
