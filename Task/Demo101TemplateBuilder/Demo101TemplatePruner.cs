using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Demo101TemplateBuilder;

/// <summary>
/// <para>
/// /demo/101_販売管理システム （デモ101）から、新規プロジェクトテンプレート（demo101-template）を作るために、
/// 販売管理業務固有の部分（商品・入荷・売上・在庫調整など）を削除する処理。
/// </para>
/// <para>
/// デモ101の nijo.xml やソースコードに機能追加・構造変更があった場合、このクラスの更新が必要になる可能性がある。
/// そのため <c>Nijo.IntegrationTest</c> にこのクラスを実行したうえで dotnet build / npm run tsc が
/// 通ることを確認するテストを用意している。
/// </para>
/// </summary>
public static class Demo101TemplatePruner {

    /// <summary>DataStructures直下の要素でUniqueIdがこれに合致するものを削除する</summary>
    private static readonly HashSet<string> REMOVE_DATA_STRUCTURE_IDS = [
        "a1b2c3d4-e5f6-4a1b-8c2d-3e4f5a6b7c8d", // 商品
        "e5f6a1b2-c3d4-4e5f-2a6b-7c8d9e0f1a2b", // 入荷
        "f6a1b2c3-d4e5-4f2a-9b3c-4d5e6f7a8b9c", // 売上
        "bf2453f1-2a89-4209-ab79-00d526fd15a9", // 入荷詳細
        "a5e5b615-6cc1-4a50-8cc7-fe910dca543b", // 入荷明細
        "d0929a1e-1490-42c3-a728-d740c85ff7b7", // 売上一覧
        "2352e942-65b1-47fe-ac3b-0c49bd4e295f", // 売上詳細
        "0d252a96-0dd0-45eb-af33-e761419117c7", // 売上詳細画面初期表示Parameter
        "ca790c30-db44-4439-9452-2a1310a901bb", // 入荷一覧
        "cf33b04a-b6a2-4559-b98c-563ddcc76394", // 商品一覧
        "2555dc15-3ed7-48cb-9e78-0e7b09de86a3", // 在庫調整Parameter
        "4a1fa98c-1503-4d77-8161-e0e2f5f2fd1d", // 在庫調整
        "9604df00-d30a-4033-affd-6d2d625558fa", // 商品在庫増減履歴
        "c766195e-c279-4a50-84bb-fcbf46ac9798", // 入荷詳細画面初期表示Parameter
        "7c3fe519-2692-4465-b151-9884cc73cdb7", // 売上新規登録ReturnValue
        "da3c0330-ac57-43ce-8cba-212da1053aab", // 入荷登録ReturnValue
    ];

    /// <summary>Commands直下の要素でUniqueIdがこれに合致するものを削除する</summary>
    private static readonly HashSet<string> REMOVE_COMMAND_IDS = [
        "c2a6e537-f471-4512-ae2b-d4a38017604e", // 入荷登録
        "6cb9f1e7-afb0-4197-be66-d9779315026c", // 商品データ取込
        "e4e427ae-7a2d-468b-864e-6536dbb0a47a", // 売上新規登録
        "590f0ad0-92c9-4281-82f7-6f8c0d04de1d", // 売上詳細画面初期表示
        "271203a5-7b54-449b-a443-92019f7f1035", // 売上修正
        "6eae3b9e-64ab-49a6-bb26-1738a31561c7", // 入荷修正
        "23aacfdc-c4d5-499f-891e-999542dc5258", // 在庫調整登録
        "d8cff664-d1a8-43c8-ac5e-cc1b2a8fb400", // 入荷詳細画面初期表示
        "00556384-1f6d-4596-89d6-2cd1a9094ea5", // 売上金額シミュレート
    ];

    /// <summary>StaticEnums直下の要素でUniqueIdがこれに合致するものを削除する</summary>
    private static readonly HashSet<string> REMOVE_ENUM_IDS = [
        "e4a132f9-580e-477f-abf3-9359c92a643a", // 消費税区分
        "724ac342-ebd2-4a0b-bb75-5bd174cd5f60", // 売上明細区分
    ];

    /// <summary>
    /// CustomAttributes直下の要素（タグ名 "Custom-&lt;UniqueId&gt;"）でこれに合致するものを削除する。
    /// "金額項目"(IsCurrency)・"数量"(IsQuantity) はどの項目にも使用されなくなるが、
    /// 汎用的な書式指定として今後利用者が自分の項目に付与できるように定義自体は残す
    /// （削除すると client/src/app/fieldMetadata.ts が参照する生成後の型からプロパティが消え、コンパイルエラーになる）。
    /// </summary>
    private static readonly HashSet<string> REMOVE_CUSTOM_ATTRIBUTE_IDS = [
        "0f2d701d-f481-42d4-8dfb-5296c1b40246", // 0以上のみ
    ];

    /// <summary>業務固有のフォルダ・ファイル。テンプレートには不要なので削除する。</summary>
    private static readonly string[] DELETE_PATHS = [
        "Core/入荷",
        "Core/商品",
        "Core/在庫調整",
        "Core/売上",
        "Core/外部システム/商品管理システム",
        "client/src/pages/P100_売上.tsx",
        "client/src/pages/P101_売上詳細.tsx",
        "client/src/pages/P200_入荷.tsx",
        "client/src/pages/P201_入荷詳細.tsx",
        "client/src/pages/P300_商品.tsx",
        "client/src/pages/P301_商品詳細.tsx",
        "client/src/pages/shared/StockAdjustmentDialog.tsx",
        // ER図デバッグ画面は @nijo/ui-components （このモノレポ内でのみ解決できるパッケージ）に依存しており、
        // 単体で展開されるテンプレートではビルドできないため削除する。
        "client/src/debug-rooms/ER図.tsx",
        "WebApi/Debugging/ERDiagramController.cs",
        // テンプレートには列挙体を1つも含まないため、列挙体の存在を前提にしたこれらのコンポーネントは型エラーになる。
        // UIコンポーネントカタログでの参照も削除済みで他に利用箇所がないため削除する。
        "client/src/app/EnumSelection.tsx",
        "client/src/app/EnumSearchCondition.tsx",
    ];

    /// <summary>
    /// <paramref name="workDir"/> に展開されたデモ101のソース一式から、
    /// 販売管理業務固有の部分を削除する。
    /// </summary>
    public static void Prune(string workDir) {
        PruneNijoXml(Path.Combine(workDir, "nijo.xml"));
        DeleteUnneededPaths(workDir);
        EditSourceReferences(workDir);
    }

    // ============================================================
    // 1. nijo.xml から販売管理業務固有の定義を削除する
    // ============================================================

    private static void PruneNijoXml(string xmlPath) {
        var doc = XDocument.Load(xmlPath);
        var root = doc.Root ?? throw new InvalidOperationException($"ルート要素が見つかりません: {xmlPath}");

        RemoveChildrenByUniqueId(root.Element("DataStructures"), REMOVE_DATA_STRUCTURE_IDS);
        RemoveChildrenByUniqueId(root.Element("Commands"), REMOVE_COMMAND_IDS);
        RemoveChildrenByUniqueId(root.Element("StaticEnums"), REMOVE_ENUM_IDS);

        var customAttributes = root.Element("CustomAttributes");
        if (customAttributes != null) {
            foreach (var el in customAttributes.Elements().ToArray()) {
                const string PREFIX = "Custom-";
                if (!el.Name.LocalName.StartsWith(PREFIX)) continue;
                var id = el.Name.LocalName[PREFIX.Length..];
                if (REMOVE_CUSTOM_ATTRIBUTE_IDS.Contains(id)) {
                    RemoveWithPrecedingComment(el);
                }
            }
        }

        using var writer = XmlWriter.Create(xmlPath, new XmlWriterSettings {
            Indent = true,
            Encoding = new UTF8Encoding(false),
        });
        doc.Save(writer);
    }

    private static void RemoveChildrenByUniqueId(XElement? section, HashSet<string> ids) {
        if (section == null) return;
        foreach (var el in section.Elements().ToArray()) {
            var uniqueId = (string?)el.Attribute("UniqueId");
            if (uniqueId != null && ids.Contains(uniqueId)) {
                RemoveWithPrecedingComment(el);
            }
        }
    }

    /// <summary>
    /// 要素を削除する。その要素の説明になっている直前のXMLコメントがあれば、それもあわせて削除する。
    /// </summary>
    private static void RemoveWithPrecedingComment(XElement el) {
        if (el.PreviousNode is XComment comment) {
            comment.Remove();
        }
        el.Remove();
    }

    // ============================================================
    // 2. 業務固有のフォルダ・ファイルを削除する
    // ============================================================

    private static void DeleteUnneededPaths(string workDir) {
        foreach (var relativePath in DELETE_PATHS) {
            var target = Path.Combine(workDir, relativePath);
            if (Directory.Exists(target)) {
                Directory.Delete(target, recursive: true);
            } else if (File.Exists(target)) {
                File.Delete(target);
            } else {
                throw new InvalidOperationException($"削除対象が見つかりません（デモ101の構造が変わった可能性があります）: {relativePath}");
            }
        }
    }

    // ============================================================
    // 3. 業務固有のコードへの参照が残るファイルを編集する
    // ============================================================

    private static void EditSourceReferences(string workDir) {
        EditCoreConfigureServices(workDir);
        EditCoreOverridedApplicationService(workDir);
        EditCoreRuntimeSetting(workDir);
        EditCoreOverridedDbContext(workDir);
        ReplaceOverridedDummyDataGenerator(workDir);
        EditClientRoutes(workDir);
        EditClientUiComponentCatalog(workDir);
        EditClientDebugMenu(workDir);
        ReplaceClientPackageJson(workDir);
    }

    /// <summary>
    /// ファイル内の <paramref name="oldText"/> を <paramref name="newText"/> に置換する。
    /// 見つからない場合（＝デモ101側の構造が変わり、この置換内容が古くなった場合）は例外を送出する。
    /// </summary>
    private static void ReplaceExactlyOnce(string filePath, string oldText, string newText) {
        // ファイルの改行コード（CRLF/LF）は .gitattributes によりファイル種別ごとに異なるため、
        // 比較・置換は LF に正規化した状態で行い、書き戻す際に元の改行コードへ戻す。
        var original = File.ReadAllText(filePath);
        var usesCrlf = original.Contains('\r');
        var text = original.Replace("\r\n", "\n");
        var old = oldText.Replace("\r\n", "\n");
        var replacement = newText.Replace("\r\n", "\n");

        var firstIndex = text.IndexOf(old, StringComparison.Ordinal);
        if (firstIndex < 0) {
            throw new InvalidOperationException($"置換対象の文字列が見つかりませんでした: {filePath}\n---\n{old}\n---");
        }
        if (text.IndexOf(old, firstIndex + old.Length, StringComparison.Ordinal) >= 0) {
            throw new InvalidOperationException($"置換対象の文字列が複数箇所で見つかりました（一意に特定できません）: {filePath}");
        }

        var result = text.Replace(old, replacement);
        if (usesCrlf) result = result.Replace("\n", "\r\n");
        File.WriteAllText(filePath, result, new UTF8Encoding(false));
    }

    private static void EditCoreConfigureServices(string workDir) {
        var path = Path.Combine(workDir, "Core/ConfigureServices.cs");

        ReplaceExactlyOnce(
            path,
            """
            using Microsoft.Extensions.DependencyInjection;
            using MyApp.Core.外部システム.商品管理システム;
            using NLog.Extensions.Logging;
            """,
            """
            using Microsoft.Extensions.DependencyInjection;
            using NLog.Extensions.Logging;
            """);

        ReplaceExactlyOnce(
            path,
            """
                    // ログ設定
                    LogSettings.ConfigureServices(services, myAppSection, basePath);

                    // 商品管理システムの設定をバインド。
                    // appsettings.json の設定に従い、モック/実際の外部システムクラスを切り替える。
                    if (myAppSection.GetValue<bool>($"{nameof(RuntimeSetting.商品管理システム)}:{nameof(商品管理システムSettings.UseMock)}")) {
                        services.AddTransient<I商品管理システム, 商品管理システムMock>();
                    } else {
                        services.AddTransient<I商品管理システム, 商品管理システム本番>();
                    }
                }
            """,
            """
                    // ログ設定
                    LogSettings.ConfigureServices(services, myAppSection, basePath);
                }
            """);
    }

    private static void EditCoreOverridedApplicationService(string workDir) {
        var path = Path.Combine(workDir, "Core/OverridedApplicationService.cs");

        ReplaceExactlyOnce(
            path,
            """
            using Microsoft.Extensions.DependencyInjection;
            using MyApp.Core.外部システム;
            using MyApp.Core.外部システム.商品管理システム;
            using System;
            using System.Reflection;
            using System.Text.Json;
            using System.Threading.Tasks;
            """,
            """
            using Microsoft.Extensions.DependencyInjection;
            using MyApp.Core.外部システム;
            using System;
            using System.Text.Json;
            using System.Threading.Tasks;
            """);

        ReplaceExactlyOnce(
            path,
            """
                /// <summary>
                /// 商品管理システムインターフェース。
                /// </summary>
                public I商品管理システム 商品管理システム => _cached商品管理システム ??= ServiceProvider.GetRequiredService<I商品管理システム>();
                private I商品管理システム? _cached商品管理システム;

                /// <summary>
                /// 外部リソース更新用のトランザクションを開始する。
                /// </summary>
            """,
            """
                /// <summary>
                /// 外部リソース更新用のトランザクションを開始する。
                /// </summary>
            """);

        // "0以上のみ" カスタム属性（NotNegativeバリデーション）はテンプレートには含めないため、
        // それに対応するオーバーライドメソッドも不要になる。
        ReplaceExactlyOnce(
            path,
            """

                public override string? ValidateNotNegative(decimal? value, PropertyInfo propertyInfo) {
                    if (value.HasValue && value.Value < 0m) {
                        return "負の値は許可されていません。";
                    }
                    return null;
                }
            }
            """,
            """
            }
            """);
    }

    private static void EditCoreOverridedDbContext(string workDir) {
        var path = Path.Combine(workDir, "Core/OverridedDbContext.cs");

        // "sequence" 型の項目（商品SEQ・売上SEQ）はテンプレートには含めないため、
        // それに対応するオーバーライドメソッドも不要になる。
        ReplaceExactlyOnce(
            path,
            """

                protected override void ConfigureSequenceMember(
                    Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder,
                    Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder entity,
                    Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<int?> property,
                    string sequenceName) {

                    // SQLite の場合（SQLiteにはシーケンスがないため、AUTO_INCREMENTを使用）
                    property.HasAnnotation("Sqlite:Autoincrement", true);
                }
            }
            """,
            """
            }
            """);
    }

    private static void EditCoreRuntimeSetting(string workDir) {
        var path = Path.Combine(workDir, "Core/RuntimeSetting.cs");

        ReplaceExactlyOnce(
            path,
            """

                #region 外部システム連携設定
                /// <summary>
                /// 商品管理システム連携設定
                /// </summary>
                public Core.外部システム.商品管理システム.商品管理システムSettings 商品管理システム { get; set; } = new();
                #endregion 外部システム連携設定
            }
            """,
            """

            }
            """);
    }

    private static void ReplaceOverridedDummyDataGenerator(string workDir) {
        var path = Path.Combine(workDir, "Core/OverridedDummyDataGenerator.cs");
        if (!File.Exists(path)) {
            throw new InvalidOperationException($"ファイルが見つかりません（デモ101の構造が変わった可能性があります）: {path}");
        }

        var content = """
            #if DEBUG

            namespace MyApp;

            /// <summary>
            /// デバッグ用のダミーデータ生成。
            /// DbContext を直接使ってマスタデータを登録します。
            /// </summary>
            public class OverridedDummyDataGenerator {

                public OverridedDummyDataGenerator(Func<IMessageSetter, IPresentationContext<MessageSetter>> createPresentationContext) {
                    _createPresentationContext = createPresentationContext;
                }
                protected readonly Func<IMessageSetter, IPresentationContext<MessageSetter>> _createPresentationContext;

                public const string ADMIN_USER_ID = "admin";
                public const string DUMMY_USER_PASSWORD = "password123";

                public virtual async Task<IMessageSetter> GenerateAsync(AutoGeneratedApplicationService applicationService) {
                    var db = applicationService.DbContext;

                    // 従業員: 管理者
                    var adminSalt = OverridedApplicationService.GenerateSalt();
                    db.従業員DbSet.Add(new 従業員DbEntity {
                        従業員番号 = ADMIN_USER_ID,
                        氏名 = "デモ用ユーザー",
                        パスワード = OverridedApplicationService.ComputeHash(DUMMY_USER_PASSWORD, adminSalt),
                        SALT = adminSalt,
                        入荷担当 = true,
                        販売担当 = true,
                        システム管理者 = true,
                        Version = 0,
                    });
                    // 従業員: その他
                    for (var i = 1; i <= 10; i++) {
                        var s = OverridedApplicationService.GenerateSalt();
                        db.従業員DbSet.Add(new 従業員DbEntity {
                            従業員番号 = $"user{i:000}",
                            氏名 = $"ユーザー{i:000}",
                            パスワード = OverridedApplicationService.ComputeHash(DUMMY_USER_PASSWORD, s),
                            SALT = s,
                            入荷担当 = i % 2 == 0,
                            販売担当 = i % 3 == 0,
                            システム管理者 = false,
                            Version = 0,
                        });
                    }
                    await db.SaveChangesAsync();

                    return new MessageSetter([], new());
                }
            }

            #endif

            """;
        // *.cs は .gitattributes で eol=crlf のため、改行コードを合わせる
        File.WriteAllText(path, content.Replace("\r\n", "\n").Replace("\n", "\r\n"), new UTF8Encoding(false));
    }

    private static void EditClientRoutes(string workDir) {
        var path = Path.Combine(workDir, "client/src/routes.tsx");

        ReplaceExactlyOnce(
            path,
            """
            import P000, * as P000Module from "./pages/P000_トップページ"
            import P002, * as P002Module from "./pages/P002_ログアウト"
            import P100, * as P100Module from "./pages/P100_売上"
            import P200, * as P200Module from "./pages/P200_入荷"
            import P300, * as P300Module from "./pages/P300_商品"
            import P400, * as P400Module from "./pages/P400_従業員"
            import P101 from "./pages/P101_売上詳細"
            import P201 from "./pages/P201_入荷詳細"
            import P301 from "./pages/P301_商品詳細"
            import UIComponentCatalog from "./debug-rooms/UIコンポーネントカタログ"
            import ER図 from "./debug-rooms/ER図"
            """,
            """
            import P000, * as P000Module from "./pages/P000_トップページ"
            import P002, * as P002Module from "./pages/P002_ログアウト"
            import P400, * as P400Module from "./pages/P400_従業員"
            import UIComponentCatalog from "./debug-rooms/UIコンポーネントカタログ"
            """);

        // ルートナビゲーションに表示する業務画面は routes.tsx が合成する（RootLayout は props で受け取るだけ）。
        // 売上・入荷・商品はテンプレートに含めないため、ナビゲーション項目からも除く。
        ReplaceExactlyOnce(
            path,
            """
            const navigationItems = [
              { to: P100Module.URL, label: "売上", icon: Icon.CurrencyYenIcon },
              { to: P200Module.URL, label: "入荷", icon: Icon.TruckIcon },
              { to: P300Module.URL, label: "商品", icon: Icon.CubeIcon },
              { to: P400Module.URL, label: "従業員", icon: Icon.UserGroupIcon },
            ]
            """,
            """
            const navigationItems = [
              { to: P400Module.URL, label: "従業員", icon: Icon.UserGroupIcon },
            ]
            """);

        ReplaceExactlyOnce(
            path,
            """
                    UIComponentCatalog,
                    ER図,
                  ]),
            """,
            """
                    UIComponentCatalog,
                  ]),
            """);

        ReplaceExactlyOnce(
            path,
            """
                  // 業務画面
                  P000,
                  P002,
                  P100,
                  P200,
                  P300,
                  P400,
                  ...P101,
                  ...P201,
                  P301,
            """,
            """
                  // 業務画面
                  P000,
                  P002,
                  P400,
            """);
    }

    private static void EditClientUiComponentCatalog(string workDir) {
        var path = Path.Combine(workDir, "client/src/debug-rooms/UIコンポーネントカタログ.tsx");

        ReplaceExactlyOnce(
            path,
            """
            import { EnumSelection } from "../app/EnumSelection"
            import { EnumSearchCondition } from "../app/EnumSearchCondition"
            import { NumericTextBox } from "../ui/NumericTextBox"
            import { WordTextBox } from "../ui/WordTextBox"
            import { Button } from "../ui/Button"
            import { NowLoading } from "../ui/NowLoading"
            import * as EnumDefs from "../__autoGenerated/enum-defs"
            """,
            """
            import { NumericTextBox } from "../ui/NumericTextBox"
            import { WordTextBox } from "../ui/WordTextBox"
            import { Button } from "../ui/Button"
            import { NowLoading } from "../ui/NowLoading"
            """);

        ReplaceExactlyOnce(
            path,
            """
                descValue: string
                enumValue: EnumDefs.売上明細区分 | null
                numValue: string
                wordValue: string
                enumSearchConditionValue: EnumDefs.消費税区分SearchCondition
              }
            """,
            """
                descValue: string
                numValue: string
                wordValue: string
              }
            """);

        ReplaceExactlyOnce(
            path,
            """
                  descValue: "",
                  enumValue: null,
                  numValue: "",
                  wordValue: "",
                  enumSearchConditionValue: {},
                }
            """,
            """
                  descValue: "",
                  numValue: "",
                  wordValue: "",
                }
            """);

        ReplaceExactlyOnce(
            path,
            """
                          {/* EnumSelection */}
                          <div className="space-y-2">
                            <h3 className="font-bold">EnumSelection</h3>
                            <div className="p-4 border rounded">
                              <FormLabel>売上明細区分</FormLabel>
                              <Controller
                                control={control}
                                name="enumValue"
                                render={({ field }) => (
                                  <EnumSelection
                                    type="売上明細区分"
                                    {...field}
                                  />
                                )}
                              />
                              <div className="text-sm text-gray-500 mt-2">Value: {values.enumValue}</div>
                            </div>
                          </div>

                          {/* EnumSearchCondition */}
                          <div className="space-y-2">
                            <h3 className="font-bold">EnumSearchCondition</h3>
                            <div className="p-4 border rounded">
                              <FormLabel>消費税区分</FormLabel>
                              <EnumSearchCondition
                                control={control}
                                name="enumSearchConditionValue"
                                type="消費税区分"
                              />
                              <div className="text-sm text-gray-500 mt-2">Value: {JSON.stringify(values.enumSearchConditionValue)}</div>
                            </div>
                          </div>

                          {/* NumericTextBox */}
            """,
            """
                          {/* NumericTextBox */}
            """);
    }

    private static void EditClientDebugMenu(string workDir) {
        var path = Path.Combine(workDir, "client/src/debug-rooms/デバッグメニュー.tsx");

        ReplaceExactlyOnce(
            path,
            """
            import * as UIコンポーネントカタログ from "./UIコンポーネントカタログ"
            import * as ER図 from "./ER図"
            """,
            """
            import * as UIコンポーネントカタログ from "./UIコンポーネントカタログ"
            """);

        ReplaceExactlyOnce(
            path,
            """
                    <Link to={UIコンポーネントカタログ.URL} className="text-blue-600 underline">
                      UIコンポーネントカタログへ移動
                    </Link>
                    <Link to={ER図.URL} className="text-blue-600 underline">
                      ER図へ移動
                    </Link>
            """,
            """
                    <Link to={UIコンポーネントカタログ.URL} className="text-blue-600 underline">
                      UIコンポーネントカタログへ移動
                    </Link>
            """);
    }

    /// <summary>
    /// デモ101の client/package.json は、このモノレポの npm workspaces の一員として
    /// ルートの node_modules に依存関係をホイスティングしているため依存関係の記載がない。
    /// テンプレートとして単体で展開されたときに動作するよう、実際に使用しているパッケージを明記する。
    /// バージョンはこのモノレポのルート package.json に合わせている。
    /// </summary>
    private static void ReplaceClientPackageJson(string workDir) {
        var path = Path.Combine(workDir, "client/package.json");

        var content = """
            {
              "name": "my-app",
              "private": true,
              "version": "0.0.0",
              "type": "module",
              "scripts": {
                "dev": "vite --port 5173 --host",
                "build": "tsc && vite build",
                "preview": "vite preview",
                "tsc": "tsc --noEmit",
                "test": "vitest",
                "test:run": "vitest run"
              },
              "dependencies": {
                "@heroicons/react": "^2.2.0",
                "allotment": "^1.20.5",
                "react": "^19.2.7",
                "react-dom": "^19.2.7",
                "react-hook-form": "^7.79.0",
                "react-router-dom": "^7.17.0",
                "react-use-event-hook": "^0.9.6",
                "uuidjs": "^5.1.0"
              },
              "devDependencies": {
                "@tailwindcss/vite": "^4.3.1",
                "@types/react": "^19.2.17",
                "@types/react-dom": "^19.2.3",
                "@vitejs/plugin-react-swc": "^3.11.0",
                "tailwindcss": "^4.3.1",
                "typescript": "~5.9.3",
                "vite": "^7.3.5",
                "vite-plugin-singlefile": "^2.3.3",
                "vitest": "^3.2.6"
              }
            }

            """;
        File.WriteAllText(path, content, new UTF8Encoding(false));

        // package.json の内容が変わるため、モノレポ内で使われていた古い package-lock.json は破棄する。
        // 新規プロジェクトを開いた後、最初の npm install で作り直される。
        var lockPath = Path.Combine(workDir, "client/package-lock.json");
        if (File.Exists(lockPath)) File.Delete(lockPath);
    }
}
