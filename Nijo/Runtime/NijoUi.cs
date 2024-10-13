using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
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
                var typeDefs = EnumerateAggregateOrMemberTypes().ToList();
                var optionDefs = EnumerateOptionalAttributes().ToList();

                var rootXmlElements = _project.SchemaXml.Load().Root?.Elements() ?? [];
                var rootElements = rootXmlElements.Select(el => new NijoXmlElement(el));
                var rootAggregates = rootElements.Select(x => x.ToAbstract(typeDefs, optionDefs.ToDictionary(d => d.Key)));

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(new {
                    // このプロパティ名やデータの内容はGUIアプリ側の PageStateFromServer の型と合わせる必要がある
                    projectRoot = _project.SolutionRoot,
                    editingXmlFilePath = _project.SchemaXml.GetPath(),
                    aggregates = rootAggregates.ToList(),
                    aggregateOrMemberTypes = typeDefs,
                    optionalAttributes = optionDefs,
                }.ToJson());
            });

            return app;
        }


        private class AggregateOrMember {
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
            [JsonPropertyName("children")]
            public List<AggregateOrMember>? Children { get; set; }
            [JsonPropertyName("comment")]
            public string? Comment { get; set; }
        }
        private class OptionalAttributeValue {
            [JsonPropertyName("key")]
            public string? Key { get; set; }
            [JsonPropertyName("value")]
            public string? Value { get; set; }
        }
        private class AggregateOrMemberTypeDef {
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
        }
        private enum E_OptionalAttributeType {
            String,
            Number,
            Boolean,
        }

        /// <summary>
        /// 集約やメンバーの種類として指定することができる属性を列挙します。
        /// </summary>
        private static IEnumerable<AggregateOrMemberTypeDef> EnumerateAggregateOrMemberTypes() {

            // ルート集約に設定できる種類
            yield return new AggregateOrMemberTypeDef {
                Key = "w", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "WriteModel",
                HelpText = $$"""
                    このルート要素がDB保存されるべきデータであることを表します。
                    Entity Framework Core のエンティティ定義や、作成・更新・削除のWeb API エンドポイントなどが生成されます。
                    切り分けの目安は、データベースの排他制御やトランザクションの粒度です。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && !el.Is.ContainsKey("generate-default-read-model")
                                             && el.Is.TryGetValue("write-model-2", out var isAttribute) ? isAttribute : null,
            };
            yield return new AggregateOrMemberTypeDef {
                Key = "r", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "ReadModel",
                HelpText = $$"""
                    このルート要素が人間が閲覧するデータであることを表します。
                    一覧検索画面、詳細画面、一括編集画面などが生成されます。
                    切り分けの目安は詳細画面1個の粒度です。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && el.Is.TryGetValue("read-model-2", out var isAttribute) ? isAttribute : null,
            };
            yield return new AggregateOrMemberTypeDef {
                Key = "wr", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "Write & Read",
                HelpText = $$"""
                    ReadModel から生成されるコードと WriteModel から生成されるコードの両方が生成されます。
                    画面のデータ項目とDBのデータ構造が寸分違わず完全に一致する場合にのみ使えます。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && el.Is.ContainsKey("generate-default-read-model")
                                             && el.Is.TryGetValue("write-model-2", out var isAttribute) ? isAttribute : null,
            };
            yield return new AggregateOrMemberTypeDef {
                Key = "e", // <= この値はTypeScript側でref-toの参照先として使用可能な集約の判定に使っているので変更時は注意
                DisplayName = "Enum",
                HelpText = $$"""
                    このルート要素が列挙体であることを表します。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && el.Is.TryGetValue("enum", out var isAttribute) ? isAttribute : null,
            };
            yield return new AggregateOrMemberTypeDef {
                Key = "c",
                DisplayName = "Command",
                HelpText = $$"""
                    このルート要素が、人間や外部システムが起動する処理のパラメータであることを表します。
                    この処理を実行するWebAPIエンドポイントや、パラメータを入力するためのダイアログのUIコンポーネントなどが生成されます。
                    """,
                FindMatchingIsAttribute = el => el.Depth == 0
                                             && el.Is.TryGetValue("command", out var isAttribute) ? isAttribute : null,
            };

            // ルート以外に設定できる種類
            // ※ ref-toと列挙体は集約定義に依存するのでクライアント側で計算する
            yield return new AggregateOrMemberTypeDef {
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
            };
            yield return new AggregateOrMemberTypeDef {
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
            };
            yield return new AggregateOrMemberTypeDef {
                Key = "variation",
                DisplayName = "Variation",
                HelpText = $$"""
                    親1件に対し、異なるデータ構造をもつ複数の子要素から1種類を選択するもの。
                    """,
                FindMatchingIsAttribute = el => el.Depth > 0
                                             && el.Is.TryGetValue("variation", out var isAttribute) ? isAttribute : null,
            };
            yield return new AggregateOrMemberTypeDef {
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
            };

            yield return new AggregateOrMemberTypeDef {
                Key = "step",
                DisplayName = "ステップ",
                RequiredNumberValue = true,
                HelpText = $$"""
                    ほぼChildと同じですが、ルート集約がCommandの場合、かつルート集約の直下にのみ設定可能です。
                    そのコマンドの子要素がステップの場合、コマンドのUIがウィザード形式になります。
                    """,
                FindMatchingIsAttribute = el => el.Depth > 0
                                             && el.Is.TryGetValue("step", out var isAttribute) ? isAttribute : null,
            };

            var resolver = MemberTypeResolver.Default();
            foreach (var (key, memberType) in resolver.EnumerateAll()) {
                yield return new AggregateOrMemberTypeDef {
                    Key = $"memberType:{key}",
                    DisplayName = memberType.GetUiDisplayName(),
                    HelpText = memberType.GetHelpText(),
                    FindMatchingIsAttribute = el => el.Depth > 0 && el.Is.TryGetValue(key, out var isAttribute) ? isAttribute : null,
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
            };

            yield return new OptionalAttributeDef {
                Key = OptionalAttributeDef.DB_NAME,
                DisplayName = "DB名",
                Type = E_OptionalAttributeType.String,
                HelpText = $$"""
                    データベースのテーブル名またはカラム名を明示的に指定したい場合に設定してください。
                    既定では物理名がそのままテーブル名やカラム名になります。
                    """,
            };

            yield return new OptionalAttributeDef {
                Key = OptionalAttributeDef.LATIN,
                DisplayName = "ラテン語名",
                Type = E_OptionalAttributeType.String,
                HelpText = $$"""
                    ラテン語名しか用いることができない部分の名称を明示的に指定したい場合に設定してください（ver-0.4.0.0000 時点ではURLを定義する処理のみが該当）。
                    既定では集約を表す一意な文字列から生成されたハッシュ値が用いられます。
                    """,
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
            };

            yield return new OptionalAttributeDef {
                Key = "required",
                DisplayName = "必須",
                Type = E_OptionalAttributeType.Boolean,
                HelpText = $$"""
                    必須項目であることを表します。
                    画面上に必須項目であることを示すUIがついたり、新規登録処理や更新処理での必須入力チェック処理が自動生成されます。
                    """,
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
            };

            yield return new OptionalAttributeDef {
                Key = "has-lifecycle",
                DisplayName = "独立ライフサイクル",
                Type = E_OptionalAttributeType.Boolean,
                HelpText = $$"""
                    ReadModelの中にあるChildのうち、そのChildが追加削除できるものであることを表します。
                    """,
            };

            yield return new OptionalAttributeDef {
                Key = "readonly",
                DisplayName = "読み取り専用集約",
                Type = E_OptionalAttributeType.Boolean,
                HelpText = $$"""
                    閲覧専用の集約であることを表します。新規作成画面などが生成されなくなります。
                    ReadModelのルート集約にのみ設定可能。
                    """,
            };

            // --------------------------------------------

            yield return new OptionalAttributeDef {
                Key = "hidden",
                DisplayName = "隠し項目",
                Type = E_OptionalAttributeType.Boolean,
                HelpText = $$"""
                    この項目が画面上で常に非表示になります。
                    """,
            };

            yield return new OptionalAttributeDef {
                Key = "wide",
                DisplayName = "Wide",
                Type = E_OptionalAttributeType.Boolean,
                HelpText = $$"""
                    VForm2上でこの項目のスペースが横幅いっぱい確保されます。
                    """,
            };

            yield return new OptionalAttributeDef {
                Key = "width",
                DisplayName = "横幅",
                Type = E_OptionalAttributeType.String,
                HelpText = $$"""
                    詳細画面における当該項目の横幅を変更できます。全角10文字の場合は "z10"、半角6文字の場合は "h6" など、zかhのあとに整数を続けてください。
                    """,
            };

            yield return new OptionalAttributeDef {
                Key = "single-view-ui",
                DisplayName = "詳細画面UI",
                Type = E_OptionalAttributeType.String,
                HelpText = $$"""
                    詳細画面におけるこの項目の入力フォームが、自動生成されるものではなくここで指定した名前のコンポーネントになります。
                    コンポーネントは自前で実装する必要があります。
                    """,
            };

            yield return new OptionalAttributeDef {
                Key = "search-condition-ui",
                DisplayName = "検索条件欄UI",
                Type = E_OptionalAttributeType.String,
                HelpText = $$"""
                    詳細画面におけるこの項目の入力フォームが、自動生成されるものではなくここで指定した名前のコンポーネントになります。
                    コンポーネントは自前で実装する必要があります。
                    """,
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
            };
        }


        /// <summary>
        /// XMLのノード1個の抽象。
        /// </summary>
        private class NijoXmlElement {

            public NijoXmlElement(XElement xElement) {
                _xElement = xElement;
            }
            private readonly XElement _xElement;

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
            public static NijoXmlElement FromAbstract(AggregateOrMember aggregateOrMember) {
                var csSafe = aggregateOrMember.DisplayName?.ToCSharpSafe() ?? string.Empty;
                var physicalName = aggregateOrMember.AttrValues
                    ?.SingleOrDefault(x => x.Key == OptionalAttributeDef.PHYSICAL_NAME)
                    ?.Value ?? csSafe;
                var el = new XElement(physicalName);

                if (aggregateOrMember.DisplayName != physicalName) {
                    el.SetAttributeValue(DISPLAY_NAME, aggregateOrMember.DisplayName);
                }

                var dbName = aggregateOrMember.AttrValues
                    ?.SingleOrDefault(x => x.Key == OptionalAttributeDef.DB_NAME)
                    ?.Value;
                if (!string.IsNullOrWhiteSpace(dbName)) {
                    el.SetAttributeValue(DB_NAME, dbName);
                }

                var latinName = aggregateOrMember.AttrValues
                    ?.SingleOrDefault(x => x.Key == OptionalAttributeDef.LATIN)
                    ?.Value;
                if (!string.IsNullOrWhiteSpace(latinName)) {
                    el.SetAttributeValue(LATIN, latinName);
                }

                var isAttrs = aggregateOrMember.AttrValues
                    ?.Where(attr => attr.Key != DISPLAY_NAME
                                 && attr.Key != DB_NAME
                                 && attr.Key != LATIN)
                    .Select(attr => {
                        if (string.IsNullOrWhiteSpace(attr.Key)) {
                            return string.Empty;
                        } else if (string.IsNullOrWhiteSpace(attr.Value)) {
                            return attr.Key;
                        } else {
                            return $"{attr.Key.Trim()}:{attr.Value.Trim()}";
                        }
                    })
                    .Join(" ");
                if (!string.IsNullOrWhiteSpace(isAttrs)) {
                    el.SetAttributeValue(IS, isAttrs);
                }

                return new NijoXmlElement(el);
            }
            /// <summary>
            /// XML要素を nijo ui の画面上で編集されるデータに変換する
            /// </summary>
            public AggregateOrMember ToAbstract(
                IEnumerable<AggregateOrMemberTypeDef> typeDefs,
                IReadOnlyDictionary<string, OptionalAttributeDef> optionDefs) {

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
                        type = $"ref-to:{GetXElementUniqueId(refToElement)}";
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
                        type = $"enum:{GetXElementUniqueId(matchedEnum)}";
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
                var comment = _xElement.PreviousNode is XComment xComment
                    ? xComment.Value
                    : string.Empty;

                var children = _xElement
                    .Elements()
                    .Where(el => el.NodeType != System.Xml.XmlNodeType.Comment)
                    .Select(el => new NijoXmlElement(el).ToAbstract(typeDefs, optionDefs));

                return new AggregateOrMember {
                    UniqueId = GetXElementUniqueId(_xElement),
                    DisplayName = displayName,
                    Type = type,
                    TypeDetail = typeDetail,
                    AttrValues = attrs,
                    Comment = comment,
                    Children = children.ToList(),
                };
            }

            private static string GetXElementUniqueId(XElement xElement) {
                return xElement.GetHashCode().ToString().ToHashedString();
            }
            #endregion nijo ui の画面表示用データとの変換

            public const string IS = "is";
            public const string DISPLAY_NAME = "DisplayName";
            public const string DB_NAME = "DbName";
            public const string LATIN = "Latin";
        }
    }
}
