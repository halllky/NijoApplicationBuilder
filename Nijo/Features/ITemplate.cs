using Nijo.DotnetEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nijo.Features {
    public class SourceFile {
        public required string FileName { get; init; }
        public required Func<CodeRenderingContext, string> RenderContent { get; init; }
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

        internal static TemplateTextHelper If(bool condition, Func<string> text) {
            var helper = new TemplateTextHelper(new StringBuilder());
            if (condition) {
                helper._stringBuilder.AppendLine(text());
                helper._eval = false;
            }
            return helper;
        }
        internal TemplateTextHelper ElseIf(bool condition, Func<string> text) {
            if (_eval && condition) {
                _stringBuilder.AppendLine(text());
                _eval = false;
            }
            return this;
        }
        internal string Else(Func<string> text) {
            if (_eval) _stringBuilder.AppendLine(text());
            return ToString();
        }

        public override string ToString() {
            return _stringBuilder.ToString().TrimEnd();
        }

        internal static string WithIndent(IEnumerable<string> content, string indent) {
            return content
                .Select(x => WithIndent(x, indent))
                .Join(Environment.NewLine + indent);
        }
        internal static string WithIndent(string content, string indent) {
            return content
                .Split(Environment.NewLine)
                .Join(Environment.NewLine + indent);
        }
    }
    internal static class TemplateTextHelperExtensions {
        internal static string SelectTextTemplate<T>(this IEnumerable<T> values, Func<T, string> selector) {
            return values.Select(selector).Join(Environment.NewLine);
        }
        internal static string SelectTextTemplate<T>(this IEnumerable<T> values, Func<T, int, string> selector) {
            return values.Select(selector).Join(Environment.NewLine);
        }
        internal static string SelectTextTemplate<T>(this IEnumerable<T> values, Func<T, TemplateTextHelper> selector) {
            return values
                .Select(selector)
                .Select(helper => helper.ToString())
                .Join(Environment.NewLine);
        }
    }
}

