using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HalApplicationBuilder.CodeRendering {
    internal interface ITemplate {
        string FileName { get; }

        void PushIndent(string indent);
        string PopIndent();

        void Write(string textToAppend);
        void WriteLine(string appendToText);

        string TransformText();

        void Render(ITemplate template) {
            foreach (var line in template.TransformText().Split(Environment.NewLine)) {
                WriteLine(line);
            }
        }
    }

    internal abstract class TemplateBase : ITemplate {

        public abstract string FileName { get; }
        protected abstract string Template();

        public string TransformText() {
            var indent = string.Concat(_indent);
            var lines = Template()
                .Split(Environment.NewLine)
                .Select(line => indent + line);
            return string.Join(Environment.NewLine, lines);
        }

        private readonly Stack<string> _indent = new();
        public void PushIndent(string indent) {
            _indent.Push(indent);
        }
        public string PopIndent() {
            return _indent.Pop();
        }

        [Obsolete]
        public void Write(string textToAppend) {
            throw new NotImplementedException();
        }
        [Obsolete]
        public void WriteLine(string appendToText) {
            throw new NotImplementedException();
        }
    }
}

