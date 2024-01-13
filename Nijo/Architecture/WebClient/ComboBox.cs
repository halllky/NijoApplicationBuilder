using Nijo.Architecture.WebServer;
using Nijo.Architecture.Utility;
using Nijo.Core;
using Nijo.Util.DotnetEx;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;
using Nijo.Features.Repository;

namespace Nijo.Architecture.WebClient {
    internal class ComboBox {
        internal ComboBox(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string ComponentName => $"ComboBox{_aggregate.Item.DisplayName.ToCSharpSafe()}";
        internal string Api => new KeywordSearchingFeature(_aggregate).GetUri();
        internal RefTargetKeyName KeyName => new RefTargetKeyName(_aggregate);

        internal static SourceFile RenderDeclaringFile(ICodeRenderingContext ctx) => new SourceFile {
            FileName = "AggregateComboBox.tsx",
            RenderContent = () => $$"""
                import React, { useState, useCallback } from "react"
                import { useHttpRequest, defineCustomComponent } from "../util"
                import { AsyncComboBox } from "../input"
                import * as Types from "../types"

                {{ctx.Schema.AllAggregates().Where(a => a.IsStored()).Select(a => new ComboBox(a)).SelectTextTemplate((combo, index) => $$"""
                export const {{combo.ComponentName}} = defineCustomComponent<Types.{{combo.KeyName.TypeScriptTypeName}}>((props, ref) => {
                  const [queryKey, setQueryKey] = useState<string>('combo-{{combo._aggregate.Item.UniqueId}}::')
                  const { get } = useHttpRequest()
                  const query = useCallback(async (keyword: string | undefined) => {
                    setQueryKey(`combo-{{combo._aggregate.Item.UniqueId}}::${keyword ?? ''}`)
                    const response = await get<Types.{{combo.KeyName.TypeScriptTypeName}}[]>(`{{combo.Api}}`, { keyword })
                    return response.ok ? response.data : []
                  }, [get])

                  return (
                    <AsyncComboBox
                      {...props}
                      ref={ref}
                      queryKey={queryKey}
                      query={query}
                      keySelector={item => JSON.stringify([{{combo.KeyName.GetKeyMembers().Select(m => "item." + m.MemberName).Join(", ")}}])}
                      textSelector={item => `{{combo.KeyName.GetNameMembers().Select(m => "${item." + m.MemberName + "}").Join("&nbsp;")}}`}
                    />
                  )
                })
                """)}}
                """,
        };

        internal string RenderCaller(string raectHookFormId, params string[] attrs) {
            var attributes = attrs
                .Where(str => !string.IsNullOrWhiteSpace(str))
                .Join(" ");
            return $$"""
                <Input.{{ComponentName}} {...registerEx({{raectHookFormId}})} {{attributes}} />
                """;
        }
    }
}
