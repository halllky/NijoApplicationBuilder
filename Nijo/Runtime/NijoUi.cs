using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Nijo.Runtime {
    /// <summary>
    /// nijo.xml を生XMLではなくブラウザ上のGUIで編集する機能
    /// </summary>
    internal class NijoUi {
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
                var typeDefs = EnumerateGridRowTypes().ToList();
                var optionDefs = EnumerateOptionalAttributes().ToList();

                var rootAggregates = new NijoXmlFile(_project.SchemaXmlPath)
                    .GetRootAggregatesRecursively()
                    .SelectMany(x => x.ToGridRow(typeDefs, optionDefs.ToDictionary(d => d.Key)));

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(new InitialLoadData {
                    // このプロパティ名やデータの内容はGUIアプリ側の InitialLoadData の型と合わせる必要がある
                    ProjectRoot = _project.SolutionRoot,
                    EditingXmlFilePath = _project.SchemaXmlPath,
                    GridRows = rootAggregates.ToList(),
                    GridRowTypes = typeDefs,
                    OptionalAttributes = optionDefs,
                }.ConvertToJson());
            });

            // 編集中のバリデーション
            app.MapPost("/validate", async context => {
                try {
                    var collection = await ToGridRowList(context.Request.Body);
                    var errors = ValidationError.ToErrorObjectJson(collection.CollectVaridationErrors());

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
                    var collection = await ToGridRowList(context.Request.Body);
                    var errors = collection.CollectVaridationErrors().ToArray();
                    if (errors.Length > 0) {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.ContentType = "application/json";
                        var errorsJson = ValidationError.ToErrorObjectJson(errors);
                        await context.Response.WriteAsync(errorsJson);
                        return;
                    }

                    // 保存
                    var nijoXml = new NijoXmlFile(_project.SchemaXmlPath);
                    nijoXml.ClearRecursively();

                    var allElements = collection
                        .RootAggregates()
                        .Select(a => NijoXmlElement.FromGridRow(a, collection));
                    foreach (var el in allElements) {
                        nijoXml.Add(el);
                    }
                    nijoXml.SaveRecursively(_project.BuildSchema().ApplicationName);

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
        /// HTTPリクエストボディから <see cref="NijoUiGridRowList"/> のインスタンスを作成
        /// </summary>
        /// <returns></returns>
        private async Task<NijoUiGridRowList> ToGridRowList(Stream httpRequestBody) {
            using var sr = new StreamReader(httpRequestBody);
            var json = await sr.ReadToEndAsync();
            var obj = json.ParseAsJson<ClientRequest>();

            return new NijoUiGridRowList(obj.Aggregates ?? []);
        }

        /// <summary>
        /// 深さの情報だけを持っている <see cref="NijoUiGridRow"/> の一覧に対して、
        /// 祖先や子孫を取得するといったツリー構造データに対する操作を提供します。
        /// </summary>
        private class NijoUiGridRowList : IReadOnlyList<NijoUiGridRow> {
            public NijoUiGridRowList(IList<NijoUiGridRow> list) {
                _list = list;
            }
            private readonly IList<NijoUiGridRow> _list;

            /// <summary>
            /// ルート集約のみ列挙する
            /// </summary>
            public IEnumerable<NijoUiGridRow> RootAggregates() {
                foreach (var item in _list) {
                    if (item.Depth == 0) yield return item;
                }
            }

            /// <summary>
            /// 直近の親を返す
            /// </summary>
            public NijoUiGridRow? GetParent(NijoUiGridRow agg) {
                var currentIndex = _list.IndexOf(agg);
                if (currentIndex == -1) throw new InvalidOperationException($"{agg}はこの一覧に属していません。");
                while (true) {
                    currentIndex--;
                    if (currentIndex < 0) return null;

                    var maybeParent = _list[currentIndex];
                    if (maybeParent.Depth < agg.Depth) return maybeParent;
                }
            }
            /// <summary>
            /// ルート要素を返す
            /// </summary>
            public NijoUiGridRow GetRoot(NijoUiGridRow agg) {
                return GetAncestors(agg).FirstOrDefault() ?? agg;
            }
            /// <summary>
            /// 祖先を返す。より階層が浅いほうが先。
            /// </summary>
            public IEnumerable<NijoUiGridRow> GetAncestors(NijoUiGridRow agg) {
                var ancestors = new List<NijoUiGridRow>();
                var currentDepth = agg.Depth;
                var currentIndex = _list.IndexOf(agg);
                if (currentIndex == -1) throw new InvalidOperationException($"{agg}はこの一覧に属していません。");
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
            /// <param name="agg"></param>
            /// <returns></returns>
            public IEnumerable<NijoUiGridRow> GetChildren(NijoUiGridRow agg) {
                var currentIndex = _list.IndexOf(agg);
                if (currentIndex == -1) throw new InvalidOperationException($"{agg}はこの一覧に属していません。");
                while (true) {
                    currentIndex++;
                    if (currentIndex >= _list.Count) yield break;

                    var maybeChild = _list[currentIndex];
                    if (maybeChild.Depth <= agg.Depth) yield break;

                    if (GetParent(maybeChild) == agg) yield return maybeChild;
                }
            }
            /// <summary>
            /// 子孫を返す
            /// </summary>
            public IEnumerable<NijoUiGridRow> GetDescendants(NijoUiGridRow agg) {
                var currentIndex = _list.IndexOf(agg);
                if (currentIndex == -1) throw new InvalidOperationException($"{agg}はこの一覧に属していません。");
                while (true) {
                    currentIndex++;
                    if (currentIndex >= _list.Count) yield break;

                    var maybeDescendant = _list[currentIndex];
                    if (maybeDescendant.Depth <= agg.Depth) yield break;

                    yield return maybeDescendant;
                }
            }

            /// <summary>
            /// エラーを収集します。
            /// </summary>
            public IEnumerable<ValidationError> CollectVaridationErrors() {
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
                    if (node.Depth > 0 && root.Type == "enum") {
                        // enumの要素なのに型が指定されている
                        if (!string.IsNullOrWhiteSpace(node.Type)) {
                            yield return new ValidationError {
                                Node = node,
                                Key = ValidationError.ERR_TO_TYPE,
                                Message = "列挙体の要素に型を指定することはできません。",
                            };
                        }

                        foreach (var attrValue in node.AttrValues ?? []) {
                            yield return new ValidationError {
                                Node = node,
                                Key = attrValue.Key!,
                                Message = "列挙体の要素にこの属性を指定することはできません。",
                            };
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
                    } else if (node.Type.StartsWith(NijoXmlElement.REFTO_PREFIX)) {
                        // ref-to
                        var uniqueId = node.Type.Substring(NijoXmlElement.REFTO_PREFIX.Length);
                        var refTo = _list.SingleOrDefault(n => n.UniqueId == uniqueId);
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
                        }
                    } else if (node.Type.StartsWith(NijoXmlElement.ENUM_PREFIX)) {
                        // 列挙体
                        var uniqueId = node.Type.Substring(NijoXmlElement.ENUM_PREFIX.Length);
                        if (!RootAggregates().Any(n => n.UniqueId == uniqueId
                                                    && n.Type == "enum")) {
                            yield return new ValidationError {
                                Node = node,
                                Key = ValidationError.ERR_TO_TYPE,
                                Message = "指定された列挙体定義が見つかりません。",
                            };
                        }
                    } else {
                        // 上記以外
                        var typeDef = EnumerateGridRowTypes().SingleOrDefault(d => d.Key == node.Type);
                        if (typeDef == null) {
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
                        var optionDef = EnumerateOptionalAttributes().SingleOrDefault(d => d.Key == attrValue.Key);
                        if (optionDef == null) {
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
            public NijoUiGridRow this[int index] => _list[index];
            public int Count => _list.Count;
            public IEnumerator<NijoUiGridRow> GetEnumerator() {
                return _list.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
            #endregion IReadOnlyListの実装
        }
        private class ValidationError {
            /// <summary>どの集約またはメンバーでエラーが発生したか</summary>
            public required NijoUiGridRow Node { get; init; }
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
                var keyNameDict = EnumerateOptionalAttributes().ToDictionary(x => x.Key, x => x.DisplayName);
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
            [JsonPropertyName("aggregates")]
            public List<NijoUiGridRow>? GridRows { get; set; }
            [JsonPropertyName("aggregateOrMemberTypes")]
            public List<GridRowTypeDef>? GridRowTypes { get; set; }
            [JsonPropertyName("optionalAttributes")]
            public List<OptionalAttributeDef>? OptionalAttributes { get; set; }
        }
        /// <summary>
        /// クライアントからサーバーへ送るデータ
        /// </summary>
        private class ClientRequest {
            [JsonPropertyName("aggregates")]
            public List<NijoUiGridRow>? Aggregates { get; set; }
        }

        private class NijoUiGridRow {
            [JsonPropertyName("depth")]
            public required int Depth { get; set; }
            [JsonPropertyName("uniqueId")]
            public required string UniqueId { get; set; }
            [JsonPropertyName("displayName")]
            public string? DisplayName { get; set; }
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

            public string GetPhysicalName() {
                return AttrValues
                    ?.FirstOrDefault(attr => attr.Key == OptionalAttributeDef.PHYSICAL_NAME)
                    ?.Value
                    ?? DisplayName?.ToCSharpSafe()
                    ?? string.Empty;
            }
            public string GetRefToPath(NijoUiGridRowList collection) {
                return collection
                    .GetAncestors(this)
                    .Concat([this])
                    .Select(agg => agg.GetPhysicalName())
                    .Join("/");
            }
            public E_NodeType? GetNodeType() {
                if (string.IsNullOrWhiteSpace(Type)) return null;
                if (Type.StartsWith(NijoXmlElement.REFTO_PREFIX)) return E_NodeType.Ref;
                if (Type.StartsWith(NijoXmlElement.ENUM_PREFIX)) return E_NodeType.Enum;
                return EnumerateGridRowTypes().SingleOrDefault(t => t.Key == Type)?.NodeType;
            }
            public bool IsWriteModel(NijoUiGridRowList schema) {
                var root = schema.GetRoot(this);
                return root.Type?.Contains("write-model-2") == true;
            }
            public bool IsReadModel(NijoUiGridRowList schema) {
                var root = schema.GetRoot(this);
                return root.Type?.Contains("read-model-2") == true
                    || root.Type?.Contains("generate-default-read-model") == true;
            }
            /// <summary>
            /// エラーチェック
            /// </summary>
            public void Validate(NijoUiGridRowList schema, ICollection<string> errors) {
                // 名前必須
                if (string.IsNullOrWhiteSpace(DisplayName)) {
                    errors.Add("項目名を指定してください。");
                }

                // 主キー必須
                var children = schema.GetChildren(this).ToArray();
                var nodeType = GetNodeType();
                if (Depth == 0
                    && children.All(c => c.AttrValues == null || !c.AttrValues.Any(a => a.Key == "key"))
                    && (IsWriteModel(schema) || IsReadModel(schema))
                    && (nodeType == E_NodeType.RootAggregate || Type == "children")) {
                    errors.Add("ルート集約とChildrenではキー指定が必須です。");
                }
            }

            public override string ToString() {
                return DisplayName ?? $"ID::{UniqueId}";
            }
        }
        /// <summary>
        /// <see cref="NijoUiGridRow.Type"/> の種類
        /// </summary>
        [Flags]
        private enum E_NodeType {
            /// <summary>集約</summary>
            Aggregate = 0b0001,
            /// <summary>集約メンバー</summary>
            AggregateMember = 0b0010,

            // ------------------------------------------

            /// <summary>ルート集約</summary>
            RootAggregate = 0b01001,
            /// <summary>Child, Children, VariationItem</summary>
            DescendantAggregate = 0b00101,

            /// <summary>ref-to</summary>
            Ref = 0b0000110,
            /// <summary>列挙体</summary>
            Enum = 0b0001010,
            /// <summary>バリエーションのコンテナの方（VariationItemでない方）</summary>
            Variation = 0b0010010,
            /// <summary>上記以外</summary>
            SchalarMember = 0b0100010,
        }
        private class OptionalAttributeValue {
            [JsonPropertyName("key")]
            public string? Key { get; set; }
            [JsonPropertyName("value")]
            public string? Value { get; set; }
        }
        private class GridRowTypeDef {
            [JsonPropertyName("key")]
            public string? Key { get; set; }
            [JsonPropertyName("displayName")]
            public string? DisplayName { get; set; }
            [JsonPropertyName("helpText")]
            public string? HelpText { get; set; }
            [JsonPropertyName("requiredNumberValue")]
            public bool? RequiredNumberValue { get; set; }

            /// <summary>
            /// XML要素の内容を判断し、この型に属するかどうかを返します。
            /// 属する場合、該当の <see cref="NijoXmlElement.IsAttribute"/> オブジェクトを返します。
            /// </summary>
            [JsonIgnore]
            public Func<NijoXmlElement, NijoXmlElement.IsAttribute?> FindMatchingIsAttribute { get; set; } = (_ => null);
            /// <summary>
            /// バリデーション
            /// </summary>
            [JsonIgnore]
            public Action<NijoUiGridRow, NijoUiGridRowList, ICollection<string>> Validate { get; set; } = ((_, _, _) => { });
            /// <summary>
            /// この種別に属するノードの種類
            /// </summary>
            [JsonIgnore]
            public E_NodeType NodeType { get; set; }
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
            public Action<string?, NijoUiGridRow, NijoUiGridRowList, ICollection<string>> Validate { get; set; } = ((_, _, _, _) => { });
        }
        private enum E_OptionalAttributeType {
            String,
            Number,
            Boolean,
        }

        /// <summary>
        /// 集約やメンバーの種類として指定することができる属性を列挙します。
        /// </summary>
        private static IEnumerable<GridRowTypeDef> EnumerateGridRowTypes() {

            // ルート集約に設定できる種類
            yield return new GridRowTypeDef {
                NodeType = E_NodeType.RootAggregate,
                Key = "write-model-2", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "WriteModel",
                HelpText = $$"""
                    このルート要素がDB保存されるべきデータであることを表します。
                    Entity Framework Core のエンティティ定義や、作成・更新・削除のWeb API エンドポイントなどが生成されます。
                    切り分けの目安は、データベースの排他制御やトランザクションの粒度です。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && !el.Is.ContainsKey("generate-default-read-model")
                                             && el.Is.TryGetValue("write-model-2", out var isAttribute) ? isAttribute : null,
                Validate = (node, schema, errors) => {
                    if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");
                },
            };
            yield return new GridRowTypeDef {
                NodeType = E_NodeType.RootAggregate,
                Key = "read-model-2", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "ReadModel",
                HelpText = $$"""
                    このルート要素が人間が閲覧するデータであることを表します。
                    一覧検索画面、詳細画面、一括編集画面などが生成されます。
                    切り分けの目安は詳細画面1個の粒度です。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && el.Is.TryGetValue("read-model-2", out var isAttribute) ? isAttribute : null,
                Validate = (node, schema, errors) => {
                    if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");

                },
            };
            yield return new GridRowTypeDef {
                NodeType = E_NodeType.RootAggregate,
                Key = "write-model-2 generate-default-read-model", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "Write & Read",
                HelpText = $$"""
                    ReadModel から生成されるコードと WriteModel から生成されるコードの両方が生成されます。
                    画面のデータ項目とDBのデータ構造が寸分違わず完全に一致する場合にのみ使えます。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && el.Is.ContainsKey("generate-default-read-model")
                                             && el.Is.TryGetValue("write-model-2", out var isAttribute) ? isAttribute : null,
                Validate = (node, schema, errors) => {
                    if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");

                },
            };
            yield return new GridRowTypeDef {
                NodeType = E_NodeType.RootAggregate,
                Key = "enum", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "Enum",
                HelpText = $$"""
                    このルート要素が列挙体であることを表します。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && el.Is.TryGetValue("enum", out var isAttribute) ? isAttribute : null,
                Validate = (node, schema, errors) => {
                    if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");

                },
            };
            yield return new GridRowTypeDef {
                NodeType = E_NodeType.RootAggregate,
                Key = "command",
                DisplayName = "Command",
                HelpText = $$"""
                    このルート要素が、人間や外部システムが起動する処理のパラメータであることを表します。
                    この処理を実行するWebAPIエンドポイントや、パラメータを入力するためのダイアログのUIコンポーネントなどが生成されます。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && el.Is.TryGetValue("command", out var isAttribute) ? isAttribute : null,
                Validate = (node, schema, errors) => {
                    if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");

                    var children = schema.GetChildren(node).ToArray();
                    if (children.Any(x => x.Type == "step") && children.Any(x => x.Type != "step")) {
                        errors.Add("ステップ属性を定義する場合は全てステップにする必要があります。");
                    }
                },
            };
            yield return new GridRowTypeDef {
                NodeType = E_NodeType.RootAggregate,
                Key = "value-object",
                DisplayName = "値オブジェクト(ValueObject)",
                HelpText = $$"""
                    値オブジェクト。主として「○○コード」などの識別子の型として使われる。
                    同値比較がそのインスタンスの参照ではなく値によって行われる。不変（immutable）である。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && el.Is.TryGetValue("value-object", out var isAttribute) ? isAttribute : null,
                Validate = (node, schema, errors) => {
                    if (node.Depth != 0) errors.Add("この型はルート要素にしか設定できません。");
                    if (schema.GetChildren(node).Any()) errors.Add("この型に子要素を設定することはできません。");
                },
            };

            // ルート以外に設定できる種類
            // ※ ref-toと列挙体は集約定義に依存するのでクライアント側で計算する
            yield return new GridRowTypeDef {
                NodeType = E_NodeType.DescendantAggregate,
                Key = "child", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "Child",
                HelpText = $$"""
                    親1件に対する1件の子要素。
                    """,
                FindMatchingIsAttribute = el => {
                    if (el.Depth == 0) return null;
                    if (el.Is.TryGetValue("child", out var isAttribute1)) return isAttribute1;
                    if (el.Is.TryGetValue("section", out var isAttribute2)) return isAttribute2; // section属性は廃止予定
                    return null;
                },
                Validate = (node, schema, errors) => {
                    if (node.Depth == 0) errors.Add("この型は子孫要素にしか設定できません。");

                },
            };
            yield return new GridRowTypeDef {
                NodeType = E_NodeType.DescendantAggregate,
                Key = "children", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "Children",
                HelpText = $$"""
                    親1件に対する複数件の子要素。 
                    """,
                FindMatchingIsAttribute = el => {
                    if (el.Depth == 0) return null;
                    if (el.Is.TryGetValue("children", out var isAttribute1)) return isAttribute1;
                    if (el.Is.TryGetValue("array", out var isAttribute2)) return isAttribute2; // array属性は廃止予定
                    return null;
                },
                Validate = (node, schema, errors) => {
                    if (node.Depth == 0) errors.Add("この型は子孫要素にしか設定できません。");

                },
            };
            yield return new GridRowTypeDef {
                NodeType = E_NodeType.Variation,
                Key = "variation",
                DisplayName = "Variation",
                HelpText = $$"""
                    親1件に対し、異なるデータ構造をもつ複数の子要素から1種類を選択するもの。
                    """,
                FindMatchingIsAttribute = el => el.Depth > 0
                                             && el.Is.TryGetValue("variation", out var isAttribute) ? isAttribute : null,
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
            yield return new GridRowTypeDef {
                NodeType = E_NodeType.DescendantAggregate,
                Key = "variation-item", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "VariationItem",
                RequiredNumberValue = true,
                HelpText = $$"""
                    バリエーションの1種類を表します。
                    """,
                FindMatchingIsAttribute = el => {
                    if (el.Depth == 0) return null;
                    if (el.Is.TryGetValue("variation-item", out var isAttribute1)) return isAttribute1;
                    if (el.Is.TryGetValue("variation-key", out var isAttribute2)) return isAttribute2; // variation-key属性は廃止予定
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
            };

            yield return new GridRowTypeDef {
                NodeType = E_NodeType.DescendantAggregate,
                Key = "step",
                DisplayName = "ステップ",
                RequiredNumberValue = true,
                HelpText = $$"""
                    ほぼChildと同じですが、ルート集約がCommandの場合、かつルート集約の直下にのみ設定可能です。
                    そのコマンドの子要素がステップの場合、コマンドのUIがウィザード形式になります。
                    """,
                FindMatchingIsAttribute = el => el.Depth > 0
                                             && el.Is.TryGetValue("step", out var isAttribute) ? isAttribute : null,
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
            };

            var resolver = MemberTypeResolver.Default();
            foreach (var (key, memberType) in resolver.EnumerateAll()) {
                yield return new GridRowTypeDef {
                    NodeType = E_NodeType.SchalarMember,
                    Key = key,
                    DisplayName = memberType.GetUiDisplayName(),
                    HelpText = memberType.GetHelpText(),
                    FindMatchingIsAttribute = el => el.Depth > 0 && el.Is.TryGetValue(key, out var isAttribute) ? isAttribute : null,
                    Validate = (node, schema, errors) => {

                    },
                };
            }
        }
        /// <summary>
        /// 集約やメンバーのオプショナル属性として指定することができる属性を列挙します。
        /// </summary>
        private static IEnumerable<OptionalAttributeDef> EnumerateOptionalAttributes() {

            yield return new OptionalAttributeDef {
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
                    }
                },
            };

            yield return new OptionalAttributeDef {
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
            };

            yield return new OptionalAttributeDef {
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
            };

            // --------------------------------------------

            yield return new OptionalAttributeDef {
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
                    } else if (root.Type == "enum") {
                        errors.Add("列挙体定義にキーを指定することはできません。");
                    }

                    var parent = schema.GetParent(node);
                    if (parent == null) {
                        errors.Add("ルート集約にキーを指定することはできません。");
                    } else if (parent.Type == "child") {
                        errors.Add("Childにキーを指定することはできません。");
                    } else if (parent.Type == "variation" || parent.Type == "variation-item") {
                        errors.Add("バリエーションにキーを指定することはできません。");
                    }
                },
            };

            yield return new OptionalAttributeDef {
                Key = "name",
                DisplayName = "Name",
                Type = E_OptionalAttributeType.Boolean,
                HelpText = $$"""
                    この項目がその集約の名前項目であることを表します。
                    詳細画面のタイトルやコンボボックスの表示テキストにどの項目が使われるかで参照されます。
                    未指定の場合はキーが表示名称として使われます。
                    """,
                Validate = (value, node, schema, errors) => {

                },
            };

            yield return new OptionalAttributeDef {
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
            };

            yield return new OptionalAttributeDef {
                Key = "max",
                DisplayName = "Max",
                Type = E_OptionalAttributeType.Number,
                HelpText = $$"""
                    文字列項目の最大長。整数で指定してください。
                    """,
                Validate = (value, node, schema, errors) => {
                    // TODO: チェック処理未実装
                },
            };

            // --------------------------------------------

            yield return new OptionalAttributeDef {
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
                    }
                },
            };

            yield return new OptionalAttributeDef {
                Key = "has-lifecycle",
                DisplayName = "独立ライフサイクル",
                Type = E_OptionalAttributeType.Boolean,
                HelpText = $$"""
                    ReadModelの中にあるChildのうち、そのChildが追加削除できるものであることを表します。
                    """,
                Validate = (value, node, schema, errors) => {
                    if (!node.IsReadModel(schema) || node.Type != "child") {
                        errors.Add("この属性はReadModelのChildにのみ設定可能です。");
                    }
                },
            };

            yield return new OptionalAttributeDef {
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
            };

            // --------------------------------------------

            yield return new OptionalAttributeDef {
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
            };

            yield return new OptionalAttributeDef {
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
            };

            yield return new OptionalAttributeDef {
                Key = "width",
                DisplayName = "横幅",
                Type = E_OptionalAttributeType.String,
                HelpText = $$"""
                    詳細画面における当該項目の横幅を変更できます。全角10文字の場合は "z10"、半角6文字の場合は "h6" など、zかhのあとに整数を続けてください。
                    """,
                Validate = (value, node, schema, errors) => {
                    if (node.GetNodeType() != E_NodeType.SchalarMember) { 
                        errors.Add("この属性はテキストボックスをもつ項目にのみ指定できます。");
                    }
                },
            };

            yield return new OptionalAttributeDef {
                Key = "single-view-ui",
                DisplayName = "詳細画面UI",
                Type = E_OptionalAttributeType.String,
                HelpText = $$"""
                    詳細画面におけるこの項目の入力フォームが、自動生成されるものではなくここで指定した名前のコンポーネントになります。
                    コンポーネントは自前で実装する必要があります。
                    """,
                Validate = (value, node, schema, errors) => {
                    var nodeType = node.GetNodeType();
                    if (nodeType != E_NodeType.Ref && nodeType != E_NodeType.Enum && nodeType != E_NodeType.SchalarMember) {
                        errors.Add("この属性は入力フォームをもつ項目にのみ指定できます。");
                    }
                },
            };

            yield return new OptionalAttributeDef {
                Key = "search-condition-ui",
                DisplayName = "検索条件欄UI",
                Type = E_OptionalAttributeType.String,
                HelpText = $$"""
                    詳細画面におけるこの項目の入力フォームが、自動生成されるものではなくここで指定した名前のコンポーネントになります。
                    コンポーネントは自前で実装する必要があります。
                    """,
                Validate = (value, node, schema, errors) => {
                    var nodeType = node.GetNodeType();
                    if (nodeType != E_NodeType.Ref && nodeType != E_NodeType.Enum && nodeType != E_NodeType.SchalarMember) {
                        errors.Add("この属性は入力フォームをもつ項目にのみ指定できます。");
                    }
                },
            };

            // -------------------------------
            // 将来的に修正される予定の属性

            yield return new OptionalAttributeDef {
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
            };

            yield return new OptionalAttributeDef {
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
            };
        }


        /// <summary>
        /// 仕様上、XMLは複数ファイルに分けて記載することができるところ、
        /// ほかのXMLファイルの参照情報を含んだ情報
        /// </summary>
        private class NijoXmlFile {
            public NijoXmlFile(string fullpath) {
                FullPath = fullpath;
            }
            /// <summary>
            /// このXMLファイルのフルパス
            /// </summary>
            public string FullPath { get; }
            private readonly List<NijoXmlElement> _rootAggregates = new();
            private readonly List<NijoXmlFile> _included = new();

            /// <summary>
            /// XMLに書かれているルート集約、またはこのオブジェクトが持っている未保存のルート集約を再帰的に列挙する
            /// </summary>
            public IEnumerable<NijoXmlElement> GetRootAggregatesRecursively() {
                LoadIfNotInitialized();
                foreach (var el in _rootAggregates) {
                    yield return el;
                }
                foreach (var el in _included.SelectMany(xml => xml.GetRootAggregatesRecursively())) {
                    yield return el;
                }
            }
            /// <summary>
            /// このオブジェクトまたはIncludeされたオブジェクトが保持している集約をクリアする
            /// </summary>
            public void ClearRecursively() {
                LoadIfNotInitialized();
                _rootAggregates.Clear();
                foreach (var xml in _included) {
                    xml.ClearRecursively();
                }
            }
            /// <summary>
            /// このオブジェクトまたはIncludeされたオブジェクトにルート集約を加える。
            /// <see cref="NijoXmlElement.XmlFileFullpath"/> を参照し、可能な限りそのXMLファイルが元々保存されていたファイルに戻す。
            /// </summary>
            public void Add(NijoXmlElement element) {
                LoadIfNotInitialized();
                if (TryAddRecursivelyIfFileNameEquals(element)) {
                    return;
                }
                // どのInclude先XMLファイルにもAddできなかった場合は仕方なくこのオブジェクトにAddする
                _rootAggregates.Add(element);
            }
            private bool TryAddRecursivelyIfFileNameEquals(NijoXmlElement element) {
                LoadIfNotInitialized();
                if (element.XmlFileFullpath == FullPath) {
                    _rootAggregates.Add(element);
                    return true;
                }
                foreach (var xml in _included) {
                    if (xml.TryAddRecursivelyIfFileNameEquals(element)) return true;
                }
                return false;
            }
            /// <summary>
            /// XMLファイルへの保存
            /// </summary>
            public void SaveRecursively(string rootNodeName) {
                LoadIfNotInitialized();
                var doc = new XDocument();
                doc.Declaration = new XDeclaration("1.0", "utf-8", null);

                var rootNode = new XElement(rootNodeName);
                var dirName = Path.GetDirectoryName(FullPath);
                foreach (var xml in _included) {
                    var include = new XElement(XName.Get(AppSchemaXml.INCLUDE));
                    var relativePath = dirName == null
                        ? xml.FullPath
                        : Path.GetRelativePath(dirName, xml.FullPath);
                    include.SetAttributeValue(AppSchemaXml.PATH, relativePath);
                    rootNode.Add(include);
                }
                foreach (var el in _rootAggregates.SelectMany(x => x.ToXNodes())) {
                    rootNode.Add(el);
                }
                doc.Add(rootNode);

                using (var writer = XmlWriter.Create(FullPath, new() {
                    Indent = true,
                    Encoding = new UTF8Encoding(false, false),
                    NewLineChars = "\n",
                })) {
                    doc.Save(writer);
                }

                foreach (var xml in _included) {
                    xml.SaveRecursively(rootNodeName);
                }
            }

            private bool _initialized = false;
            private void LoadIfNotInitialized() {
                if (_initialized) return;
                _initialized = true;

                using var stream = File.Open(FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var xmlContent = reader.ReadToEnd();
                var xDocument = XDocument.Parse(xmlContent);
                var rootNodes = xDocument.Root?.Nodes().ToArray() ?? [];
                var comments = new List<XComment>();
                for (int i = 0; i < rootNodes.Length; i++) {
                    var node = rootNodes[i];
                    if (node is XComment xComment) {
                        comments.Add(xComment);

                    } else if (node is XElement xElement) {
                        if (xElement.Name.LocalName == AppSchemaXml.INCLUDE) {
                            // <Include Path="..." /> で他のXMLファイルを読み込む
                            var relativePath = xElement.Attribute(AppSchemaXml.PATH)?.Value;
                            if (relativePath != null) {
                                var dirName = Path.GetDirectoryName(FullPath);
                                var fullpath = dirName == null
                                    ? Path.GetFullPath(relativePath)
                                    : Path.GetFullPath(Path.Combine(dirName, relativePath));
                                var includeded = new NijoXmlFile(fullpath);
                                _included.Add(includeded);
                            }
                        } else {
                            // ルート集約
                            _rootAggregates.Add(new NijoXmlElement(xElement, FullPath, comments.ToArray()));
                            comments.Clear();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// XMLのノード1個の抽象。
        /// </summary>
        private class NijoXmlElement {

            /// <summary>
            /// XMLのノード1個の抽象を作成します。
            /// </summary>
            /// <param name="xElement">XML要素</param>
            /// <param name="xmlFilePath">この要素が記載されていたXMLファイルの名前</param>
            /// <param name="comments">XML要素の前（ <see cref="XNode.PreviousNode"/> ）にあるコメント</param>
            public NijoXmlElement(XElement xElement, string? xmlFilePath, IEnumerable<XComment> comments) {
                _xElement = xElement;
                _comments = comments;
                XmlFileFullpath = xmlFilePath;
            }
            private readonly XElement _xElement;
            private readonly IEnumerable<XComment> _comments;
            /// <summary>
            /// このエレメントがもともと保存されていたXMLファイルの名前。このエレメントが子孫要素の場合はnull
            /// </summary>
            public string? XmlFileFullpath { get; }

            #region 深さ
            /// <summary>
            /// 要素の深さ。ルート集約ならば0。
            /// </summary>
            public int Depth => _depth ??= GetDepth();
            private int? _depth;
            private int GetDepth() {
                var element = _xElement;
                var depth = -1; // XDocumentのRootを-1とする
                while (element.Parent != null) {
                    depth++;
                    element = element.Parent;
                }
                return depth;
            }
            #endregion 深さ

            #region is属性
            /// <summary>
            /// is="" 属性の内容
            /// </summary>
            public IReadOnlyDictionary<string, IsAttribute> Is => _isAttrCache ??= ParseIsAttribute().ToDictionary(x => x.Key);
            private IReadOnlyDictionary<string, IsAttribute>? _isAttrCache;
            private IEnumerable<IsAttribute> ParseIsAttribute() {
                var isAttributeValue = _xElement.Attribute(IS)?.Value;
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

            #region nijo ui の画面表示用データとの変換
            /// <summary>
            /// nijo ui の画面上で編集されるデータをXML要素に変換する
            /// </summary>
            /// <param name="gridRow">変換元</param>
            /// <param name="collection">全ての集約が入ったコレクション</param>
            public static NijoXmlElement FromGridRow(
                NijoUiGridRow gridRow,
                NijoUiGridRowList collection) {

                var physicalName = gridRow.GetPhysicalName();
                var el = new XElement(physicalName);

                // ---------------------------------
                // is属性
                var isAttrs = new List<string>();

                // 型
                if (!string.IsNullOrWhiteSpace(gridRow.Type)) {
                    if (gridRow.Type.StartsWith(REFTO_PREFIX)) {
                        // ref-to
                        var uniqueId = gridRow.Type.Substring(REFTO_PREFIX.Length);
                        var refToPath = collection
                            .FirstOrDefault(agg => agg.UniqueId == uniqueId)
                            ?.GetRefToPath(collection);
                        if (refToPath == null) {
                            isAttrs.Add(REFTO_PREFIX);
                        } else {
                            isAttrs.Add($"{REFTO_PREFIX}{refToPath}");
                        }

                    } else if (gridRow.Type.StartsWith(ENUM_PREFIX)) {
                        // enum
                        var uniqueId = gridRow.Type.Substring(ENUM_PREFIX.Length);
                        var enumName = collection
                            .RootAggregates()
                            .FirstOrDefault(agg => agg.UniqueId == uniqueId
                                                && agg.Type == "enum")
                            ?.GetPhysicalName();
                        if (enumName != null) {
                            isAttrs.Add(enumName);
                        }

                    } else if (!string.IsNullOrWhiteSpace(gridRow.TypeDetail)) {
                        isAttrs.Add($"{gridRow.Type}:{gridRow.TypeDetail}");

                    } else {
                        isAttrs.Add(gridRow.Type);
                    }
                }

                // 型以外のis属性
                foreach (var attr in gridRow.AttrValues ?? []) {
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

                if (gridRow.DisplayName != physicalName) {
                    el.SetAttributeValue(DISPLAY_NAME, gridRow.DisplayName);
                }

                var dbName = gridRow.AttrValues
                    ?.SingleOrDefault(x => x.Key == OptionalAttributeDef.DB_NAME)
                    ?.Value;
                if (!string.IsNullOrWhiteSpace(dbName)) {
                    el.SetAttributeValue(DB_NAME, dbName);
                }

                var latinName = gridRow.AttrValues
                    ?.SingleOrDefault(x => x.Key == OptionalAttributeDef.LATIN)
                    ?.Value;
                if (!string.IsNullOrWhiteSpace(latinName)) {
                    el.SetAttributeValue(LATIN, latinName);
                }

                // ---------------------------------
                // 子要素
                foreach (var child in collection.GetChildren(gridRow)) {
                    if (!string.IsNullOrWhiteSpace(child.Comment)) el.Add(new XComment(child.Comment));
                    el.Add(FromGridRow(child, collection)._xElement);
                }

                var comment = string.IsNullOrWhiteSpace(gridRow.Comment)
                    ? Array.Empty<XComment>()
                    : [new XComment(gridRow.Comment)];

                return new NijoXmlElement(el, gridRow.XmlFileFullPath, comment);
            }
            /// <summary>
            /// XML要素を nijo ui の画面上で編集されるデータに変換する
            /// </summary>
            public IEnumerable<NijoUiGridRow> ToGridRow(
                IEnumerable<GridRowTypeDef> typeDefs,
                IReadOnlyDictionary<string, OptionalAttributeDef> optionDefs) {
                return ToGridRowPrivate(typeDefs, optionDefs, 0);
            }
            private IEnumerable<NijoUiGridRow> ToGridRowPrivate(
                IEnumerable<GridRowTypeDef> typeDefs,
                IReadOnlyDictionary<string, OptionalAttributeDef> optionDefs,
                int depth) {

                // XMLと nijo ui では DisplayName と PhysicalName の扱いが逆
                var displayName = _xElement
                    .Attribute(DISPLAY_NAME)?.Value
                    ?? _xElement.Name.LocalName;

                // -------------------------------------
                // 型

                // ref-toとenum以外の型
                string? type = null;
                string? typeDetail = null;
                foreach (var def in typeDefs.OrderBy(d => d.Key)) {
                    var isAttr = def.FindMatchingIsAttribute(this);
                    if (isAttr != null) {
                        type = def.Key;
                        typeDetail = isAttr.Value;
                        break;
                    }
                }

                // ref-to
                if (type == null && Is.TryGetValue("ref-to", out var refTo)) {
                    var refToElement = _xElement.Document?.XPathSelectElement($"/{_xElement.Document.Root?.Name.LocalName}/{refTo.Value}");
                    if (refToElement != null) {
                        // ここの記法はTypeScript側の型選択コンボボックスのルールと合わせる必要あり
                        type = $"{REFTO_PREFIX}{GetXElementUniqueId(refToElement)}";
                    }
                }

                // enum
                if (type == null) {
                    var isAttr = Is.Keys.OrderBy(k => k).ToArray();
                    var matchedEnum = _xElement.Document?.Root
                        ?.Elements()
                        .FirstOrDefault(el => el.Attribute(IS)?.Value.Contains("enum") == true
                                           && isAttr.Contains(el.Name.LocalName));
                    if (matchedEnum != null) {
                        // ここの記法はTypeScript側の型選択コンボボックスのルールと合わせる必要あり
                        type = $"{ENUM_PREFIX}{GetXElementUniqueId(matchedEnum)}";
                    }
                }

                // -------------------------------------
                // オプショナル属性
                var attrs = Is.Values
                    .Select(a => new OptionalAttributeValue { Key = a.Key, Value = a.Value })
                    .ToList();
                if (_xElement.Attribute(DISPLAY_NAME) != null) {
                    // XMLと nijo ui では DisplayName と PhysicalName の扱いが逆
                    attrs.Add(new OptionalAttributeValue { Key = OptionalAttributeDef.PHYSICAL_NAME, Value = _xElement.Name.LocalName });
                }
                if (_xElement.Attribute(DB_NAME) != null) {
                    attrs.Add(new OptionalAttributeValue { Key = OptionalAttributeDef.DB_NAME, Value = _xElement.Attribute(DB_NAME)?.Value });
                }
                if (_xElement.Attribute(LATIN) != null) {
                    attrs.Add(new OptionalAttributeValue { Key = OptionalAttributeDef.LATIN, Value = _xElement.Attribute(LATIN)?.Value });
                }

                // is属性のうちtypeの方で既にハンドリング済みのものは除外
                attrs.RemoveAll(a => a.Key == null || !optionDefs.ContainsKey(a.Key));

                // -------------------------------------
                // コメント。PreviousNodeを使って取得する都合上、XMLを下から順番に辿る形になる。
                var comment = new StringBuilder();
                var currentElement = (XNode?)_xElement;
                while (currentElement?.PreviousNode is XComment xComment) {
                    var value = comment.Length == 0
                        ? xComment.Value
                        : (xComment.Value + Environment.NewLine);
                    comment.Insert(0, value);
                    currentElement = currentElement.PreviousNode;
                }

                yield return new NijoUiGridRow {
                    Depth = depth,
                    UniqueId = GetXElementUniqueId(_xElement),
                    DisplayName = displayName,
                    Type = type,
                    TypeDetail = typeDetail,
                    AttrValues = attrs,
                    Comment = comment.ToString(),
                    XmlFileFullPath = XmlFileFullpath,
                };

                // -------------------------------------
                // コメント
                var nodes = _xElement.Nodes().ToArray();
                var descendants = new List<NijoUiGridRow>();
                var comments = new List<XComment>();
                for (int i = 0; i < nodes.Length; i++) {
                    var node = nodes[i];
                    if (node is XComment xComment) {
                        comments.Add(xComment);
                    } else if (node is XElement xElement) {
                        var nijoXmlElement = new NijoXmlElement(xElement, null, comments.ToArray());
                        descendants.AddRange(nijoXmlElement.ToGridRowPrivate(typeDefs, optionDefs, depth + 1));
                        comments.Clear();
                    }
                }
                foreach (var descendant in descendants) {
                    yield return descendant;
                }
            }

            private static string GetXElementUniqueId(XElement xElement) {
                return xElement.GetHashCode().ToString().ToHashedString();
            }
            #endregion nijo ui の画面表示用データとの変換

            public IEnumerable<XNode> ToXNodes() {
                foreach (var comment in _comments) {
                    yield return comment;
                }
                yield return _xElement;
            }

            public override string ToString() {
                return _xElement.ToString();
            }

            public const string IS = "is";
            public const string DISPLAY_NAME = "DisplayName";
            public const string DB_NAME = "DbName";
            public const string LATIN = "Latin";

            public const string REFTO_PREFIX = "ref-to:";
            public const string ENUM_PREFIX = "enum:";
        }
    }
}
