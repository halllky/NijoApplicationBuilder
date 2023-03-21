using System;
namespace HalApplicationBuilder.CodeRendering
{
    internal interface ITemplate
    {
        void PushIndent(string indent);
        string PopIndent();
        void WriteLine(string appendToText);
    }
}

