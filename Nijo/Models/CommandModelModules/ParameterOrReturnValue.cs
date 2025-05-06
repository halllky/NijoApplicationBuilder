using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Models;
using Nijo.Models.QueryModelModules;
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

            IValueMemberType IInstanceValuePropertyMetadata.Type => _vm.Type;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _vm;
            string IInstancePropertyMetadata.PropertyName => _vm.PhysicalName;

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

            public string CsType => _rm.RefToObject switch {
                RefToMember.E_RefToObject.SearchCondition => new SearchCondition.Entry((RootAggregate)_rm.RefTo).CsClassName,
                RefToMember.E_RefToObject.DisplayData => new DisplayData(_rm.RefTo).CsClassName,
                _ => throw new InvalidOperationException(),
            };
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _rm;
            string IInstancePropertyMetadata.PropertyName => _rm.PhysicalName;
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
                    public {{CsType}} {{_rm.PhysicalName}} { get; set; } = new();
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
        }
        internal class CommandDescendantMember : ICommandMember, IInstanceStructurePropertyMetadata {
            internal CommandDescendantMember(AggregateBase aggregate) {
                _aggregate = aggregate;
            }
            private readonly AggregateBase _aggregate;

            internal string CsType => $"{_aggregate.GetRoot().PhysicalName}Parameter_{_aggregate.PhysicalName}";
            internal string DisplayName => _aggregate.DisplayName;
            internal IEnumerable<ICommandMember> GetMembers() {
                foreach (var m in _aggregate.GetMembers()) {
                    yield return m switch {
                        ValueMember vm => new CommandValueMember(vm),
                        RefToMember rm => new CommandRefToMember(rm),
                        ChildAggregate child => new CommandDescendantMember(child),
                        ChildrenAggregate children => new CommandDescendantMember(children),
                        _ => throw new InvalidOperationException(),
                    };
                }
            }

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _aggregate;
            string IInstancePropertyMetadata.PropertyName => _aggregate.PhysicalName;
            string IInstanceStructurePropertyMetadata.CsType => CsType;
            bool IInstanceStructurePropertyMetadata.IsArray => _aggregate is ChildrenAggregate;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();

            string ICommandMember.RenderCSharpDeclaration(CodeRenderingContext ctx) {
                var csTypeWithArray = _aggregate is ChildrenAggregate ? $"List<{CsType}>" : CsType;

                return $$"""
                    public {{csTypeWithArray}} {{_aggregate.PhysicalName}} { get; set; } = new();
                    """;
            }

            string ICommandMember.RenderTypeScriptDeclaration(CodeRenderingContext ctx) {
                var array = _aggregate is ChildrenAggregate ? "[]" : "";

                return $$"""
                    {{_aggregate.PhysicalName}}: {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderTypeScriptDeclaration(ctx), "  ")}}
                    """)}}
                    }{{array}}
                    """;
            }
            string ICommandMember.RenderTsInitializer() {
                if (_aggregate is ChildrenAggregate) {
                    return $$"""
                        {{_aggregate.PhysicalName}}: [],
                        """;
                } else {
                    return $$"""
                        {{_aggregate.PhysicalName}}: {
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
