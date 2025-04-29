# ValueMemberTypes名前空間

このフォルダには、`nijo.exe` で定義されている標準データ型の列挙型が含まれています。

## 主な責務

- 標準データ型の定義
- 値型メンバーのインターフェイス定義
- 型固有のバリデーションルールの実装
- コード生成時の型マッピング

## ValueMemberTypes名前空間の外部仕様

このネームスペースには、WordTypeやDateTimeTypeなど`nijo.exe`が標準で定義している単語型・日時型などの列挙型が含まれています。これらの列挙型は基本的なデータ型の特性を定義します。

### インターフェイス IValueMemberType

`IValueMemberType`は、すべての値型メンバーが実装すべきインターフェイスです。このインターフェイスにより、型に応じた適切なデータ検証やコード生成が可能になります。

```csharp
public interface IValueMemberType
{
    // 指定された値メンバーのC#型名を取得
    string GetCSharpTypeName(ValueMember member, bool nullable);

    // 指定された値メンバーのTypeScript型名を取得
    string GetTypeScriptTypeName(ValueMember member);

    // 指定された値メンバーのSQL型名を取得
    string GetSqlTypeName(ValueMember member);

    // 指定された値メンバーの検証ルールを取得
    IEnumerable<ValidationRule> GetValidationRules(ValueMember member);
}
```

### 基本データ型

#### Word (単語型)

単語型は、テキスト文字列を表現するための基本的なデータ型です。

**特徴:**
- 最大長・最小長の指定
- 正規表現による値の検証
- データベースでは VARCHAR/NVARCHAR にマッピング

**XML 定義例:**
```xml
<ValueMember name="CustomerName" displayName="顧客名" type="Word" maxLength="100" isRequired="true" />
<ValueMember name="PostalCode" displayName="郵便番号" type="Word" pattern="^\d{3}-\d{4}$" />
```

**生成される C# コード例:**
```csharp
[Required]
[StringLength(100)]
public string CustomerName { get; set; }

[RegularExpression(@"^\d{3}-\d{4}$")]
public string PostalCode { get; set; }
```

**生成される TypeScript コード例:**
```typescript
customerName: string; // required, max length: 100
postalCode?: string; // pattern: ^\d{3}-\d{4}$
```

**生成される SQL 定義例:**
```sql
CustomerName NVARCHAR(100) NOT NULL,
PostalCode NVARCHAR(20) NULL
```

#### Int (整数型)

整数型は、整数値を表現するための基本的なデータ型です。

**特徴:**
- 最大値・最小値の指定
- 自動採番機能（オートインクリメント）
- データベースでは INT/BIGINT にマッピング

**XML 定義例:**
```xml
<ValueMember name="Age" displayName="年齢" type="Int" min="0" max="120" />
<ValueMember name="CustomerId" displayName="顧客ID" type="Int" isIdentity="true" isPrimaryKey="true" />
```

**生成される C# コード例:**
```csharp
[Range(0, 120)]
public int Age { get; set; }

[Key]
[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
public int CustomerId { get; set; }
```

**生成される TypeScript コード例:**
```typescript
age?: number; // min: 0, max: 120
customerId: number; // primary key
```

**生成される SQL 定義例:**
```sql
Age INT NULL,
CustomerId INT NOT NULL IDENTITY(1,1) PRIMARY KEY
```

#### Decimal (小数型)

小数型は、小数点を含む数値を表現するための基本的なデータ型です。

**特徴:**
- 精度(precision)とスケール(scale)の指定
- 最大値・最小値の指定
- データベースでは DECIMAL/NUMERIC にマッピング

**XML 定義例:**
```xml
<ValueMember name="Price" displayName="価格" type="Decimal" precision="10" scale="2" min="0" />
```

**生成される C# コード例:**
```csharp
[Range(typeof(decimal), "0", "99999999.99")]
[Column(TypeName = "decimal(10,2)")]
public decimal Price { get; set; }
```

**生成される TypeScript コード例:**
```typescript
price?: number; // min: 0, precision: 10, scale: 2
```

**生成される SQL 定義例:**
```sql
Price DECIMAL(10,2) NULL
```

#### Bool (真偽値型)

真偽値型は、真/偽の二値を表現するための基本的なデータ型です。

**特徴:**
- カスタマイズ可能な表示テキスト
- データベースでは BIT にマッピング

**XML 定義例:**
```xml
<ValueMember name="IsActive" displayName="有効フラグ" type="Bool" defaultValue="true" />
```

**生成される C# コード例:**
```csharp
public bool IsActive { get; set; } = true;
```

**生成される TypeScript コード例:**
```typescript
isActive: boolean = true;
```

**生成される SQL 定義例:**
```sql
IsActive BIT NOT NULL DEFAULT (1)
```

#### Date (日付型)

日付型は、日付（年月日）を表現するための基本的なデータ型です。

**特徴:**
- 最小日付・最大日付の指定
- データベースでは DATE にマッピング

**XML 定義例:**
```xml
<ValueMember name="BirthDate" displayName="生年月日" type="Date" min="1900-01-01" />
```

**生成される C# コード例:**
```csharp
[DataType(DataType.Date)]
[Range(typeof(DateTime), "1900-01-01", "9999-12-31")]
public DateTime BirthDate { get; set; }
```

**生成される TypeScript コード例:**
```typescript
birthDate?: string; // format: YYYY-MM-DD, min: 1900-01-01
```

**生成される SQL 定義例:**
```sql
BirthDate DATE NULL
```

#### DateTime (日時型)

日時型は、日付と時刻を表現するための基本的なデータ型です。

**特徴:**
- タイムゾーン設定
- データベースでは DATETIME/TIMESTAMP にマッピング

**XML 定義例:**
```xml
<ValueMember name="CreatedAt" displayName="作成日時" type="DateTime" defaultValue="CURRENT_TIMESTAMP" />
```

**生成される C# コード例:**
```csharp
[DataType(DataType.DateTime)]
[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
public DateTime CreatedAt { get; set; }
```

**生成される TypeScript コード例:**
```typescript
createdAt: string; // format: ISO 8601
```

**生成される SQL 定義例:**
```sql
CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
```

#### ByteArray (バイナリ型)

バイナリ型は、バイナリデータを表現するための基本的なデータ型です。

**特徴:**
- 最大サイズの指定
- データベースでは VARBINARY/BLOB にマッピング

**XML 定義例:**
```xml
<ValueMember name="ProfileImage" displayName="プロフィール画像" type="ByteArray" maxLength="1048576" />
```

**生成される C# コード例:**
```csharp
[MaxLength(1048576)]
public byte[] ProfileImage { get; set; }
```

**生成される TypeScript コード例:**
```typescript
profileImage?: string; // base64 encoded
```

**生成される SQL 定義例:**
```sql
ProfileImage VARBINARY(MAX) NULL
```

### 拡張データ型

#### Description (説明文型)

説明文型は、長文テキストを表現するための拡張データ型です。

**特徴:**
- 複数行テキスト
- HTML入力対応
- データベースでは TEXT/NTEXT/VARCHAR(MAX) にマッピング

**XML 定義例:**
```xml
<ValueMember name="ProductDescription" displayName="商品説明" type="Description" isHtml="true" />
```

**生成される C# コード例:**
```csharp
[DataType(DataType.Html)]
public string ProductDescription { get; set; }
```

**生成される TypeScript コード例:**
```typescript
productDescription?: string; // HTML content
```

**生成される SQL 定義例:**
```sql
ProductDescription NVARCHAR(MAX) NULL
```

#### YearMonth (年月型)

年月型は、年と月の組み合わせを表現するための拡張データ型です。

**特徴:**
- YYYY-MM 形式
- データベースでは CHAR(7) またはカスタム型にマッピング

**XML 定義例:**
```xml
<ValueMember name="ReleaseYearMonth" displayName="リリース年月" type="YearMonth" />
```

**生成される C# コード例:**
```csharp
[RegularExpression(@"^\d{4}-\d{2}$")]
public string ReleaseYearMonth { get; set; }
```

**生成される TypeScript コード例:**
```typescript
releaseYearMonth?: string; // format: YYYY-MM
```

**生成される SQL 定義例:**
```sql
ReleaseYearMonth CHAR(7) NULL
```

#### Year (年型)

年型は、年を表現するための拡張データ型です。

**特徴:**
- 4桁の年
- データベースでは INT または SMALLINT にマッピング

**XML 定義例:**
```xml
<ValueMember name="FoundationYear" displayName="設立年" type="Year" min="1800" />
```

**生成される C# コード例:**
```csharp
[Range(1800, 9999)]
public int FoundationYear { get; set; }
```

**生成される TypeScript コード例:**
```typescript
foundationYear?: number; // min: 1800, max: 9999
```

**生成される SQL 定義例:**
```sql
FoundationYear SMALLINT NULL
```

#### StaticEnum (静的列挙型)

静的列挙型は、定義済みの列挙値セットから選択される値を表現するための拡張データ型です。

**特徴:**
- 事前定義された値セット
- 表示名と値のマッピング
- C#では列挙型にマッピング

**XML 定義例:**
```xml
<StaticEnumModel name="CustomerType" displayName="顧客区分">
  <EnumValue name="Individual" displayName="個人" value="1" />
  <EnumValue name="Corporate" displayName="法人" value="2" />
</StaticEnumModel>

<ValueMember name="Type" displayName="顧客区分" type="StaticEnum" enumName="CustomerType" />
```

**生成される C# コード例:**
```csharp
// 列挙型定義
public enum CustomerType
{
    Individual = 1,
    Corporate = 2
}

// プロパティ
public CustomerType Type { get; set; }
```

**生成される TypeScript コード例:**
```typescript
// 列挙型定義
export enum CustomerType {
    Individual = 1,
    Corporate = 2
}

// プロパティ
type: CustomerType;
```

**生成される SQL 定義例:**
```sql
Type INT NULL
```

#### ValueObject (値オブジェクト型)

値オブジェクト型は、複数のフィールドから構成される複合値を表現するための拡張データ型です。

**特徴:**
- 複数のプロパティを持つ
- イミュータブル
- 値の等価性

**XML 定義例:**
```xml
<ValueObjectModel name="Address" displayName="住所">
  <ValueMember name="PostalCode" displayName="郵便番号" type="Word" />
  <ValueMember name="Prefecture" displayName="都道府県" type="Word" />
  <ValueMember name="City" displayName="市区町村" type="Word" />
  <ValueMember name="Street" displayName="番地" type="Word" />
</ValueObjectModel>

<ValueMember name="Address" displayName="住所" type="ValueObject" valueObjectName="Address" />
```

**生成される C# コード例:**
```csharp
// 値オブジェクトクラス
public class Address
{
    public string PostalCode { get; set; }
    public string Prefecture { get; set; }
    public string City { get; set; }
    public string Street { get; set; }

    // 等価性メソッド
    public override bool Equals(object obj)
    {
        // Equals実装
    }

    public override int GetHashCode()
    {
        // GetHashCode実装
    }
}

// プロパティ
public Address Address { get; set; }
```

**生成される TypeScript コード例:**
```typescript
// インターフェイス定義
export interface Address {
    postalCode?: string;
    prefecture?: string;
    city?: string;
    street?: string;
}

// プロパティ
address?: Address;
```

**生成される SQL 定義例:**
```sql
-- JSON形式で格納する場合
Address NVARCHAR(MAX) NULL,

-- または正規化する場合
AddressPostalCode NVARCHAR(20) NULL,
AddressPrefecture NVARCHAR(50) NULL,
AddressCity NVARCHAR(100) NULL,
AddressStreet NVARCHAR(200) NULL
```

#### Sequence (シーケンス型)

シーケンス型は、連番を生成するための拡張データ型です。

**特徴:**
- 採番ルールのカスタマイズ
- プレフィックス/サフィックスの設定
- データベースではカスタムシーケンスやトリガーを使用

**XML 定義例:**
```xml
<ValueMember name="OrderNumber" displayName="注文番号" type="Sequence" prefix="ORD-" format="{0:000000}" />
```

**生成される C# コード例:**
```csharp
[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
public string OrderNumber { get; set; }
```

**生成される TypeScript コード例:**
```typescript
orderNumber: string; // format: ORD-000001, ORD-000002, ...
```

**生成される SQL 定義例:**
```sql
-- シーケンス定義
CREATE SEQUENCE seq_order_number START WITH 1 INCREMENT BY 1;

-- カラム定義
OrderNumber VARCHAR(10) NOT NULL DEFAULT ('ORD-' + FORMAT(NEXT VALUE FOR seq_order_number, '000000'))
```

### カスタムデータ型の作成

新しいデータ型を追加するには、以下の手順に従います：

1. `IValueMemberType` インターフェイスを実装する新しいクラスを作成
2. 必要なメソッドをオーバーライド:
   - `GetCSharpTypeName`
   - `GetTypeScriptTypeName`
   - `GetSqlTypeName`
   - `GetValidationRules`
3. `ValueMemberTypeRegistry` に新しい型を登録

**実装例:**
```csharp
public class EmailType : IValueMemberType
{
    public string GetCSharpTypeName(ValueMember member, bool nullable)
    {
        return nullable ? "string?" : "string";
    }

    public string GetTypeScriptTypeName(ValueMember member)
    {
        return "string";
    }

    public string GetSqlTypeName(ValueMember member)
    {
        int maxLength = member.MaxLength ?? 256;
        return $"NVARCHAR({maxLength})";
    }

    public IEnumerable<ValidationRule> GetValidationRules(ValueMember member)
    {
        yield return new RegexValidationRule(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");

        if (member.IsRequired)
        {
            yield return new RequiredValidationRule();
        }
    }
}

// 登録
ValueMemberTypeRegistry.Register("Email", new EmailType());
```

**使用例:**
```xml
<ValueMember name="Email" displayName="メールアドレス" type="Email" isRequired="true" />
```

### データ型拡張のベストプラクティス

1. **既存の型を拡張する**
   - 既存の基本型を継承し、特定の検証ルールを追加することで、特殊なビジネスデータ型を作成できます

2. **ドメイン固有型の作成**
   - ビジネスドメインに特化した型（例：ISBN型、クレジットカード型）を作成することで、より堅牢なモデリングが可能です

3. **言語間の型マッピングの一貫性**
   - C#、TypeScript、SQLの間で一貫した型マッピングを維持し、データ整合性を確保します

4. **バリデーションルールの再利用**
   - 共通のバリデーションロジックを抽出し、複数の型で再利用することでコードの重複を避けます
