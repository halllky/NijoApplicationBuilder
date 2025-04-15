using Nijo.SchemaParsing;
using Nijo.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.Models.DataModelModules;
using Nijo.Models.QueryModelModules;
using Nijo.CodeGenerating.Helpers;
using Nijo.Util.DotnetEx;
using Nijo.Models;
using Nijo.Models.CommandModelModules;

namespace Nijo.ImmutableSchema {
    /// <summary>
    /// アプリケーションスキーマ。
    /// </summary>
    public class ApplicationSchema {

        internal ApplicationSchema(XDocument xDocument, SchemaParseContext parseContext) {
            _xDocument = xDocument;
            _parseContext = parseContext;
        }
        private readonly XDocument _xDocument;
        private readonly SchemaParseContext _parseContext;

        /// <summary>
        /// このスキーマで定義されているルート集約を返します。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IEnumerable<RootAggregate> GetRootAggregates() {
            foreach (var xElement in _xDocument.Root?.Elements() ?? []) {
                var aggregate = _parseContext.ToAggregateBase(xElement, null);
                if (aggregate is not RootAggregate rootAggregate) {
                    throw new InvalidOperationException();
                }
                yield return rootAggregate;
            }
        }

        /// <summary>
        /// スキーマ定義とプロパティパスの情報をMarkdown形式で生成します。
        /// </summary>
        /// <returns>Markdown形式の文字列</returns>
        public string GenerateMarkdownDump() {
            var rootAggregates = GetRootAggregates().ToArray();

            // 全集約のクラス定義を取得
            var allAggregates = rootAggregates.SelectMany(root => root.EnumerateThisAndDescendants());

            // メインMarkdown構造
            return $$"""
                # スキーマ定義ダンプ

                ## 集約関係図

                ```mermaid
                classDiagram
                {{GenerateClassDiagram(allAggregates)}}
                ```

                ## メンバー情報
                説明

                - 集約: そのメンバーを保有している集約
                - メンバー: メンバー名
                - 種類: 以下いずれか
                  - 配列(Children または 被Ref)
                  - 構造体(Child または Ref)
                  - 値(ValueMember)
                - GetFlattenArrayPathの結果: あるルート集約の変数xの配下にあるそのメンバーをすべて列挙するソースコード
                - null許容: メンバーがnull許容か否か
                - メタデータクラス名: ソースコード自動生成処理(Nijo.csproj)の中でこのメンバーの定義を司っているクラスの名前
                - MappingKey: 変換処理をレンダリングする際の右辺左辺の紐づけのために相手方を一意に識別する値

                {{GeneratePropertyPaths(rootAggregates)}}
                """.Replace(SKIP_MARKER, "");
        }

        /// <summary>
        /// クラス図を生成します
        /// </summary>
        private string GenerateClassDiagram(IEnumerable<AggregateBase> allAggregates) {
            var classDefinitions = allAggregates.SelectTextTemplate(aggregate => {
                return $$"""
                    class {{aggregate.PhysicalName}} {
                {{GenerateClassMembers(aggregate)}}
                    }
                """;
            });

            var relationships = allAggregates.SelectTextTemplate(aggregate => {
                var parent = aggregate.GetParent();
                var parentRelation = parent != null
                    ? $"    {parent.PhysicalName} <|-- {aggregate.PhysicalName} : Parent"
                    : "";

                var memberRelations = aggregate.GetMembers().SelectTextTemplate(member => {
                    if (member is RefToMember refTo) {
                        return $"    {aggregate.PhysicalName} --> {refTo.RefTo.PhysicalName} : {refTo.PhysicalName}";
                    } else if (member is ChildAggregate child) {
                        return $"    {aggregate.PhysicalName} *-- {child.PhysicalName} : {child.PhysicalName}";
                    } else if (member is ChildrenAggregate children) {
                        return $"    {aggregate.PhysicalName} *-- \"{children.PhysicalName}[]\" {children.PhysicalName} : {children.PhysicalName}[]";
                    }
                    return "";
                });

                return $"""
                {parentRelation}
                {memberRelations}
                """;
            });

            return $"""
            {classDefinitions}
            {relationships}
            """;
        }

        /// <summary>
        /// クラスメンバー定義を生成します
        /// </summary>
        private string GenerateClassMembers(AggregateBase aggregate) {
            return aggregate.GetMembers().SelectTextTemplate(member => {
                if (member is ValueMember vm) {
                    string keyMarker = vm.IsKey ? "PK" : "";
                    return $"        {vm.PhysicalName}: {vm.Type.CsPrimitiveTypeName} {keyMarker}";
                }
                return "";
            });
        }

        /// <summary>
        /// プロパティパスのセクションを生成します
        /// </summary>
        private string GeneratePropertyPaths(IEnumerable<RootAggregate> rootAggregates) {
            return $$"""
                {{rootAggregates.SelectTextTemplate(rootAggregate => $$"""
                ### {{rootAggregate.DisplayName}}

                {{WithIndent(RenderRootAggregate(rootAggregate), "")}}

                """)}}
                """;
        }

        private static IEnumerable<string> RenderRootAggregate(RootAggregate rootAggregate) {

            var structures = new List<IInstancePropertyOwnerMetadata>();
            if (rootAggregate.Model is DataModel) {
                structures.Add(new EFCoreEntity(rootAggregate));
                structures.Add(new SaveCommand(rootAggregate, SaveCommand.E_Type.Update));
                structures.Add(new KeyClass.KeyClassEntry(rootAggregate));
            }
            if (rootAggregate.Model is QueryModel || rootAggregate.Model is DataModel && rootAggregate.GenerateDefaultQueryModel) {
                structures.Add(new DisplayData(rootAggregate));
                structures.Add(new SearchCondition.Filter(rootAggregate));
                structures.Add(new DisplayDataRef.Entry(rootAggregate));
            }
            if (rootAggregate.Model is CommandModel) {
                structures.Add(new ParameterType(rootAggregate));
                structures.Add(new ReturnValue(rootAggregate));
            }

            if (structures.Count == 0) {
                return ["この集約から生成される構造体はありません。"];
            }

            return structures.Select(rootStructure => {
                var rootVariable = new Variable("x");
                var properties = rootVariable.CreatePropertiesRecursively(rootStructure);

                return $$"""
                    #### {{rootAggregate.DisplayName}}: {{rootStructure.GetType().FullName}}

                    | 集約 | メンバー | 種類 | GetFlattenArrayPathの結果 | null許容 | メタデータクラス名 | MappingKey |
                    | :-- | :-- | :-- | :-- | :-- | :-- | :-- |
                    {{properties.SelectTextTemplate(property => $$"""
                    | {{RenderProperty(property).Join(" | ")}} |
                    """)}}

                    """;

                IEnumerable<string> RenderProperty(IInstanceProperty property) {
                    yield return property.Owner == rootVariable ? "-" : property.Owner.Name;
                    yield return property.Metadata.DisplayName;
                    yield return property switch {
                        InstanceValueProperty v => $"値 ({v.Metadata.Type.GetType().Name})",
                        InstanceStructureProperty s => s.Metadata.IsArray ? "配列" : "構造体",
                        _ => throw new NotImplementedException(),
                    };
                    yield return $"`x.{property.GetFlattenArrayPath(E_CsTs.CSharp, out _).Join(".")}`"; // プロパティパス
                    yield return property.IsNullable.ToString();
                    yield return property.Metadata.GetType().Name ?? "";
                    yield return property.Metadata.MappingKey.ToString();
                }
            });
        }
    }
}
