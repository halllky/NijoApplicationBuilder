using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nijo.Core;
using Nijo.Core.AggregateMemberTypes;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

[assembly: InternalsVisibleTo("Nijo.Ui")]

namespace Nijo.Runtime {
    /// <summary>
    /// nijo.xml を生XMLではなくブラウザ上のGUIで編集する機能
    /// </summary>
    internal class NijoUi {

        internal static AppSchema BuildAppSchemaFromXml(string xmlFilePath) {
            var schema = MutableSchema.FromXmlDocument(xmlFilePath);
            return schema.BuildAppSchema();
        }

        internal NijoUi(GeneratedProject project) {
            _project = project;
        }

        private readonly GeneratedProject _project;

        /// <summary>
        /// Webアプリケーションを定義して返します。
        /// </summary>
        internal WebApplication CreateApp() {
            var builder = WebApplication.CreateBuilder();

            // React側のデバッグのためにポートが異なっていてもアクセスできるようにする
            const string CORS_POLICY_NAME = "AllowAll";
            builder.Services.AddCors(options => {
                options.AddPolicy(CORS_POLICY_NAME, builder => {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            var app = builder.Build();
            app.UseRouting();
            app.UseCors(CORS_POLICY_NAME);

            // ルートにアクセスされた場合、GUI設定プロジェクトのビルド後html（js, css がすべて1つのhtmlファイル内にバンドルされているもの）を返す。
            app.MapGet("/", async context => {
                var assembly = Assembly.GetExecutingAssembly();

                // このプロジェクトのプロジェクト名 + csprojで指定したリソース名 + 実ファイル名(index.html)
                const string RESOURCE_NAME = "Nijo.GuiWebAppHtml.index.html";

                using var stream = assembly.GetManifestResourceStream(RESOURCE_NAME)
                    ?? throw new InvalidOperationException("GUI設定プロジェクトのビルド結果が見つかりません。");

                using var reader = new StreamReader(stream);
                var html = await reader.ReadToEndAsync();

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(html);
            });

            // 画面初期表示時データ読み込み処理
            app.MapGet("/load", async context => {
                var schema = MutableSchema.FromXmlDocument(_project.SchemaXmlPath);

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(new InitialLoadData {
                    // このプロパティ名やデータの内容はGUIアプリ側の InitialLoadData の型と合わせる必要がある
                    ProjectRoot = _project.SolutionRoot,
                    EditingXmlFilePath = _project.SchemaXmlPath,
                    Config = schema.Config,
                    Nodes = schema.ToList(),
                    SchemaNodeTypes = NODE_TYPE.Values.ToList(),
                    OptionalAttributes = ATTR_DEF.Values.ToList(),
                }.ConvertToJson());
            });

            // mermaid.js によるグラフ表示
            app.MapPost("/mermaid", async context => {
                try {
                    var schema = await MutableSchema.FromHttpRequest(context.Request.Body);
                    var onlyRoot = context.Request.Query.ContainsKey("only-root");

                    // グラフ中のノードのIDは整数の連番
                    var nodeIdDict = schema
                        .Select((node, index) => new { node, index })
                        .ToDictionary(x => x.node.UniqueId, x => x.index);

                    // 集約メンバーを表示するとノードが多すぎて見づらいことが多いため集約のみ表示
                    var VIEW_AGGRTEGATE_TYPES = new[] { WriteModel.Key, ReadModel.Key, WriteRead.Key, Command.Key };
                    var rootNodes = schema
                        .RootNodes()
                        .Where(n => VIEW_AGGRTEGATE_TYPES.Contains(schema.GetRoot(n).Type))
                        .ToArray();

                    // ref
                    var existsInitialOrTerminal = rootNodes
                        .Concat(rootNodes.SelectMany(n => EnumerateDescendantSubgraphNodes(n)))
                        .ToDictionary(n => n.UniqueId);
                    var references = schema
                        .Where(n => n.GetNodeType()?.HasFlag(E_NodeType.Ref) == true)
                        .Select(n => new {
                            // 参照しているノード同士の線
                            DescToDesc = new {
                                Initial = schema.GetParent(n)?.UniqueId,
                                Terminal = n.Type?.Substring(MutableSchemaNode.REFTO_PREFIX.Length),
                            },
                            // 参照しているノードのルート同士の線（ルート集約のみ表示するオプションの場合に必要）
                            RootToRoot = new {
                                Initial = schema.GetRoot(n)?.UniqueId,
                                Terminal = schema.GetRoot(schema.FindRefToNode(n.Type) ?? throw new InvalidOperationException())?.UniqueId,
                            },
                            Label = string.IsNullOrEmpty(n.DisplayName) ? "???(Ref)" : $"{n.DisplayName}(Ref)",
                        })
                        // 視認性向上のために矢印の根元と先端が同じものは1本の矢印にまとめる
                        .GroupBy(n => onlyRoot ? n.RootToRoot : n.DescToDesc)
                        .Where(n => n.Key.Initial != null
                                 && n.Key.Terminal != null
                                 && existsInitialOrTerminal.ContainsKey(n.Key.Initial)
                                 && existsInitialOrTerminal.ContainsKey(n.Key.Terminal));

                    // グラフの方向
                    var graphDirection = context.Request.Query.TryGetValue("graph-direction", out var d)
                        ? (string?)d
                        : "RL";

                    // レンダリング
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync($$"""
                        graph {{graphDirection}};
                        {{rootNodes.SelectTextTemplate(node => $$"""
                          {{WithIndent(RenderSubgraphRecursively(node), "  ")}}
                        """)}}
                        {{references.SelectTextTemplate(g => $$"""
                          {{nodeIdDict[g.Key.Initial!]}} --"{{g.First().Label.Replace("\"", "”")}}{{(g.Skip(1).Any() ? $"など計{g.Count()}個の参照" : "")}}"--> {{nodeIdDict[g.Key.Terminal!]}}
                        """)}}
                        """.Replace(SKIP_MARKER, string.Empty), new UTF8Encoding(false, false));

                    IEnumerable<MutableSchemaNode> EnumerateDescendantSubgraphNodes(MutableSchemaNode node) {
                        foreach (var child in EnumerateChildSubgraphNodes(node)) {
                            yield return child;

                            foreach (var desc in EnumerateDescendantSubgraphNodes(child)) {
                                yield return desc;
                            }
                        }
                    }
                    IEnumerable<MutableSchemaNode> EnumerateChildSubgraphNodes(MutableSchemaNode node) {
                        foreach (var childNode in schema.GetChildren(node)) {
                            var nodeType = childNode.GetNodeType();
                            if (nodeType?.HasFlag(E_NodeType.Aggregate) == true || nodeType == E_NodeType.Variation) {
                                yield return childNode;
                            }
                        }
                    }

                    string RenderSubgraphRecursively(MutableSchemaNode node) {
                        var displayName = string.IsNullOrEmpty(node.DisplayName)
                            ? "???"
                            : node.DisplayName;
                        displayName += node.Type != null && NODE_TYPE.TryGetValue(node.Type, out var type)
                            ? $"({type.DisplayName})"
                            : $"(???)";

                        return $$"""
                            subgraph {{nodeIdDict[node.UniqueId]}}["{{displayName.Replace("\"", "”")}}"]
                            {{If(!onlyRoot, () => $$"""
                            {{EnumerateChildSubgraphNodes(node).SelectTextTemplate(childNode => $$"""
                              {{WithIndent(RenderSubgraphRecursively(childNode), "  ")}}
                            """)}}
                            """)}}
                            end
                            """;
                    }

                } catch (Exception ex) {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(new[] { ex.Message }.ConvertToJson());
                }
            });

            // 編集中のバリデーション
            app.MapPost("/validate", async context => {
                try {
                    var schema = await MutableSchema.FromHttpRequest(context.Request.Body);
                    var errors = ValidationError.ToErrorObjectJson(schema.CollectVaridationErrors());

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(errors);

                } catch (Exception ex) {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(new[] { ex.Message }.ConvertToJson());
                }
            });

            app.MapPost("/save", async context => {
                try {
                    // バリデーション
                    var schema = await MutableSchema.FromHttpRequest(context.Request.Body);
                    var errors = schema.CollectVaridationErrors().ToArray();
                    if (errors.Length > 0) {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.ContentType = "application/json";
                        var errorsJson = ValidationError.ToErrorObjectJson(errors);
                        await context.Response.WriteAsync(errorsJson);
                        return;
                    }

                    // 保存
                    schema.Save(_project.SchemaXmlPath);

                    // コード自動生成かけなおし
                    if (context.Request.Query.ContainsKey("build")) {
                        _project.CodeGenerator.GenerateCode();
                    }

                    context.Response.StatusCode = (int)HttpStatusCode.OK;

                } catch (Exception ex) {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync(new[] { ex.Message }.ConvertToJson());
                }
            });

            return app;
        }

        /// <summary>
        /// 深さの情報だけを持っている <see cref="MutableSchemaNode"/> の一覧に対して、
        /// 祖先や子孫を取得するといったツリー構造データに対する操作を提供します。
        /// </summary>
        private class MutableSchema : IReadOnlyList<MutableSchemaNode> {

            #region 入出力
            /// <summary>
            /// HTTPリクエストボディから <see cref="MutableSchema"/> のインスタンスを作成
            /// </summary>
            public static async Task<MutableSchema> FromHttpRequest(Stream httpRequestBody) {
                using var sr = new StreamReader(httpRequestBody);
                var json = await sr.ReadToEndAsync();
                var obj = json.ParseAsJson<ClientRequest>();

                return new MutableSchema(obj.Config!, obj.Nodes ?? []);
            }
            /// <summary>
            /// XMLドキュメントから <see cref="MutableSchema"/> のインスタンスを作成
            /// </summary>
            public static MutableSchema FromXmlDocument(string entryXmlFilePath) {
                XDocument? entry = null;
                var xDocuments = GetXDocumentsRecursively(entryXmlFilePath).ToList();
                var rootNameSpace = entry!.Root?.Name.LocalName ?? string.Empty;
                var allNodes = xDocuments.SelectMany(GetSchemaNodes).ToList();
                return new MutableSchema(Config.FromXml(entry!), allNodes);

                IEnumerable<XDocumentAndPath> GetXDocumentsRecursively(string xmlFilePath) {
                    var xDocument = XDocument.Load(xmlFilePath);
                    if (xmlFilePath == entryXmlFilePath) entry = xDocument;
                    yield return new() { XDocument = xDocument, FilePath = xmlFilePath };

                    // <Include Path="(略)" /> で他のXMLファイルを読み込む
                    foreach (var el in xDocument.Root?.Elements() ?? []) {
                        if (el.Name.LocalName != AppSchemaXml.INCLUDE) continue;

                        var path = el.Attribute(AppSchemaXml.PATH)?.Value;
                        if (string.IsNullOrWhiteSpace(path)) continue;

                        var dirName = Path.GetDirectoryName(xmlFilePath);
                        var absolutePath = dirName == null
                            ? path
                            : Path.GetFullPath(Path.Combine(dirName, path));
                        foreach (var includedXDocument in GetXDocumentsRecursively(absolutePath)) {
                            yield return includedXDocument;
                        }
                    }
                }

                IEnumerable<MutableSchemaNode> GetSchemaNodes(XDocumentAndPath doc) {
                    foreach (var el in doc.XDocument.Root?.Elements() ?? []) {
                        if (el.Name.LocalName == AppSchemaXml.INCLUDE) continue;

                        var nodes = MutableSchemaNode.FromXElement(
                            el,
                            doc.FilePath,
                            refToPath => {
                                var found = xDocuments
                                    .Select(doc => doc.XDocument.XPathSelectElement($"/{rootNameSpace}/{refToPath}"))
                                    .OfType<XElement>()
                                    .FirstOrDefault();
                                return found;
                            },
                            isAttributeKeys => {
                                foreach (var el in xDocuments.SelectMany(doc => doc.XDocument.Root?.Elements() ?? [])) {
                                    if (el.Attribute(MutableSchemaNode.IS)?.Value.Contains(EnumDef.Key) != true) continue;
                                    if (!isAttributeKeys.Contains(el.Name.LocalName)) continue;
                                    return el;
                                }
                                return null;
                            },
                            isAttributeKeys => {
                                foreach (var el in xDocuments.SelectMany(doc => doc.XDocument.Root?.Elements() ?? [])) {
                                    if (el.Attribute(MutableSchemaNode.IS)?.Value.Contains(ValueObjectDef.Key) != true) continue;
                                    if (!isAttributeKeys.Contains(el.Name.LocalName)) continue;
                                    return el;
                                }
                                return null;
                            });

                        foreach (var node in nodes) {
                            yield return node;
                        }
                    }
                }
            }
            /// <summary>
            /// XMLドキュメントに保存する。
            /// ファイルパスは <see cref="MutableSchemaNode.XmlFileFullPath"/> が持っている。
            /// </summary>
            public void Save(string entryFilePath) {
                var entryDocument = XDocument.Load(entryFilePath);
                var documents = new Dictionary<string, XDocument>() { [entryFilePath] = entryDocument };

                // ルート要素を編集
                if (entryDocument.Root == null) entryDocument.Add(new XElement(Config.RootNamespace));
                Config.ToXElement(entryDocument.Root!);

                // 既存のルート直下要素を削除
                entryDocument.Root?.RemoveNodes();

                // 編集後ノードを各XMLのルート直下へ追加
                foreach (var rootNode in RootNodes()) {
                    var xNodes = MutableSchemaNode.ToXmlNodes(rootNode, this);

                    if (string.IsNullOrWhiteSpace(rootNode.XmlFileFullPath)) {
                        entryDocument.Root?.Add(xNodes);

                    } else if (documents.TryGetValue(rootNode.XmlFileFullPath, out var xDocument)) {
                        xDocument.Root?.Add(xNodes);

                    } else {
                        xDocument = XDocument.Load(rootNode.XmlFileFullPath);
                        documents[rootNode.XmlFileFullPath] = xDocument;

                        // 既存のルート直下要素を削除、編集後ノードを各XMLのルート直下へ追加
                        xDocument.Root?.RemoveNodes();
                        xDocument.Root?.Add(xNodes);

                        // エントリーXMLのIncludeに登録
                        var dirName = Path.GetDirectoryName(entryFilePath);
                        var relativePath = dirName == null
                            ? rootNode.XmlFileFullPath
                            : Path.GetRelativePath(dirName, rootNode.XmlFileFullPath);
                        var include = new XElement(AppSchemaXml.INCLUDE);
                        include.SetAttributeValue(AppSchemaXml.PATH, relativePath);
                        entryDocument.Root?.Add(include);
                    }
                }

                // 保存
                foreach (var doc in documents) {
                    using var writer = XmlWriter.Create(doc.Key, new() {
                        Indent = true,
                        Encoding = new UTF8Encoding(false, false),
                        NewLineChars = "\n",
                    });
                    doc.Value.Save(writer);
                }
            }

            /// <summary>
            /// NijoApplicationBuidlerのアプリケーションスキーマを構築します。
            /// </summary>
            public AppSchema BuildAppSchema() {
                var validationErrors = CollectVaridationErrors().ToArray();
                if (validationErrors.Length > 0) {
                    throw new InvalidOperationException($"バリデーションエラーがある状態でスキーマを構築することはできません: {validationErrors.Select(e => e.Message).Join(", ")}");
                }

                var memberTypeResolver = MemberTypeResolver.Default();
                var graphNodes = new List<IGraphNode>();
                var graphEdges = new List<GraphEdgeInfo>();

                // -------------------------------------------
                // 列挙体定義の登録
                var enums = new List<EnumDefinition>();
                foreach (var node in RootNodes().Where(n => n.Type == EnumDef.Key)) {
                    var usedInt = GetChildren(node)
                        .Where(child => !string.IsNullOrWhiteSpace(child.TypeDetail))
                        .Select(child => int.Parse(child.TypeDetail!))
                        .ToArray();
                    var unusedInt = usedInt.Length == 0
                        ? 0
                        : (usedInt.Max() + 1);
                    var items = new List<EnumDefinition.Item>();
                    foreach (var child in GetChildren(node)) {
                        int value;
                        if (string.IsNullOrWhiteSpace(child.TypeDetail)) {
                            value = unusedInt;
                            unusedInt++;
                        } else {
                            value = int.Parse(child.TypeDetail!);
                        }
                        items.Add(new EnumDefinition.Item {
                            DisplayName = child.DisplayName,
                            PhysicalName = child.GetPhysicalName(),
                            Value = value,
                        });
                    }
                    if (!EnumDefinition.TryCreate(node.GetPhysicalName(), items, out var enumDef, out var errors)) {
                        throw new InvalidOperationException($"列挙体の構築時にエラーが発生しました。:{errors.Join(", ")}");
                    }
                    enums.Add(enumDef);
                    memberTypeResolver.Register(MutableSchemaNode.ENUM_PREFIX + node.UniqueId, new EnumList(enumDef));
                }

                // -------------------------------------------
                // 列挙体定義の登録（variation switch）
                var variationGroups = this
                    .Where(node => node.Type == VariationItem.Key)
                    .GroupBy(node => GetParent(node) ?? throw new InvalidOperationException());
                foreach (var variationGroup in variationGroups) {
                    var enumValues = new List<EnumDefinition.Item>();
                    foreach (var node in variationGroup) {
                        var strValue = node.TypeDetail;
                        if (!int.TryParse(strValue, out var intValue)) throw new InvalidOperationException($"Variationのキー '{strValue}' が整数ではありません。");
                        enumValues.Add(new EnumDefinition.Item {
                            Value = intValue,
                            PhysicalName = node.GetPhysicalName(),
                            DisplayName = node.DisplayName,
                        });
                    }
                    if (!EnumDefinition.TryCreate($"E_{variationGroup.Key.GetPhysicalName()}", enumValues, out var enumDef, out var errors)) {
                        throw new InvalidOperationException($"列挙体の構築時にエラーが発生しました。:{errors.Join(", ")}");
                    }
                    enums.Add(enumDef);
                }

                // -------------------------------------------
                // 値オブジェクト定義の登録
                foreach (var node in RootNodes().Where(n => n.Type == ValueObjectDef.Key)) {
                    // メンバー型解決
                    var voMember = new ValueObjectMember(
                        node.GetPhysicalName(),
                        Models.ValueObjectModel.PRIMITIVE_TYPE,
                        E_SearchBehavior.PartialMatch); // 当面文字列型の部分一致しか使わないので決め打ち
                    memberTypeResolver.Register(MutableSchemaNode.VALUE_OBJECT_PREFIX + node.UniqueId, voMember);

                    // 集約（有向グラフの頂点）を登録
                    var nodeId = node.ToGraphNodeId(this);
                    var aggregate = new Aggregate(
                        nodeId,
                        node.GetPhysicalName(),
                        node.CreateAggregateOption(this));
                    graphNodes.Add(aggregate);
                }

                // -------------------------------------------
                // 動的列挙体（区分マスタ）種類の登録
                var dynamicEnumTypes = RootNodes()
                    .Where(n => n.Type == DynamicEnumDefType.Key)
                    .Select(n => new DynamicEnumTypeInfo {
                        DisplayName = n.DisplayName,
                        PhysicalName = n.GetPhysicalName(),
                        TypeKey = n.TypeDetail!,
                    });

                // -------------------------------------------
                // WriteModel, ReadModel, Command の登録
                foreach (var node in this) {
                    var root = GetRoot(node);
                    if (root.Type != WriteModel.Key
                        && root.Type != ReadModel.Key
                        && root.Type != WriteRead.Key
                        && root.Type != Command.Key) continue;

                    var nodeType = node.GetNodeType();
                    if (nodeType?.HasFlag(E_NodeType.Aggregate) == true) {

                        // 集約（有向グラフの頂点）を登録
                        var nodeId = node.ToGraphNodeId(this);
                        var aggregate = new Aggregate(
                            nodeId,
                            node.GetPhysicalName(),
                            node.CreateAggregateOption(this));
                        graphNodes.Add(aggregate);

                        // 親との関係性（有向グラフの辺）を登録
                        var parent = GetParent(node);
                        if (parent?.Type == Variation.Key) {
                            // variation switch のところはUI上では2段階の入れ子のため
                            parent = GetParent(parent) ?? throw new InvalidOperationException();
                        }
                        if (parent != null) {
                            graphEdges.Add(new GraphEdgeInfo {
                                Initial = parent.ToGraphNodeId(this),
                                Terminal = nodeId,
                                RelationName = node.GetPhysicalName(),
                                Attributes = new Dictionary<string, object?> {
                                    { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_PARENT_CHILD },
                                    { DirectedEdgeExtensions.REL_ATTR_MULTIPLE, aggregate.Options.IsArray == true },
                                    { DirectedEdgeExtensions.REL_ATTR_VARIATIONSWITCH, aggregate.Options.IsVariationGroupMember?.Key ?? string.Empty },
                                    { DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUPNAME, aggregate.Options.IsVariationGroupMember?.GroupName ?? string.Empty },
                                    { DirectedEdgeExtensions.REL_ATTR_VARIATIONGROUP_DISPLAYNAME, aggregate.Options.IsVariationGroupMember?.GroupDisplayName ?? string.Empty },
                                    { DirectedEdgeExtensions.REL_ATTR_IS_COMBO, aggregate.Options.IsCombo == true },
                                    { DirectedEdgeExtensions.REL_ATTR_IS_RADIO, aggregate.Options.IsRadio == true },
                                    { DirectedEdgeExtensions.REL_ATTR_IS_PRIMARY, aggregate.Options.IsPrimary == true },
                                    { DirectedEdgeExtensions.REL_ATTR_IS_REQUIRED, aggregate.Options.IsRequiredArray == true },
                                    { DirectedEdgeExtensions.REL_ATTR_INVISIBLE_IN_GUI, aggregate.Options.InvisibleInGui == true },
                                    { DirectedEdgeExtensions.REL_ATTR_DISPLAY_NAME, aggregate.Options.DisplayName },
                                    { DirectedEdgeExtensions.REL_ATTR_DB_NAME, aggregate.Options.DbName },
                                    { DirectedEdgeExtensions.REL_ATTR_MEMBER_ORDER, _list.IndexOf(node) },
                                },
                            });
                        }

                    } else if (nodeType == E_NodeType.Variation) {
                        // variation switch のノードと対応する有向グラフの辺や頂点は無い

                    } else if (nodeType == E_NodeType.Ref) {
                        var parent = GetParent(node) ?? throw new InvalidOperationException();
                        var refTo = this.FindRefToNode(node.Type) ?? throw new InvalidOperationException($"参照先 '{node.Type}' が見つかりません。");
                        var options = node.CreateAggregateMemberOption(this);

                        // ref-toはグラフの辺だけ登録
                        graphEdges.Add(new GraphEdgeInfo {
                            Initial = parent.ToGraphNodeId(this),
                            Terminal = refTo.ToGraphNodeId(this),
                            RelationName = node.GetPhysicalName(),
                            Attributes = new Dictionary<string, object?> {
                                { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_REFERENCE },
                                { DirectedEdgeExtensions.REL_ATTR_IS_PRIMARY, options.IsPrimary == true },
                                { DirectedEdgeExtensions.REL_ATTR_IS_INSTANCE_NAME, options.IsDisplayName == true },
                                { DirectedEdgeExtensions.REL_ATTR_IS_NAME_LIKE, options.IsNameLike == true },
                                { DirectedEdgeExtensions.REL_ATTR_IS_REQUIRED, options.IsRequired == true },
                                { DirectedEdgeExtensions.REL_ATTR_IS_WIDE, options.WideInVForm },
                                { DirectedEdgeExtensions.REL_ATTR_INVISIBLE_IN_GUI, options.InvisibleInGui == true },
                                { DirectedEdgeExtensions.REL_ATTR_SINGLEVIEW_CUSTOM_UI_COMPONENT_NAME, options.SingleViewCustomUiComponentName },
                                { DirectedEdgeExtensions.REL_ATTR_SEARCHCONDITION_CUSTOM_UI_COMPONENT_NAME, options.SearchConditionCustomUiComponentName },
                                { DirectedEdgeExtensions.REL_ATTR_DISPLAY_NAME, options.DisplayName },
                                { DirectedEdgeExtensions.REL_ATTR_DB_NAME, options.DbName },
                                { DirectedEdgeExtensions.REL_ATTR_MEMBER_ORDER, _list.IndexOf(node) },
                                { DirectedEdgeExtensions.REL_ATTR_DYNAMIC_ENUM_TYPE_NAME, options.DynamicEnumTypePhysicalName },
                                { DirectedEdgeExtensions.REL_ATTR_PROXY, options.ForeignKeyProxies },
                            },
                        });

                    } else if (nodeType?.HasFlag(E_NodeType.AggregateMember) == true) {

                        // 集約メンバーの登録（グラフノード）
                        if (node.Type == null || !memberTypeResolver.TryResolve(node.Type, out var memberType)) {
                            throw new InvalidOperationException($"メンバーの型 '{node.Type}' の種類が定まりません。");
                        }
                        var nodeId = node.ToGraphNodeId(this);
                        var options = node.CreateAggregateMemberOption(this);
                        graphNodes.Add(new AggregateMemberNode {
                            Id = nodeId,
                            MemberName = node.GetPhysicalName(),
                            MemberType = memberType,
                            IsKey = options.IsPrimary == true,
                            IsDisplayName = options.IsDisplayName == true,
                            IsNameLike = options.IsNameLike == true,
                            IsRequired = options.IsRequired == true,
                            InvisibleInGui = options.InvisibleInGui == true,
                            SingleViewCustomUiComponentName = options.SingleViewCustomUiComponentName,
                            SearchConditionCustomUiComponentName = options.SearchConditionCustomUiComponentName,
                            UiWidth = options.UiWidthRem,
                            WideInVForm = options.WideInVForm,
                            IsCombo = options.IsCombo == true,
                            IsRadio = options.IsRadio == true,
                            DisplayName = options.DisplayName,
                            DbName = options.DbName,
                            SearchBehavior = options.SearchBehavior,
                            CharacterType = options.CharacterType,
                            MaxLength = options.MaxLength,
                            EnumSqlParamType = options.EnumSqlParamType,
                        });

                        // 集約メンバーの登録（親とこのメンバーの間のエッジ）
                        var parent = GetParent(node) ?? throw new InvalidOperationException();
                        graphEdges.Add(new GraphEdgeInfo {
                            Initial = parent.ToGraphNodeId(this),
                            Terminal = nodeId,
                            RelationName = node.GetPhysicalName(),
                            Attributes = new Dictionary<string, object?> {
                                { DirectedEdgeExtensions.REL_ATTR_RELATION_TYPE, DirectedEdgeExtensions.REL_ATTRVALUE_HAVING },
                                { DirectedEdgeExtensions.REL_ATTR_MEMBER_ORDER, _list.IndexOf(node) },
                            },
                        });
                    }
                }

                // -------------------------------------------
                // グラフを作成してアプリケーションスキーマを構築して返す
                if (!DirectedGraph.TryCreate(graphNodes, graphEdges, out var graph, out var errors1)) {
                    throw new InvalidOperationException($"列挙体の構築時にエラーが発生しました。:{errors1.Join(", ")}");
                }
                var appSchema = new AppSchema(Config.RootNamespace, graph, enums, dynamicEnumTypes.ToArray());
                return appSchema;
            }
            #endregion 入出力


            private MutableSchema(Config config, IList<MutableSchemaNode> list) {
                Config = config;
                _list = list;
            }

            /// <summary>アプリケーション全体に対する設定</summary>
            public Config Config { get; }

            private readonly IList<MutableSchemaNode> _list;

            #region ツリー構造
            /// <summary>
            /// ルート集約のみ列挙する
            /// </summary>
            public IEnumerable<MutableSchemaNode> RootNodes() {
                foreach (var item in _list) {
                    if (item.Depth == 0) yield return item;
                }
            }
            /// <summary>
            /// 直近の親を返す
            /// </summary>
            public MutableSchemaNode? GetParent(MutableSchemaNode node) {
                var currentIndex = _list.IndexOf(node);
                if (currentIndex == -1) throw new InvalidOperationException($"{node}はこの一覧に属していません。");
                while (true) {
                    currentIndex--;
                    if (currentIndex < 0) return null;

                    var maybeParent = _list[currentIndex];
                    if (maybeParent.Depth < node.Depth) return maybeParent;
                }
            }
            /// <summary>
            /// ルート要素を返す
            /// </summary>
            public MutableSchemaNode GetRoot(MutableSchemaNode node) {
                return GetAncestors(node).FirstOrDefault() ?? node;
            }
            /// <summary>
            /// 祖先を返す。より階層が浅いほうが先。
            /// </summary>
            public IEnumerable<MutableSchemaNode> GetAncestors(MutableSchemaNode node) {
                var ancestors = new List<MutableSchemaNode>();
                var currentDepth = node.Depth;
                var currentIndex = _list.IndexOf(node);
                if (currentIndex == -1) throw new InvalidOperationException($"{node}はこの一覧に属していません。");
                while (true) {
                    if (currentDepth == 0) break;

                    currentIndex--;
                    if (currentIndex < 0) break;

                    var maybeAncestor = _list[currentIndex];
                    if (maybeAncestor.Depth < currentDepth) {
                        ancestors.Add(maybeAncestor);
                        currentDepth = maybeAncestor.Depth;
                    }
                }
                ancestors.Reverse();
                return ancestors;
            }
            /// <summary>
            /// 直近の子を返す
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            public IEnumerable<MutableSchemaNode> GetChildren(MutableSchemaNode node) {
                var currentIndex = _list.IndexOf(node);
                if (currentIndex == -1) throw new InvalidOperationException($"{node}はこの一覧に属していません。");
                while (true) {
                    currentIndex++;
                    if (currentIndex >= _list.Count) yield break;

                    var maybeChild = _list[currentIndex];
                    if (maybeChild.Depth <= node.Depth) yield break;

                    if (GetParent(maybeChild) == node) yield return maybeChild;
                }
            }
            /// <summary>
            /// 子孫を返す
            /// </summary>
            public IEnumerable<MutableSchemaNode> GetDescendants(MutableSchemaNode node) {
                var currentIndex = _list.IndexOf(node);
                if (currentIndex == -1) throw new InvalidOperationException($"{node}はこの一覧に属していません。");
                while (true) {
                    currentIndex++;
                    if (currentIndex >= _list.Count) yield break;

                    var maybeDescendant = _list[currentIndex];
                    if (maybeDescendant.Depth <= node.Depth) yield break;

                    yield return maybeDescendant;
                }
            }
            #endregion ツリー構造


            /// <summary>
            /// エラーを収集します。
            /// </summary>
            public IEnumerable<ValidationError> CollectVaridationErrors() {

                // 物理名の重複チェック
                var samePhysicalNameGroups = _list
                    .Where(node => node.GetNodeType()?.HasFlag(E_NodeType.Aggregate) == true)
                    .GroupBy(node => node.GetPhysicalName())
                    .Where(group => group.Count() >= 2);
                foreach (var group in samePhysicalNameGroups) {
                    foreach (var node in group) {
                        yield return new ValidationError {
                            Node = node,
                            Key = PhysicalName.Key,
                            Message = $"物理名「{group.Key}」が重複しています。",
                        };
                    }
                }

                // 区分マスタは1個だけ
                var dynamicEnumWriteModels = _list
                    .Where(n => n.AttrValues?.Any(a => a.Key == IsDynamicEnumWriteModel.Key) == true)
                    .ToArray();
                if (dynamicEnumWriteModels.Length >= 2) {
                    foreach (var node in dynamicEnumWriteModels) {
                        yield return new ValidationError {
                            Key = IsDynamicEnumWriteModel.Key,
                            Node = node,
                            Message = $"{IsDynamicEnumWriteModel.DisplayName}はアプリケーション全体で1つしか定義できません。",
                        };
                    }
                }

                // ノード1個ずつのチェック
                var errorList = new List<string>();
                foreach (var node in _list) {
                    // 行全体に対するエラー
                    errorList.Clear();
                    node.Validate(this, errorList);
                    foreach (var error in errorList) {
                        yield return new ValidationError {
                            Node = node,
                            Key = ValidationError.ERR_TO_ROW,
                            Message = error,
                        };
                    }

                    // enumの要素のバリデーション（いろいろ特殊）
                    var root = GetRoot(node);
                    if (node.Depth > 0 && root.Type == EnumDef.Key) {
                        // enumの要素なのに型が指定されている
                        if (!string.IsNullOrWhiteSpace(node.Type)) {
                            yield return new ValidationError {
                                Node = node,
                                Key = ValidationError.ERR_TO_TYPE,
                                Message = "列挙体の要素に型を指定することはできません。",
                            };
                        }

                        foreach (var attrValue in node.AttrValues ?? []) {
                            // 列挙体の値であっても指定可能な属性の場合はcontinue
                            if (attrValue.Key == PhysicalName.Key) continue;

                            yield return new ValidationError {
                                Node = node,
                                Key = attrValue.Key!,
                                Message = "列挙体の要素にこの属性を指定することはできません。",
                            };
                        }

                        if (!string.IsNullOrWhiteSpace(node.TypeDetail)) {
                            if (!int.TryParse(node.TypeDetail, out var intEnumKey)) {
                                // 列挙体の値のキーは整数のみ
                                yield return new ValidationError {
                                    Node = node,
                                    Key = ValidationError.ERR_TO_TYPE_DETAIL,
                                    Message = "整数のみ指定できます。",
                                };

                            } else {
                                // 列挙体の値のキー重複チェック
                                var parent = GetParent(node);
                                var siblings = parent == null ? [] : GetChildren(parent);
                                foreach (var sibling in siblings) {
                                    if (sibling == node) continue;
                                    if (string.IsNullOrWhiteSpace(sibling.TypeDetail)) continue;
                                    if (!int.TryParse(sibling.TypeDetail, out var siblingKey)) continue;
                                    if (siblingKey != intEnumKey) continue;

                                    yield return new ValidationError {
                                        Node = node,
                                        Key = ValidationError.ERR_TO_TYPE_DETAIL,
                                        Message = "キーが重複しています。",
                                    };
                                }
                            }
                        }


                        // enumの要素はこれ以外のバリデーション不要
                        continue;
                    }

                    // 型と型詳細に対するエラー
                    if (string.IsNullOrWhiteSpace(node.Type)) {
                        // 型未指定
                        yield return new ValidationError {
                            Node = node,
                            Key = ValidationError.ERR_TO_TYPE,
                            Message = "型を指定してください。",
                        };
                    } else if (node.Type.StartsWith(MutableSchemaNode.REFTO_PREFIX)) {
                        // ref-to
                        var refTo = this.FindRefToNode(node.Type);
                        if (refTo == null) {
                            yield return new ValidationError {
                                Node = node,
                                Key = ValidationError.ERR_TO_TYPE,
                                Message = "参照先に指定されている項目が見つかりません。",
                            };
                        } else if (refTo.GetNodeType()?.HasFlag(E_NodeType.Aggregate) != true) {
                            yield return new ValidationError {
                                Node = node,
                                Key = ValidationError.ERR_TO_TYPE,
                                Message = "参照先に指定されている項目はref-toの参照先として使えません。",
                            };
                        } else {
                            // モデルによって参照できるものが異なる
                            if (node.IsWriteModel(this)) {
                                if (!refTo.IsWriteModel(this)) {
                                    yield return new ValidationError {
                                        Node = node,
                                        Key = ValidationError.ERR_TO_TYPE,
                                        Message = "DBの外部キーが定義できるようにするため、WriteModelが参照する先はWriteModelである必要があります。",
                                    };
                                }
                            }
                            if (node.IsReadModel(this)) {
                                if (!refTo.IsReadModel(this)) {
                                    yield return new ValidationError {
                                        Node = node,
                                        Key = ValidationError.ERR_TO_TYPE,
                                        Message = "UI部品などが利用できるかどうかが異なるため、ReadModelの参照先はReadModelである必要があります。",
                                    };
                                }
                            }
                            if (node.IsCommandModel(this)) {
                                if (!refTo.IsReadModel(this)) {
                                    yield return new ValidationError {
                                        Node = node,
                                        Key = ValidationError.ERR_TO_TYPE,
                                        Message = "UI部品などが利用できるかどうかが異なるため、CommandModelの参照先はReadModelである必要があります。",
                                    };
                                }
                            }
                        }

                        if (node.IsRefToDynamicEnum(this)) {
                            if (node.AttrValues?.Any(a => a.Key == DynamicEnumTypePhysicalName.Key) != true) {
                                yield return new ValidationError {
                                    Node = node,
                                    Key = DynamicEnumTypePhysicalName.Key,
                                    Message = "区分マスタの種類が指定されていません。",
                                };
                            }
                        }

                    } else if (node.Type.StartsWith(MutableSchemaNode.ENUM_PREFIX)) {
                        // 列挙体
                        var uniqueId = node.Type.Substring(MutableSchemaNode.ENUM_PREFIX.Length);
                        if (!RootNodes().Any(n => n.UniqueId == uniqueId
                                                    && n.Type == EnumDef.Key)) {
                            yield return new ValidationError {
                                Node = node,
                                Key = ValidationError.ERR_TO_TYPE,
                                Message = "指定された列挙体定義が見つかりません。",
                            };
                        }
                    } else if (node.Type.StartsWith(MutableSchemaNode.VALUE_OBJECT_PREFIX)) {
                        // 値オブジェクト
                        var uniqueId = node.Type.Substring(MutableSchemaNode.VALUE_OBJECT_PREFIX.Length);
                        if (!RootNodes().Any(n => n.UniqueId == uniqueId
                                               && n.Type == ValueObjectDef.Key)) {
                            yield return new ValidationError {
                                Node = node,
                                Key = ValidationError.ERR_TO_TYPE,
                                Message = "指定された値オブジェクト定義が見つかりません。",
                            };
                        }
                    } else {
                        // 上記以外
                        if (!NODE_TYPE.TryGetValue(node.Type, out var typeDef)) {
                            yield return new ValidationError {
                                Node = node,
                                Key = ValidationError.ERR_TO_TYPE,
                                Message = $"型 '{node.Type}' が不正です。",
                            };
                        } else {
                            errorList.Clear();
                            typeDef.Validate(node, this, errorList);
                            foreach (var error in errorList) {
                                yield return new ValidationError {
                                    Node = node,
                                    Key = ValidationError.ERR_TO_TYPE,
                                    Message = error,
                                };
                            }
                        }
                    }

                    // オプショナル属性に関するエラー
                    foreach (var attrValue in node.AttrValues ?? []) {
                        if (attrValue.Key == null || !ATTR_DEF.TryGetValue(attrValue.Key, out var optionDef)) {
                            yield return new ValidationError {
                                Node = node,
                                Key = attrValue.Key!,
                                Message = $"オプショナル属性の型 '{attrValue.Key}' が不正です。",
                            };
                        } else {
                            errorList.Clear();
                            optionDef.Validate(attrValue.Value, node, this, errorList);
                            foreach (var error in errorList) {
                                yield return new ValidationError {
                                    Node = node,
                                    Key = attrValue.Key!,
                                    Message = error,
                                };
                            }
                        }
                    }
                }
            }


            #region IReadOnlyListの実装
            public MutableSchemaNode this[int index] => _list[index];
            public int Count => _list.Count;
            public IEnumerator<MutableSchemaNode> GetEnumerator() {
                return _list.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
            #endregion IReadOnlyListの実装

            /// <summary>
            /// ref-to:xxxxxxxxx の文字列から "xxxxxxxxxx" のIDに該当するノードを検索して返す
            /// </summary>
            /// <param name="type"><see cref="MutableSchemaNode.Type"/> を渡してください。</param>
            internal MutableSchemaNode? FindRefToNode(string? type) {
                var uniqueId = type?.Substring(MutableSchemaNode.REFTO_PREFIX.Length);
                return this.SingleOrDefault(n => n.UniqueId == uniqueId);
            }
        }
        private class ValidationError {
            /// <summary>どの集約またはメンバーでエラーが発生したか</summary>
            public required MutableSchemaNode Node { get; init; }
            /// <summary>
            /// どの項目でエラーが発生したか。
            /// <see cref="ERR_TO_ROW"/> の場合、メンバー全体に対するエラー。
            /// <see cref="ERR_TO_TYPE"/>, <see cref="ERR_TO_TYPE_DETAIL"/>, <see cref="ERR_TO_COMMENT"/> の場合、それぞれの項目に対するエラー。
            /// オプショナル属性に対するエラーは <see cref="OptionalAttributeDef.Key"/> と同じ文字列。
            /// </summary>
            public required string Key { get; init; }
            /// <summary>エラーメッセージ</summary>
            public required string Message { get; init; }

            // ここの定義は TypeScript の useColumnDef と合わせる必要あり
            public const string ERR_TO_ROW = "-";
            public const string ERR_TO_TYPE = "type";
            public const string ERR_TO_TYPE_DETAIL = "typeDetail";
            public const string ERR_TO_COMMENT = "comment";

            /// <summary>
            /// バリデーションエラーの配列をTypeScript側で処理しやすい形に変換する
            /// </summary>
            public static string ToErrorObjectJson(IEnumerable<ValidationError> errors) {
                /*
                 * 戻り値のJSONオブジェクトの例（※ルートオブジェクトのキーはn行目のデータのユニークIDの想定）
                 *
                 * {
                 *   "x34jrlfst": {
                 *     "type": ["【種類】タイプを指定してください。"],
                 *     "dbName": ["【DB名】DB名に記号は使えません。", "【DB名】DB名が長すぎます。"]
                 *   },
                 *   "xh6h5jlhd": {
                 *     "-": ["キーを指定してください。"]
                 *   }
                 * }
                 */
                var keyNameDict = ATTR_DEF.ToDictionary(x => x.Key, x => x.Value.DisplayName);
                keyNameDict[ERR_TO_TYPE] = "種類";
                keyNameDict[ERR_TO_TYPE_DETAIL] = "種類";
                keyNameDict[ERR_TO_COMMENT] = "コメント";

                var rootObject = new JsonObject();
                foreach (var group in errors.GroupBy(err => err.Node.UniqueId)) {
                    var uniqueId = group.Key;
                    var errorObjectByUniqueId = new JsonObject();

                    foreach (var errorsByKey in group.GroupBy(g => g.Key)) {
                        var errorArray = new JsonArray();
                        foreach (var error in errorsByKey) {
                            var message = errorsByKey.Key == ERR_TO_ROW
                                ? error.Message
                                : $"【{keyNameDict[error.Key]}】{error.Message}";
                            errorArray.Add(message);
                        }
                        errorObjectByUniqueId[errorsByKey.Key] = errorArray;
                    }

                    rootObject[uniqueId] = errorObjectByUniqueId;
                }
                return rootObject.ToJsonString(StringExtension.JsonSerializerOptions);
            }
        }


        /// <summary>
        /// サーバーからクライアントへ送るデータ
        /// </summary>
        private class InitialLoadData {
            [JsonPropertyName("projectRoot")]
            public string? ProjectRoot { get; set; }
            [JsonPropertyName("editingXmlFilePath")]
            public string? EditingXmlFilePath { get; set; }
            [JsonPropertyName("config")]
            public Config? Config { get; set; }
            [JsonPropertyName("aggregates")]
            public List<MutableSchemaNode>? Nodes { get; set; }
            [JsonPropertyName("aggregateOrMemberTypes")]
            public List<SchemaNodeTypeDef>? SchemaNodeTypes { get; set; }
            [JsonPropertyName("optionalAttributes")]
            public List<OptionalAttributeDef>? OptionalAttributes { get; set; }
        }
        /// <summary>
        /// クライアントからサーバーへ送るデータ
        /// </summary>
        private class ClientRequest {
            [JsonPropertyName("config")]
            public Config? Config { get; set; }
            [JsonPropertyName("aggregates")]
            public List<MutableSchemaNode>? Nodes { get; set; }
        }

        /// <summary>
        /// NijoApplicationBuilderのスキーマのノード。
        /// XMLのノードと1対1対応する。
        /// 不変ではない（つまりインスタンス作成後に状態を変更することができる）。
        /// UI上で編集される対象となるため、集約定義や集約メンバー定義として不正な状態であったとしても許容する。
        /// 親ノードや子ノードへの参照を持たない（親子関係は <see cref="MutableSchema"/> 経由で参照する）。
        /// </summary>
        private class MutableSchemaNode {
            [JsonPropertyName("depth")]
            public required int Depth { get; set; }
            [JsonPropertyName("uniqueId")]
            public required string UniqueId { get; set; }
            [JsonPropertyName("displayName")]
            public required string DisplayName { get; set; }
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            [JsonPropertyName("typeDetail")]
            public string? TypeDetail { get; set; }
            [JsonPropertyName("attrValues")]
            public List<OptionalAttributeValue>? AttrValues { get; set; }
            [JsonPropertyName("comment")]
            public string? Comment { get; set; }
            [JsonPropertyName("xmlFileFullPath")]
            public string? XmlFileFullPath { get; set; }

            #region is属性
            /// <summary>
            /// is="" 属性の内容
            /// </summary>
            private static IEnumerable<IsAttribute> ParseIsAttribute(string? isAttributeValue) {
                if (string.IsNullOrWhiteSpace(isAttributeValue)) yield break;

                var parsingKey = false;
                var parsingValue = false;
                var key = new List<char>();
                var value = new List<char>();
                foreach (var character in isAttributeValue) {
                    // 半角スペースが登場したら属性の区切り、コロンが登場したらキーと値の区切り、と解釈する。
                    if (character == ' ') {
                        if (parsingKey || parsingValue) {
                            yield return new IsAttribute {
                                Key = new string(key.ToArray()),
                                Value = new string(value.ToArray()),
                            };
                            key.Clear();
                            value.Clear();
                            parsingKey = false;
                            parsingValue = false;
                        }

                    } else if (character == ':') {
                        if (!parsingValue) {
                            parsingKey = false;
                            parsingValue = true;
                        }

                    } else {
                        if (parsingKey) {
                            key.Add(character);
                        } else if (parsingValue) {
                            value.Add(character);
                        } else {
                            parsingKey = true;
                            key.Add(character);
                        }
                    }
                }
                if (parsingKey || parsingValue) {
                    yield return new IsAttribute {
                        Key = new string(key.ToArray()),
                        Value = new string(value.ToArray()),
                    };
                }
            }

            /// <summary>
            /// is="" の中身。半角スペースで区切られる設定値1個分。
            /// </summary>
            public class IsAttribute {
                public required string Key { get; init; }
                public required string Value { get; init; }

                public override string ToString() {
                    return string.IsNullOrWhiteSpace(Value)
                        ? Key
                        : $"{Key}:{Value}";
                }
            }
            #endregion is属性

            public string GetPhysicalName() {
                return AttrValues
                    ?.FirstOrDefault(attr => attr.Key == OptionalAttributeDef.PHYSICAL_NAME)
                    ?.Value
                    ?? DisplayName?.ToCSharpSafe()
                    ?? string.Empty;
            }
            public string GetRefToPath(MutableSchema schema) {
                return schema
                    .GetAncestors(this)
                    .Concat([this])
                    .Select(agg => agg.GetPhysicalName())
                    .Join("/");
            }
            public E_NodeType? GetNodeType() {
                if (string.IsNullOrWhiteSpace(Type)) return null;
                if (Type.StartsWith(REFTO_PREFIX)) return E_NodeType.Ref;
                if (Type.StartsWith(ENUM_PREFIX)) return E_NodeType.Enum;
                if (Type.StartsWith(VALUE_OBJECT_PREFIX)) return E_NodeType.ValueObject;
                return NODE_TYPE.TryGetValue(Type, out var def) ? def?.NodeType : null;
            }
            public bool IsWriteModel(MutableSchema schema) {
                var root = schema.GetRoot(this);
                return root.Type == WriteModel.Key
                    || root.Type == WriteRead.Key;
            }
            public bool IsReadModel(MutableSchema schema) {
                var root = schema.GetRoot(this);
                return root.Type == ReadModel.Key
                    || root.Type == WriteRead.Key;
            }
            public bool IsCommandModel(MutableSchema schema) {
                var root = schema.GetRoot(this);
                return root.Type == Command.Key;
            }
            public bool IsRefToDynamicEnum(MutableSchema schema) {
                if (Type?.StartsWith(REFTO_PREFIX) != true) return false;
                return schema.FindRefToNode(Type)?.AttrValues?.Any(a => a.Key == IsDynamicEnumWriteModel.Key) == true;
            }

            /// <summary>
            /// エラーチェック
            /// </summary>
            public void Validate(MutableSchema schema, ICollection<string> errors) {
                // 名前必須
                if (string.IsNullOrWhiteSpace(DisplayName)) {
                    errors.Add("項目名を指定してください。");
                }

                // 主キー必須
                var children = schema.GetChildren(this).ToArray();
                var nodeType = GetNodeType();
                if (children.All(c => c.AttrValues == null || !c.AttrValues.Any(a => a.Key == KeyDef.Key))
                    && (IsWriteModel(schema) || IsReadModel(schema))
                    && (nodeType == E_NodeType.RootAggregate || Type == Children.Key)) {
                    errors.Add("ルート集約とChildrenではキー指定が必須です。");
                }

                // 動的列挙体（区分マスタ）
                if (AttrValues?.Any(a => a.Key == IsDynamicEnumWriteModel.Key) == true) {
                    if (!children.Any(c => c.GetPhysicalName() == Models.DynamicEnum.PK_PROP_NAME)) {
                        errors.Add($"動的列挙体（区分マスタ）のWriteModelは '{Models.DynamicEnum.PK_PROP_NAME}' という物理名の要素を持つ必要があります。");
                    }
                    if (!children.Any(c => c.GetPhysicalName() == Models.DynamicEnum.TYPE_PROP_NAME)) {
                        errors.Add($"動的列挙体（区分マスタ）のWriteModelは '{Models.DynamicEnum.TYPE_PROP_NAME}' という物理名の要素を持つ必要があります。");
                    }
                    if (!children.Any(c => c.GetPhysicalName() == Models.DynamicEnum.DISPLAY_NAME_PROP_NAME)) {
                        errors.Add($"動的列挙体（区分マスタ）のWriteModelは '{Models.DynamicEnum.DISPLAY_NAME_PROP_NAME}' という物理名の要素を持つ必要があります。");
                    }
                    if (!children.Any(c => c.GetPhysicalName() == Models.DynamicEnum.VALUE_PROP_NAME)) {
                        errors.Add($"動的列挙体（区分マスタ）のWriteModelは '{Models.DynamicEnum.VALUE_PROP_NAME}' という物理名の要素を持つ必要があります。");
                    }
                }
            }

            public override string ToString() {
                return DisplayName ?? $"ID::{UniqueId}";
            }


            #region 入出力（XML）
            /// <summary>
            /// nijo ui の画面上で編集されるデータをXML要素に変換する
            /// </summary>
            /// <param name="node">変換元</param>
            /// <param name="schema">全ての集約が入ったコレクション</param>
            public static IEnumerable<XNode> ToXmlNodes(
                MutableSchemaNode node,
                MutableSchema schema) {

                var physicalName = node.GetPhysicalName();
                var el = new XElement(physicalName);

                // ---------------------------------
                // is属性
                var isAttrs = new List<string>();

                // 型
                if (!string.IsNullOrWhiteSpace(node.Type)) {
                    if (node.Type.StartsWith(REFTO_PREFIX)) {
                        // ref-to
                        var uniqueId = node.Type.Substring(REFTO_PREFIX.Length);
                        var refToPath = schema
                            .FirstOrDefault(agg => agg.UniqueId == uniqueId)
                            ?.GetRefToPath(schema);
                        if (refToPath == null) {
                            isAttrs.Add(REFTO_PREFIX);
                        } else {
                            isAttrs.Add($"{REFTO_PREFIX}{refToPath}");
                        }

                    } else if (node.Type.StartsWith(ENUM_PREFIX)) {
                        // enum
                        var uniqueId = node.Type.Substring(ENUM_PREFIX.Length);
                        var enumName = schema
                            .RootNodes()
                            .FirstOrDefault(agg => agg.UniqueId == uniqueId
                                                && agg.Type == EnumDef.Key)
                            ?.GetPhysicalName();
                        if (enumName != null) {
                            isAttrs.Add(enumName);
                        }

                    } else if (node.Type.StartsWith(VALUE_OBJECT_PREFIX)) {
                        // value-object
                        var uniqueId = node.Type.Substring(VALUE_OBJECT_PREFIX.Length);
                        var enumName = schema
                            .RootNodes()
                            .FirstOrDefault(agg => agg.UniqueId == uniqueId
                                                && agg.Type == ValueObjectDef.Key)
                            ?.GetPhysicalName();
                        if (enumName != null) {
                            isAttrs.Add(enumName);
                        }

                    } else if (!string.IsNullOrWhiteSpace(node.TypeDetail)) {
                        isAttrs.Add($"{node.Type}:{node.TypeDetail}");

                    } else {
                        isAttrs.Add(node.Type);
                    }
                }

                // enumの値
                if (schema.GetParent(node)?.Type == EnumDef.Key
                    && !string.IsNullOrWhiteSpace(node.TypeDetail)
                    && int.TryParse(node.TypeDetail, out var intEnumValue)) {

                    el.SetAttributeValue(AppSchemaXml.ENUM_VALUE_KEY, intEnumValue);
                }

                // 型以外のis属性
                foreach (var attr in node.AttrValues ?? []) {
                    // これらは後の処理で考慮済みなので除外
                    if (attr.Key == OptionalAttributeDef.PHYSICAL_NAME
                        || attr.Key == OptionalAttributeDef.DB_NAME
                        || attr.Key == OptionalAttributeDef.LATIN) continue;

                    if (string.IsNullOrWhiteSpace(attr.Key)) {
                        continue;

                    } else if (string.IsNullOrWhiteSpace(attr.Value)) {
                        isAttrs.Add(attr.Key);

                    } else {
                        isAttrs.Add($"{attr.Key.Trim()}:{attr.Value.Trim()}");
                    }
                }

                if (isAttrs.Count > 0) {
                    el.SetAttributeValue(IS, isAttrs.Join(" "));
                }

                // ---------------------------------
                // is以外の属性

                if (node.DisplayName != physicalName) {
                    el.SetAttributeValue(DISPLAY_NAME, node.DisplayName);
                }

                var dbName = node.AttrValues
                    ?.SingleOrDefault(x => x.Key == OptionalAttributeDef.DB_NAME)
                    ?.Value;
                if (!string.IsNullOrWhiteSpace(dbName)) {
                    el.SetAttributeValue(DB_NAME, dbName);
                }

                var latinName = node.AttrValues
                    ?.SingleOrDefault(x => x.Key == OptionalAttributeDef.LATIN)
                    ?.Value;
                if (!string.IsNullOrWhiteSpace(latinName)) {
                    el.SetAttributeValue(LATIN, latinName);
                }

                // ---------------------------------
                // 子要素
                var childNodes = schema
                    .GetChildren(node)
                    .SelectMany(c => ToXmlNodes(c, schema));
                el.Add(childNodes);

                // ---------------------------------
                if (!string.IsNullOrWhiteSpace(node.Comment)) {
                    yield return new XComment(node.Comment);
                }
                yield return el;
            }

            /// <summary>
            /// XML要素を nijo ui の画面上で編集されるデータに変換する
            /// </summary>
            public static IEnumerable<MutableSchemaNode> FromXElement(
                XElement xElement,
                string xmlDocumentFilePath,
                Func<string, XElement?> findRefToElement,
                Func<string[], XElement?> findEnumElement,
                Func<string[], XElement?> findValueObjectElement) {
                return FromXElementPrivate(xElement, xmlDocumentFilePath, 0, findRefToElement, findEnumElement, findValueObjectElement);
            }
            private static IEnumerable<MutableSchemaNode> FromXElementPrivate(
                XElement xElement,
                string xmlDocumentFilePath,
                int depth,
                Func<string, XElement?> findRefToElement,
                Func<string[], XElement?> findEnumElement,
                Func<string[], XElement?> findValueObjectElement) {

                // XMLと nijo ui では DisplayName と PhysicalName の扱いが逆
                var displayName = xElement
                    .Attribute(DISPLAY_NAME)?.Value
                    ?? xElement.Name.LocalName;

                // -------------------------------------
                // 型
                var isAttrValues = ParseIsAttribute(xElement.Attribute(IS)?.Value).ToDictionary(x => x.Key);

                // ref-toやenumなど以外の型
                string? type = null;
                string? typeDetail = null;
                foreach (var def in NODE_TYPE.Values.OrderBy(d => d.Key)) {
                    var isAttr = def.FindMatchingIsAttribute(depth, isAttrValues);
                    if (isAttr != null) {
                        type = def.Key;
                        typeDetail = isAttr.Value;
                        break;
                    }
                }

                // ref-to
                if (type == null && isAttrValues.TryGetValue("ref-to", out var refTo)) {
                    var refToElement = findRefToElement(refTo.Value);
                    if (refToElement != null) {
                        // ここの記法はTypeScript側の型選択コンボボックスのルールと合わせる必要あり
                        type = $"{REFTO_PREFIX}{GetXElementUniqueId(refToElement)}";
                    }
                }

                // enum
                if (type == null) {
                    var isAttr = isAttrValues.Keys.OrderBy(k => k).ToArray();
                    var matchedEnum = findEnumElement(isAttr);
                    if (matchedEnum != null) {
                        // ここの記法はTypeScript側の型選択コンボボックスのルールと合わせる必要あり
                        type = $"{ENUM_PREFIX}{GetXElementUniqueId(matchedEnum)}";
                    }
                }

                // value-object
                if (type == null) {
                    var isAttr = isAttrValues.Keys.OrderBy(k => k).ToArray();
                    var matchedEnum = findValueObjectElement(isAttr);
                    if (matchedEnum != null) {
                        // ここの記法はTypeScript側の型選択コンボボックスのルールと合わせる必要あり
                        type = $"{VALUE_OBJECT_PREFIX}{GetXElementUniqueId(matchedEnum)}";
                    }
                }

                // enumの値
                if (typeDetail == null && xElement.Parent?.Attribute(IS)?.Value.Contains(EnumDef.Key) == true) {
                    typeDetail = xElement.Attribute(AppSchemaXml.ENUM_VALUE_KEY)?.Value;
                }

                // -------------------------------------
                // オプショナル属性
                var attrs = isAttrValues.Values
                    .Select(a => new OptionalAttributeValue { Key = a.Key, Value = a.Value })
                    .ToList();
                if (xElement.Attribute(DISPLAY_NAME) != null) {
                    // XMLと nijo ui では DisplayName と PhysicalName の扱いが逆
                    attrs.Add(new OptionalAttributeValue { Key = OptionalAttributeDef.PHYSICAL_NAME, Value = xElement.Name.LocalName });
                }
                if (xElement.Attribute(DB_NAME) != null) {
                    attrs.Add(new OptionalAttributeValue { Key = OptionalAttributeDef.DB_NAME, Value = xElement.Attribute(DB_NAME)?.Value });
                }
                if (xElement.Attribute(LATIN) != null) {
                    attrs.Add(new OptionalAttributeValue { Key = OptionalAttributeDef.LATIN, Value = xElement.Attribute(LATIN)?.Value });
                }

                // is属性のうちtypeの方で既にハンドリング済みのものは除外
                attrs.RemoveAll(a => a.Key == null || !ATTR_DEF.ContainsKey(a.Key));

                // -------------------------------------
                // コメント。PreviousNodeを使って取得する都合上、XMLを下から順番に辿る形になる。
                var comment = new StringBuilder();
                var currentElement = (XNode?)xElement;
                while (currentElement?.PreviousNode is XComment xComment) {
                    var value = comment.Length == 0
                        ? xComment.Value
                        : (xComment.Value + Environment.NewLine);
                    comment.Insert(0, value);
                    currentElement = currentElement.PreviousNode;
                }

                yield return new MutableSchemaNode {
                    Depth = depth,
                    UniqueId = GetXElementUniqueId(xElement),
                    DisplayName = displayName,
                    Type = type,
                    TypeDetail = typeDetail,
                    AttrValues = attrs,
                    Comment = comment.ToString(),
                    XmlFileFullPath = xmlDocumentFilePath,
                };

                // -------------------------------------
                // コメント
                var nodes = xElement.Nodes().ToArray();
                var descendants = new List<MutableSchemaNode>();
                var comments = new List<XComment>();
                for (int i = 0; i < nodes.Length; i++) {
                    var node = nodes[i];
                    if (node is XComment xComment) {
                        comments.Add(xComment);
                    } else if (node is XElement childXmlElement) {
                        descendants.AddRange(FromXElementPrivate(childXmlElement, xmlDocumentFilePath, depth + 1, findRefToElement, findEnumElement, findValueObjectElement));
                        comments.Clear();
                    }
                }
                foreach (var descendant in descendants) {
                    yield return descendant;
                }
            }
            #endregion 入出力（XML）


            #region 入出力（有向グラフ）
            /// <summary><see cref="DirectedGraph"/> のグラフIDに変換</summary>
            public NodeId ToGraphNodeId(MutableSchema schema) {
                var ancestorsAndThis = schema.GetAncestors(this).Concat([this]);
                return new NodeId(ancestorsAndThis.Select(x => "/" + x.GetPhysicalName()).Join(""));
            }
            public AggregateBuildOption CreateAggregateOption(MutableSchema schema) {
                var options = new AggregateBuildOption();
                foreach (var attrValue in AttrValues ?? []) {
                    if (attrValue.Key == null || !ATTR_DEF.TryGetValue(attrValue.Key, out var type)) continue;
                    type.EditAggregateOption(attrValue.Value, this, schema, options);
                }

                if (Type != null && NODE_TYPE.TryGetValue(Type, out var nodeType)) {
                    nodeType.EditAggregateOption(this, schema, options);
                }

                options.DisplayName = DisplayName;

                return options;
            }
            public AggregateMemberBuildOption CreateAggregateMemberOption(MutableSchema schema) {
                var options = new AggregateMemberBuildOption {
                    MemberType = Type,
                };
                foreach (var attrValue in AttrValues ?? []) {
                    if (attrValue.Key == null || !ATTR_DEF.TryGetValue(attrValue.Key, out var type)) continue;
                    type.EditAggregateMemberOption(attrValue.Value, this, schema, options);
                }

                if (Type != null && NODE_TYPE.TryGetValue(Type, out var nodeType)) {
                    nodeType.EditAggregateMemberOption(this, schema, options);
                }

                options.DisplayName = DisplayName;

                return options;
            }
            #endregion 入出力（有向グラフ）


            private static string GetXElementUniqueId(XElement xElement) {
                return xElement.GetHashCode().ToString().ToHashedString();
            }

            public const string IS = "is";
            public const string DISPLAY_NAME = "DisplayName";
            public const string DB_NAME = "DbName";
            public const string LATIN = "Latin";

            public const string REFTO_PREFIX = "ref-to:";
            public const string ENUM_PREFIX = "enum:";
            public const string VALUE_OBJECT_PREFIX = "value-object:";
        }

        private class XDocumentAndPath {
            public required XDocument XDocument { get; init; }
            public required string FilePath { get; init; }
        }

        /// <summary>
        /// <see cref="MutableSchemaNode.Type"/> の種類
        /// </summary>
        [Flags]
        private enum E_NodeType {
            /// <summary>集約</summary>
            Aggregate = 0b0000001,
            /// <summary>集約メンバー</summary>
            AggregateMember = 0b0000010,
            /// <summary>ルート集約</summary>
            RootAggregate = 0b0000101,
            /// <summary>Child, Children, VariationItem</summary>
            DescendantAggregate = 0b0001001,
            /// <summary>ref-to</summary>
            Ref = 0b0000110,
            /// <summary>列挙体</summary>
            Enum = 0b0001010,
            /// <summary>値オブジェクト</summary>
            ValueObject = 0b0010010,
            /// <summary>バリエーションのコンテナの方（VariationItemでない方）</summary>
            Variation = 0b0100010,
            /// <summary>上記以外</summary>
            SchalarMember = 0b1000010,
        }
        private class OptionalAttributeValue {
            [JsonPropertyName("key")]
            public string? Key { get; set; }
            [JsonPropertyName("value")]
            public string? Value { get; set; }
        }
        private class SchemaNodeTypeDef {
            [JsonPropertyName("key")]
            public string Key { get; set; } = "";
            [JsonPropertyName("displayName")]
            public string? DisplayName { get; set; }
            [JsonPropertyName("helpText")]
            public string? HelpText { get; set; }
            [JsonPropertyName("requiredNumberValue")]
            public bool? RequiredNumberValue { get; set; }

            /// <summary>
            /// XML要素の内容を判断し、この型に属するかどうかを返します。
            /// 属する場合、該当の <see cref="MutableSchemaNode.IsAttribute"/> オブジェクトを返します。
            /// </summary>
            [JsonIgnore]
            public Func<int, IReadOnlyDictionary<string, MutableSchemaNode.IsAttribute>, MutableSchemaNode.IsAttribute?> FindMatchingIsAttribute { get; set; } = ((_, _) => null);
            /// <summary>
            /// バリデーション
            /// </summary>
            [JsonIgnore]
            public Action<MutableSchemaNode, MutableSchema, ICollection<string>> Validate { get; set; } = ((_, _, _) => { });
            /// <summary>
            /// この種別に属するノードの種類
            /// </summary>
            [JsonIgnore]
            public E_NodeType NodeType { get; set; }
            /// <summary>
            /// このオプションが設定されているときの <see cref="AggregateBuildOption"/> の編集処理
            /// </summary>
            [JsonIgnore]
            public Action<MutableSchemaNode, MutableSchema, AggregateBuildOption> EditAggregateOption { get; set; } = ((_, _, _) => { });
            /// <summary>
            /// このオプションが設定されているときの <see cref="AggregateMemberBuildOption"/> の編集処理
            /// </summary>
            [JsonIgnore]
            public Action<MutableSchemaNode, MutableSchema, AggregateMemberBuildOption> EditAggregateMemberOption { get; set; } = ((_, _, _) => { });
        }
        private class OptionalAttributeDef {
            [JsonPropertyName("key")]
            public required string Key { get; set; }
            [JsonPropertyName("displayName")]
            public string? DisplayName { get; set; }
            [JsonPropertyName("helpText")]
            public string? HelpText { get; set; }
            [JsonPropertyName("type")]
            public E_OptionalAttributeType? Type { get; set; }

            public const string PHYSICAL_NAME = "physical-name";
            public const string DB_NAME = "db-name";
            public const string LATIN = "latin";

            /// <summary>
            /// バリデーション
            /// </summary>
            [JsonIgnore]
            public Action<string?, MutableSchemaNode, MutableSchema, ICollection<string>> Validate { get; set; } = ((_, _, _, _) => { });
            /// <summary>
            /// このオプションが設定されているときの <see cref="AggregateBuildOption"/> の編集処理
            /// </summary>
            [JsonIgnore]
            public Action<string?, MutableSchemaNode, MutableSchema, AggregateBuildOption> EditAggregateOption { get; set; } = ((_, _, _, _) => { });
            /// <summary>
            /// このオプションが設定されているときの <see cref="AggregateMemberBuildOption"/> の編集処理
            /// </summary>
            [JsonIgnore]
            public Action<string?, MutableSchemaNode, MutableSchema, AggregateMemberBuildOption> EditAggregateMemberOption { get; set; } = ((_, _, _, _) => { });
        }
        private enum E_OptionalAttributeType {
            String,
            Number,
            Boolean,
        }

        /// <summary>
        /// 集約やメンバーの種類として指定することができる属性を列挙します。
        /// </summary>
        private static IReadOnlyDictionary<string, SchemaNodeTypeDef> NODE_TYPE => _cacheOfNodeType ??= EnumerateSchemaNodeTypes().ToDictionary(x => x.Key);
        private static IReadOnlyDictionary<string, SchemaNodeTypeDef>? _cacheOfNodeType;
        private static IEnumerable<SchemaNodeTypeDef> EnumerateSchemaNodeTypes() {
            // ルート集約に設定できる種類
            yield return WriteModel;
            yield return ReadModel;
            yield return WriteRead;
            yield return EnumDef;
            yield return Command;
            yield return ValueObjectDef;
            yield return DynamicEnumDefType;

            // 子孫集約に設定できる種類
            yield return Child;
            yield return Children;
            yield return Variation;
            yield return VariationItem;
            yield return Step;

            var resolver = MemberTypeResolver.Default();
            foreach (var (key, memberType) in resolver.EnumerateAll()) {
                yield return new SchemaNodeTypeDef {
                    NodeType = E_NodeType.SchalarMember,
                    Key = key,
                    DisplayName = memberType.GetUiDisplayName(),
                    HelpText = memberType.GetHelpText(),
                    FindMatchingIsAttribute = (depth, isAttr) => depth > 0 && isAttr.TryGetValue(key, out var isAttribute) ? isAttribute : null,
                    Validate = (node, schema, errors) => {

                    },
                };
            }
        }

        /// <summary>
        /// 集約やメンバーのオプショナル属性として指定することができる属性を列挙します。
        /// </summary>
        private static IReadOnlyDictionary<string, OptionalAttributeDef> ATTR_DEF => _cacheOfAttrDef ??= EnumerateOptionalAttributes().ToDictionary(x => x.Key);
        private static IReadOnlyDictionary<string, OptionalAttributeDef>? _cacheOfAttrDef;
        private static IEnumerable<OptionalAttributeDef> EnumerateOptionalAttributes() {
            yield return PhysicalName;
            yield return DbName;
            yield return LatinName;

            yield return DynamicEnumTypePhysicalName;

            yield return KeyDef;
            yield return NameDef;
            yield return Required;

            yield return MaxLength;
            yield return FormLabelWidth;
            yield return HasLifeCycle;
            yield return ReadOnly;
            yield return Hidden;
            yield return Wide;
            yield return Width;

            yield return Combo;
            yield return Radio;

            yield return SearchBehavior;
            yield return CharacterType;

            yield return EnumSqlParamType;

            yield return IsDynamicEnumWriteModel;

            yield return ForeignKeyProxy;
        }

        #region ルート集約に設定できる種類
        private static SchemaNodeTypeDef WriteModel => new SchemaNodeTypeDef {
            NodeType = E_NodeType.RootAggregate,
            Key = "write-model-2", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
            DisplayName = "WriteModel",
            HelpText = $$"""
                このルート要素がDB保存されるべきデータであることを表します。
                Entity Framework Core のエンティティ定義や、作成・更新・削除のWeb API エンドポイントなどが生成されます。
                切り分けの目安は、データベースの排他制御やトランザクションの粒度です。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => depth == 0
                                         && !isAttr.ContainsKey("generate-default-read-model")
                                         && isAttr.TryGetValue("write-model-2", out var isAttribute) ? isAttribute : null,
            Validate = (node, schema, errors) => {
                if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");
            },
            EditAggregateOption = (node, schema, opt) => {
                opt.Handler = NijoCodeGenerator.Models.WriteModel2.Key;
            },
        };
        private static SchemaNodeTypeDef ReadModel => new SchemaNodeTypeDef {
            NodeType = E_NodeType.RootAggregate,
            Key = "read-model-2", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
            DisplayName = "ReadModel",
            HelpText = $$"""
                このルート要素が人間が閲覧するデータであることを表します。
                一覧検索画面、詳細画面、一括編集画面などが生成されます。
                切り分けの目安は詳細画面1個の粒度です。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => depth == 0
                                         && isAttr.TryGetValue("read-model-2", out var isAttribute) ? isAttribute : null,
            Validate = (node, schema, errors) => {
                if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");
            },
            EditAggregateOption = (node, schema, opt) => {
                opt.Handler = NijoCodeGenerator.Models.ReadModel2.Key;
            },
        };
        private static SchemaNodeTypeDef WriteRead => new SchemaNodeTypeDef {
            NodeType = E_NodeType.RootAggregate,
            Key = "write-model-2 generate-default-read-model", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
            DisplayName = "Write & Read",
            HelpText = $$"""
                ReadModel から生成されるコードと WriteModel から生成されるコードの両方が生成されます。
                画面のデータ項目とDBのデータ構造が寸分違わず完全に一致する場合にのみ使えます。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => depth == 0
                                                      && isAttr.ContainsKey("generate-default-read-model")
                                                      && isAttr.TryGetValue("write-model-2", out var isAttribute) ? isAttribute : null,
            Validate = (node, schema, errors) => {
                if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");
            },
            EditAggregateOption = (node, schema, opt) => {
                opt.Handler = NijoCodeGenerator.Models.WriteModel2.Key;
                opt.GenerateDefaultReadModel = true;
            },
        };
        private static SchemaNodeTypeDef EnumDef => new SchemaNodeTypeDef {
            NodeType = E_NodeType.RootAggregate,
            Key = "enum", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
            DisplayName = "Enum",
            HelpText = $$"""
                このルート要素が列挙体であることを表します。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => depth == 0
                                                      && isAttr.TryGetValue("enum", out var isAttribute) ? isAttribute : null,
            Validate = (node, schema, errors) => {
                if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");
            },
        };
        private static SchemaNodeTypeDef Command => new SchemaNodeTypeDef {
            NodeType = E_NodeType.RootAggregate,
            Key = "command",
            DisplayName = "Command",
            HelpText = $$"""
                このルート要素が、人間や外部システムが起動する処理のパラメータであることを表します。
                この処理を実行するWebAPIエンドポイントや、パラメータを入力するためのダイアログのUIコンポーネントなどが生成されます。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => depth == 0
                                                      && isAttr.TryGetValue("command", out var isAttribute) ? isAttribute : null,
            Validate = (node, schema, errors) => {
                if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");

                var children = schema.GetChildren(node).ToArray();
                if (children.Any(x => x.Type == "step") && children.Any(x => x.Type != "step")) {
                    errors.Add("ステップ属性を定義する場合は全てステップにする必要があります。");
                }
            },
            EditAggregateOption = (node, schema, opt) => {
                opt.Handler = NijoCodeGenerator.Models.CommandModel.Key;
            },
        };
        private static SchemaNodeTypeDef ValueObjectDef => new SchemaNodeTypeDef {
            NodeType = E_NodeType.RootAggregate,
            Key = "value-object",
            DisplayName = "値オブジェクト(ValueObject)",
            HelpText = $$"""
                値オブジェクト。主として「○○コード」などの識別子の型として使われる。
                同値比較がそのインスタンスの参照ではなく値によって行われる。不変（immutable）である。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => depth == 0
                                                      && isAttr.TryGetValue("value-object", out var isAttribute) ? isAttribute : null,
            Validate = (node, schema, errors) => {
                if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");
                if (schema.GetChildren(node).Any()) errors.Add("この型に子要素を設定することはできません。");
            },
            EditAggregateOption = (node, schema, opt) => {
                opt.Handler = NijoCodeGenerator.Models.ValueObjectModel.Key;
            },
        };
        private static SchemaNodeTypeDef DynamicEnumDefType => new SchemaNodeTypeDef {
            NodeType = E_NodeType.RootAggregate,
            Key = "dynamic-enum-type",
            DisplayName = "区分マスタ（動的列挙体）の種類",
            HelpText = $$"""
                これが区分マスタの種類であることを表します。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => depth == 0
                                                      && isAttr.TryGetValue("dynamic-enum-type", out var isAttribute) ? isAttribute : null,
            Validate = (node, schema, errors) => {
                if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");
                if (schema.GetChildren(node).Any()) errors.Add("この型に子要素を設定することはできません。");
                if (string.IsNullOrWhiteSpace(node.TypeDetail)) errors.Add("値を指定してください。");
            },
        };
        #endregion ルート集約に設定できる種類

        #region ルート以外に設定できる種類 ※ ref-toと列挙体は集約定義に依存するのでクライアント側で計算する
        private static SchemaNodeTypeDef Child => new SchemaNodeTypeDef {
            NodeType = E_NodeType.DescendantAggregate,
            Key = "child", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
            DisplayName = "Child",
            HelpText = $$"""
                親1件に対する1件の子要素。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => {
                if (depth == 0) return null;
                if (isAttr.TryGetValue("child", out var isAttribute1)) return isAttribute1;
                if (isAttr.TryGetValue("section", out var isAttribute2)) return isAttribute2; // section属性は廃止予定
                return null;
            },
            Validate = (node, schema, errors) => {
                if (node.Depth == 0) errors.Add("この型は子孫要素にしか設定できません。");
            },
        };
        private static SchemaNodeTypeDef Children => new SchemaNodeTypeDef {
            NodeType = E_NodeType.DescendantAggregate,
            Key = "children", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
            DisplayName = "Children",
            HelpText = $$"""
                親1件に対する複数件の子要素。 
                """,
            FindMatchingIsAttribute = (depth, isAttr) => {
                if (depth == 0) return null;
                if (isAttr.TryGetValue("children", out var isAttribute1)) return isAttribute1;
                if (isAttr.TryGetValue("array", out var isAttribute2)) return isAttribute2; // array属性は廃止予定
                return null;
            },
            Validate = (node, schema, errors) => {
                if (node.Depth == 0) errors.Add("この型は子孫要素にしか設定できません。");
            },
            EditAggregateOption = (node, schema, opt) => {
                opt.IsArray = true;
            },
        };
        private static SchemaNodeTypeDef Variation => new SchemaNodeTypeDef {
            NodeType = E_NodeType.Variation,
            Key = "variation",
            DisplayName = "Variation",
            HelpText = $$"""
                親1件に対し、異なるデータ構造をもつ複数の子要素から1種類を選択するもの。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => depth > 0
                                         && isAttr.TryGetValue("variation", out var isAttribute) ? isAttribute : null,
            Validate = (node, schema, errors) => {
                if (node.Depth == 0) errors.Add("この型は子孫要素にしか設定できません。");

                var children = schema.GetChildren(node).ToArray();
                if (children.Length == 0) {
                    errors.Add("バリエーションには1つ以上の種類を定義する必要があります。");
                } else if (children.Any(x => x.Type != "variation-item")) {
                    errors.Add("Variationの直下に定義できるのはVariationItemのみです。");
                }
            },
        };
        private static SchemaNodeTypeDef VariationItem => new SchemaNodeTypeDef {
            NodeType = E_NodeType.DescendantAggregate,
            Key = "variation-item", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
            DisplayName = "VariationItem",
            RequiredNumberValue = true,
            HelpText = $$"""
                バリエーションの1種類を表します。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => {
                if (depth == 0) return null;
                if (isAttr.TryGetValue("variation-item", out var isAttribute1)) return isAttribute1;
                if (isAttr.TryGetValue("variation-key", out var isAttribute2)) return isAttribute2; // variation-key属性は廃止予定
                return null;
            },
            Validate = (node, schema, errors) => {
                if (schema.GetParent(node)?.Type != "variation") {
                    errors.Add("VariationItemはVariationの直下にのみ定義できます。");
                }
                if (string.IsNullOrWhiteSpace(node.TypeDetail)) {
                    errors.Add("VariationItemには種類を識別するための整数を定義する必要があります。");
                } else if (!int.TryParse(node.TypeDetail, out var _)) {
                    errors.Add($"区分値 '{node.TypeDetail}' を整数として解釈できません。");
                }
            },
            EditAggregateOption = (node, schema, opt) => {
                var parent = schema.GetParent(node);
                opt.IsVariationGroupMember = new() {
                    GroupName = parent?.GetPhysicalName() ?? "",
                    GroupDisplayName = parent?.DisplayName ?? "",
                    Key = node.TypeDetail ?? "",
                };
            },
        };
        private static SchemaNodeTypeDef Step => new SchemaNodeTypeDef {
            NodeType = E_NodeType.DescendantAggregate,
            Key = "step",
            DisplayName = "Step",
            RequiredNumberValue = true,
            HelpText = $$"""
                ほぼChildと同じですが、ルート集約がCommandの場合、かつルート集約の直下にのみ設定可能です。
                そのコマンドの子要素がステップの場合、コマンドのUIがウィザード形式になります。
                """,
            FindMatchingIsAttribute = (depth, isAttr) => depth > 0
                                                      && isAttr.TryGetValue("step", out var isAttribute) ? isAttribute : null,
            Validate = (node, schema, errors) => {
                if (node.Depth != 1 || schema.GetRoot(node).Type != "command") {
                    errors.Add("ステップ属性はコマンドの直下にのみ定義できます。");
                }
                if (string.IsNullOrWhiteSpace(node.TypeDetail)) {
                    errors.Add("ステップ番号を識別するための整数を定義する必要があります。");
                } else if (!int.TryParse(node.TypeDetail, out var _)) {
                    errors.Add($"ステップ番号 '{node.TypeDetail}' を整数として解釈できません。");
                }
            },
            EditAggregateOption = (node, schema, opt) => {
                opt.Step = int.Parse(node.TypeDetail ?? throw new InvalidOperationException());
            },
        };
        #endregion ルート以外に設定できる種類 ※ ref-toと列挙体は集約定義に依存するのでクライアント側で計算する

        #region オプショナル属性
        private static OptionalAttributeDef PhysicalName => new OptionalAttributeDef {
            Key = OptionalAttributeDef.PHYSICAL_NAME,
            DisplayName = "物理名",
            Type = E_OptionalAttributeType.String,
            HelpText = $$"""
                物理名を明示的に指定したい場合に設定してください。
                既定では論理名のうちソースコードに使用できない文字が置換されたものが物理名になります。
                """,
            Validate = (value, node, schema, errors) => {
                if (string.IsNullOrEmpty(value)) return; // 未指定の場合はチェックしない

                if (!value.All(c =>
                    char.IsLetterOrDigit(c) // 英数字
                    || c == '_'             // アンダースコア
                    || (c >= 0x3040 && c <= 0x309F)     // ひらがな
                    || (c >= 0x30A0 && c <= 0x30FF)     // カタカナ
                    || (c >= 0x4E00 && c <= 0x9FAF))) { // 漢字
                    errors.Add("物理名には、英数字、アンダースコア、ひらがな、カタカナ、Unicodeの範囲での漢字のみが使えます。");

                } else if (char.IsDigit(value[0])) {
                    errors.Add("物理名を数字から始めることはできません。");
                } else if (char.IsLower(value[0])) {
                    errors.Add("Reactのコンポーネント名が小文字始まりだとエラーになるので大文字から始めてください。");
                } else {
                    try {
                        XmlConvert.VerifyName(value);
                    } catch (XmlException ex) {
                        errors.Add($"XMLの要素名として使用できない文字が含まれています。（{ex.Message}）");
                    }
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
        };
        private static OptionalAttributeDef DbName => new OptionalAttributeDef {
            Key = OptionalAttributeDef.DB_NAME,
            DisplayName = "DB名",
            Type = E_OptionalAttributeType.String,
            HelpText = $$"""
                データベースのテーブル名またはカラム名を明示的に指定したい場合に設定してください。
                既定では物理名がそのままテーブル名やカラム名になります。
                """,
            Validate = (value, node, schema, errors) => {
                if (string.IsNullOrEmpty(value)) return; // 未指定の場合はチェックしない
                if (!value.All(c =>
                    char.IsLetterOrDigit(c) // 英数字
                    || c == '_'             // アンダースコア
                    || (c >= 0x3040 && c <= 0x309F)     // ひらがな
                    || (c >= 0x30A0 && c <= 0x30FF)     // カタカナ
                    || (c >= 0x4E00 && c <= 0x9FAF))) { // 漢字
                    errors.Add("DBの名称には、英数字、アンダースコア、ひらがな、カタカナ、Unicodeの範囲での漢字のみが使えます。");

                } else if (char.IsDigit(value[0])) {
                    errors.Add("DB名を数字から始めることはできません。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                opt.DbName = value;
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.DbName = value;
            },
        };
        private static OptionalAttributeDef LatinName => new OptionalAttributeDef {
            Key = OptionalAttributeDef.LATIN,
            DisplayName = "ラテン語名",
            Type = E_OptionalAttributeType.String,
            HelpText = $$"""
                ラテン語名しか用いることができない部分の名称を明示的に指定したい場合に設定してください（ver-0.4.0.0000 時点ではURLを定義する処理のみが該当）。
                既定では集約を表す一意な文字列から生成されたハッシュ値が用いられます。
                """,
            Validate = (value, node, schema, errors) => {
                if (string.IsNullOrEmpty(value)) return; // 未指定の場合はチェックしない

                // ラテン語名は英字、数字、ハイフン(-)、アンダースコア(_)を許可
                if (!value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ')) {
                    errors.Add("ラテン語名には英数字、ハイフン、アンダースコア、半角スペースのみが使用できます。");

                } else if (char.IsDigit(value[0])) {
                    errors.Add("ラテン語名を数字から始めることはできません。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                opt.LatinName = value;
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
        };

        private static OptionalAttributeDef DynamicEnumTypePhysicalName => new OptionalAttributeDef {
            Key = "dynamic-enum-type-physical-name",
            DisplayName = "区分マスタの種類",
            Type = E_OptionalAttributeType.String,
            HelpText = $$"""
                この項目が区分マスタのうちどの種類をとりうるか。
                ref-to:区分マスタ にのみ定義可能。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.IsRefToDynamicEnum(schema)) {

                    if (string.IsNullOrWhiteSpace(value)) {
                        errors.Add("区分マスタのうちどの種類かを指定してください。");
                    } else if (!schema.RootNodes().Where(n => n.Type == DynamicEnumDefType.Key).Any(n => n.GetPhysicalName() == value)) {
                        errors.Add($"区分マスタに種類'{value}'が存在しません。");
                    }

                } else {
                    errors.Add("この設定は区分マスタを参照する項目にのみ指定できます。");
                }
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.DynamicEnumTypePhysicalName = value;
            },
        };

        private static OptionalAttributeDef KeyDef => new OptionalAttributeDef {
            Key = "key",
            DisplayName = "Key",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                この項目がその集約のキーであることを表します。
                ルート集約またはChildrenの場合、指定必須。
                ChildやVariationには指定不可。Commandの要素にも指定不可。
                """,
            Validate = (value, node, schema, errors) => {
                var root = schema.GetRoot(node);
                if (root.Type == "command") {
                    errors.Add("コマンドにキーを指定することはできません。");
                } else if (root.Type == EnumDef.Key) {
                    errors.Add("列挙体定義にキーを指定することはできません。");
                }

                var parent = schema.GetParent(node);
                if (parent == null) {
                    errors.Add("ルート集約にキーを指定することはできません。");
                } else if (parent.Type == "child" && !(parent.AttrValues ?? []).Any(x => x.Key == HasLifeCycle.Key)) {
                    errors.Add("Childにキーを指定することはできません。");
                } else if (parent.Type == "variation" || parent.Type == "variation-item") {
                    errors.Add("バリエーションにキーを指定することはできません。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.IsPrimary = true;
                opt.IsRequired = true;
            },
        };
        private static OptionalAttributeDef NameDef => new OptionalAttributeDef {
            Key = "name",
            DisplayName = "Name",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                この項目がその集約の名前項目であることを表します。
                詳細画面のタイトルやコンボボックスの表示テキストにどの項目が使われるかで参照されます。
                未指定の場合はキーが表示名称として使われます。
                """,
            Validate = (value, node, schema, errors) => {
                // 特に処理なし
            },
            EditAggregateOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.IsDisplayName = true;
            },
        };
        private static OptionalAttributeDef Required => new OptionalAttributeDef {
            Key = "required",
            DisplayName = "必須",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                必須項目であることを表します。
                画面上に必須項目であることを示すUIがついたり、新規登録処理や更新処理での必須入力チェック処理が自動生成されます。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.Depth == 0
                    || node.Type == "child"
                    || node.Type == "variation"
                    || node.Type == "variation-item") {
                    errors.Add("この項目に必須指定をすることはできません。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                if (node.Type == Children.Key) opt.IsRequiredArray = true;
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.IsRequired = true;
            },
        };

        private static OptionalAttributeDef MaxLength => new OptionalAttributeDef {
            Key = "max-length",
            DisplayName = "MaxLength",
            Type = E_OptionalAttributeType.Number,
            HelpText = $$"""
                文字列項目の最大長。整数で指定してください。
                """,
            Validate = (value, node, schema, errors) => {
                if (string.IsNullOrWhiteSpace(value)) return;
                if (!int.TryParse(value, out var parsed)) {
                    errors.Add("整数で入力してください。");
                } else if (parsed <= 0) {
                    errors.Add("正の数で入力してください。");
                }

                var availableTypes = new[] {
                    MemberTypeResolver.TYPE_WORD,
                    MemberTypeResolver.TYPE_SENTENCE,
                };
                if (node.Type != null
                    && !node.Type.StartsWith(MutableSchemaNode.VALUE_OBJECT_PREFIX)
                    && !availableTypes.Contains(node.Type)) {
                    errors.Add("数値か文字列にのみ設定可能です。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.MaxLength = int.Parse(value!);
            },
        };
        private static OptionalAttributeDef FormLabelWidth => new OptionalAttributeDef {
            Key = "form-label-width",
            DisplayName = "フォームのラベルの横幅",
            Type = E_OptionalAttributeType.Number,
            HelpText = $$"""
                VForm2のラベル列の横幅。数値で定義してください（小数使用可能）。単位はCSSのrem。
                ReadModelのルート集約にのみ設定可能。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.Depth != 0 || !node.IsReadModel(schema)) {
                    errors.Add("この属性はReadModelのルート集約にのみ設定可能です。");
                } else if (!decimal.TryParse(value, out var _)) {
                    errors.Add("数値で指定してください。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                if (!string.IsNullOrWhiteSpace(value)) {
                    opt.EstimatedLabelWidth = decimal.Parse(value);
                }
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
        };
        private static OptionalAttributeDef HasLifeCycle => new OptionalAttributeDef {
            Key = "has-lifecycle",
            DisplayName = "独立ライフサイクル",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                ReadModelの中にあるChildのうち、そのChildが追加削除できるものであることを表します。
                """,
            Validate = (value, node, schema, errors) => {
                if (!node.IsReadModel(schema) || (node.Type != Child.Key && node.Type != Children.Key)) {
                    errors.Add("この属性はReadModelのChildまたはChildrenにのみ設定可能です。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                opt.HasLifeCycle = true;
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
        };
        private static OptionalAttributeDef ReadOnly => new OptionalAttributeDef {
            Key = "readonly",
            DisplayName = "読み取り専用集約",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                閲覧専用の集約であることを表します。新規作成画面などが生成されなくなります。
                ReadModelのルート集約にのみ設定可能。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.Depth != 0 || !node.IsReadModel(schema)) {
                    errors.Add("この属性はReadModelのルート集約にのみ設定可能です。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                opt.IsReadOnlyAggregate = true;
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
        };
        private static OptionalAttributeDef Hidden => new OptionalAttributeDef {
            Key = "hidden",
            DisplayName = "隠し項目",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                この項目が画面上で常に非表示になります。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.Depth == 0) {
                    errors.Add("ルート集約を隠し項目にすることはできません。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.InvisibleInGui = true;
            },
        };
        private static OptionalAttributeDef Wide => new OptionalAttributeDef {
            Key = "wide",
            DisplayName = "Wide",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                VForm2上でこの項目のスペースが横幅いっぱい確保されます。
                """,
            Validate = (value, node, schema, errors) => {
                var nodeType = node.GetNodeType();
                if (nodeType != E_NodeType.Ref && nodeType != E_NodeType.Enum && nodeType != E_NodeType.SchalarMember) {
                    errors.Add("この属性は入力フォームをもつ項目にのみ指定できます。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                if (string.IsNullOrWhiteSpace(value)) {
                    opt.WideInVForm = true;
                } else if (bool.TryParse(value, out var bln)) {
                    // ref-toに対しては is="wide:false" のように「wideにしたくない」形の指定があるのでTryParseにかけている
                    opt.WideInVForm = bln;
                }
            },
        };
        private static OptionalAttributeDef Width => new OptionalAttributeDef {
            Key = "width",
            DisplayName = "横幅",
            Type = E_OptionalAttributeType.String,
            HelpText = $$"""
                詳細画面における当該項目の横幅を変更できます。全角10文字の場合は "z10"、半角6文字の場合は "h6" など、zかhのあとに整数を続けてください。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.GetNodeType() != E_NodeType.SchalarMember) {
                    errors.Add("この属性はテキストボックスをもつ項目にのみ指定できます。");

                } else if (!string.IsNullOrWhiteSpace(value)) {
                    var first = value.FirstOrDefault();
                    if (first != 'h' && first != 'z') {
                        errors.Add("全角10文字なら'z10', 半角10文字なら'h10'のようにzかhをつけて指定してください。");
                    }
                    if (!int.TryParse(value.AsSpan(1), out var _)) {
                        errors.Add("2文字目以降を数値として解釈できません。");
                    }
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                if (string.IsNullOrWhiteSpace(value)) return;
                opt.UiWidthRem = new TextBoxWidth {
                    ZenHan = value?.FirstOrDefault() switch {
                        'z' => TextBoxWidth.E_ZenHan.Zenkaku,
                        'h' => TextBoxWidth.E_ZenHan.Hankaku,
                        _ => throw new InvalidOperationException(),
                    },
                    CharCount = int.Parse(value.AsSpan(1)),
                };
            },
        };


        [Obsolete("#60 最終的にnijo.xmlではなくApp.tsxで決められるようにするためこの属性は削除する")]
        private static OptionalAttributeDef Combo => new OptionalAttributeDef {
            Key = "combo",
            DisplayName = "コンボボックス",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                列挙体のメンバーまたはバリエーションにのみ使用可能。
                詳細画面の入力フォームがコンボボックスに固定されます。
                指定しない場合は動的に決まります（選択肢の数が多ければコンボボックス、少なければラジオボタン）。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.GetNodeType() != E_NodeType.Enum) {
                    errors.Add("この属性は列挙体の項目にのみ指定できます。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.IsCombo = true;
            },
        };
        [Obsolete("#60 最終的にnijo.xmlではなくApp.tsxで決められるようにするためこの属性は削除する")]
        private static OptionalAttributeDef Radio => new OptionalAttributeDef {
            Key = "radio",
            DisplayName = "ラジオボタン",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                列挙体のメンバーまたはバリエーションにのみ使用可能。
                詳細画面の入力フォームがラジオボタンに固定されます。
                指定しない場合は動的に決まります（選択肢の数が多ければコンボボックス、少なければラジオボタン）。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.GetNodeType() != E_NodeType.Enum) {
                    errors.Add("この属性は列挙体の項目にのみ指定できます。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                // 特に処理なし
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.IsRadio = true;
            },
        };

        private static OptionalAttributeDef SearchBehavior => new OptionalAttributeDef {
            Key = "search-behavior",
            DisplayName = "検索時の挙動",
            Type = E_OptionalAttributeType.String,
            HelpText = $$"""
                検索時の挙動。単語型でのみ使用可能。
                「前方一致」「後方一致」「完全一致」「部分一致」「範囲検索」のいずれかを指定してください。
                """,
            Validate = (value, node, schema, errors) => {
                if (string.IsNullOrWhiteSpace(value)) return;
                if (node.Type != MemberTypeResolver.TYPE_WORD) {
                    errors.Add("この属性は単語型にのみ設定できます。");
                    return;
                }
                var behaviors = new[] { "前方一致", "後方一致", "完全一致", "部分一致", "範囲検索" };
                if (!behaviors.Contains(value)) {
                    errors.Add($"{behaviors.Select(x => $"\"{x}\"").Join(", ")}のいずれかを入力してください。");
                }
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.SearchBehavior = value switch {
                    "前方一致" => E_SearchBehavior.ForwardMatch,
                    "後方一致" => E_SearchBehavior.BackwardMatch,
                    "完全一致" => E_SearchBehavior.Strict,
                    "部分一致" => E_SearchBehavior.PartialMatch,
                    "範囲検索" => E_SearchBehavior.Range,
                    _ => null,
                };
            },
        };
        private static OptionalAttributeDef CharacterType => new OptionalAttributeDef {
            Key = "character-type",
            DisplayName = "文字種",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                文字列型のメンバーがとることのできる文字の種類。
                {{Enum.GetValues<E_CharacterType>().Select(c => $"'{c}'").Join(", ")}} が使用可能。
                """,
            Validate = (value, node, schema, errors) => {
                if (string.IsNullOrWhiteSpace(value)) return;
                if (node.Type != MemberTypeResolver.TYPE_WORD
                    && node.Type != MemberTypeResolver.TYPE_SENTENCE
                    && node.Type?.StartsWith(MutableSchemaNode.VALUE_OBJECT_PREFIX) != true) {
                    errors.Add("この属性は文字列系項目にのみ設定できます。");
                    return;
                }

                var available = Enum.GetValues<E_CharacterType>().Select(enm => enm.ToString()).ToArray();
                if (!available.Contains(value)) {
                    errors.Add($"{available.Select(x => $"\"{x}\"").Join(", ")}のいずれかを入力してください。");
                }
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.CharacterType = value == null ? null : Enum.Parse<E_CharacterType>(value);
            },
        };

        private static OptionalAttributeDef EnumSqlParamType => new OptionalAttributeDef {
            Key = "enum-sql-param-type",
            DisplayName = "列挙体のSQLクエリパラメータの型",
            Type = E_OptionalAttributeType.String,
            HelpText = $$"""
                ソース生成後のDbContextにて列挙体のDBの型をintやvarcharに設定したとき、
                LINQで組み立てるSQLのパラメータの型がenumのままだと例外が出るので、その回避策。
                intのみ対応。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.Type?.StartsWith(MutableSchemaNode.ENUM_PREFIX) != true) {
                    errors.Add("この属性は列挙体にのみ設定できます。");
                    return;
                }
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                opt.EnumSqlParamType = value;
            },
        };

        private static OptionalAttributeDef IsDynamicEnumWriteModel => new OptionalAttributeDef {
            Key = "is-dynamic-enum-write-model",
            DisplayName = "区分マスタ",
            Type = E_OptionalAttributeType.Boolean,
            HelpText = $$"""
                このWriteModelが区分マスタであることを表します。
                区分マスタはアプリケーション中で1つしか定義できません。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.Depth != 0) {
                    errors.Add("この属性はルート集約にのみ設定できます。");
                } else if (!node.IsWriteModel(schema)) {
                    errors.Add("この属性はWriteModelにのみ設定できます。");
                }
            },
            EditAggregateOption = (value, node, schema, opt) => {
                opt.IsDynamicEnumWriteModel = true;
            },
        };

        private static OptionalAttributeDef ForeignKeyProxy => new OptionalAttributeDef {
            Key = "foreign-key-proxy",
            DisplayName = "外部キー代理",
            Type = E_OptionalAttributeType.String,
            HelpText = $$"""
                通常はref-to毎にDBの外部キーのカラムが生成されるところ、
                そのうちの一部を、ref-toが存在しなかった場合であっても存在する元々あるカラムで代替させる設定。
                ref.Prop1.Prop2=this.PARENT.Prop3.Prop4;Prop5=Prop6 のように、「ref.略=this.略」の形で書く。
                略の部分はDB名ではなく物理名。複数のキーを代理させる場合は「ref.略=this.略;ref.略=this.略;」のようにセミコロンで区切る。
                【注意】2024-12-28現在、時間の都合上、ユニットテスト105番「105_外部キー代理.xml」のパターンしかテストしていません。
                """,
            Validate = (value, node, schema, errors) => {
                if (node.Type?.StartsWith(MutableSchemaNode.REFTO_PREFIX) != true) {
                    errors.Add($"{MutableSchemaNode.REFTO_PREFIX} にのみ設定できます。");

                } else {
                    var refTo = schema.FindRefToNode(node.Type);
                    var parent = schema.GetParent(node);
                    if (refTo != null && parent != null) {
                        var availableRefToKeys = GetProxyValidationItem(refTo, schema, true).ToArray();
                        var availableProxies = GetProxyValidationItem(parent, schema, false).ToArray();
                        var splitted = value?.Split(FOREIGN_KEY_PROXY_MEMBER_SPLITTER) ?? [];
                        foreach (var x in splitted) {
                            var errorMessage = RefForeignKeyProxySetting.ParseOrGetErrorMessage(x, availableRefToKeys, availableProxies, out var _);
                            if (errorMessage != null) errors.Add(errorMessage);
                        }
                    }
                }
            },
            EditAggregateMemberOption = (value, node, schema, opt) => {
                var refTo = schema.FindRefToNode(node.Type);
                var parent = schema.GetParent(node);
                var availableRefToKeys = GetProxyValidationItem(refTo ?? throw new InvalidOperationException("バリデーションでチェックがかかっているはずなのでこの分岐に来ることは無い"), schema, true).ToArray();
                var availableProxies = GetProxyValidationItem(parent ?? throw new InvalidOperationException("バリデーションでチェックがかかっているはずなのでこの分岐に来ることは無い"), schema, false).ToArray();

                opt.ForeignKeyProxies = value
                    ?.Split(FOREIGN_KEY_PROXY_MEMBER_SPLITTER)
                    .Select(x => RefForeignKeyProxySetting.ParseOrGetErrorMessage(x, availableRefToKeys, availableProxies, out var p) == null
                        ? p
                        : throw new InvalidOperationException("バリデーションでチェックがかかっているはずなのでこの分岐に来ることは無い"))
                    .ToArray();
            },
        };
        private const char FOREIGN_KEY_PROXY_MEMBER_SPLITTER = ';';
        private static IEnumerable<RefForeignKeyProxySetting.AvailableItem> GetProxyValidationItem(MutableSchemaNode node, MutableSchema schema, bool keyOnly) {
            var parent = schema.GetParent(node);
            if (parent != null) {
                yield return new() {
                    RelationPhysicalName = AggregateMember.PARENT_PROPNAME,
                    GetNeighborItems = () => GetProxyValidationItem(parent, schema, keyOnly),
                };
            }

            foreach (var child in schema.GetChildren(node)) {
                // 参照先の子要素はkey指定されたもののみ指定可能
                if (keyOnly && (child.AttrValues?.Any(kv => kv.Key == KeyDef.Key) != true)) continue;

                yield return new() {
                    RelationPhysicalName = child.GetPhysicalName(),
                    GetNeighborItems = () => {
                        if (child.Type?.StartsWith(MutableSchemaNode.REFTO_PREFIX) == true) {
                            var refTo = schema.FindRefToNode(child.Type);
                            return refTo != null ? GetProxyValidationItem(refTo, schema, keyOnly) : [];
                        } else {
                            return GetProxyValidationItem(child, schema, keyOnly);
                        }
                    },
                };
            }
        }
        #endregion オプショナル属性
    }
}
