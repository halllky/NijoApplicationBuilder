# 値オブジェクト（ValueObject）
スキーマ定義での指定方法: `value-object`

識別子や複合値といった特殊な値を表すためのオブジェクト。
主に集約のキーとして使用されたり、特定の業務ルールをもった値を表現する形で使用される。

この値オブジェクトは、スキーマ定義上の他のモデルの属性の種類として使用することができる。

値オブジェクトの定義からは以下のモジュールが生成される。

## C#の値オブジェクトクラス
値オブジェクトを表すC#のクラスが生成される。
このクラスは値の等価性比較を行うためのメソッドを持つ。
また、`string`型との間で明示的なキャスト (`(string)valueObject` や `(ValueObject)stringValue`) が可能です。

## TypeScriptの型定義
TypeScript側での型定義が生成される。
具体的には、`string`型にユニークなブランド付けを行う公称型 (nominal typing) として定義され、他の文字列型との意図しない代入を防ぎます。
例えば、`MyValueObject` という名前の値オブジェクトは `export type MyValueObject = string & { readonly __brand: unique symbol }` のように定義されます。
