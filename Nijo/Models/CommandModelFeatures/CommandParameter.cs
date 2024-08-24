using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.CommandModelFeatures {
    /// <summary>
    /// <see cref="CommandModel"/> のパラメータの型
    /// </summary>
    internal class CommandParameter {
        internal CommandParameter(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CsClassName => $"{_aggregate.Item.PhysicalName}Parameter{GetUniqueId()}";
        internal string TsTypeName => $"{_aggregate.Item.PhysicalName}Parameter{GetUniqueId()}";
        /// <summary>
        /// 異なるコマンドの子孫要素同士で名称衝突するのを防ぐためにフルパスの経路をクラス名に含める
        /// </summary>
        private string GetUniqueId() {
            return _aggregate.IsRoot()
                ? string.Empty
                : $"_{_aggregate.GetRoot().Item.UniqueId.Substring(0, 8)}"; // 8桁も切り取れば重複しないはず
        }

        private IEnumerable<Member> GetOwnMembers() {
            return _aggregate
                .GetMembers()
                .Where(m => m.DeclaringAggregate == _aggregate)
                .Select(m => new Member(m));
        }
        private IEnumerable<CommandParameter> EnumerateThisAndDescendants() {
            yield return this;

            var child = GetOwnMembers()
                .Select(m => m.GetMemberParameter())
                .OfType<CommandParameter>()
                .SelectMany(c => c.EnumerateThisAndDescendants());
            foreach (var item in child) {
                yield return item;
            }
        }

        internal string RenderCSharpDeclaring(CodeRenderingContext context) {
            return EnumerateThisAndDescendants().SelectTextTemplate(param => $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}処理のパラメータ{{(param._aggregate.IsRoot() ? "" : "の一部")}}
                /// </summary>
                public partial class {{param.CsClassName}} {
                {{param.GetOwnMembers().SelectTextTemplate(m => $$"""
                    public virtual {{m.CsTypeName}}? {{m.MemberName}} { get; set; }
                """)}}
                }
                """);
        }

        internal string RenderTsDeclaring(CodeRenderingContext context) {
            return EnumerateThisAndDescendants().SelectTextTemplate(param => $$"""
                /** {{_aggregate.Item.DisplayName}}処理のパラメータ{{(param._aggregate.IsRoot() ? "" : "の一部")}} */
                export type {{param.TsTypeName}} = {
                {{param.GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}?: {{m.TsTypeName}}
                """)}}
                }
                """);
        }

        /// <summary>
        /// <see cref="CommandParameter"/> のメンバー
        /// </summary>
        private class Member {
            internal Member(AggregateMember.AggregateMemberBase member) {
                _member = member;
            }
            private readonly AggregateMember.AggregateMemberBase _member;

            internal string MemberName => _member.MemberName;
            internal string CsTypeName => _member switch {
                AggregateMember.ValueMember vm => vm.Options.MemberType.GetCSharpTypeName(),
                AggregateMember.Children children => $"{new CommandParameter(children.ChildrenAggregate).CsClassName}[]",
                AggregateMember.Child child => new CommandParameter(child.ChildAggregate).CsClassName,
                AggregateMember.VariationItem variation => new CommandParameter(variation.VariationAggregate).CsClassName,
                AggregateMember.Ref @ref => new RefTo.RefDisplayData(@ref.RefTo, @ref.RefTo).CsClassName,
                _ => throw new NotImplementedException(),
            };
            internal string TsTypeName => _member switch {
                AggregateMember.ValueMember vm => vm.Options.MemberType.GetTypeScriptTypeName(),
                AggregateMember.Children children => $"{new CommandParameter(children.ChildrenAggregate).TsTypeName}[]",
                AggregateMember.Child child => new CommandParameter(child.ChildAggregate).TsTypeName,
                AggregateMember.VariationItem variation => new CommandParameter(variation.VariationAggregate).TsTypeName,
                AggregateMember.Ref @ref => new RefTo.RefDisplayData(@ref.RefTo, @ref.RefTo).TsTypeName,
                _ => throw new NotImplementedException(),
            };

            internal CommandParameter? GetMemberParameter() {
                if (_member is not AggregateMember.RelationMember rel) return null;
                if (_member is AggregateMember.Parent) return null;
                if (_member is AggregateMember.Ref) return null;

                return new CommandParameter(rel.MemberAggregate);
            }
        }
    }
}
