using Nijo.Core;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BatchUpdate {
    internal class BatchUpdateTask {

        internal static SourceFile Render(CodeRenderingContext context) {
            var appSrv = new ApplicationService();
            var availableAggregates = context.Schema
                .RootAggregates()
                .Where(a => a.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key);

            return new SourceFile {
                FileName = "BatchUpdateTask.cs",
                RenderContent = () => $$"""
                    namespace {{context.Config.RootNamespace}} {
                        public class BatchUpdateParameter {
                            public required string DataType { get; set; }
                            public required BatchUpdateData Data { get; set; }
                        }
                        public class BatchUpdateData {
                            public List<object> Added { get; set; } = new();
                            public List<object> Deleted { get; set; } = new();
                            public List<object> Modified { get; set; } = new();
                        }

                        public class BatchUpdateTask : BackgroundTask<BatchUpdateParameter> {
                            public override string BatchTypeId => "NIJO::BATCH-UPDATE";

                            public override string GetJobName(BatchUpdateParameter param) {
                                return $"一括アップデート（{param.DataType}）";
                            }

                            public override IEnumerable<string> ValidateParameter(BatchUpdateParameter parameter) {
                    {{availableAggregates.Select(agg => GetKey(agg)).SelectTextTemplate(key => $$"""
                                if (parameter.DataType == "{{key}}") yield break;
                    """)}}
                                yield return $"識別子 '{parameter.DataType}' と対応する一括更新処理はありません。";
                            }

                            public override void Execute(JobChainWithParameter<BatchUpdateParameter> job) {
                                job.Section("更新処理実行", context => {

                                });
                            }
                        }
                    }
                    """,
            };
        }

        private static string GetKey(GraphNode<Aggregate> aggregate) {
            return aggregate.Item.ClassName;
        }
    }
}
