# CLAUDE.md

## 設計規約

このリポジトリでコードを書く際は、必ず [design-conventions.md](./design-conventions.md) のモジュール分割規約に従うこと。
特に以下は作業のたびに適用される:

- 最小差分主義の禁止。タスク完了時に、変更が触れた構造が最も綺麗な状態になっていること。
- `Helper` / `Service` / `Manager` / `Util` 等の役割語クラスを新設しない。名詞クラスに振る舞いを持たせる。
- `useEffect` は外部システムとの同期のみ。導出計算・イベントハンドラ・ref で書けるものに使わない。
- 作業完了前に design-conventions.md 4章のセルフチェックを実施すること。
