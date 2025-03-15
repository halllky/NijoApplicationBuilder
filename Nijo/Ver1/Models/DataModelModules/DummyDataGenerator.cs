using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.DataModelModules {
    internal class DummyDataGenerator : IMultiAggregateSourceFile {

        private readonly List<RootAggregate> _rootAggregates = [];

        internal DummyDataGenerator Add(RootAggregate rootAggregate) {
            _rootAggregates.Add(rootAggregate);
            return this;
        }

        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }

        void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
            ctx.CoreLibrary(dir => {
                dir.Directory("Util", utilDir => {
                    utilDir.Generate(RenderBulkInsertInterface(ctx));
                    utilDir.Generate(RenderDummyDataGenerator(ctx));
                });
            });
        }

        private static SourceFile RenderBulkInsertInterface(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "IBulkInsert.cs",
                Contents = $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// 大量のデータを高速に一括更新する機能を提供します。
                    /// </summary>
                    public interface IBulkInsert {
                        // TODO ver.1
                    }
                    """,
            };
        }

        private SourceFile RenderDummyDataGenerator(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "DummyDataGenerator.cs",
                Contents = $$"""
                    // 何らかの事故で本番環境で実行されてしまう可能性を排除するためDEBUGビルドでのみ有効とする
                    #if DEBUG

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// デバッグ用のダミーデータ作成処理
                    /// </summary>
                    public class DummyDataGenerator {
                        /// <summary>
                        /// ダミーデータ作成処理を実行します。
                        /// </summary>
                        public async Task GenerateAsync() {
                            // TODO ver.1
                            throw new NotImplementedException();
                        }
                    }

                    #endif
                    """,
            };
        }
    }
}
