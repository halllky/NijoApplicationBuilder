using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.CodeGenerating {
    /// <summary>
    /// 機能単位などではなく集約単位でソースコードが記載されるファイル
    /// </summary>
    public class SourceFileByAggregate {

        public SourceFileByAggregate(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly RootAggregate _rootAggregate;

        public void AddAppSrvMethod(string sourceCode) {
            throw new NotImplementedException();
        }
        public void AddCSharpClass(string sourceCode) {
            throw new NotImplementedException();
        }
        public void AddWebapiControllerAction(string sourceCode) {
            throw new NotImplementedException();
        }
        public void AddTypeScriptSource(string sourceCode) {
            throw new NotImplementedException();
        }

        public void Render(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
}
