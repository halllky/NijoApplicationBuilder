using Nijo.Core;
using Nijo.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.CodeGenerating {
    public interface ICodeRenderingContext {
        Config Config { get; }
        AppSchema Schema { get; }

        void EditWebApiDirectory(Action<NijoCodeGenerator.DirectorySetupper> webapiDirHandler);
        void EditReactDirectory(Action<NijoCodeGenerator.DirectorySetupper> webapiDirHandler);

        void Render<T>(Action<T> handler) where T : NijoFeatureBaseNonAggregate;
        void Render<T>(GraphNode<Aggregate> aggregate, Action<T> handler) where T : NijoFeatureBaseByAggregate;
    }
}
