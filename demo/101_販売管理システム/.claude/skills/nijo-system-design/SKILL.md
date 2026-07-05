---
name: nijo-system-design
description: ヒアリング済みの要求ドキュメント（`design/` 配下の requirements.md / stakeholders.md / usecases.md 等。nijo-requirement-elicitation の成果物）から、欠落・矛盾を洗い出してユーザーと確認しながら設計に落とし込み、画面一覧（design/screens.md）と NijoAppScaffold の nijo.xml スキーマ定義を作成するときに使う。「要件を詰めてnijo.xmlに落として」「設計して」等の依頼で発動。要望がまだ言葉になっていない・design/ の要求ドキュメントが無い段階では、先に nijo-requirement-elicitation を使う。
---

# Nijo を用いたシステム設計（要求ドキュメントから）

ヒアリング済みの要求ドキュメント（design/）を入力として、以下の価値を提供しながら画面一覧と nijo.xml に落とし込む:

1. **構造化**: 要求に潜む欠落・矛盾を発見し、必要な点だけユーザーに確認して設計判断を確定する
2. **多面的な設計**: 業務フロー妥当性 / 現行フロー整合 / データ活用可能性 / 運用容易性 / セキュリティ・権限・監査 / 性能・データ量 / 例外系・逆方向業務 / 外部連携の信頼性 / データ移行・初期データ / 法令・保存期間・証跡 / Nijo適合性・実装コスト / 将来拡張性 の観点で設計する
3. **適切なモデル分割**: 各モデル（data/query/command/structure/enum/value-object 等）の性質を踏まえた粒度で分割する

スコープは design/screens.md・design/handwork.md と nijo.xml の作成、および design/ 要求ドキュメントへの設計情報の追記（要求一覧の「落とし先」列、ユースケースの「対応モデル・対応画面」列、非機能要件の「設計値・方針」）まで。コード生成後の実装は範囲外。

## 責務分担

- **ヒアリング（要求の聞き出し）は [nijo-requirement-elicitation](../nijo-requirement-elicitation/SKILL.md) の責務**。requirements.md / stakeholders.md / glossary.md / usecases.md / nonfunctional.md は「ユーザーヒアリングが正」のドキュメントであり、本スキルはこれらを**入力として読む**。設計上必要な追記（対応モデル・対応画面・落とし先）は行うが、業務の事実を勝手に書き換えない
- **本スキルが正を持つ成果物**:
  - **design/screens.md**: 画面一覧。ユースケースと集約の突合せ、利用可能ロール・編集単位・排他・同期/非同期の方針
  - **design/handwork.md**: Nijo 生成範囲外の手実装要件（外部連携仕様・バッチ・帳票・通知等）の台帳
  - **nijo.xml**: nijo.xml で表現できる情報は**すべて**ここに落とす。画面項目定義（query-model / structure-model）、データ構造（ER 相当は生成後の EF Core が正となるため図は描かない）、設計判断の理由（XML コメント）
- **design/issues.md**: 両スキル共同の課題管理表。ユーザー確認事項・保留事項・AIが置いた仮定（状態「仮置き」）をここで管理する

詳細は references/10 の責務マトリクスに従う。

## ワークフロー

各 Phase の冒頭で、指定された references ファイル（本 skill と同じフォルダの `references/`）を読むこと。
Phase を飛ばさない。特にレビューA・Bと validate は品質ゲートであり省略禁止。

### Phase 0: インプット読解

出力先プロジェクトフォルダ（nijo.xml の置き場所）が不明ならこの時点でユーザーに確認する（既存プロジェクトか `nijo new` するか）。

`<プロジェクト>/design/` の要求ドキュメント群と issues.md をすべて読む。issues.md の「保留」「未確認」は設計上の未決事項として尊重する（聞き直さない。設計がそこに依存する場合のみ Phase 1 のブロッカーとして扱う）。

**要求ドキュメントが存在しない・要求がまだ言葉になっていない場合は、先に nijo-requirement-elicitation を実行する**（これが原則）。ユーザーが「ヒアリングは省略して仮でいいから進めて」と明示した場合のみ、渡されたインプットから本スキルが要求ドキュメントのドラフトを作成してよいが、その場合はドラフトである旨を各ファイル冒頭に明記し、issues.md に「仮置き」で起票する。

### Phase 1: 欠落・矛盾検出と設計判断の確定

→ **`references/00_elicitation.md` を読む**

観点カタログを1つずつ当てて欠落・矛盾を洗い出し、「ブロッカー」と「仮定可能」に分類する。
issues.md に既に挙がっている論点は、そこを起点に検討する（ゼロから洗い出し直さない）。
**ブロッカーのみ、AskUserQuestion で1回にまとめて確認する**（質問の乱発禁止）。仮定可能なものは妥当なデフォルトを置いて先へ進み、issues.md に「仮置き」で起票する。
なお nijo-requirement-elicitation では「AIが仮定で埋めること」を禁止しているが、本スキルでは仮定を置いて進んでよい。あちらは要求を言葉にする段階（ユーザー自身が定義すべき）、こちらは要件を設計に落とす段階（慣例的なデフォルトで補完してよい）という責務の違いによる。

### Phase 2: 画面一覧の作成 + ユーザー承認

→ **`references/10_intermediate-artifacts.md` を読む**

`design/screens.md` をテンプレートに従って作成する。**この時点で記入するのは ID・画面・種別・対応UC・利用可能ロール・編集単位・処理方式まで**。「対象集約」「排他」の2列は集約がまだ決まっていないため書かない（Phase 3 で埋める）。
ユースケース一覧と突合し、全 UC に対応画面（または画面不要の明記）があること、考慮が漏れやすい画面（設定・お知らせ・マスタメンテ等）を検討したことを確認する。あわせて usecases.md の一覧に「対応画面」列を、requirements.md の「落とし先」列を追記する。

作成後、**画面一覧・UC↔画面の対応、issues.md の「仮置き」一覧をユーザーに提示し、承認を得る**。ここが nijo.xml 執筆前の最後の安価な軌道修正ポイント。修正指示があれば反映してから先へ進む。

### Phase 3: モデル設計 + レビューA

→ **`references/20_model-catalog.md` を読む**

名詞→データ系モデル、動詞（ユースケース）→command/query-model の手順で割当て、DataModel の境界を楽観排他の単位で決定する。集約名・項目名は glossary.md の用語に従う。成果物は「モデル割当て方針メモ」（DataModel 境界の理由、ユースケース↔モデル対応表、区分値表現の選択理由、Nijo 生成範囲外の要件リスト）。

方針メモはセッションで揮発してよいが、design/ に残すべき情報はこの時点で書き出す:
- Nijo 生成範囲外の要件リスト → **design/handwork.md** に永続化（references/10 のテンプレートに従う）
- screens.md の「対象集約」「排他」列を埋める（Phase 2 で保留していた分）
- usecases.md の一覧に「対応モデル」列を追記する

→ **`references/40_review-checklist.md` を読み、レビューA をサブエージェントで実行する**

Agent ツール（general-purpose）に、references/40 のプロンプト雛形に従って ①design/ 一式 ②方針メモ を渡す。**会話履歴・設計経緯は渡さない**。Blocker は修正してから Phase 4 へ。

### Phase 4: nijo.xml 作成

→ **`references/30_xml-authoring.md` を読む**

デモ101 のスタイルに従って書く。特に:
- 全要素に新規採番した GUID の UniqueId（`uuidgen` を使う。コピー・手打ち捏造禁止）
- 設計判断の理由は XML コメントに長文で残す。`@[集約名](UniqueId)` で相互参照
- 記法に迷ったら `demo/101_販売管理システム/nijo.xml` と `Document/docs/02_workflow/` の現物を正とする

### Phase 5: validate ループ

```bash
dotnet run --project Nijo --no-launch-profile -- validate <プロジェクトフォルダへの相対パス>
```

通るまで修正する。エラーは推測で直さず、ドキュメント・デモの現物を確認してから直す（references/30 の典型エラー表を参照）。

### Phase 6: レビューB（スキーマレビュー）

→ **`references/40_review-checklist.md` のレビューB を、サブエージェントで実行する**

①design/ 一式 ②nijo.xml を渡す。Blocker は修正 → 再 validate → 再レビュー（**1回まで**）。Should は修正するか理由を記録。2回目でも Blocker が残るなら最終報告に明記してユーザーの判断を仰ぐ。

### Phase 7: 最終報告

以下を簡潔に報告する:
- モデル構成の全体像（どの要件をどのモデル・どの画面で実現したか。要求↔UC↔画面↔モデルのトレース）
- 多面的観点（冒頭の12観点）のうち、特に設計判断に効いたものとその対応
- issues.md の現況（「仮置き」＝承認待ちの仮定、「保留」「未確認」＝残った未決事項）
- **意図的に設計から外したこと**と、Nijo の生成範囲外として手実装に切り出した要件（handwork.md の要約）
- レビューで見送った提案（Consider）

## 禁止事項

- ユーザー確認を Phase 1（ブロッカー質問）と Phase 2（承認）以外で乱発しない。Phase 3 以降はレビューと validate を品質ゲートとして自走する
- design/ に nijo.xml で表現できる情報（項目定義・型・ER 図）を書かない
- ヒアリングが正のドキュメント（requirements / stakeholders / glossary / usecases / nonfunctional）の業務内容を、ユーザー確認なしに書き換えない
- 置いた仮定を issues.md に起票せず会話の中だけで済ませない（セッションが終わると揮発する）
- レビューを自分自身（メイン会話の文脈）で済ませない。必ずサブエージェントを使う
- validate を通さずに完了報告しない
