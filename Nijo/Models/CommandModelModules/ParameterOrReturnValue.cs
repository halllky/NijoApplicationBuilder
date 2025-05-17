using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Models;
using Nijo.Models.QueryModelModules;
using Nijo.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nijo.Models.CommandModelModules {
    /// <summary>
    /// コマンドのパラメータ、または戻り値の型
    /// </summary>
    internal class ParameterOrReturnValue : IInstancePropertyOwnerMetadata {

        internal enum E_Type {
            Parameter,
            ReturnValue,
        }

        internal ParameterOrReturnValue(RootAggregate aggregate, E_Type type) {
            _rootAggregate = aggregate;
            _type = type;
        }
        private readonly RootAggregate _rootAggregate;
        private readonly E_Type _type;

        internal string CsClassName => _type == E_Type.Parameter
            ? $"{_rootAggregate.PhysicalName}Parameter"
            : $"{_rootAggregate.PhysicalName}ReturnValue";
        internal string TsTypeName => _type == E_Type.Parameter
            ? $"{_rootAggregate.PhysicalName}Parameter"
            : $"{_rootAggregate.PhysicalName}ReturnValue";

        internal IEnumerable<ICommandMember> GetMembers() {
            var objectRoot = _type == E_Type.Parameter
                ? _rootAggregate.GetCommandModelParameterChild()
                : _rootAggregate.GetCommandModelReturnValueChild();

            foreach (var member in objectRoot.GetMembers()) {
                yield return member switch {
                    ValueMember vm => new CommandValueMember(vm),
                    RefToMember rm => new CommandRefToMember(rm),
                    ChildAggregate child => new CommandDescendantMember(child),
                    ChildrenAggregate children => new CommandDescendantMember(children),
                    _ => throw new InvalidOperationException(),
                };
            }
        }
        IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();

        internal string RenderCSharpRecursively(CodeRenderingContext ctx) {
            var objectRoot = _type == E_Type.Parameter
                ? _rootAggregate.GetCommandModelParameterChild()
                : _rootAggregate.GetCommandModelReturnValueChild();
            var descendants = objectRoot
                .EnumerateDescendants()
                .Select(descendant => new CommandDescendantMember(descendant));

            return $$"""
                /// <summary>
                /// {{_rootAggregate.DisplayName}}の実行時にクライアント側から渡される引数
                /// </summary>
                public partial class {{CsClassName}} {
                {{GetMembers().SelectTextTemplate(member => $$"""
                    {{WithIndent(member.RenderCSharpDeclaration(ctx), "    ")}}
                """)}}
                }
                {{descendants.SelectTextTemplate(descendant => $$"""

                /// <summary>
                /// {{_rootAggregate.DisplayName}}の実行時にクライアント側から渡される引数のうち{{descendant.DisplayName}}の部分
                /// </summary>
                public partial class {{descendant.CsType}} {
                {{descendant.GetMembers().SelectTextTemplate(member => $$"""
                    {{WithIndent(member.RenderCSharpDeclaration(ctx), "    ")}}
                """)}}
                }
                """)}}
                """;
        }

        internal string RenderTypeScript(CodeRenderingContext ctx) {
            return $$"""
                /** {{_rootAggregate.DisplayName}}の実行時にクライアント側から渡される引数 */
                export type {{TsTypeName}} = {
                {{GetMembers().SelectTextTemplate(member => $$"""
                  {{WithIndent(member.RenderTypeScriptDeclaration(ctx), "  ")}}
                """)}}
                }
                """;
        }

        #region メンバー
        internal interface ICommandMember : IInstancePropertyMetadata {
            string RenderCSharpDeclaration(CodeRenderingContext ctx);
            string RenderTypeScriptDeclaration(CodeRenderingContext ctx);
            string RenderTsInitializer();
        }
        internal class CommandValueMember : ICommandMember, IInstanceValuePropertyMetadata {
            internal CommandValueMember(ValueMember vm) {
                _vm = vm;
            }
            private readonly ValueMember _vm;

            public IValueMemberType Type => _vm.Type;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _vm;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => _vm.PhysicalName;

            string ICommandMember.RenderCSharpDeclaration(CodeRenderingContext ctx) {
                return $$"""
                    public {{_vm.Type.CsDomainTypeName}}? {{_vm.PhysicalName}} { get; set; }
                    """;
            }
            string ICommandMember.RenderTypeScriptDeclaration(CodeRenderingContext ctx) {
                return $$"""
                    {{_vm.PhysicalName}}: {{_vm.Type.TsTypeName}} | undefined
                    """;
            }
            string ICommandMember.RenderTsInitializer() {
                return $$"""
                    {{_vm.PhysicalName}}: undefined,
                    """;
            }

        }
        internal class CommandRefToMember : ICommandMember, IInstanceStructurePropertyMetadata {
            internal CommandRefToMember(RefToMember rm) {
                _rm = rm;
            }
            private readonly RefToMember _rm;

            public string GetTypeName(E_CsTs csts) => _rm.RefToObject switch {
                RefToMember.E_RefToObject.SearchCondition => csts == E_CsTs.CSharp
                    ? new SearchCondition.Entry((RootAggregate)_rm.RefTo).CsClassName
                    : new SearchCondition.Entry((RootAggregate)_rm.RefTo).TsTypeName,
                RefToMember.E_RefToObject.DisplayData => csts == E_CsTs.CSharp
                    ? new DisplayData(_rm.RefTo).CsClassName
                    : new DisplayData(_rm.RefTo).TsTypeName,
                _ => throw new InvalidOperationException(),
            };
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _rm;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => _rm.PhysicalName;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
                IInstancePropertyOwnerMetadata obj = _rm.RefToObject switch {
                    RefToMember.E_RefToObject.SearchCondition => new SearchCondition.Entry((RootAggregate)_rm.RefTo),
                    RefToMember.E_RefToObject.DisplayData => new DisplayData(_rm.RefTo),
                    _ => throw new InvalidOperationException(),
                };
                return obj.GetMembers();
            }

            string ICommandMember.RenderCSharpDeclaration(CodeRenderingContext ctx) {
                return $$"""
                    public {{GetTypeName(E_CsTs.CSharp)}} {{_rm.PhysicalName}} { get; set; } = new();
                    """;
            }
            string ICommandMember.RenderTypeScriptDeclaration(CodeRenderingContext ctx) {
                string type = _rm.RefToObject switch {
                    RefToMember.E_RefToObject.SearchCondition => new SearchCondition.Entry((RootAggregate)_rm.RefTo).TsTypeName,
                    RefToMember.E_RefToObject.DisplayData => new DisplayData(_rm.RefTo).TsTypeName,
                    _ => throw new InvalidOperationException(),
                };

                return $$"""
                    {{_rm.PhysicalName}}: {{type}}
                    """;
            }
            string ICommandMember.RenderTsInitializer() {
                var initFunction = _rm.RefToObject switch {
                    RefToMember.E_RefToObject.SearchCondition => new SearchCondition.Entry((RootAggregate)_rm.RefTo).TsNewObjectFunction,
                    RefToMember.E_RefToObject.DisplayData => new DisplayData(_rm.RefTo).TsNewObjectFunction,
                    _ => throw new InvalidOperationException(),
                };
                return $$"""
                    {{_rm.PhysicalName}}: {{initFunction}}(),
                    """;
            }

            /// <summary>
            /// このメンバーに対応するメッセージの入れ物
            /// </summary>
            internal MessageContainer GetMessageContainer() {
                return _rm.RefToObject switch {
                    RefToMember.E_RefToObject.SearchCondition => new SearchConditionMessageContainer(_rm.RefTo),
                    RefToMember.E_RefToObject.DisplayData => new DisplayDataMessageContainer(_rm.RefTo),
                    _ => throw new InvalidOperationException(),
                };
            }
        }
        internal class CommandDescendantMember : ICommandMember, IInstanceStructurePropertyMetadata {
            internal CommandDescendantMember(AggregateBase aggregate) {
                Aggregate = aggregate;
            }
            internal AggregateBase Aggregate { get; }

            internal string CsType => $"{Aggregate.GetRoot().PhysicalName}Parameter_{Aggregate.PhysicalName}";
            internal string DisplayName => Aggregate.DisplayName;
            internal IEnumerable<ICommandMember> GetMembers() {
                foreach (var m in Aggregate.GetMembers()) {
                    yield return m switch {
                        ValueMember vm => new CommandValueMember(vm),
                        RefToMember rm => new CommandRefToMember(rm),
                        ChildAggregate child => new CommandDescendantMember(child),
                        ChildrenAggregate children => new CommandDescendantMember(children),
                        _ => throw new InvalidOperationException(),
                    };
                }
            }

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Aggregate;
            string IInstancePropertyMetadata.GetPropertyName(E_CsTs csts) => Aggregate.PhysicalName;
            string IInstanceStructurePropertyMetadata.GetTypeName(E_CsTs csts) => CsType;
            bool IInstanceStructurePropertyMetadata.IsArray => Aggregate is ChildrenAggregate;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();

            string ICommandMember.RenderCSharpDeclaration(CodeRenderingContext ctx) {
                var csTypeWithArray = Aggregate is ChildrenAggregate ? $"List<{CsType}>" : CsType;

                return $$"""
                    public {{csTypeWithArray}} {{Aggregate.PhysicalName}} { get; set; } = new();
                    """;
            }

            string ICommandMember.RenderTypeScriptDeclaration(CodeRenderingContext ctx) {
                var array = Aggregate is ChildrenAggregate ? "[]" : "";

                return $$"""
                    {{Aggregate.PhysicalName}}: {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderTypeScriptDeclaration(ctx), "  ")}}
                    """)}}
                    }{{array}}
                    """;
            }
            string ICommandMember.RenderTsInitializer() {
                if (Aggregate is ChildrenAggregate) {
                    return $$"""
                        {{Aggregate.PhysicalName}}: [],
                        """;
                } else {
                    return $$"""
                        {{Aggregate.PhysicalName}}: {
                        {{GetMembers().SelectTextTemplate(member => $$"""
                          {{WithIndent(member.RenderTsInitializer(), "  ")}}
                        """)}}
                        },
                        """;
                }
            }
        }
        #endregion メンバー


        #region クライアント側新規オブジェクト作成関数
        internal string TsNewObjectFunction => $"createNew{TsTypeName}";
        internal string RenderNewObjectFn() {
            return $$"""
                /** {{_rootAggregate.DisplayName}}の{{(_type == E_Type.Parameter ? "パラメータ" : "戻り値")}}オブジェクトの新規作成関数 */
                export const {{TsNewObjectFunction}} = (): {{TsTypeName}} => ({
                {{GetMembers().SelectTextTemplate(member => $$"""
                  {{WithIndent(member.RenderTsInitializer(), "  ")}}
                """)}}
                })
                """;
        }
        #endregion クライアント側新規オブジェクト作成関数
    }
}
