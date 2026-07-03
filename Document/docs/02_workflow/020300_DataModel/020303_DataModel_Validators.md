---

---

# バリデーション

DataModel の属性に特定の XML 属性を付与することで、新規登録・更新処理の中でバリデーションが自動的に生成されます。
また、DB レベルの制約も同時に付与されます。

## バリデーションが実行されるタイミング

バリデーションは CRUD メソッド（`Create`, `Update`）の内部で実行されます。

```
入力値受け取り
↓
【自動バリデーション（このページの内容）】
↓
OnBeforeCreate / OnBeforeUpdate フック（手動実装の追加チェック）
↓
エラーがあれば中断
↓
DB への保存（SaveChanges）
```

エラーが1件でもあると、DB への保存（`SaveChanges`）は実行されません。
エラー内容は例外ではなく、メッセージコンテナオブジェクトに格納されて返されます。

## バリデーション属性一覧

### IsKey / IsNotNull — 必須チェック

`IsKey="true"` または `IsNotNull="true"` が付いている項目は必須入力として扱われます。

| XML 属性           | 適用対象                               | 説明                                |
| ------------------ | -------------------------------------- | ----------------------------------- |
| `IsKey="true"`     | Root・Children の ValueMember / ref-to | 主キー。必須入力 + DB NOT NULL 制約 |
| `IsNotNull="true"` | DataModel の ValueMember / ref-to      | 必須入力 + DB NOT NULL 制約         |

```xml
<Order Type="data-model" DisplayName="受注">
  <OrderId     IsKey="true"     DisplayName="受注番号" />   <!-- 必須 -->
  <CustomerRef Type="ref-to:Customer" IsNotNull="true" DisplayName="得意先" />  <!-- 必須 -->
  <Note        DisplayName="備考" />                         <!-- 任意 -->
</Order>
```

**生成されるチェック（文字列の場合）:**
```csharp
if (string.IsNullOrWhiteSpace(dbEntity.OrderId)) {
    messages.OrderId.AddError(MSG.RequiredError("受注番号"));
}
```

**生成されるチェック（数値・日付などの場合）:**
```csharp
if (dbEntity.Quantity == null) {
    messages.Quantity.AddError(MSG.RequiredError("数量"));
}
```

:::note シーケンス（自動採番）の場合
`SequenceName` が指定された項目はシステムが自動採番するため、新規登録時は null チェックをスキップします。
更新時は null チェックが行われます。
:::

---

### MaxLength — 最大文字数チェック

文字列項目に付与します。DB のカラム長制約も同時に生成されます。

```xml
<Customer Type="data-model" DisplayName="得意先">
  <CustomerId IsKey="true"                DisplayName="得意先コード" />
  <Name       IsNotNull="true" MaxLength="100" DisplayName="得意先名" />
  <Phone      MaxLength="20"              DisplayName="電話番号" />
</Customer>
```

**生成されるチェック:**
```csharp
if (!string.IsNullOrEmpty(dbEntity.Name)
    && new System.Globalization.StringInfo(dbEntity.Name).LengthInTextElements > 100) {
    messages.Name.AddError(MSG.MaxLengthError("得意先名", "100"));
}
```

文字数の計算には `StringInfo.LengthInTextElements` を使用するため、絵文字などのサロゲートペアも1文字として正しくカウントします。

---

### TotalDigit / DecimalPlace — 桁数チェック

数値型（int / long / decimal）の項目に付与します。

| XML 属性           | 説明                                                                                |
| ------------------ | ----------------------------------------------------------------------------------- |
| `TotalDigit="N"`   | 整数型の場合: 10^N 未満であること。decimal の場合: 整数部と小数部を合わせた最大桁数 |
| `DecimalPlace="M"` | decimal のみ。小数部の最大桁数（`TotalDigit` と組み合わせて使用）                   |

```xml
<OrderDetail Type="children" DisplayName="受注明細">
  <Quantity   IsKey="true"  TotalDigit="5"              DisplayName="数量" />       <!-- 最大 99999 -->
  <UnitPrice  IsNotNull="true" TotalDigit="10" DecimalPlace="2" DisplayName="単価" />  <!-- 最大 99999999.99 -->
</OrderDetail>
```

**整数型の場合の生成されるチェック:**
```csharp
if (dbEntity.Quantity != null && Math.Abs((long)dbEntity.Quantity.Value) >= 100000) {
    messages.Quantity.AddError(MSG.DigitsError("数量", "5", "0"));
}
```

**decimal 型の場合の生成されるチェック:**
```csharp
if (dbEntity.UnitPrice != null && (
    Math.Abs(Math.Truncate(dbEntity.UnitPrice.Value)) >= 100000000m ||
    dbEntity.UnitPrice.Value * 100m % 1 != 0)) {
    messages.UnitPrice.AddError(MSG.DigitsError("単価", "8", "2"));
}
```

---

### 汎用参照テーブルの存在チェック

`ref-to` で汎用参照テーブル（`IsGenericLookupTable="true"`）を参照している場合、
指定された区分値がテーブルに存在するかどうかのチェックが自動生成されます。

詳細は [汎用参照テーブル](./020306_DataModel_GenericLookupTable.md) を参照してください。

---

## カスタムバリデーション

スキーマ定義では表現できない複雑なバリデーションは、`OnBeforeCreate` / `OnBeforeUpdate` フックで実装します。

```csharp
public override void OnBeforeCreateOrder(
    OrderCreateCommand command,
    IOrderMessages messages,
    IPresentationContext context) {

    // 受注日が未来日でないかチェック
    if (command.OrderDate > DateTime.Today) {
        messages.OrderDate.AddError("受注日に未来日は指定できません。");
    }

    // 明細の合計数量チェック
    var totalQty = command.OrderDetails?.Sum(d => d.Quantity) ?? 0;
    if (totalQty > 9999) {
        messages.AddError("合計数量が上限を超えています。");
    }
}
```

:::note どこに書くか
- **このデータモデルが関係するどのユースケースからの更新でも守られるべきルール** → `OnBefore{集約名}` フックに実装
- **特定のユースケースでのみ適用されるルール** → `CommandModel` の処理の中に実装
:::

## バリデーションエラーのメッセージ構造

エラーメッセージはネストした構造を持ちます。Children（明細行）のエラーは行番号付きで管理されます。

```
IOrderMessages
  ├─ OrderId       → エラーメッセージリスト
  ├─ CustomerRef   → エラーメッセージリスト
  └─ OrderDetails
       ├─ [0]
       │   ├─ LineNo  → エラーメッセージリスト
       │   └─ ...
       └─ [1]
           └─ ...
```
