using System;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.Core {
    public class AggregateInstanceBuildContext {
        public AggregateInstanceBuildContext(string boundObjectPath, int initialIndent) {
            _variableNameResolver = new VariableNameResolver(boundObjectPath);
            _indent = new Indent(initialIndent);
        }

        private readonly VariableNameResolver _variableNameResolver;
        public string CurrentMemberName => _variableNameResolver.CurrentMemberName;
        public string CurrentMemberWithoutLoopIndex => _variableNameResolver.CurrentMemberWithoutLoopIndex;
        public string CurrentLoopVar => _variableNameResolver.CurrentLoopVar;
        public void Push(string memberName) => _variableNameResolver.Push(memberName);
        public void PushArrayMember(string memberName) => _variableNameResolver.PushArrayMember(memberName);
        public void Pop() => _variableNameResolver.Pop();

        private readonly Indent _indent;
        public string CurrentIndent => _indent.CurrentIndent;
        public AggregateInstanceBuildContext WithIndent(int added) => new(_variableNameResolver, _indent.Added(added));
        private AggregateInstanceBuildContext(VariableNameResolver variableNameResolver, Indent indent) {
            _variableNameResolver = variableNameResolver;
            _indent = indent;
        }


        /// <summary>
        /// 生成されるコード中で使われるループ変数の名前を算出したり asp-forにバインドするプロパティ名を算出したりするためのクラス
        /// </summary>
        private class VariableNameResolver {
            public VariableNameResolver(string boundObjectPath) {
                _boundObjectPath = boundObjectPath;
            }

            private readonly string _boundObjectPath;
            private readonly Stack<string> _path = new();

            public string CurrentMemberName => string.IsNullOrEmpty(_boundObjectPath)
                ? string.Join(".", _path.Reverse())
                : _boundObjectPath + "." + string.Join(".", _path.Reverse());
            public string CurrentMemberWithoutLoopIndex => CurrentMemberName.EndsWith("]")
                ? CurrentMemberName.Substring(0, CurrentMemberName.Length - 3)
                : CurrentMemberName;
            /// <summary>i, j, k など。多重ループ内では親で使われている名前のループ変数名が使用できない問題をなんとかする</summary>
            public string CurrentLoopVar {
                get {
                    // zまで使い切ることはないだろう
                    if (_path.Count > 18) throw new InvalidOperationException("ループ変数名が多すぎます。");
                    return char.ConvertFromUtf32(_path.Count + 104); // 深さ1のときループ変数名"i"
                }
            }

            public void Push(string memberName) {
                _path.Push(memberName);
            }
            public void PushArrayMember(string memberName) {
                var i = char.ConvertFromUtf32(_path.Count + 1 + 104);
                _path.Push($"{memberName}[{i}]");
            }
            public void Pop() {
                _path.Pop();
            }
        }

        /// <summary>
        /// 生成されるコードのインデントを整えるためのクラス
        /// </summary>
        private class Indent {
            public Indent(int value) {
                _value = value;
            }
            private readonly int _value;

            public string CurrentIndent => string.Concat(Enumerable.Range(0, _value).Select(_ => INDENT));

            public Indent Added(int v) => new(_value + v);

            private const string INDENT = "    ";
        }
    }
}
