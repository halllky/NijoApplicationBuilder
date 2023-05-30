using System;
namespace HalApplicationBuilder.CodeRendering20230514 {
    internal interface ITemplate {
        string FileName { get; }

        void PushIndent(string indent);
        string PopIndent();
        void WriteLine(string appendToText);

        string TransformText();
    }
}

