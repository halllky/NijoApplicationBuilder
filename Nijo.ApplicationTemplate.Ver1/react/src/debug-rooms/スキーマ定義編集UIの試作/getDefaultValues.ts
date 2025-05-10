import { getAttrTypeOptions } from "./NijoUi.getAttrTypeOptions"
import { ApplicationState, ATTR_TYPE, ValueMemberType, XmlElementAttribute, XmlElementAttributeName, XmlElementItem } from "./types"

/**
 * デバッグ用のデフォルトデータ。
 * 本来は、画面表示時に nijo.xml を解析するなどして、サーバーから読み込む。
 */
export const getDefaultValues = (): ApplicationState => {

  // xmlElementTreesは「Nijo.ApplicationTemplate.Ver1/nijo.xml」に倣う。
  // IDは連番。
  const toAttrMap = (obj: Record<string, string>) => {
    return new Map(Object.entries(obj).map(([k, v]) => [k as XmlElementAttributeName, v]))
  }
  const xmlElementTrees: XmlElementItem[][] = [
    [
      { id: '00001', indent: 0, localName: '商品マスタ', attributes: toAttrMap({ Type: 'data-model', GenerateDefaultQueryModel: 'True', GenerateBatchUpdateCommand: 'True', LatinName: 'Product Master' }) },
      { id: '00002', indent: 1, localName: '商品ID', attributes: toAttrMap({ Type: 'word', IsKey: 'True', IsRequired: 'True', DbName: 'PRODUCT_ID', MaxLength: '20' }) },
      { id: '00003', indent: 1, localName: '商品名', attributes: toAttrMap({ Type: 'word', IsRequired: 'True', DbName: 'PRODUCT_NAME', MaxLength: '100' }) },
      { id: '00004', indent: 1, localName: '価格', attributes: toAttrMap({ Type: 'int', IsRequired: 'True', DbName: 'PRICE' }) },
      { id: '00005', indent: 1, localName: 'カテゴリ', attributes: toAttrMap({ Type: 'ref-to:カテゴリマスタ', IsRequired: 'True', DbName: 'CATEGORY_ID' }) },
      { id: '00006', indent: 1, localName: '仕入先', attributes: toAttrMap({ Type: 'ref-to:仕入先マスタ', IsRequired: 'False', DbName: 'SUPPLIER_ID' }) },
      { id: '00007', indent: 1, localName: '商品詳細', attributes: toAttrMap({ Type: 'child', DbName: 'PRODUCT_DETAIL' }) },
      { id: '00008', indent: 2, localName: '説明文', attributes: toAttrMap({ Type: 'description', DbName: 'DESCRIPTION', MaxLength: '1000' }) },
      { id: '00009', indent: 2, localName: '商品仕様', attributes: toAttrMap({ Type: 'child', DbName: 'PRODUCT_SPEC' }) },
      { id: '00010', indent: 3, localName: '重量', attributes: toAttrMap({ Type: 'int', DbName: 'WEIGHT' }) },
      { id: '00011', indent: 3, localName: 'サイズ', attributes: toAttrMap({ Type: 'int', DbName: 'SIZE' }) },
      { id: '00012', indent: 4, localName: '幅', attributes: toAttrMap({ Type: 'int', DbName: 'WIDTH' }) },
      { id: '00013', indent: 4, localName: '高さ', attributes: toAttrMap({ Type: 'int', DbName: 'HEIGHT' }) },
      { id: '00014', indent: 4, localName: '奥行', attributes: toAttrMap({ Type: 'int', DbName: 'DEPTH' }) },
      { id: '00015', indent: 2, localName: '付属品', attributes: toAttrMap({ Type: 'children', DbName: 'ACCESSORIES' }) },
      { id: '00016', indent: 3, localName: '付属品ID', attributes: toAttrMap({ Type: 'word', IsKey: 'True', IsRequired: 'True', DbName: 'ACCESSORY_ID', MaxLength: '20' }) },
      { id: '00017', indent: 3, localName: '付属品名', attributes: toAttrMap({ Type: 'word', IsRequired: 'True', DbName: 'ACCESSORY_NAME', MaxLength: '50' }) },
      { id: '00018', indent: 3, localName: '数量', attributes: toAttrMap({ Type: 'int', DbName: 'QUANTITY' }) },
      { id: '00019', indent: 2, localName: '在庫情報', attributes: toAttrMap({ Type: 'children', DbName: 'INVENTORY' }) },
      { id: '00020', indent: 3, localName: '倉庫', attributes: toAttrMap({ Type: 'ref-to:倉庫マスタ', IsKey: 'True', IsRequired: 'True', DbName: 'WAREHOUSE_ID' }) },
      { id: '00021', indent: 3, localName: '在庫数', attributes: toAttrMap({ Type: 'int', DbName: 'STOCK_QUANTITY' }) },
      { id: '00022', indent: 3, localName: '棚卸日時', attributes: toAttrMap({ Type: 'datetime', DbName: 'INVENTORY_DATE' }) },
      { id: '00023', indent: 3, localName: '在庫状況履歴', attributes: toAttrMap({ Type: 'children', DbName: 'STOCK_HISTORY' }) },
      { id: '00024', indent: 4, localName: '履歴ID', attributes: toAttrMap({ Type: 'word', IsKey: 'True', IsRequired: 'True', DbName: 'HISTORY_ID', MaxLength: '36' }) },
      { id: '00025', indent: 4, localName: '変更日時', attributes: toAttrMap({ Type: 'datetime', DbName: 'CHANGE_DATE' }) },
      { id: '00026', indent: 4, localName: '変更前在庫数', attributes: toAttrMap({ Type: 'int', DbName: 'PREVIOUS_QUANTITY' }) },
      { id: '00027', indent: 4, localName: '変更後在庫数', attributes: toAttrMap({ Type: 'int', DbName: 'CURRENT_QUANTITY' }) },
      { id: '00028', indent: 4, localName: '担当者', attributes: toAttrMap({ Type: 'ref-to:従業員マスタ', DbName: 'STAFF_ID' }) },
    ],

    // ※ 途中の集約はいったん割愛してenumとvalue-objectを定義 ※

    [
      { id: '50000', indent: 0, localName: '従業員ID型', attributes: toAttrMap({ Type: 'value-object' }) },
    ],
    [
      { id: '50001', indent: 0, localName: 'payment_type', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50002', indent: 1, localName: '現金', attributes: toAttrMap({ DisplayName: '現金決済', Key: '1' }) },
      { id: '50003', indent: 1, localName: 'クレジットカード', attributes: toAttrMap({ DisplayName: 'クレジットカード決済', Key: '2' }) },
      { id: '50004', indent: 1, localName: '電子マネー', attributes: toAttrMap({ DisplayName: '電子マネー決済', Key: '3' }) },
      { id: '50005', indent: 1, localName: '代金引換', attributes: toAttrMap({ DisplayName: '代金引換', Key: '4' }) },
      { id: '50006', indent: 1, localName: '銀行振込', attributes: toAttrMap({ DisplayName: '銀行振込', Key: '5' }) },
    ],
    [
      { id: '50007', indent: 0, localName: 'payment_status', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50008', indent: 1, localName: '未払い', attributes: toAttrMap({ DisplayName: '未払い', Key: '1' }) },
      { id: '50009', indent: 1, localName: '支払済', attributes: toAttrMap({ DisplayName: '支払済', Key: '2' }) },
      { id: '50010', indent: 1, localName: '一部支払い', attributes: toAttrMap({ DisplayName: '一部支払い', Key: '3' }) },
      { id: '50011', indent: 1, localName: 'キャンセル', attributes: toAttrMap({ DisplayName: 'キャンセル', Key: '4' }) },
    ],
    [
      { id: '50012', indent: 0, localName: 'card_type', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50013', indent: 1, localName: 'VISA', attributes: toAttrMap({ DisplayName: 'VISA Card', Key: '1' }) },
      { id: '50014', indent: 1, localName: 'MasterCard', attributes: toAttrMap({ DisplayName: 'MasterCard', Key: '2' }) },
      { id: '50015', indent: 1, localName: 'JCB', attributes: toAttrMap({ DisplayName: 'JCB', Key: '3' }) },
      { id: '50016', indent: 1, localName: 'American Express', attributes: toAttrMap({ DisplayName: 'American Express', Key: '4' }) },
    ],
    [
      { id: '50017', indent: 0, localName: 'shipping_method', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50018', indent: 1, localName: '宅配便', attributes: toAttrMap({ DisplayName: '宅配便', Key: '1' }) },
      { id: '50019', indent: 1, localName: 'メール便', attributes: toAttrMap({ DisplayName: 'メール便', Key: '2' }) },
      { id: '50020', indent: 1, localName: '店舗受取', attributes: toAttrMap({ DisplayName: '店舗受取', Key: '3' }) },
    ],
    [
      { id: '50021', indent: 0, localName: 'shipping_status', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50022', indent: 1, localName: '準備中', attributes: toAttrMap({ DisplayName: '準備中', Key: '1' }) },
      { id: '50023', indent: 1, localName: '発送済', attributes: toAttrMap({ DisplayName: '発送済', Key: '2' }) },
      { id: '50024', indent: 1, localName: '配送中', attributes: toAttrMap({ DisplayName: '配送中', Key: '3' }) },
    ],
    [
      { id: '50025', indent: 0, localName: 'shipping_type', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50026', indent: 1, localName: '国内配送', attributes: toAttrMap({ DisplayName: '国内配送', Key: '1' }) },
      { id: '50027', indent: 1, localName: '海外配送', attributes: toAttrMap({ DisplayName: '海外配送', Key: '2' }) },
    ],
    [
      { id: '50028', indent: 0, localName: 'shipping_method', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50029', indent: 1, localName: '宅配便', attributes: toAttrMap({ DisplayName: '宅配便', Key: '1' }) },
      { id: '50030', indent: 1, localName: 'メール便', attributes: toAttrMap({ DisplayName: 'メール便', Key: '2' }) },
      { id: '50031', indent: 1, localName: '店舗受取', attributes: toAttrMap({ DisplayName: '店舗受取', Key: '3' }) },
    ],
    [
      { id: '50032', indent: 0, localName: 'shipping_status', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50033', indent: 1, localName: '未発送', attributes: toAttrMap({ DisplayName: '未発送', Key: '1' }) },
      { id: '50034', indent: 1, localName: '発送中', attributes: toAttrMap({ DisplayName: '発送中', Key: '2' }) },
      { id: '50035', indent: 1, localName: '配送中', attributes: toAttrMap({ DisplayName: '配送中', Key: '3' }) },
      { id: '50036', indent: 1, localName: '到着済', attributes: toAttrMap({ DisplayName: '到着済', Key: '4' }) },
    ],
    [
      { id: '50037', indent: 0, localName: 'gender_type', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50038', indent: 1, localName: '男性', attributes: toAttrMap({ DisplayName: '男性', Key: '1' }) },
      { id: '50039', indent: 1, localName: '女性', attributes: toAttrMap({ DisplayName: '女性', Key: '2' }) },
      { id: '50040', indent: 1, localName: 'その他', attributes: toAttrMap({ DisplayName: 'その他', Key: '3' }) },
      { id: '50041', indent: 1, localName: '回答しない', attributes: toAttrMap({ DisplayName: '回答しない', Key: '4' }) },
    ],
    [
      { id: '50042', indent: 0, localName: 'member_rank', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50043', indent: 1, localName: '一般', attributes: toAttrMap({ DisplayName: '一般', Key: '1' }) },
      { id: '50044', indent: 1, localName: 'シルバー', attributes: toAttrMap({ DisplayName: 'シルバー', Key: '2' }) },
      { id: '50045', indent: 1, localName: 'ゴールド', attributes: toAttrMap({ DisplayName: 'ゴールド', Key: '3' }) },
      { id: '50046', indent: 1, localName: 'プラチナ', attributes: toAttrMap({ DisplayName: 'プラチナ', Key: '4' }) },
    ],
    [
      { id: '50047', indent: 0, localName: 'position_type', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50048', indent: 1, localName: '一般社員', attributes: toAttrMap({ DisplayName: '一般社員', Key: '1' }) },
      { id: '50049', indent: 1, localName: '主任', attributes: toAttrMap({ DisplayName: '主任', Key: '2' }) },
      { id: '50050', indent: 1, localName: '課長', attributes: toAttrMap({ DisplayName: '課長', Key: '3' }) },
    ],
    [
      { id: '50051', indent: 0, localName: 'reservation_type', attributes: toAttrMap({ Type: 'enum' }) },
      { id: '50052', indent: 1, localName: '初診', attributes: toAttrMap({ DisplayName: '初診', Key: '1' }) },
      { id: '50053', indent: 1, localName: '再診', attributes: toAttrMap({ DisplayName: '再診', Key: '2' }) },
      { id: '50054', indent: 1, localName: '検査', attributes: toAttrMap({ DisplayName: '検査', Key: '3' }) },
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
    { attributeName: ATTR_TYPE, type: 'select', getOptions: getAttrTypeOptions },
    { attributeName: 'DisplayName' as XmlElementAttributeName, type: 'string' },
    { attributeName: 'DbName' as XmlElementAttributeName, type: 'string' },
    { attributeName: 'LatinName' as XmlElementAttributeName, type: 'string' },
    { attributeName: 'IsKey' as XmlElementAttributeName, type: 'bool' },
    { attributeName: 'IsRequired' as XmlElementAttributeName, type: 'bool' },
    { attributeName: 'GenerateDefaultQueryModel' as XmlElementAttributeName, type: 'bool' },
    { attributeName: 'GenerateBatchUpdateCommand' as XmlElementAttributeName, type: 'bool' },
    { attributeName: 'IsReadOnly' as XmlElementAttributeName, type: 'bool' },
    { attributeName: 'HasLifeCycle' as XmlElementAttributeName, type: 'bool' },
    { attributeName: 'MaxLength' as XmlElementAttributeName, type: 'string' },
    { attributeName: 'CharacterType' as XmlElementAttributeName, type: 'string' },
    { attributeName: 'TotalDigit' as XmlElementAttributeName, type: 'string' },
    { attributeName: 'DecimalPlace' as XmlElementAttributeName, type: 'string' },
    { attributeName: 'SequenceName' as XmlElementAttributeName, type: 'string' },
  ]


  return {
    xmlElementTrees: xmlElementTrees.map(tree => ({ xmlElements: tree })),
    attributeDefs,
    valueMemberTypes,
  }
}
