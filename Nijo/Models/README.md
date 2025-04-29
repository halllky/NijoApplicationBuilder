# モデル定義
このフォルダには、スキーマ定義から生成される各種モデルの実装が含まれています。
nijo.xmlのルート要素直下のXML要素は、その種類によりどういったソースコードが生成されるかが異なります。例えば、永続化されるべきデータを表したモデル(DataModel)からは、RDBMSのテーブル定義や、Entity Framework Core のDbContextの設定が生成されます。一方、画面や帳票に表示されるべきデータを表したモデル(QueryModel)からは、検索条件のオブジェクト定義や、検索処理が生成されます。このフォルダにはモデル毎にそのモデルからどういったソースコードが生成されるべきかを定義する処理が書かれています。

このフォルダのルートに置かれるファイルは必ず `...Model.cs` という名前を持ちます。
また、そこで定義されるクラスは必ず [IModel](../CodeGenerating/IModel.cs) インターフェースを具備します。

各モデルから生成される各種モジュールは `...ModelModules` という名前のフォルダに入っています。
IModelのクラスは、それと対応する `...ModelModules` フォルダの各モジュールのレンダリングメソッドを呼び出し、生成後のプロジェクトにどういったファイルが生成されるべきかを定義します。

各モデルが何を表しているかはそのモデルの `HelpTextMarkdown` プロパティを参照してください。

## 主な責務
- スキーマ定義の内部表現モデル
- コード生成のための中間モデル
- モデル間の変換ロジック

## スキーマ定義の外部仕様

### モデルタイプ
ここでは、nijo.xmlで定義できる主要なモデルタイプとその構成要素について説明します。

#### DataModel
永続化されるデータモデルを表します。RDBMSのテーブル定義やEntity Framework Coreの設定が生成されます。

- **構成要素**：
  - スカラー値メンバー（プリミティブ型）
  - 参照メンバー（他のモデルへの参照）
  - 集約メンバー（1対多の関係）
  - 主キー・外部キー定義
  - インデックス定義
  - デフォルト値と制約

**XML構文例**:
```xml
<DataModel name="Customer" displayName="顧客" table="CUSTOMERS" isHistoryManaged="true">
  <ValueMember name="Id" displayName="顧客ID" isPrimaryKey="true" type="Int" />
  <ValueMember name="Name" displayName="顧客名" type="Word" maxLength="100" isRequired="true" />
  <ValueMember name="Email" displayName="メールアドレス" type="Word" maxLength="256" />
  <ValueMember name="CreatedAt" displayName="作成日時" type="DateTime" isCreatedTimestamp="true" />
  <ValueMember name="UpdatedAt" displayName="更新日時" type="DateTime" isUpdatedTimestamp="true" />

  <RefToMember name="Category" displayName="顧客区分" refModelName="CustomerCategory" />

  <HasManyMember name="Orders" displayName="注文履歴" elementName="Order" refModelName="Order" />

  <Index name="IX_Customer_Email" isUnique="true">
    <IndexColumn name="Email" />
  </Index>
</DataModel>
```

**生成されるファイル**:
- C#のエンティティクラス
- DbContextの設定
- リポジトリクラス
- マイグレーションコード
- TypeScriptの型定義

#### QueryModel
画面や帳票に表示されるデータを表します。検索条件のオブジェクト定義や検索処理が生成されます。

- **構成要素**：
  - 表示用スカラーメンバー
  - 検索条件メンバー
  - ソート順定義
  - ページング設定
  - データソース定義（SQLクエリなど）

**XML構文例**:
```xml
<QueryModel name="CustomerList" displayName="顧客一覧" pagingEnabled="true" defaultPageSize="20">
  <DisplayMember name="Id" displayName="顧客ID" type="Int" sourceColumn="c.Id" />
  <DisplayMember name="Name" displayName="顧客名" type="Word" sourceColumn="c.Name" />
  <DisplayMember name="Email" displayName="メールアドレス" type="Word" sourceColumn="c.Email" />
  <DisplayMember name="CategoryName" displayName="顧客区分" type="Word" sourceColumn="cc.Name" />
  <DisplayMember name="OrderCount" displayName="注文数" type="Int" sourceColumn="(SELECT COUNT(*) FROM Orders o WHERE o.CustomerId = c.Id)" />

  <SearchCondition name="NameLike" displayName="顧客名（部分一致）" type="Word" operator="Contains" targetColumn="c.Name" />
  <SearchCondition name="CategoryId" displayName="顧客区分" type="Int" operator="Equals" targetColumn="c.CategoryId" />

  <OrderBy>
    <OrderByMember name="Name" direction="Ascending" />
  </OrderBy>

  <DataSource>
    <![CDATA[
    FROM Customers c
    LEFT JOIN CustomerCategories cc ON c.CategoryId = cc.Id
    WHERE 1=1
    {{if NameLike != null}}
    AND c.Name LIKE '%' + @NameLike + '%'
    {{endif}}
    {{if CategoryId != null}}
    AND c.CategoryId = @CategoryId
    {{endif}}
    ]]>
  </DataSource>
</QueryModel>
```

**生成されるファイル**:
- 検索条件クラス
- 検索結果DTOクラス
- 検索サービスクラス
- APIエンドポイント
- フロントエンド検索コンポーネント

#### CommandModel
ユーザーの操作などによって実行されるコマンドを表します。APIエンドポイントやバリデーション処理が生成されます。

- **構成要素**：
  - コマンドパラメータ
  - 入力検証ルール
  - 前処理・後処理定義
  - トランザクション設定

**XML構文例**:
```xml
<CommandModel name="CreateCustomer" displayName="顧客登録" transactionRequired="true">
  <Parameter name="Name" displayName="顧客名" type="Word" maxLength="100" isRequired="true" />
  <Parameter name="Email" displayName="メールアドレス" type="Word" maxLength="256">
    <Validation type="Regex" pattern="^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$" message="有効なメールアドレスを入力してください。" />
  </Parameter>
  <Parameter name="CategoryId" displayName="顧客区分ID" type="Int" isRequired="true" />

  <Processor type="SystemCommand">
    <![CDATA[
    public void Execute(CreateCustomerCommand command, IDbContext dbContext)
    {
        var customer = new Customer
        {
            Name = command.Name,
            Email = command.Email,
            CategoryId = command.CategoryId
        };

        dbContext.Customers.Add(customer);
        dbContext.SaveChanges();

        return new CreateCustomerResult
        {
            CustomerId = customer.Id
        };
    }
    ]]>
  </Processor>
</CommandModel>
```

**生成されるファイル**:
- コマンドクラス
- バリデータクラス
- 実行プロセッサクラス
- APIエンドポイント
- フロントエンドフォームコンポーネント

#### StaticEnumModel
静的な列挙型を表します。C#の列挙型やTypeScriptの定数定義などが生成されます。

- **構成要素**：
  - 列挙値
  - 表示名
  - 説明
  - ソート順

**XML構文例**:
```xml
<StaticEnumModel name="OrderStatus" displayName="注文状態">
  <EnumValue name="Pending" displayName="処理中" value="10" />
  <EnumValue name="Confirmed" displayName="確認済" value="20" />
  <EnumValue name="Shipped" displayName="発送済" value="30" />
  <EnumValue name="Delivered" displayName="配達済" value="40" />
  <EnumValue name="Canceled" displayName="キャンセル" value="90" description="キャンセルされた注文" />
</StaticEnumModel>
```

**生成されるファイル**:
- C# 列挙型定義
- 列挙型ヘルパークラス
- TypeScript 列挙型定義
- 選択リスト用データ

#### ValueObjectModel
値オブジェクトを表します。複数のプリミティブ値をひとまとめにした不変オブジェクトが生成されます。

- **構成要素**：
  - スカラー値メンバー
  - バリデーションルール
  - 等価比較処理

**XML構文例**:
```xml
<ValueObjectModel name="Address" displayName="住所">
  <ValueMember name="PostalCode" displayName="郵便番号" type="Word" maxLength="8" isRequired="true">
    <Validation type="Regex" pattern="^\d{3}-?\d{4}$" message="正しい郵便番号形式で入力してください。" />
  </ValueMember>
  <ValueMember name="Prefecture" displayName="都道府県" type="Word" maxLength="10" isRequired="true" />
  <ValueMember name="City" displayName="市区町村" type="Word" maxLength="50" isRequired="true" />
  <ValueMember name="Street" displayName="町名・番地" type="Word" maxLength="100" isRequired="true" />
  <ValueMember name="Building" displayName="建物名・部屋番号" type="Word" maxLength="100" />
</ValueObjectModel>
```

**生成されるファイル**:
- 値オブジェクトクラス
- バリデータクラス
- TypeScript型定義
- フォームコンポーネント

### モデル間の関係
モデル間の関係性を定義する方法について説明します。

#### 参照関係（1対1、多対1）
参照関係は、`RefToMember`要素を使用して定義します。これにより、Entity Framework Coreの外部キー関係が自動的に設定されます。

```xml
<RefToMember name="Category" displayName="カテゴリ" refModelName="Category" isRequired="true" />
```

主な属性:
- `name`: 参照プロパティの名前
- `displayName`: 画面表示用の名前
- `refModelName`: 参照先のモデル名
- `isRequired`: 必須かどうか
- `cascadeDelete`: 親レコード削除時に子レコードも削除するかどうか
- `foreignKeyName`: 外部キー制約の名前（省略時は自動生成）

#### 集約関係（1対多）
集約関係は、`HasManyMember`要素を使用して定義します。これにより、Entity Framework Coreのコレクションナビゲーションプロパティが設定されます。

```xml
<HasManyMember name="OrderItems" displayName="注文明細" elementName="OrderItem" refModelName="OrderItem" />
```

主な属性:
- `name`: コレクションプロパティの名前
- `displayName`: 画面表示用の名前
- `elementName`: コレクション要素の単数形名
- `refModelName`: 参照先のモデル名
- `isComposition`: 構成関係であるかどうか（親に対する強い所有関係）
- `inversePropertyName`: 逆参照プロパティ名

#### 依存関係
モデル間の依存関係（非リレーショナルな関係）は、XML属性で表現します。

```xml
<QueryModel name="OrderReport" displayName="注文レポート" dependsOn="Order,Customer" />
```

主な属性:
- `dependsOn`: 依存するモデル名（カンマ区切り）

### 共通設定項目
すべてのモデルに共通する設定項目について説明します。

#### 物理名（データベース上の名前）
- `name`: C#/TypeScriptのクラス名として使用される識別子
- `table`: データベーステーブル名（DataModelのみ）
- `schema`: データベーススキーマ名（DataModelのみ）

#### 論理名（画面表示用の名前）
- `displayName`: ユーザーインターフェースに表示される名前
- `pluralName`: 複数形の表示名
- `displayOrder`: 表示順序

#### 説明文
- `description`: モデルの詳細な説明
- `note`: 開発者向けの注釈

#### 履歴管理設定
- `isHistoryManaged`: 履歴管理を行うかどうか（DataModelのみ）
- `historyTable`: 履歴テーブル名（省略時は自動生成）

#### 論理削除設定
- `isLogicalDeleteEnabled`: 論理削除を使用するかどうか（DataModelのみ）
- `deletedFlagColumn`: 削除フラグカラム名（省略時はIsDeleted）

#### ソフトウェア生成設定
- `generateTs`: TypeScriptコードを生成するかどうか
- `generateCSharp`: C#コードを生成するかどうか
- `generateReact`: Reactコンポーネントを生成するかどうか
- `uiHint`: UIの生成方法に関するヒント

### スキーマオプション
モデル定義に追加できるオプションについて説明します。

#### 国際化対応
- `enableI18n`: 国際化対応を有効にするかどうか
- `i18nScope`: 国際化リソースのスコープ

```xml
<DataModel name="Product" enableI18n="true" i18nScope="catalog">
  <ValueMember name="Name" type="Word" enableI18n="true" />
  <ValueMember name="Description" type="Description" enableI18n="true" />
</DataModel>
```

#### 権限制御
- `securityLevel`: セキュリティレベル（Public, Protected, Private）
- `requiredRoles`: 必要なロール（カンマ区切り）

```xml
<QueryModel name="SalesReport" securityLevel="Protected" requiredRoles="Manager,Admin">
  <!-- メンバー定義 -->
</QueryModel>
```

#### 監査証跡
監査証跡を設定すると、作成・更新・削除の履歴が自動的に記録されます。

```xml
<DataModel name="Order" enableAudit="true">
  <ValueMember name="CreatedBy" type="Word" isCreatedByUserField="true" />
  <ValueMember name="CreatedAt" type="DateTime" isCreatedTimestamp="true" />
  <ValueMember name="UpdatedBy" type="Word" isUpdatedByUserField="true" />
  <ValueMember name="UpdatedAt" type="DateTime" isUpdatedTimestamp="true" />
</DataModel>
```

#### カスタム属性
拡張性のためにカスタム属性を定義できます。これらはコード生成時に特別な処理を行うために使用されます。

```xml
<DataModel name="Invoice" custom:reportTemplate="InvoiceTemplate" custom:printEnabled="true">
  <!-- メンバー定義 -->
</DataModel>
```

### 高度な機能

#### 複合主キー
複数のカラムを組み合わせた主キーを定義できます。

```xml
<DataModel name="OrderItem">
  <ValueMember name="OrderId" type="Int" isPrimaryKey="true" primaryKeyOrder="1" />
  <ValueMember name="LineNumber" type="Int" isPrimaryKey="true" primaryKeyOrder="2" />
  <!-- その他のメンバー -->
</DataModel>
```

#### 計算フィールド
データベースには存在しないが、計算によって導出されるフィールドを定義できます。

```xml
<DataModel name="Product">
  <ValueMember name="Price" type="Decimal" />
  <ValueMember name="TaxRate" type="Decimal" />
  <ValueMember name="PriceWithTax" type="Decimal" isCalculated="true">
    <CalculationExpression>Price * (1 + TaxRate)</CalculationExpression>
  </ValueMember>
</DataModel>
```

#### カスタムSQL
QueryModelの検索処理で、より複雑なSQLを直接指定できます。

```xml
<QueryModel name="ComplexReport">
  <!-- 表示メンバー定義 -->
  <CustomSql>
  <![CDATA[
  WITH RecentOrders AS (
    SELECT c.Id AS CustomerId, COUNT(*) AS OrderCount, MAX(o.OrderDate) AS LastOrderDate
    FROM Customers c
    JOIN Orders o ON c.Id = o.CustomerId
    WHERE o.OrderDate >= DATEADD(month, -3, GETDATE())
    GROUP BY c.Id
  )
  SELECT c.Id, c.Name, c.Email, ro.OrderCount, ro.LastOrderDate
  FROM Customers c
  LEFT JOIN RecentOrders ro ON c.Id = ro.CustomerId
  WHERE {{Conditions}}
  ORDER BY {{OrderBy}}
  ]]>
  </CustomSql>
</QueryModel>
```

#### バリデーションルール
様々な種類のバリデーションルールを定義できます。

```xml
<ValueMember name="Username" type="Word">
  <Validation type="Required" message="ユーザー名は必須です" />
  <Validation type="MinLength" value="3" message="ユーザー名は3文字以上必要です" />
  <Validation type="MaxLength" value="50" message="ユーザー名は50文字以内にしてください" />
  <Validation type="Regex" pattern="^[a-zA-Z0-9_]+$" message="英数字とアンダースコアのみ使用できます" />
  <Validation type="Custom" customValidator="IsUniqueUsername" message="このユーザー名は既に使用されています" />
</ValueMember>
```

#### バッチ処理
大量データ処理用のバッチ処理コマンドを定義できます。

```xml
<CommandModel name="ImportProducts" displayName="商品一括インポート" isBatchProcess="true" maxDegreeOfParallelism="4">
  <Parameter name="FileUrl" type="Word" />
  <Parameter name="UpdateExisting" type="Bool" defaultValue="true" />
  <!-- 処理定義 -->
</CommandModel>
```
