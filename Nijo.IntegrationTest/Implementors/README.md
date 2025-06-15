# Implementors
[DataPatterns](../DataPatterns/) には様々なパターンのテスト用のスキーマ定義が存在しますが、NijoApplicationBuilderではソース自動生成後に開発者が自身の手で実装しなければならない処理が存在する。
例えば、QueryModelのCreateQuerySourceメソッドでは、そのクエリモデルがどのデータソースからデータを抽出して構築するかはスキーマ定義からは自明に決まらないことがあるため、どこからデータを持ってくるかは開発者が自身の手でソースコードを書いて実装しなければならない。

このフォルダに記載されているソースは、 [DataPatterns](../DataPatterns/) の各XMLと対応した、そのXMLでソースが作成されたあとの開発者自身の実装をシミュレートするものである。

[DataPatternTest](../DataPatternTest.cs) は、以下の手順でテストを行ない、ソース自動生成処理の品質を確かめる。

1. DataPatternsのスキーマ定義を用いてソースを自動生成
2. Implementorsのソースで一部のソース（本来の開発であれば開発者が自身の手で書き換えるソース）を上書き
3. C#, TypeScriptのビルド
4. ダミーデータの登録や、登録されたダミーデータの検索

## ルール
- DataPatternsのXMLと対応するImplementorを必ずこのフォルダ内に作成すること。
- このフォルダに記述するファイルは `(対応するDataPatternsのXMLの名前)Implementor.cs` というファイル名にすること。