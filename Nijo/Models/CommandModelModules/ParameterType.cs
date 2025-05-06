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
    /// コマンドのパラメータ型定義
    /// </summary>
    internal class ParameterType : IInstancePropertyOwnerMetadata {
        internal ParameterType(RootAggregate aggregate) {
            _rootAggregate = aggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string CsClassName => $"{_rootAggregate.PhysicalName}Parameter";
        internal string TsTypeName => $"{_rootAggregate.PhysicalName}Parameter";

        internal IEnumerable<ICommandParameterMember> GetMembers() {
            var param = _rootAggregate.GetCommandModelParameterChild();
            foreach (var member in param.GetMembers()) {
                yield return member switch {
                    ValueMember vm => new CommandParameterValueMember(vm),
                    RefToMember rm => new CommandParameterRefToMember(rm),
                    ChildAggregate child => new CommandParameterDescendantMember(child),
                    ChildrenAggregate children => new CommandParameterDescendantMember(children),
                    _ => throw new InvalidOperationException(),
                };
            }
        }
        IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();

        internal string RenderCSharpRecursively(CodeRenderingContext ctx) {
            var descendants = _rootAggregate
                .GetCommandModelParameterChild()
                .EnumerateDescendants()
                .Select(descendant => new CommandParameterDescendantMember(descendant));

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
        internal interface ICommandParameterMember : IInstancePropertyMetadata {
            string RenderCSharpDeclaration(CodeRenderingContext ctx);
            string RenderTypeScriptDeclaration(CodeRenderingContext ctx);
            string RenderTsInitializer();
        }
        internal class CommandParameterValueMember : ICommandParameterMember, IInstanceValuePropertyMetadata {
            internal CommandParameterValueMember(ValueMember vm) {
                _vm = vm;
            }
            private readonly ValueMember _vm;

            IValueMemberType IInstanceValuePropertyMetadata.Type => _vm.Type;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _vm;
            string IInstancePropertyMetadata.PropertyName => _vm.PhysicalName;

            string ICommandParameterMember.RenderCSharpDeclaration(CodeRenderingContext ctx) {
                return $$"""
                    public {{_vm.Type.CsDomainTypeName}}? {{_vm.PhysicalName}} { get; set; }
                    """;
            }
            string ICommandParameterMember.RenderTypeScriptDeclaration(CodeRenderingContext ctx) {
                return $$"""
                    {{_vm.PhysicalName}}: {{_vm.Type.TsTypeName}} | undefined
                    """;
            }
            string ICommandParameterMember.RenderTsInitializer() {
                return $$"""
                    {{_vm.PhysicalName}}: undefined,
                    """;
            }

        }
        internal class CommandParameterRefToMember : ICommandParameterMember, IInstanceStructurePropertyMetadata {
            internal CommandParameterRefToMember(RefToMember rm) {
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

            string ICommandParameterMember.RenderCSharpDeclaration(CodeRenderingContext ctx) {
                return $$"""
                    public {{CsType}} {{_rm.PhysicalName}} { get; set; } = new();
                    """;
            }
            string ICommandParameterMember.RenderTypeScriptDeclaration(CodeRenderingContext ctx) {
                string type = _rm.RefToObject switch {
                    RefToMember.E_RefToObject.SearchCondition => new SearchCondition.Entry((RootAggregate)_rm.RefTo).TsTypeName,
                    RefToMember.E_RefToObject.DisplayData => new DisplayData(_rm.RefTo).TsTypeName,
                    _ => throw new InvalidOperationException(),
                };

                return $$"""
                    {{_rm.PhysicalName}}: {{type}}
                    """;
            }
            string ICommandParameterMember.RenderTsInitializer() {
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
        internal class CommandParameterDescendantMember : ICommandParameterMember, IInstanceStructurePropertyMetadata {
            internal CommandParameterDescendantMember(AggregateBase aggregate) {
                _aggregate = aggregate;
            }
            private readonly AggregateBase _aggregate;

            internal string CsType => $"{_aggregate.GetRoot().PhysicalName}Parameter_{_aggregate.PhysicalName}";
            internal string DisplayName => _aggregate.DisplayName;
            internal IEnumerable<ICommandParameterMember> GetMembers() {
                foreach (var m in _aggregate.GetMembers()) {
                    yield return m switch {
                        ValueMember vm => new CommandParameterValueMember(vm),
                        RefToMember rm => new CommandParameterRefToMember(rm),
                        ChildAggregate child => new CommandParameterDescendantMember(child),
                        ChildrenAggregate children => new CommandParameterDescendantMember(children),
                        _ => throw new InvalidOperationException(),
                    };
                }
            }

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _aggregate;
            string IInstancePropertyMetadata.PropertyName => _aggregate.PhysicalName;
            string IInstanceStructurePropertyMetadata.CsType => CsType;
            bool IInstanceStructurePropertyMetadata.IsArray => _aggregate is ChildrenAggregate;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();

            string ICommandParameterMember.RenderCSharpDeclaration(CodeRenderingContext ctx) {
                var csTypeWithArray = _aggregate is ChildrenAggregate ? $"List<{CsType}>" : CsType;

                return $$"""
                    public {{csTypeWithArray}} {{_aggregate.PhysicalName}} { get; set; } = new();
                    """;
            }

            string ICommandParameterMember.RenderTypeScriptDeclaration(CodeRenderingContext ctx) {
                var array = _aggregate is ChildrenAggregate ? "[]" : "";

                return $$"""
                    {{_aggregate.PhysicalName}}: {
                    {{GetMembers().SelectTextTemplate(member => $$"""
                      {{WithIndent(member.RenderTypeScriptDeclaration(ctx), "  ")}}
                    """)}}
                    }{{array}}
                    """;
            }
            string ICommandParameterMember.RenderTsInitializer() {
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
                /** {{_rootAggregate.DisplayName}}のパラメータオブジェクトの新規作成関数 */
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
