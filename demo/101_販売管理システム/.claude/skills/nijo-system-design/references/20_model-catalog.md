# Phase 3: モデルカタログと粒度判断

Nijo の全モデル種別の性質と、要求をどのモデルに割り当てるかの判断基準。
本ファイルはリポジトリ内ドキュメント（`Document/docs/02_workflow/`）からの転記を含む。**記述が現物のドキュメント・デモと食い違う場合は現物を正とする。**

## 1. モデル種別一覧

| モデル | Type 属性値 | 置き場所 | DB永続化 | キー | 役割 |
|---|---|---|---|---|---|
| DataModel | `data-model` | `<DataStructures>` | あり | 必須 | 永続化されるデータ。楽観排他の単位 |
| QueryModel | `query-model` | `<DataStructures>` | なし（読取専用） | あり | 一覧検索。フィルタ・ソート・ページング自動生成 |
| CommandModel | `command-model` | `<Commands>` | - | - | 処理1サイクル（引数→処理→戻り値）。本体は手実装 |
| StructureModel | `structure-model` | `<DataStructures>` | なし | 不要 | C#/TS 共有のデータ構造。画面入出力用 |
| StaticEnum | `enum` | `<StaticEnums>` | key(int)が保存 | - | コード管理の区分値。C# enum / TS リテラル型 |
| ValueObject | `value-object` | `<ValueObjects>` | string が保存 | - | ID・コードの専用型。誤代入をコンパイルエラーに |
| ConstantModel | `constant-model` | `<Constants>` | - | - | C#/TS で同期される定数 |
| 汎用参照テーブル | data-model + `IsGenericLookupTable="True"` | `<DataStructures>` + `<GenericLookupTableCategories>` | あり | - | 画面から変更できる区分値マスタ |

## 2. DataModel（永続化データ）

**楽観排他制御がかかるべき単位で1つ** 定義する。これが最重要の粒度基準。

- RDBMS のテーブル定義をトランザクション境界ごとにまとめたもの
- フロントエンドには直接露出しない。必ず Command/Query を介して操作される
- 自動生成: EF Core エンティティ + DbContext 設定、CRUD メソッド（新規・更新・物理削除）、入力バリデーション、論理削除（`UseSoftDelete="True"`）、汎用参照テーブル

### 集約構造

| 種類 | Type | 多重度 | キー |
|---|---|---|---|
| Root | （ルート要素） | - | 必須（1個以上） |
| Child | `child` | 親:子 = 1:0..1 | 指定不可（親キーを自動継承） |
| Children | `children` | 親:子 = 1:0..N | 必須（親キーに加えて自身のキー） |

- Child / Children は同一集約内で混在・多段ネスト可（デモ200: 機器詳細(child) → 機器仕様(child) → サイズ(child)、デモ101: 売上明細(children) → 引当明細(children) の2階層）
- 例:「受注」と「受注明細」は常にセットで更新される → 受注をルート、受注明細を children にした **1つの DataModel**

### キー設計

- `IsKey="True"` で指定。サロゲートキー（`Type="sequence"` + `SequenceName`、または UUID を `word` で）とナチュラルキー（社員コード等）のどちらも可
- 外部システム連携があるデータは、内部キーをサロゲートにし、外部側 ID を別項目 + `UniqueConstraints` で一意担保するのがデモ101の流儀（商品SEQ + 外部システム側ID）
- `UniqueConstraints="<メンバーのUniqueId>;"` をルート要素に付けるとユニーク制約（セミコロン区切りで複合可）

### 参照（ref-to）

- `Type="ref-to:参照先集約名"`。外部キー制約として実装される
- 子孫集約への参照はスラッシュ区切り: `ref-to:医療機器マスタ/在庫情報`
- 参照項目に `IsKey="True"` を付けると識別関係（Identifying Relationship）
- `RefToObject="RefTarget"` / `"DisplayData"` で参照対象の種別を指定できる

## 3. QueryModel（一覧検索）

**ユーザーが一覧検索する粒度 ≒ 一覧画面の項目定義** で定義する。

- 自動生成: 検索処理本体（WHERE / SKIP / TAKE / ORDER BY）、SearchCondition クラス、DisplayData クラス、ページング
- 検索条件は型に応じて自動生成される（文字列: 完全/部分/前方/後方一致（`StringSearchBehavior`）、数値・日付: 範囲、enum: チェックボックス群）
- ref-to を持たせると参照先の属性による絞り込み条件も生成される

### 実装パターンの選択

| パターン | 使う場面 |
|---|---|
| `GenerateDefaultQueryModel="True"`（DataModel に付与） | DataModel の構造をほぼそのまま一覧表示すれば足りるマスタ等 |
| 専用 query-model を手で定義 | 集計項目（合計金額等）を持つ一覧、複数集約を跨ぐ一覧、項目を絞った参照用（後述の Ref パターン） |
| 専用 query-model + `MapToView="True"`（+ `DbName`） | 複雑な JOIN・集計・パフォーマンスチューニングが必要な場合。DB ビューにマップし、FROM/SELECT は開発者が SQL で書く |

**重要**: Nijo が自動生成するのは一覧検索 SQL の WHERE / SKIP / TAKE / ORDER BY のみ。複雑な結合・集計・サブクエリ（FROM / SELECT）は開発者に委ねられる設計思想。集計を含む一覧は MapToView を積極的に検討する。

### 参照最適化（Ref パターン、デモ101 の流儀）

実体の DataModel（従業員）とは別に、参照に必要な最小限の項目だけを持つ query-model（従業員Ref）を定義し、各所の `担当者` は `ref-to:従業員Ref` を参照する。参照が多い集約はこのパターンを検討する。

## 4. CommandModel（処理1サイクル）

**「1つのユースケース（ユーザーのアクション）」につき「1つの CommandModel」**。

- 「登録ボタンを押す」「月次集計を実行する」「CSVを取り込む」「複雑な画面の初期表示」がそれぞれ1つ
- 自動生成されるのは枠組みのみ: Web API エンドポイント、引数・戻り値の型、IPresentationContext（エラー管理）。**処理本体（`Execute{Name}Async`）は必ず開発者が手実装する**
- データ構造は自身では持たず、`Parameter="構造名"` / `ReturnValue="構造名"`（または `"QueryModel名:DisplayData"`）で指定。引数なし・戻り値なしも可

```xml
<売上新規登録
  Parameter="売上詳細"
  ReturnValue="売上新規登録ReturnValue"
  Type="command-model"
  UniqueId="..." />
```

## 5. StructureModel（画面入出力の構造）

DB と無関係な C#/TS 共有データ構造。CommandModel の引数・戻り値、画面間のデータ転送に使う。

- キー不要（IsKey を書いても DB 制約にはならない）。ref-to 可（外部キー制約は生成されない）。child/children 可。再帰定義は不可
- TS 側には `createNew{名前}()` ファクトリ関数も生成される

### 詳細画面の定石（デモ101 の連携型）

同じ「売上」概念が役割別に4つに分かれる:

```
DataModel 売上          … DBの真実。引当明細まで持つ
query-model 売上一覧     … 一覧画面。集計項目を持ち明細は持たない
structure-model 売上詳細 … 詳細/編集画面の入出力。引当明細は持たない（システム内部で自動判定）
command-model 群         … 初期表示 / 新規登録 / 修正 / シミュレート が structure を Parameter/ReturnValue で束ねる
```

DataModel と画面構造が異なる（画面には出さない項目・画面にしかない項目がある）とき、この分離が効く。
DataModel をそのまま画面に出せる単純なマスタは GenerateDefaultQueryModel で済ませてよい。

### 一括更新の定石

`GenerateBatchUpdateCommand` のような属性は**存在しない**。一括更新は「children を持つ structure-model（更新対象一覧に `ref-to:対象 RefToObject="DisplayData"`）+ それを Parameter に取る command-model」で表現する（デモ101 の 従業員一括更新）。

## 6. 区分値の表現: StaticEnum vs 汎用参照テーブル vs ValueObject

| | StaticEnum | 汎用参照テーブル | ValueObject |
|---|---|---|---|
| 値の管理 | nijo.xml（コード） | DB テーブル（画面から変更可） | -（型のみ） |
| 変更時 | コード変更・再デプロイ | 運用中に画面から追加・変更 | - |
| 型安全性 | C# enum / TS リテラル型 | 文字列 | 専用型（誤代入をコンパイルエラー化） |
| 適する対象 | ロジックが分岐する区分（消費税区分、ステータス） | 頻繁に増減する選択肢（部門、カテゴリ） | ID・コード類 |

**判断基準: その区分値で C# のロジックが分岐するなら StaticEnum、単なる選択肢で運用中に増減するなら汎用参照テーブル。** 将来増減しうる区分を StaticEnum に固定しないこと（変更のたびに再デプロイになる）。

- StaticEnum: `<StaticEnums>` 内に `Type="enum"` で定義し、各値は `key`（整数・一意・DB保存値）を持つ。メンバーからは `Type="消費税区分"` のように enum 名を直接指定
- 汎用参照テーブル: data-model に `IsGenericLookupTable="True"`、ルート直下 `<GenericLookupTableCategories>` でカテゴリ定義、参照側は `Category="契約区分" Type="ref-to:汎用マスタ"`
- ValueObject: `<ValueObjects>` 内に定義し、メンバーからは `Type="値オブジェクト名"` で使用。キーにも使える

## 7. ConstantModel と CustomAttributes

- ConstantModel: C#/TS で同期される定数。`ConstantType`（string / int / decimal / template / child）と `ConstantValue`。template は `{0}` プレースホルダから引数付き関数を生成。child でネスト可
- CustomAttributes: 任意のメタデータを項目に付与し、横断的処理（マスキング、通貨表示、バリデーション）に使う。`<CustomAttributes>` で `PhysicalName` / `DisplayName` / `Type`（Boolean/String/Decimal/Enum）/ `AvailableModels` / `IsValidation` を定義し、項目側に `Custom-<GUID>="True"` 等で付与。デモ101 の実例: Masking（ログマスキング）、IsCurrency（金額表示）、IsQuantity、CustomMaxLength、NotNegative（IsValidation=True で検証フック生成）

個人情報を含む項目には Masking 相当の CustomAttribute の定義・付与を検討すること。

## 8. 粒度判断ヒューリスティクス（最重要）

### DataModel の分割・統合判定

| 兆候 | 判断 |
|---|---|
| 2つのデータが**常に同時に**登録・更新される（明細だけ単独で存在しない） | 1つの DataModel（親 + children） |
| **別々のアクター・別々のタイミング**で更新されるデータが1集約に同居している | 分割する。同居させると無関係な更新同士が楽観排他で衝突する |
| children が親と独立したライフサイクルを持ち始めた（単独で検索したい、他集約から参照したい） | 別 Root + ref-to に昇格 |
| 常に一緒に更新されるのに ref-to で分断され、整合性維持が手実装になっている | 統合する |

デモ101 の実例:「入荷」と「入荷明細」は**別 DataModel**。入荷明細は売上引当のたびに残数が更新されるため、同一集約だと入荷ヘッダの編集と在庫引当が排他衝突する。**「その更新は誰が・いつ起こすか」を項目単位で考えて境界を切る**のがこのシステムの肝（nijo.xml のコメントに明記されている）。

### モデル割当ての手順

1. 要望中の**名詞**を洗い出す → 永続化が必要なら DataModel 候補、区分値なら StaticEnum/汎用参照テーブル候補、ID/コードなら ValueObject 候補
2. 要望中の**動詞**（ユースケース）を洗い出す → 1つずつ command-model か query-model に対応させる
   - 「〜を検索する・一覧を見る」→ query-model（単純なら GenerateDefaultQueryModel）
   - それ以外の操作（登録・修正・取込・出力・締め…）→ command-model
3. 詳細画面を持つトランザクションデータには structure-model + command-model 群（初期表示/登録/修正）の定石を適用
4. どのモデルにも割当てられない要件（リアルタイム通知、複雑な帳票レイアウト、外部 API 呼び出しの詳細等）は「Nijo の生成範囲外・手実装」として明示的にリスト化し、`design/handwork.md` に永続化する（隠さない。方針メモはセッションで揮発するが handwork.md は残る）

### 割当て結果の記述

Phase 3 の成果物は「モデル割当て方針メモ」（レビューAへの入力）。以下を含める:
- DataModel 一覧と各々の境界の理由（なぜここで切るか、楽観排他の観点で）
- ユースケース ID ↔ command/query model の対応表
- 区分値の表現方法の選択と理由
- Nijo の生成範囲外として切り出した要件のリスト（design/handwork.md にも書き出す）
