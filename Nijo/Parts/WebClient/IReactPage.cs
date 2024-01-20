using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    public interface IReactPage {
        string Url { get; }
        string DirNameInPageDir { get; }
        string ComponentPhysicalName { get; }
        bool ShowMenu { get; }
        string? LabelInMenu { get; }
        SourceFile GetSourceFile();
    }
}
