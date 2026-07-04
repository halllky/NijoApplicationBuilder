# Phase 4-5: nijo.xml の記述と検証

記法リファレンスとデモ101 の模範パターン。
**本ファイルの内容が `Document/docs/02_workflow/`、`Document/src/pages/reference/ValueMemberTypes.md`、`demo/` の現物と食い違う場合は現物を正とする。** 網羅的な最新リファレンスが必要なら `dotnet run --project Nijo --no-launch-profile -- generate-reference -o <出力先>` で生成できる。

## 1. ファイル全体の構造

```xml
<?xml version="1.0" encoding="utf-8"?>
<NijoAppScaffold
  ApplicationName="アプリ名"
  RootNamespace="MyApp">
  <DataStructures>      <!-- data-model / query-model / structure-model -->
  </DataStructures>
  <Commands>            <!-- command-model -->
  </Commands>
  <StaticEnums>         <!-- enum -->
  </StaticEnums>
  <ValueObjects>        <!-- value-object -->
  </ValueObjects>
  <Constants>           <!-- constant-model -->
  </Constants>
  <GenericLookupTableCategories>  <!-- 汎用参照テーブルのカテゴリ（使う場合のみ） -->
  </GenericLookupTableCategories>
  <CustomAttributes>    <!-- Custom-xxxx の定義 -->
  </CustomAttributes>
</NijoAppScaffold>
```

使わないセクションは書かない。要素名（集約名・項目名）は日本語でよい（デモの流儀）。属性はアルファベット順に並べる。

## 2. UniqueId のルール

- **全要素に GUID の `UniqueId` が必須**。要素ごとに新規採番する（他要素からのコピー厳禁）
- 一度決めた UniqueId は変えない（UniqueConstraints・コメントの `@[名前](UniqueId)` から参照される）
- 採番は `uuidgen` などで機械的に行うこと。手打ちで捏造しない

## 3. 値型一覧

| Type | 内容 | 検索条件 | 備考 |
|---|---|---|---|
| `word` | 改行なし文字列 | 完全/部分/前方/後方一致（`StringSearchBehavior`で指定） | |
| `description` | 改行を含む長文 | 同上 | 備考・メモ用 |
| `int` | 整数 | 範囲 | |
| `decimal` | 実数 | 範囲 | `TotalDigit` / `DecimalPlace` で桁指定 |
| `sequence` | DB自動採番 | 範囲 | `SequenceName="XXX_SEQ"` 必須 |
| `datetime` | 日付時刻 | 期間 | |
| `date` | 日付のみ | 期間 | |
| `yearmonth` | 年月 | 期間 | |
| `year` | 年（4桁） | 範囲 | |
| `bool` | 真偽値 | 真のみ/偽のみ | |
| `bytearray` | バイト配列 | - | DataModel でのみ定義可 |
| `child` / `children` | 子集約 | - | 20_model-catalog.md 参照 |
| `ref-to:集約名` | 外部参照 | 参照先属性で絞込 | 子孫は `ref-to:親/子` |
| `<enum名>` | 静的区分値 | チェックボックス群 | 例 `Type="消費税区分"` |
| `<value-object名>` | 値オブジェクト | - | 例 `Type="医療従事者ID型"` |

## 4. 属性リファレンス

### 集約ルートに付くもの

| 属性 | 意味 |
|---|---|
| `Type` | モデル種別（必須） |
| `UniqueId` | GUID（必須） |
| `GenerateDefaultQueryModel="True"` | data-model から既定の QueryModel を自動生成 |
| `UniqueConstraints="<UniqueId>;"` | ユニーク制約。対象メンバーの UniqueId をセミコロン区切りで（複合キーは1グループ内に複数列挙） |
| `UseSoftDelete="True"` | 論理削除 |
| `MapToView="True"` | query-model を DB ビューにマップ |
| `DbName="..."` | DB 上の物理テーブル/ビュー名 |
| `IsGenericLookupTable="True"` | 汎用参照テーブル化 |
| `LatinName="..."` | 物理名補助（英語名） |
| `Parameter` / `ReturnValue` | command-model 専用。構造名または `QueryModel名:DisplayData` |

### メンバー（項目）に付くもの

| 属性 | 意味 |
|---|---|
| `IsKey="True"` | 主キー |
| `IsNotNull="True"` | 必須（NOT NULL） |
| `DisplayName="..."` | 画面表示名（物理名と別にしたい時） |
| `SequenceName="..."` | sequence 型の採番シーケンス名 |
| `StringSearchBehavior="Exact"` 等 | 文字列検索の挙動（コード類は Exact にする） |
| `MaxLength="100"` | 最大長 |
| `TotalDigit` / `DecimalPlace` | 数値の総桁数 / 小数桁数 |
| `Category="..."` | 汎用参照テーブル参照時のカテゴリ名 |
| `RefToObject="RefTarget"` / `"DisplayData"` | ref-to の参照対象種別 |
| `OnlySearchCondition="True"` | 検索条件専用項目（エンティティに持たない） |
| `IsHardCodedPrimaryKey="True"` | ハードコード PK |
| `Custom-<GUID>="値"` | カスタム属性の付与 |

## 5. XML コメントの流儀（デモ101 準拠。品質の要）

設計判断は **nijo.xml のコメントに** 残す。design/ に書かない。

1. **集約の直前**: ビジネス上の意味・存在理由・データの由来
2. **設計判断の理由**: なぜこの境界で切ったか、なぜこのキーか。長文でよい
3. **項目の直前**: 必須でない理由、null になる条件、非表示フラグ、負数の意味など
4. **相互参照**: `@[集約名](UniqueId)` 記法で他集約へリンク

模範例（デモ101 原文）:

```xml
<!--入荷・販売の対象となる商品マスタデータ。
このシステムで登録せず、外部システムから日次で連携される。-->
<商品
  GenerateDefaultQueryModel="True"
  Type="data-model"
  UniqueConstraints="f5f225fa-eddd-4cca-ae58-b408e1b34100;"
  UniqueId="a1b2c3d4-e5f6-4a1b-8c2d-3e4f5a6b7c8d">
  <!--このシステムにおける識別番号。非表示-->
  <商品SEQ
    IsKey="True"
    SequenceName="ITEM_SEQ"
    Type="sequence"
    UniqueId="b2c3d4e5-f6a1-4b2c-9d3e-4f5a6b7c8d9e" />
  <!--外部システム側の識別番号-->
  <外部システム側ID
    DisplayName="商品コード"
    IsNotNull="True"
    StringSearchBehavior="Exact"
    Type="word"
    UniqueId="f5f225fa-eddd-4cca-ae58-b408e1b34100" />
  <!--必須でないのは、キャンペーン用販促商品など価格が存在しない商品があるため。-->
  <売値単価_税抜
    Custom-0f2d701d-f481-42d4-8dfb-5296c1b40246="True"
    Type="decimal"
    UniqueId="d4e5f6a1-b2c3-4d4e-1f5a-6b7c8d9e0f1a" />
</商品>
```

children の2階層ネスト例（デモ101 の売上明細→引当明細、抜粋）:

```xml
<売上明細
  Type="children"
  UniqueId="f102d81b-ad4a-4799-a59f-1e41d575f0de">
  <!--UUID。非表示。-->
  <明細ID
    IsKey="True"
    Type="word"
    UniqueId="b2c3d4e5-f6a1-4b0c-7d1e-2f3a4b5c6d7e" />
  <商品
    IsNotNull="True"
    Type="ref-to:商品"
    UniqueId="c3d4e5f6-a1b2-4c5d-2e6f-7a8b9c0d1e2f" />
  <!--どの売上がどの入荷の残数を消費したかを紐づける中間テーブル。-->
  <引当明細
    Type="children"
    UniqueId="a1b2c3d4-e5f6-4a9b-6c0d-1e2f3a4b5c6d">
    <入荷
      IsKey="True"
      Type="ref-to:入荷明細"
      UniqueId="c3d4e5f6-a1b2-4c1d-8e2f-3a4b5c6d7e8f" />
    <!--この売上が該当の入荷明細から何個引き当てたか。取消の場合は負の数で登録される。-->
    <引当数量
      IsNotNull="True"
      Type="int"
      UniqueId="d4e5f6a1-b2c3-4d2e-9f3a-4b5c6d7e8f9a" />
  </引当明細>
</売上明細>
```

## 6. 検証（validate）

```bash
# nijo.xml を含むプロジェクトフォルダを指定
# --no-launch-profile が無いと launchSettings.json のプロファイルが優先されてしまう
dotnet run --project Nijo --no-launch-profile -- validate <プロジェクトフォルダへの相対パス>
```

- exit code 0 かつ出力なしなら成功
- エラーが出た場合、**推測で直さない**。`Document/docs/02_workflow/` と `demo/` の現物からエラーに該当する記法を確認してから修正し、再実行する
- よくある誤り: UniqueId の重複・欠落、children にキーが無い、Child にキーを付けた、ref-to の参照先名の誤記、Parameter/ReturnValue に存在しない構造名を指定、enum のメンバーに key（整数・一意）が無い
