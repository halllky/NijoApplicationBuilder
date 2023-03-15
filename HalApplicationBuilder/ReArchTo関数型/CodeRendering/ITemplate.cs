using System;
namespace HalApplicationBuilder.ReArchTo関数型.CodeRendering
{
    internal interface ITemplate
    {
        void PushIndent(string indent);
        string PopIndent();
        void WriteLine(string appendToText);
    }
}

