# スキーマパース処理

このフォルダには、nijo.xmlのパース処理に関する実装が含まれています。

## 主な責務

- nijo.xmlの読み込みと解析
- スキーマ定義の妥当性検証
- スキーマ定義からオブジェクトモデルへの変換

## 処理フロー

1. XMLファイルの読み込み
2. スキーマ定義の構文解析
3. バリデーションチェック
4. 内部モデルへの変換

## 関連コンポーネント

- [`../Models/`](../Models/): 変換先の内部モデル定義
- [`../ImmutableSchema/`](../ImmutableSchema/): 不変なスキーマ定義

## スキーマ解析の外部仕様

### XML構文の基本構造
nijo.xmlの基本構造とルートエレメントについて説明します。

```xml
<?xml version="1.0" encoding="utf-8"?>
<nijo version="1.0">
  <!-- モデル定義 -->
</nijo>
```

スキーマファイルのルート要素は必ず `<nijo>` でなければなりません。また、`version` 属性はスキーマのバージョンを示し、現在は "1.0" をサポートしています。

ルート要素の下には、以下のような各種モデル定義要素を配置できます：

```xml
<nijo version="1.0">
  <!-- データモデル定義 -->
  <DataModel name="Customer" displayName="顧客">
    <!-- メンバー定義 -->
  </DataModel>

  <!-- クエリモデル定義 -->
  <QueryModel name="CustomerList" displayName="顧客一覧">
    <!-- メンバー定義 -->
  </QueryModel>

  <!-- コマンドモデル定義 -->
  <CommandModel name="CreateCustomer" displayName="顧客登録">
    <!-- パラメータ定義 -->
  </CommandModel>

  <!-- 静的列挙型定義 -->
  <StaticEnumModel name="CustomerType" displayName="顧客区分">
    <!-- 列挙値定義 -->
  </StaticEnumModel>

  <!-- 値オブジェクト定義 -->
  <ValueObjectModel name="Address" displayName="住所">
    <!-- メンバー定義 -->
  </ValueObjectModel>
</nijo>
```

### ノードタイプと解析ルール
スキーマ定義で使用される主要なノードタイプと、それぞれの解析ルールについて説明します。

#### ノードタイプ
スキーマパース処理で認識される主要なノードタイプは [E_NodeType.cs](./E_NodeType.cs) で定義されており、以下のようなものがあります：

- データモデル関連ノード
  - `DataModel`: データモデルのルート要素
  - `ValueMember`: スカラー値メンバー（プリミティブ型）
  - `RefToMember`: 他のモデルへの参照メンバー
  - `HasManyMember`: 1対多の関係を表す集約メンバー
  - `Index`: インデックス定義

- クエリモデル関連ノード
  - `QueryModel`: クエリモデルのルート要素
  - `DisplayMember`: 表示用メンバー
  - `SearchCondition`: 検索条件メンバー
  - `OrderBy`: ソート順定義
  - `DataSource`: データソース定義

- コマンドモデル関連ノード
  - `CommandModel`: コマンドモデルのルート要素
  - `Parameter`: コマンドパラメータ
  - `Processor`: 処理実行部分の定義

- 静的列挙型関連ノード
  - `StaticEnumModel`: 静的列挙型のルート要素
  - `EnumValue`: 列挙値定義

- 値オブジェクト関連ノード
  - `ValueObjectModel`: 値オブジェクトのルート要素
  - `ValueMember`: 値オブジェクトのメンバー

- メンバー定義ノード
  - `Validation`: バリデーション定義
  - `DisplayTexts`: 表示テキスト定義
  - `CalculationExpression`: 計算式定義

各ノードタイプの完全な定義は [E_NodeType.cs](./E_NodeType.cs) ファイルに記載されています。

#### 解析ルール
解析ルールは [SchemaParseRule.cs](./SchemaParseRule.cs) で定義されており、以下のような検証が行われます：

- 必須属性の存在確認
  ```csharp
  // 必須属性チェックの例
  public static readonly SchemaParseRule NameRequired =
      new SchemaParseRule("name属性は必須です", n => n.Attributes["name"] != null);
  ```

- 属性値の型検証
  ```csharp
  // 型検証の例
  public static readonly SchemaParseRule IsRequiredIsBool =
      new SchemaParseRule("isRequired属性はtrueまたはfalseでなければなりません",
          n => n.Attributes["isRequired"] == null ||
               bool.TryParse(n.Attributes["isRequired"].Value, out _));
  ```

- 名前の一意性検証
  ```csharp
  // 一意性検証の例（実装の一部）
  var duplicateNames = nodes
      .GroupBy(n => n.Attributes["name"]?.Value)
      .Where(g => g.Key != null && g.Count() > 1)
      .Select(g => g.Key)
      .ToList();

  if (duplicateNames.Any())
  {
      // 重複名エラー処理
  }
  ```

- 参照整合性検証
  ```csharp
  // 参照整合性検証の例（実装の一部）
  var refName = node.Attributes["refModelName"]?.Value;
  if (refName != null && !modelNames.Contains(refName))
  {
      // 参照先不存在エラー処理
  }
  ```

- 循環参照検証
  ```csharp
  // 循環参照検証（概念的な例）
  private bool DetectCycles(string modelName, HashSet<string> visited)
  {
      if (visited.Contains(modelName))
          return true; // 循環検出

      visited.Add(modelName);

      // 参照先を再帰的にチェック
      foreach (var refTo in GetReferences(modelName))
      {
          if (DetectCycles(refTo, new HashSet<string>(visited)))
              return true;
      }

      return false;
  }
  ```

### ノードオプション
各ノードに設定できるオプション属性について説明します。すべてのオプションは [NodeOption.cs](./NodeOption.cs) で定義されています。

#### 共通オプション
すべてのノードタイプで使用できる共通オプション：

- `name`: 物理名（識別子）
  - 必須属性
  - クラス名・プロパティ名の生成に使用
  - C#の命名規則に従う必要がある
  - 例: `name="Customer"`

- `displayName`: 表示名
  - UI表示に使用
  - 多言語対応の場合は翻訳キーになる
  - 例: `displayName="顧客情報"`

- `description`: 説明
  - ドキュメント生成や詳細説明に使用
  - 任意の長さのテキスト
  - 例: `description="顧客の基本情報を管理するモデルです。"`

- `note`: 注釈（開発者向けメモ）
  - 開発時の注意点やメモとして使用
  - コード生成には影響しない
  - 例: `note="v2.0でFields追加予定"`

#### 特殊オプション
特定のノードタイプでのみ使用できる特殊オプション：

- データモデル固有オプション
  - `table`: テーブル名
    - データベーステーブル名の指定
    - 省略時は `name` 属性から自動生成
    - 例: `table="TBL_CUSTOMERS"`

  - `schema`: スキーマ名
    - データベーススキーマの指定
    - 省略時はデフォルトスキーマを使用
    - 例: `schema="sales"`

  - `isHistoryManaged`: 履歴管理の有無
    - 履歴管理テーブルを生成するかどうか
    - true/falseで指定（デフォルトはfalse）
    - 例: `isHistoryManaged="true"`

  - `isLogicalDeleteEnabled`: 論理削除の有無
    - 論理削除を使用するかどうか
    - true/falseで指定（デフォルトはfalse）
    - 例: `isLogicalDeleteEnabled="true"`

- クエリモデル固有オプション
  - `dataSource`: データソース定義
    - 静的にデータソースを指定する場合に使用
    - SQL文やデータリソース名
    - 例: `dataSource="Sales.CustomerView"`

  - `pagingEnabled`: ページング有無
    - ページング機能を有効にするかどうか
    - true/falseで指定（デフォルトはfalse）
    - 例: `pagingEnabled="true"`

  - `defaultPageSize`: デフォルトページサイズ
    - ページごとの表示件数のデフォルト値
    - 正の整数で指定
    - 例: `defaultPageSize="20"`

- メンバー固有オプション
  - `type`: データ型
    - メンバーのデータ型
    - 登録されているValueMemberType名
    - 例: `type="Word"`, `type="Int"`, `type="Date"`

  - `isRequired`: 必須かどうか
    - 必須項目かどうかを指定
    - true/falseで指定（デフォルトはfalse）
    - 例: `isRequired="true"`

  - `isUnique`: 一意制約の有無
    - 一意制約を設定するかどうか
    - true/falseで指定（デフォルトはfalse）
    - 例: `isUnique="true"`

  - `defaultValue`: デフォルト値
    - プロパティのデフォルト値
    - 型に応じた形式で指定
    - 例: `defaultValue="0"`, `defaultValue="true"`, `defaultValue="未分類"`

使用可能なすべてのオプションとその詳細は [NodeOption.cs](./NodeOption.cs) ファイルに定義されています。

### バリデーションルール
スキーマ定義のバリデーションルールについて説明します。

#### 構文検証
XML構文の基本的な検証：

- XML整形式チェック
  - XMLとして正しい構文であること
  - 閉じタグの対応が正しいこと
  - 必須の属性が存在すること

- 必須要素・属性の存在確認
  - 各ノードに必須の属性が存在するか確認
  - 例: `name` 属性はすべてのモデル定義に必須

- 値の型チェック
  - 各属性値が期待される型であるか確認
  - 例: `isRequired` 属性はブール値（true/false）でなければならない

スキーマパース処理は、まずXMLドキュメントの整形式をチェックし、その後に各ノードの属性値の型を検証します。

```csharp
// XML整形式チェックの例
try
{
    var doc = XDocument.Load(xmlFilePath);
    // 以降の処理...
}
catch (XmlException ex)
{
    errors.Add($"XML構文エラー: {ex.Message} (行: {ex.LineNumber}, 位置: {ex.LinePosition})");
}
```

#### セマンティック検証
意味的な検証：

- 名前の一意性
  - 同じ種類のモデル内で名前が重複していないことを確認
  - 同じ親ノード内でメンバー名が重複していないことを確認

  ```csharp
  // 名前の一意性チェックの例
  var modelNames = rootNode.Children
      .Where(c => c.NodeType == E_NodeType.DataModel || c.NodeType == E_NodeType.QueryModel /* etc. */)
      .Select(c => c.Attributes["name"]?.Value)
      .Where(n => n != null)
      .ToList();

  var duplicates = modelNames
      .GroupBy(n => n)
      .Where(g => g.Count() > 1)
      .Select(g => g.Key);

  foreach (var name in duplicates)
  {
      errors.Add($"モデル名の重複: '{name}'");
  }
  ```

- 参照先の存在確認
  - 参照先のモデルやメンバーが実際に存在することを確認
  - 例: `refModelName` 属性で指定されたモデルが存在するか

  ```csharp
  // 参照先の存在確認の例
  foreach (var refNode in allNodes.Where(n => n.NodeType == E_NodeType.RefToMember))
  {
      var refModelName = refNode.Attributes["refModelName"]?.Value;
      if (refModelName != null && !modelNames.Contains(refModelName))
      {
          errors.Add($"参照先モデル '{refModelName}' が存在しません");
      }
  }
  ```

- 循環参照の検出
  - モデル間の参照関係に循環がないことを確認
  - 例: A→B→C→Aのような循環参照があると無限ループの原因になる

  ```csharp
  // 循環参照の検出例（概念的なコード）
  foreach (var model in modelNodes)
  {
      var visited = new HashSet<string>();
      if (DetectCycle(model.Attributes["name"].Value, visited, referenceGraph))
      {
          errors.Add($"循環参照が検出されました: {string.Join(" -> ", cyclePath)}");
      }
  }
  ```

- データ型の互換性
  - 参照やリレーションで使用される型が互換性を持つことを確認
  - 例: 外部キーの型と主キーの型が一致すること

  ```csharp
  // 型互換性チェックの例
  var primaryKey = targetModel.GetPrimaryKey();
  var foreignKey = refMember.GetForeignKey();

  if (primaryKey.Type != foreignKey.Type)
  {
      errors.Add($"型の不一致: 主キー '{primaryKey.Name}' ({primaryKey.Type}) と外部キー '{foreignKey.Name}' ({foreignKey.Type})");
  }
  ```

#### エラーハンドリング
エラー発生時の処理：

- エラーメッセージのフォーマット
  - エラーの場所（ファイル名、行番号、要素名）
  - エラーの種類（構文エラー、参照エラー、型エラーなど）
  - エラーの詳細説明

  ```csharp
  // エラーメッセージのフォーマット例
  var errorMessage = $"{xmlFilePath}({lineNumber},{columnNumber}): エラー {errorCode}: {element} - {description}";
  ```

- 処理の中断基準
  - 致命的なエラーの場合は処理を中断
  - 警告レベルのエラーの場合は処理を継続
  - エラーの蓄積と一括報告

  ```csharp
  // 処理中断の例
  if (errors.Any(e => e.Severity == ErrorSeverity.Fatal))
  {
      throw new SchemaValidationException("致命的なエラーが発生したため処理を中断します", errors);
  }
  ```

- 警告とエラーの区別
  - 重大度によるエラーの分類
  - 警告：推奨されない構文や将来的に問題を起こす可能性があるもの
  - エラー：処理続行不可能な重大な問題

  ```csharp
  // 警告とエラーの区別の例
  public enum ErrorSeverity
  {
      Warning,    // 警告（処理続行可能）
      Error,      // エラー（モデル生成は続行可能だが完全性は保証されない）
      Fatal       // 致命的エラー（処理続行不可能）
  }
  ```

### エラーコードと対応方法
主要なエラーコードとその対応方法について説明します。

#### エラーコード例
- `SCHEMA001`: XML構文エラー
  - 説明: XMLの構文が不正
  - 例: タグの閉じ忘れ、属性の引用符不足
  - 対応: XML構文を修正する

- `SCHEMA002`: 必須属性不足
  - 説明: 必須の属性が指定されていない
  - 例: `name` 属性がない
  - 対応: 不足している属性を追加する

- `SCHEMA003`: 重複名エラー
  - 説明: 同じスコープ内で名前が重複している
  - 例: 同じモデル内に同名のプロパティがある
  - 対応: 一意な名前を使用する

- `SCHEMA004`: 参照先不存在
  - 説明: 参照先のモデルやプロパティが存在しない
  - 例: 存在しないモデルを `refModelName` で指定
  - 対応: 正しい参照先名を指定するか、参照先を先に定義する

- `SCHEMA005`: 循環参照検出
  - 説明: モデル間の参照に循環が存在する
  - 例: A→B→C→Aのような循環参照
  - 対応: 参照構造を見直し、循環を解消する

#### 対応方法
各エラーの対応方法や修正の指針を以下に示します。

1. エラーメッセージの確認
   - エラーコードからエラーの種類を特定
   - エラーメッセージから問題の箇所を特定

2. スキーマファイルの修正
   - 構文エラーの場合はXML構文を修正
   - 参照エラーの場合は正しい参照名を設定
   - 型エラーの場合は互換性のある型を使用

3. 再検証の実行
   - 修正後にスキーマ検証を再実行
   - すべてのエラーが解消されるまで繰り返す

4. エラー解決のベストプラクティス
   - 先にベース定義を行い、それを参照する定義を後に記述する
   - 命名規則を一貫させる（例：PascalCaseを使用）
   - 複雑な循環参照が必要な場合は、モデル設計を見直す

実際の開発では、スキーマパース処理からのエラーメッセージを参考に、nijo.xmlを修正していきます。エラーメッセージには問題の箇所と修正に必要な情報が含まれているため、それに従って修正を行うことで、有効なスキーマ定義を作成できます。
