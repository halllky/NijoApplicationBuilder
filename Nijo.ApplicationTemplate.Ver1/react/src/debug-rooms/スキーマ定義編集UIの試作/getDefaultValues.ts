import { UUID } from "uuidjs"
import { getAttrTypeOptions } from "./NijoUi.getAttrTypeOptions"
import { ApplicationState, ATTR_TYPE, ValueMemberType, XmlElementAttribute, XmlElementAttributeName, XmlElementItem } from "./types"

/**
 * デバッグ用のデフォルトデータ。
 * 本来は、画面表示時に nijo.xml を解析するなどして、サーバーから読み込む。
 */
export const getDefaultValues = (): ApplicationState => {

  // xmlElementTreesは「Nijo.ApplicationTemplate.Ver1/nijo.xml」に倣う。
  const asAttrRecord = (obj: Record<string, string>): Record<XmlElementAttributeName, string> => {
    return obj as Record<XmlElementAttributeName, string>
  }
  const xmlElementTreesWithoutId: Omit<XmlElementItem, 'id'>[][] = [
    [
      { indent: 0, localName: '商品マスタ', attributes: asAttrRecord({ Type: 'data-model', GenerateDefaultQueryModel: 'True', GenerateBatchUpdateCommand: 'True', LatinName: 'Product Master' }) },
      { indent: 1, localName: '商品ID', attributes: asAttrRecord({ Type: 'word', IsKey: 'True', IsRequired: 'True', DbName: 'PRODUCT_ID', MaxLength: '20' }) },
      { indent: 1, localName: '商品名', attributes: asAttrRecord({ Type: 'word', IsRequired: 'True', DbName: 'PRODUCT_NAME', MaxLength: '100' }) },
      { indent: 1, localName: '価格', attributes: asAttrRecord({ Type: 'int', IsRequired: 'True', DbName: 'PRICE' }) },
      { indent: 1, localName: 'カテゴリ', attributes: asAttrRecord({ Type: 'ref-to:カテゴリマスタ', IsRequired: 'True', DbName: 'CATEGORY_ID' }) },
      { indent: 1, localName: '仕入先', attributes: asAttrRecord({ Type: 'ref-to:仕入先マスタ', IsRequired: 'False', DbName: 'SUPPLIER_ID' }) },
      { indent: 1, localName: '商品詳細', attributes: asAttrRecord({ Type: 'child', DbName: 'PRODUCT_DETAIL' }) },
      { indent: 2, localName: '説明文', attributes: asAttrRecord({ Type: 'description', DbName: 'DESCRIPTION', MaxLength: '1000' }) },
      { indent: 2, localName: '商品仕様', attributes: asAttrRecord({ Type: 'child', DbName: 'PRODUCT_SPEC' }) },
      { indent: 3, localName: '重量', attributes: asAttrRecord({ Type: 'int', DbName: 'WEIGHT' }) },
      { indent: 3, localName: 'サイズ', attributes: asAttrRecord({ Type: 'int', DbName: 'SIZE' }) },
      { indent: 4, localName: '幅', attributes: asAttrRecord({ Type: 'int', DbName: 'WIDTH' }) },
      { indent: 4, localName: '高さ', attributes: asAttrRecord({ Type: 'int', DbName: 'HEIGHT' }) },
      { indent: 4, localName: '奥行', attributes: asAttrRecord({ Type: 'int', DbName: 'DEPTH' }) },
      { indent: 2, localName: '付属品', attributes: asAttrRecord({ Type: 'children', DbName: 'ACCESSORIES' }) },
      { indent: 3, localName: '付属品ID', attributes: asAttrRecord({ Type: 'word', IsKey: 'True', IsRequired: 'True', DbName: 'ACCESSORY_ID', MaxLength: '20' }) },
      { indent: 3, localName: '付属品名', attributes: asAttrRecord({ Type: 'word', IsRequired: 'True', DbName: 'ACCESSORY_NAME', MaxLength: '50' }) },
      { indent: 3, localName: '数量', attributes: asAttrRecord({ Type: 'int', DbName: 'QUANTITY' }) },
      { indent: 2, localName: '在庫情報', attributes: asAttrRecord({ Type: 'children', DbName: 'INVENTORY' }) },
      { indent: 3, localName: '倉庫', attributes: asAttrRecord({ Type: 'ref-to:倉庫マスタ', IsKey: 'True', IsRequired: 'True', DbName: 'WAREHOUSE_ID' }) },
      { indent: 3, localName: '在庫数', attributes: asAttrRecord({ Type: 'int', DbName: 'STOCK_QUANTITY' }) },
      { indent: 3, localName: '棚卸日時', attributes: asAttrRecord({ Type: 'datetime', DbName: 'INVENTORY_DATE' }) },
      { indent: 3, localName: '在庫状況履歴', attributes: asAttrRecord({ Type: 'children', DbName: 'STOCK_HISTORY' }) },
      { indent: 4, localName: '履歴ID', attributes: asAttrRecord({ Type: 'word', IsKey: 'True', IsRequired: 'True', DbName: 'HISTORY_ID', MaxLength: '36' }) },
      { indent: 4, localName: '変更日時', attributes: asAttrRecord({ Type: 'datetime', DbName: 'CHANGE_DATE' }) },
      { indent: 4, localName: '変更前在庫数', attributes: asAttrRecord({ Type: 'int', DbName: 'PREVIOUS_QUANTITY' }) },
      { indent: 4, localName: '変更後在庫数', attributes: asAttrRecord({ Type: 'int', DbName: 'CURRENT_QUANTITY' }) },
      { indent: 4, localName: '担当者', attributes: asAttrRecord({ Type: 'ref-to:従業員マスタ', DbName: 'STAFF_ID' }) },
    ],

    // ※ 途中の集約はいったん割愛してenumとvalue-objectを定義 ※

    [
      { indent: 0, localName: '従業員ID型', attributes: asAttrRecord({ Type: 'value-object' }) },
    ],
    [
      { indent: 0, localName: 'payment_type', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '現金', attributes: asAttrRecord({ DisplayName: '現金決済', Key: '1' }) },
      { indent: 1, localName: 'クレジットカード', attributes: asAttrRecord({ DisplayName: 'クレジットカード決済', Key: '2' }) },
      { indent: 1, localName: '電子マネー', attributes: asAttrRecord({ DisplayName: '電子マネー決済', Key: '3' }) },
      { indent: 1, localName: '代金引換', attributes: asAttrRecord({ DisplayName: '代金引換', Key: '4' }) },
      { indent: 1, localName: '銀行振込', attributes: asAttrRecord({ DisplayName: '銀行振込', Key: '5' }) },
    ],
    [
      { indent: 0, localName: 'payment_status', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '未払い', attributes: asAttrRecord({ DisplayName: '未払い', Key: '1' }) },
      { indent: 1, localName: '支払済', attributes: asAttrRecord({ DisplayName: '支払済', Key: '2' }) },
      { indent: 1, localName: '一部支払い', attributes: asAttrRecord({ DisplayName: '一部支払い', Key: '3' }) },
      { indent: 1, localName: 'キャンセル', attributes: asAttrRecord({ DisplayName: 'キャンセル', Key: '4' }) },
    ],
    [
      { indent: 0, localName: 'card_type', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: 'VISA', attributes: asAttrRecord({ DisplayName: 'VISA Card', Key: '1' }) },
      { indent: 1, localName: 'MasterCard', attributes: asAttrRecord({ DisplayName: 'MasterCard', Key: '2' }) },
      { indent: 1, localName: 'JCB', attributes: asAttrRecord({ DisplayName: 'JCB', Key: '3' }) },
      { indent: 1, localName: 'American Express', attributes: asAttrRecord({ DisplayName: 'American Express', Key: '4' }) },
    ],
    [
      { indent: 0, localName: 'shipping_method', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '宅配便', attributes: asAttrRecord({ DisplayName: '宅配便', Key: '1' }) },
      { indent: 1, localName: 'メール便', attributes: asAttrRecord({ DisplayName: 'メール便', Key: '2' }) },
      { indent: 1, localName: '店舗受取', attributes: asAttrRecord({ DisplayName: '店舗受取', Key: '3' }) },
    ],
    [
      { indent: 0, localName: 'shipping_status', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '準備中', attributes: asAttrRecord({ DisplayName: '準備中', Key: '1' }) },
      { indent: 1, localName: '発送済', attributes: asAttrRecord({ DisplayName: '発送済', Key: '2' }) },
      { indent: 1, localName: '配送中', attributes: asAttrRecord({ DisplayName: '配送中', Key: '3' }) },
    ],
    [
      { indent: 0, localName: 'shipping_type', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '国内配送', attributes: asAttrRecord({ DisplayName: '国内配送', Key: '1' }) },
      { indent: 1, localName: '海外配送', attributes: asAttrRecord({ DisplayName: '海外配送', Key: '2' }) },
    ],
    [
      { indent: 0, localName: 'shipping_method', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '宅配便', attributes: asAttrRecord({ DisplayName: '宅配便', Key: '1' }) },
      { indent: 1, localName: 'メール便', attributes: asAttrRecord({ DisplayName: 'メール便', Key: '2' }) },
      { indent: 1, localName: '店舗受取', attributes: asAttrRecord({ DisplayName: '店舗受取', Key: '3' }) },
    ],
    [
      { indent: 0, localName: 'shipping_status', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '未発送', attributes: asAttrRecord({ DisplayName: '未発送', Key: '1' }) },
      { indent: 1, localName: '発送中', attributes: asAttrRecord({ DisplayName: '発送中', Key: '2' }) },
      { indent: 1, localName: '配送中', attributes: asAttrRecord({ DisplayName: '配送中', Key: '3' }) },
      { indent: 1, localName: '到着済', attributes: asAttrRecord({ DisplayName: '到着済', Key: '4' }) },
    ],
    [
      { indent: 0, localName: 'gender_type', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '男性', attributes: asAttrRecord({ DisplayName: '男性', Key: '1' }) },
      { indent: 1, localName: '女性', attributes: asAttrRecord({ DisplayName: '女性', Key: '2' }) },
      { indent: 1, localName: 'その他', attributes: asAttrRecord({ DisplayName: 'その他', Key: '3' }) },
      { indent: 1, localName: '回答しない', attributes: asAttrRecord({ DisplayName: '回答しない', Key: '4' }) },
    ],
    [
      { indent: 0, localName: 'member_rank', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '一般', attributes: asAttrRecord({ DisplayName: '一般', Key: '1' }) },
      { indent: 1, localName: 'シルバー', attributes: asAttrRecord({ DisplayName: 'シルバー', Key: '2' }) },
      { indent: 1, localName: 'ゴールド', attributes: asAttrRecord({ DisplayName: 'ゴールド', Key: '3' }) },
      { indent: 1, localName: 'プラチナ', attributes: asAttrRecord({ DisplayName: 'プラチナ', Key: '4' }) },
    ],
    [
      { indent: 0, localName: 'position_type', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '一般社員', attributes: asAttrRecord({ DisplayName: '一般社員', Key: '1' }) },
      { indent: 1, localName: '主任', attributes: asAttrRecord({ DisplayName: '主任', Key: '2' }) },
      { indent: 1, localName: '課長', attributes: asAttrRecord({ DisplayName: '課長', Key: '3' }) },
    ],
    [
      { indent: 0, localName: 'reservation_type', attributes: asAttrRecord({ Type: 'enum' }) },
      { indent: 1, localName: '初診', attributes: asAttrRecord({ DisplayName: '初診', Key: '1' }) },
      { indent: 1, localName: '再診', attributes: asAttrRecord({ DisplayName: '再診', Key: '2' }) },
      { indent: 1, localName: '検査', attributes: asAttrRecord({ DisplayName: '検査', Key: '3' }) },
    ],
  ]

  // valueMemberTypesは「Nijo/SchemaParsing/SchemaParseRule.cs」の `Default()` メソッドが返す `ValueMemberTypes` に倣う。
  const valueMemberTypes: ValueMemberType[] = [
    { schemaTypeName: 'Word', typeDisplayName: '文字列' },
    { schemaTypeName: 'Description', typeDisplayName: '説明' },
    { schemaTypeName: 'IntMember', typeDisplayName: '整数' },
    { schemaTypeName: 'DecimalMember', typeDisplayName: '実数' },
    { schemaTypeName: 'DateTimeMember', typeDisplayName: '日時' },
    { schemaTypeName: 'DateMember', typeDisplayName: '日付' },
    { schemaTypeName: 'YearMonthMember', typeDisplayName: '年月' },
    { schemaTypeName: 'YearMember', typeDisplayName: '年' },
    { schemaTypeName: 'BoolMember', typeDisplayName: 'ブール' },
    { schemaTypeName: 'ByteArrayMember', typeDisplayName: 'バイト配列' },
  ]

  // attributeDefsは「Nijo/SchemaParsing/SchemaParseRule.cs」の `Default()` メソッドが返す `NodeOptions` に倣う。
  // - attributeName は `NodeOption` クラスの `AttributeName` に同じ。
  // - type は `NodeOption` クラスの `Type` に同じ。 `string` か `bool` のどちらかしか無い
  // - "Type" だけは特別
  const attributeDefs: XmlElementAttribute[] = [
    { attributeName: ATTR_TYPE, displayName: '種類', type: 'select', getOptions: getAttrTypeOptions },
    { attributeName: 'DisplayName' as XmlElementAttributeName, displayName: '表示名', type: 'string' },
    { attributeName: 'DbName' as XmlElementAttributeName, displayName: 'データベース名', type: 'string' },
    { attributeName: 'LatinName' as XmlElementAttributeName, displayName: 'ラテン語名', type: 'string' },
    { attributeName: 'IsKey' as XmlElementAttributeName, displayName: '主キー', type: 'bool' },
    { attributeName: 'IsRequired' as XmlElementAttributeName, displayName: '必須', type: 'bool' },
    { attributeName: 'GenerateDefaultQueryModel' as XmlElementAttributeName, displayName: 'デフォルトのクエリモデルを生成', type: 'bool' },
    { attributeName: 'GenerateBatchUpdateCommand' as XmlElementAttributeName, displayName: 'バッチ更新コマンドを生成', type: 'bool' },
    { attributeName: 'IsReadOnly' as XmlElementAttributeName, displayName: '読み取り専用', type: 'bool' },
    { attributeName: 'HasLifeCycle' as XmlElementAttributeName, displayName: '独立ライフサイクル', type: 'bool' },
    { attributeName: 'MaxLength' as XmlElementAttributeName, displayName: '最大文字数', type: 'string' },
    { attributeName: 'CharacterType' as XmlElementAttributeName, displayName: '文字種', type: 'string' },
    { attributeName: 'TotalDigit' as XmlElementAttributeName, displayName: '整数部小数部合計桁数', type: 'string' },
    { attributeName: 'DecimalPlace' as XmlElementAttributeName, displayName: '小数点以下の桁数', type: 'string' },
    { attributeName: 'SequenceName' as XmlElementAttributeName, displayName: 'シーケンス名', type: 'string' },
  ]


  return {
    xmlElementTrees: xmlElementTreesWithoutId.map(tree => ({
      xmlElements: tree.map(el => ({ ...el, id: UUID.generate() })),
    })),
    attributeDefs,
    valueMemberTypes,
  }
}
