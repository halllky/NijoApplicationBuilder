using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        protected static TemplateTextHelper If(bool condition, string template) {
            return TemplateTextHelper.If(condition, template);
        }
    }

    /// <summary>
    /// テンプレート文字列簡略化用
    /// </summary>
    internal class TemplateTextHelper {
        private TemplateTextHelper(StringBuilder stringBuilder) {
            _stringBuilder = stringBuilder;
            _eval = true;
        }

        private readonly StringBuilder _stringBuilder;
        private bool _eval;

        internal static TemplateTextHelper If(bool condition, string text) {
            var helper = new TemplateTextHelper(new StringBuilder());
            if (condition) {
                helper._stringBuilder.AppendLine(text);
                helper._eval = false;
            }
            return helper;
        }
        internal TemplateTextHelper ElseIf(bool condition, string text) {
            if (_eval && condition) {
                _stringBuilder.AppendLine(text);
                _eval = false;
            }
            return this;
        }
        internal string Else(string text) {
            if (_eval) _stringBuilder.AppendLine(text);
            return ToString();
        }

        public override string ToString() {
            return _stringBuilder.ToString().TrimEnd();
        }
    }
    internal static class TemplateTextHelperExtensions {
        internal static string SelectTextTemplate<T>(this IEnumerable<T> values, Func<T, TemplateTextHelper> selector) {
            return values
                .Select(selector)
                .Select(helper => helper.ToString())
                .Join(Environment.NewLine);
        }
    }
}

