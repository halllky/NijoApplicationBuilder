using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BackgroundService {
    partial class BgTaskFeature {

        private static SourceFile BgTaskBaseClass(CodeRenderingContext ctx) => new SourceFile {
            FileName = "BackgroundTask.cs",
            RenderContent = () => $$"""
                using System.Reflection;
                using System.Text.Json;

                namespace {{ctx.Config.RootNamespace}} {
                    public abstract class BackgroundTask {
                        public abstract string BatchTypeId { get; }

                        public abstract string JobName { get; }
                        public virtual string GetJobName(object? parameter) {
                            return JobName;
                        }

                        public virtual IEnumerable<string> ValidateParameter(object? parameter) {
                            yield break;
                        }

                        public abstract void Execute(JobChain job);
                    }

                    public abstract class BackgroundTask<TParameter> : BackgroundTask {
                        public abstract string GetJobName(TParameter parameter);
                        public override sealed string JobName => string.Empty;
                        public override sealed string GetJobName(object? parameter) {
                            return this.GetJobName((TParameter)parameter!);
                        }

                        public virtual IEnumerable<string> ValidateParameter(TParameter parameter) {
                            yield break;
                        }
                        public override sealed IEnumerable<string> ValidateParameter(object? parameter) {
                            return this.ValidateParameter((TParameter)parameter!);
                        }

                        public abstract void Execute(JobChainWithParameter<TParameter> job);
                        public override sealed void Execute(JobChain job) {
                            Execute((JobChainWithParameter<TParameter>)job);
                        }
                    }
                }
                """,
        };
    }
}
