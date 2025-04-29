# ValueMemberTypes名前空間
ここにはnijo.exeで既定で定義されている [IValueMemberType](../ImmutableSchema/IValueMemberType.cs) が列挙されています。
標準の単語型、日付型、数値型などの仕様はここに記載されたクラスで定義されています。

この名前空間で定義されたコード自動生成をデフォルトの設定で実行した際に使用されます。
なお、コード自動生成には任意のValueMemberTypeを指定することができ、ここで定義された既定のValueMemberTypeを一切用いないこともできます。

## 標準値型メンバーの外部仕様

### 共通インターフェース
すべての値型メンバーは [IValueMemberType](../ImmutableSchema/IValueMemberType.cs) インターフェースを実装します。これにより、型に応じた適切なデータバリデーションやコード生成が可能になります。

### 基本データ型

#### Word（単語型）
テキスト文字列を表す基本型です。

- **実装クラス**: `Word.cs`
- **特徴**:
  - 最大長・最小長の指定
  - 正規表現によるバリデーション
  - 禁止文字・許可文字の設定
  - DBマッピング: VARCHAR/NVARCHAR

#### Int（整数型）
整数値を表す型です。

- **実装クラス**: `IntMember.cs`
- **特徴**:
  - 最大値・最小値の指定
  - 自動採番（シーケンス）との連携
  - DBマッピング: INT

#### Decimal（小数型）
小数値を表す型です。

- **実装クラス**: `DecimalMember.cs`
- **特徴**:
  - 精度・桁数の指定
  - 最大値・最小値の指定
  - DBマッピング: DECIMAL

#### Bool（真偽値型）
真偽値を表す型です。

- **実装クラス**: `BoolMember.cs`
- **特徴**:
  - 表示テキストのカスタマイズ
  - DBマッピング: BIT

#### Date（日付型）
日付のみを表す型です。

- **実装クラス**: `DateMember.cs`
- **特徴**:
  - 最小日付・最大日付の指定
  - 日付形式のカスタマイズ
  - DBマッピング: DATE

#### DateTime（日時型）
日付と時刻を表す型です。

- **実装クラス**: `DateTimeMember.cs`
- **特徴**:
  - タイムゾーン設定
  - 日時形式のカスタマイズ
  - DBマッピング: DATETIME/TIMESTAMP

#### ByteArray（バイナリ型）
バイナリデータを表す型です。

- **実装クラス**: `ByteArrayMember.cs`
- **特徴**:
  - 最大サイズの指定
  - Base64エンコーディング
  - DBマッピング: VARBINARY/BLOB

### 拡張データ型

#### Description（説明文型）
長文のテキスト入力に適した型です。

- **実装クラス**: `Description.cs`
- **特徴**:
  - 複数行テキスト入力
  - リッチテキスト対応
  - DBマッピング: TEXT/NTEXT

#### YearMonth（年月型）
年と月の組み合わせを表す型です。

- **実装クラス**: `YearMonthMember.cs`
- **特徴**:
  - 年月の検証
  - 表示形式のカスタマイズ
  - DBマッピング: カスタム

#### Year（年型）
年のみを表す型です。

- **実装クラス**: `YearMember.cs`
- **特徴**:
  - 年の検証
  - 範囲指定
  - DBマッピング: INT/SMALLINT

#### StaticEnum（静的列挙型）
静的な列挙値を表す型です。

- **実装クラス**: `StaticEnumMember.cs`
- **特徴**:
  - StaticEnumModelとの連携
  - C#列挙型との連携
  - DBマッピング: INT/VARCHAR

#### ValueObject（値オブジェクト型）
複合的な値を表す型です。

- **実装クラス**: `ValueObjectMember.cs`
- **特徴**:
  - ValueObjectModelとの連携
  - 不変オブジェクトの生成
  - DBマッピング: カスタム

#### Sequence（自動採番型）
連番を自動生成する型です。

- **実装クラス**: `SequenceMember.cs`
- **特徴**:
  - 開始値・増分値の指定
  - 連番書式のカスタマイズ
  - DBマッピング: シーケンス/IDENTITY

### カスタム値型の拡張
カスタム値型を追加する方法と要件について説明します。

- IValueMemberTypeインターフェースの実装
- データベースマッピングの定義
- バリデーションロジックの実装
- UI表示制御の実装
