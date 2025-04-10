# Nijo プロジェクト
このプロジェクトは、スキーマ定義（nijo.xml）からソースコードを自動生成するプロジェクトです。
このプロジェクトが担うのはあくまでソース生成のみであり、生成されたあとのプロジェクトは、このプロジェクトを一切参照しません。

## プロジェクト構成
- [`CodeGenerating/`](./CodeGenerating/): ソースコード生成処理の実装を容易にするための各種機能を提供します。
- [`SchemaParsing/`](./SchemaParsing/): nijo.xmlのXML要素の解析に関する機能を提供します。
- [`Models/`](./Models/): モデル定義。nijo.xmlのルート要素直下のXML要素は、その種類によりどういったソースコードが生成されるかが異なります。例えば、永続化されるべきデータを表したモデル(DataModel)からは、RDBMSのテーブル定義や、Entity Framework Core のDbContextの設定が生成されます。例えば、画面や帳票に表示されるべきデータを表したモデル(QueryModel)からは、検索条件のオブジェクト定義や、検索処理が生成されます。このフォルダにはモデル毎にそのモデルからどういったソースコードが生成されるべきかを定義する処理が書かれています。
- [`ImmutableSchema/`](./ImmutableSchema/): XMLスキーマの解析が通った後の、XMLスキーマが確定していることを前提とした各種クラス。コードの自動生成ではここにある各種クラスが利用されます。
- [`ValueMemberTypes/`](./ValueMemberTypes/): 値型メンバーの型定義。単語型、整数型、日付型、日付時刻型など。
- [`Parts/`](./Parts/): スキーマ定義XMLに由来するモジュールではなく、生成後のソースに由来するモジュール。C#のORMであるEntity Framework CoreのDbContext, Node.js の.envファイル、NUnitのテスト定義など。
- [`Util.DotnetEx/`](./Util.DotnetEx/): .NET関連のユーティリティ。ここにある機能はコード自動生成処理でのみ参照されます。
