global using static Nijo.CodeGenerating.TemplateTextHelper;
using Nijo.Util.DotnetEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nijo.CodeGenerating {
    /// <summary>
    /// テンプレート文字列簡略化用
    /// </summary>
    internal partial class TemplateTextHelper {
        private TemplateTextHelper(StringBuilder stringBuilder) {
            _stringBuilder = stringBuilder;
            _evaluated = false;
        }

        private readonly StringBuilder _stringBuilder;
        private bool _evaluated;

        internal static TemplateTextHelper If(bool condition, Func<string> text) {
            var helper = new TemplateTextHelper(new StringBuilder());
            if (condition) {
                helper._stringBuilder.AppendLine(text());
                helper._evaluated = true;
            }
            return helper;
        }
        internal TemplateTextHelper ElseIf(bool condition, Func<string> text) {
            if (!_evaluated && condition) {
                _stringBuilder.AppendLine(text());
                _evaluated = true;
            }
            return this;
        }
        internal TemplateTextHelper Else(Func<string> text) {
            if (!_evaluated) {
                _stringBuilder.AppendLine(text());
                _evaluated = true;
            }
            return this;
        }

        public override string ToString() {
            if (_evaluated) {
                _stringBuilder.Append(SKIP_MARKER);
                return _stringBuilder.ToString();

            } else {
                return SKIP_MARKER;
            }
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

        /// <summary>
        /// この文字列が存在する行はファイルにレンダリングされない。
        /// 
        /// <see cref="If(bool, Func{string})"/> や
        /// <see cref="TemplateTextHelperExtensions.SelectTextTemplate{T}(IEnumerable{T}, Func{T, string})"/>
        /// によって条件に合致しなかったり要素の数が0だったりして空行が生成されてしまうのを防ぐためのもの。
        /// </summary>
        internal static string SKIP_MARKER = "\0\0\0\0\0\0\0\0\0\0\0"; // 通常のソースコード上に現れなさそうな文字列であれば何でもよい
    }
    internal static class TemplateTextHelperExtensions {
        internal static string SelectTextTemplate<T>(this IEnumerable<T> values, Func<T, string> selector) {
            var sourceCode = values.Select(selector).Join(Environment.NewLine);
            return sourceCode == string.Empty
                ? SKIP_MARKER
                : sourceCode;
        }
        internal static string SelectTextTemplate<T>(this IEnumerable<T> values, Func<T, int, string> selector) {
            var sourceCode = values.Select(selector).Join(Environment.NewLine);
            return sourceCode == string.Empty
                ? SKIP_MARKER
                : sourceCode;
        }
    }
}

