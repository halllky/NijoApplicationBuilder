using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.DataModelModules;
using Nijo.Parts.Common;
using Nijo.Parts.CSharp;
using Nijo.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Models {

    /// <summary>
    /// データモデル。
    /// アプリケーションに永続化されるデータの形を表す。
    /// トランザクションの境界の単位（より強い整合性の範囲）で区切られる。
    /// データモデルの境界を跨ぐエラーチェックは、一時的に整合性が崩れる可能性がある。
    /// DDD（ドメイン駆動設計）における集約ルートの概念とほぼ同じ。
    /// </summary>
    internal class DataModel : IModel {
        public string SchemaName => "data-model";

        public string HelpTextMarkdown => $$"""
            # DataModel データモデル
            アプリケーションに永続化されるデータの形を表す。
            トランザクションの境界の単位（より強い整合性の範囲）で区切られる。
            データモデルの境界を跨ぐエラーチェックは、一時的に整合性が崩れる可能性がある。
            DDD（ドメイン駆動設計）における集約ルートの概念とほぼ同じ。

            DataModelの集約1件からは以下のモジュールが生成される。

            ## Entity Framework Core のエンティティ定義と関連定義
            C#標準のO/R MapperであるEntity Framework Coreのエンティティ定義と、
            主キーや外部キーなどのメタデータの定義が生成される。

            エンティティクラスには以下の項目が含まれる。

            - スキーマ定義で既定された項目
            - 作成時刻, 更新時刻: データの作成日時, 更新日時。
            - 作成者, 更新者: ユーザーを識別する情報。ユーザーテーブル等への外部キーは無い。単なる文字情報。
            - 楽観排他制御用のバージョン: 集約ルートのみ。32ビット整数。

            また、Entity Framework Core を用いた開発が容易になるよう、以下のプロパティや処理も生成される。

            - DbContext の DbSet プロパティ
            - ナビゲーションプロパティ
            - DbContext の OnModelCreating の処理
                - どの項目が主キーかの設定(PRIMARY KEY 制約)
                - どの項目が必須かの設定（NOT NULL 制約）
                - どの項目が外部キーかの設定（FOREIGN KEY 制約）
                - 文字項目の最大長の設定
                - 数値の桁数や小数点以下の桁数の設定

            以下は自動生成されたあとのソースコードで指定する。（スキーマ定義で指定できるようにしてもよいが優先度が低いため予定未定）

            - インデックスの設定
            - ユニーク制約の設定
            - デフォルト値の設定

            ## 新規登録、更新、物理削除処理
            アプリケーションサービスのメソッドとして、新規登録、更新、削除メソッドが生成される。

            ### 全体的な挙動
            - 更新の粒度
                - **更新の範囲は必ずDataModelの集約の範囲となる。**
                    - つまり、ルート集約のテーブルがChildやChildren（子テーブルや明細テーブル）を持つ場合、必ずこれらの複数のテーブルはまとめて更新される。
                    - 楽観排他制御はルート集約のテーブルに対してのみ検査される。
                - 更新の際は必ずテーブル全体が更新の対象となる。そのテーブルの特定のカラムのみUPDATEというSQLを発行することはない。
                    - 整合性のチェックは必ず集約全体の単位で行なわれるため。
                    - Childrenについては、1件ごとに更新前の状態とのディープイコールによる比較が行なわれる。最終更新時刻が更新されるのもディープイコールで差分があったもののみ。
            - トランザクション
                - **トランザクションの開始と終了はこれらのメソッドには含まれない。** このメソッドを呼び出す側で行なうこと。
                - メソッドの内部で何らかのエラーが発生した場合、トランザクション全体のロールバックは行われない。
                - そのかわり、メソッドの内部で SAVEPOINT が発行され、SAVEPOINT へのロールバックは行われる。このSAVEPOINTはこの集約の更新成功時にリリースされる。
            - 例外
                - メソッドの内部で何らかのエラーが発生した場合、 **例外は送出されない。**
                - そのかわり、どの項目で何のエラーが起きたかの詳細な情報を持ったオブジェクトが返される。

            ### 処理概要
            - 更新または物理削除の場合、このメソッド内部で、更新対象をデータベースから読みだす処理が行なわれる。更新対象が見つからなかった場合はエラーとなる。
            - エラーチェック
                - 新規登録または更新の場合、スキーマ定義で表現される制約の検査がこのメソッドの中で自動的に行われる。
                    - 必須入力チェック
                    - 文字列の最大長チェック
                    - 文字種チェック（半角英数字か否かの検査など）
                    - 桁数チェック（整数の桁数、小数の桁数）
                    - 動的列挙体の種別が不正でないかの検査
                - 更新または物理削除の場合、楽観排他制御が自動的に検査される。
                    - EFCoreの `ConcurrencyCheck` を用いて確認される。（`UPDATE ... WHERE キー項目一致 AND Version = 指定されたバージョン` の結果が1件か0件かで判定）
                    - ただし、更新に使用する実際のバージョン値はこのメソッドの引数としてわたってきたものが使われる。そのため、どのバージョンをこのメソッドの引数として渡すかはよく注意すること。
                    - 特に、画面からの更新の場合、その画面を初期表示したときのバージョン値を渡さないと、実質的な楽観排他の意味が無くなってしまうので注意。
                - 上記以外に、任意のチェック処理を差しはさむことができる。
                    - アプリケーションサービスの `OnBeforeCreate` 等3メソッドをオーバーライドして実装すること。
                    - ここで行なう検査は、どのユースケースから更新がかかる場合であっても必ず守られなければならないルールを実装する。
                    - 特定のユースケースの場合のみ行なわれる検査は、ここではなく、このメソッドを呼び出す側（主に `CommandModel` で生成されるメソッド）で行なう。
            - エラーチェックのみの場合、SaveChangesが実行される前に処理が中断される。
                - 人間による画面操作での更新の場合に「エラーチェック → 更新を確定するかの確認 → 再度エラーチェック → 更新確定(SaveChanges)」という流れとするため。
                - このメソッドに渡される引数のオプションの `IgnoreConfirm` がtrueか否かで判定している。
            - UUIDやシーケンスの発番はこのメソッドの内部で行なわれる。
            - データの更新者・更新日時等のメタデータはこのメソッドの内部で自動的に設定される。
            - 更新の確定は EFCore の SaveChanges メソッドを呼び出すことで行われる。
                - そのため、開発者が記述する DbContext 操作で何らかのクエリを発行する際は `.AsNoTracking()` を指定すること。
                - AsNoTracking が自動的にオフになるようにDbContextを構成しておくことを強く推奨する。
            - 更新後処理
                - この集約の更新をメッセージング基盤やリードレプリカへ反映するための処理を差しはさむことができる。
                - アプリケーションサービスの `OnAfterCreateAsync` 等のメソッドをオーバーライドして実装すること。
                - 更新後処理の内部で例外が発生した場合、SAVEPOINT へのロールバックが行われる。

            ## ダミーデータ作成処理
            デバッグ用のダミーデータ作成処理の雛形が生成される。
            自動生成されたあとのソースで `DummyDataGenerator` またはそれを継承したクラスの `GenerateAsync` メソッドを呼ぶと実行される。
            ダミーデータの作成はスキーマ定義の `ref-to` の依存関係の順番で行なわれるため、開発者はどの順番でダミーデータを作成するかを意識する必要はない。

            `DummyDataGenerator` が作成するのはEFCoreのエンティティの配列までのため、
            そのエンティティを使って実際にどの媒体に保存するかは開発者が実装する必要がある。
            （そのままDbContextを使ってINSERTしてもよし、プロパティの値をExcel等に書き出して何かするもよし）

            `DummyDataGenerator` を継承したクラスでは以下をカスタマイズできる。
            - 集約毎のパターン。「この集約は100件欲しい」「この集約はこのステータスのパターンを掛け合わせた組み合わせテストのパターンが欲しい」など
            - 集約毎のインスタンス1件作成処理。
            - 型ごとの標準ダミー値の生成ロジック。「日付型の項目は現在時刻±1年の中からランダムな値としたい」など

            ## メタデータ
            スキーマ定義で指定された、項目ごとの文字列長や桁数や必須か否かなどの情報自体をプロジェクトで使用したい場合に使う。
            ただし、基本的なエラーチェックは、前述のバリデーションエラーチェックで行なわれるため、ほぼ使用することはないはず。
            自動生成されたあとのソースで `Metadata` クラスを使用することで利用できる。
            """;

        public void Validate(XElement rootAggregateElement, SchemaParseContext context, Action<XElement, string> addError) {
            // ルートとChildrenはキー必須
            var rootAndChildren = rootAggregateElement
                .DescendantsAndSelf()
                .Where(el => el.Parent == el.Document?.Root
                          || el.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value == SchemaParseContext.NODE_TYPE_CHILDREN);
            foreach (var el in rootAndChildren) {
                if (el.Elements().All(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) == null)) {
                    addError(el, "キーが指定されていません。");
                }
            }
        }

        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {
            var aggregateFile = new SourceFileByAggregate(rootAggregate);

            // データ型: EFCore Entity
            var efCoreEntity = new EFCoreEntity(rootAggregate);
            aggregateFile.AddCSharpClass(EFCoreEntity.RenderClassDeclaring(efCoreEntity, ctx));
            ctx.Use<DbContextClass>().AddEntities(efCoreEntity.EnumerateThisAndDescendants());

            // データ型: SaveCommand
            aggregateFile.AddCSharpClass(SaveCommand.RenderAll(rootAggregate, ctx));

            // データ型: ほかの集約から参照されるときのキー
            aggregateFile.AddCSharpClass(KeyClass.KeyClassEntry.RenderClassDeclaringRecursively(rootAggregate, ctx));

            // データ型: SaveCommandメッセージ
            var saveCommandMessage = new SaveCommandMessageContainer(rootAggregate);
            aggregateFile.AddCSharpClass(SaveCommandMessageContainer.RenderTree(rootAggregate));
            ctx.Use<MessageContainer.BaseClass>()
                .Register(saveCommandMessage.InterfaceName, saveCommandMessage.CsClassName)
                .Register(saveCommandMessage.CsClassName, saveCommandMessage.CsClassName);

            // 処理: 新規登録、更新、削除
            var create = new CreateMethod(rootAggregate);
            var update = new UpdateMethod(rootAggregate);
            var delete = new DeleteMethod(rootAggregate);
            aggregateFile.AddAppSrvMethod(create.Render(ctx));
            aggregateFile.AddAppSrvMethod(update.Render(ctx));
            aggregateFile.AddAppSrvMethod(delete.Render(ctx));

            // 処理: 自動生成されるバリデーションエラーチェック
            aggregateFile.AddAppSrvMethod($$"""
                #region 自動生成されるバリデーション処理
                {{CheckRequired.Render(rootAggregate, ctx)}}
                {{CheckMaxLength.Render(rootAggregate, ctx)}}
                {{CheckCharacterType.Render(rootAggregate, ctx)}}
                {{CheckDigitsAndScales.Render(rootAggregate, ctx)}}
                {{DynamicEnum.RenderAppSrvCheckMethod(rootAggregate, ctx)}}
                #endregion 自動生成されるバリデーション処理
                """);

            // 処理: ダミーデータ作成関数
            ctx.Use<DummyDataGenerator>()
                .Add(rootAggregate);

            // 定数: メタデータ
            ctx.Use<Metadata>().Add(rootAggregate);

            // QueryModelと全く同じ型の場合はそれぞれのモデルのソースも生成
            if (rootAggregate.GenerateDefaultQueryModel) {
                QueryModel.GenerateCode(ctx, rootAggregate, aggregateFile);
            }

            aggregateFile.ExecuteRendering(ctx);
        }

        public void GenerateCode(CodeRenderingContext ctx) {
            // メッセージ
            UpdateMethod.RegisterCommonParts(ctx);

            // TODO ver.1: 追加更新削除区分のenum(C#)
        }
    }
}
