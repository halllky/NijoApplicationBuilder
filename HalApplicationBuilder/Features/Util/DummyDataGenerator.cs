using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.Features.InstanceHandling;
using static HalApplicationBuilder.Features.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalApplicationBuilder.Features.KeywordSearching;
using HalApplicationBuilder.Core.AggregateMemberTypes;

namespace HalApplicationBuilder.Features.Util {
    internal class DummyDataGenerator {
        private const int DATA_COUNT = 4;

        internal static SourceFile Render() => new SourceFile {
            FileName = "useDummyDataGenerator.ts",
            RenderContent = ctx => {
                var random = new Random(0);
                var ordered = ctx.Schema
                    .RootAggregatesOrderByDataFlow()
                    .Where(root => root.IsCreatable());
                var xTimes = ordered
                    .SelectMany(root => Enumerable
                        .Repeat(root, DATA_COUNT)
                        .Select((root, index) => new { root, index }));

                return $$"""
                    import { useCallback } from 'react'
                    import { useHttpRequest } from './useHttpRequest'
                    import { BarMessage } from '../decoration'
                    import * as AggregateType from '../types'

                    export const useDummyDataGenerator = (setErrorMessages: (msgs: BarMessage[]) => void) => {
                      const { get, post } = useHttpRequest()

                      return useCallback(async () => {

                        {{WithIndent(xTimes.SelectTextTemplate(x => RenderAggregate(x.root, x.index, random)), "    ")}}

                        return true
                      }, [post, setErrorMessages])
                    }
                    """;
            },
        };

        private static string RenderAggregate(GraphNode<Aggregate> rootAggregate, int index, Random random) {
            var controller = new WebClient.Controller(rootAggregate.Item);
            var descendants = rootAggregate.EnumerateDescendants();
            var data = $"data{random.Next(99999999):00000000}";

            IEnumerable<string> ObjectPath(GraphNode<Aggregate> agg) => agg
                .PathFromEntry()
                .Select(path => path.Terminal.As<Aggregate>().IsChildrenMember()
                                && path.Terminal.As<Aggregate>() != agg
                    ? $"{path.RelationName}[0]"
                    : $"{path.RelationName}");

            string NewObject(GraphNode<Aggregate> agg) => agg.IsChildrenMember()
                ? $"[AggregateType.{new TSInitializerFunction(agg).FunctionName}()]"
                : $"AggregateType.{new TSInitializerFunction(agg).FunctionName}()";

            string SetDummyValue(AggregateMember.AggregateMemberBase member) {
                var path = member.Owner
                    .PathFromEntry()
                    .Select(edge => edge.Terminal.As<Aggregate>().IsChildrenMember()
                        ? $"{edge.RelationName}[0]"
                        : edge.RelationName)
                    .Concat(new[] { member.MemberName });

                if (member is AggregateMember.Variation variation) {
                    var key = random.Next(variation.VariationGroup.VariationAggregates.Count);
                    return $"{data}.{path.Join(".")} = '{variation.VariationGroup.VariationAggregates.ElementAt(key).Key}'";

                } else if (member is AggregateMember.Schalar schalar) {
                    static string RandomAlphabet(Random random, int length) {
                        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                        return new string(chars[random.Next(chars.Length)], length);
                    }
                    static string RandomEnum(EnumList enumList, Random random) {
                        var randomItem = enumList
                            .Definition
                            .Items[random.Next(enumList.Definition.Items.Count)];
                        return $"'{randomItem.PhysicalName}'";
                    }

                    var dummyValue = schalar.Options.MemberType switch {
                        Core.AggregateMemberTypes.Boolean => "true",
                        Core.AggregateMemberTypes.EnumList enumList => RandomEnum(enumList, random),
                        Core.AggregateMemberTypes.Id => $"'{random.Next(99999999):00000000}'",
                        Core.AggregateMemberTypes.Integer => random.Next(999999).ToString(),
                        Core.AggregateMemberTypes.Sentence => "'XXXXXXXXXXXXXX\\nXXXXXXXXXXXXXX'",
                        Core.AggregateMemberTypes.TimePoint => $"'{new DateTime(2000 ,1 ,1).AddDays(random.Next(3000)):yyyy-MM-dd}'",
                        Core.AggregateMemberTypes.Uuid => null, // 自動生成されるので
                        Core.AggregateMemberTypes.VariationSwitch => null, // Variationの分岐で処理済み
                        Core.AggregateMemberTypes.Word => $"'{RandomAlphabet(random, 10)}'",
                        _ => null, // 未定義
                    };
                    return dummyValue == null
                        ? string.Empty
                        : $"{data}.{path.Join(".")} = {dummyValue}";

                } else if (member is AggregateMember.Ref @ref) {
                    var api = new KeywordSearchingFeature(@ref.MemberAggregate).GetUri();
                    var res = $"response{random.Next(99999999):00000000}";
                    return $$"""
                        const {{res}} = await get<AggregateType.{{@ref.MemberAggregate.Item.TypeScriptTypeName}}[]>(`{{api}}`, {})
                        {{data}}.{{path.Join(".")}} = {{res}}.ok ? {{res}}.data[{{index}}] : undefined
                        """;

                } else if (member is AggregateMember.Child
                        || member is AggregateMember.Children
                        || member is AggregateMember.VariationItem) {
                    return string.Empty;

                } else {
                    throw new NotImplementedException();
                }
            }

            var response = $"response{random.Next(99999999):00000000}";
            return $$"""
                const {{data}} = AggregateType.{{new TSInitializerFunction(rootAggregate).FunctionName}}()
                {{new AggregateDetail(rootAggregate).GetOwnMembers().SelectTextTemplate(member => $$"""
                {{WithIndent(SetDummyValue(member), "")}}
                """)}}

                {{descendants.SelectTextTemplate(agg => $$"""
                {{data}}.{{ObjectPath(agg).Join(".")}} = {{NewObject(agg)}}
                {{new AggregateDetail(agg).GetOwnMembers().SelectTextTemplate(member => $$"""
                {{WithIndent(SetDummyValue(member), "")}}
                """)}}
                """)}}

                const {{response}} = await post<AggregateType.{{rootAggregate.Item.TypeScriptTypeName}}>(`{{controller.CreateCommandApi}}`, {{data}})
                if (!{{response}}.ok) {
                  setErrorMessages([...{{response}}.errors])
                  return false
                }

                """;
        }

        private static string Array(GraphNode<Aggregate> agg) => $"array{agg.Item.ClassName}";
    }
}
