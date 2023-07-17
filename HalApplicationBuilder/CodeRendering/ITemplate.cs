using System;
namespace HalApplicationBuilder.CodeRendering {
    internal interface ITemplate {
        string FileName { get; }

        void PushIndent(string indent);
        string PopIndent();
        void WriteLine(string appendToText);

        string TransformText();
    }
}

