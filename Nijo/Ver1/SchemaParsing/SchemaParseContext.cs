using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Nijo.Ver1.SchemaParsing {
    /// <summary>
    /// XML要素をこのアプリケーションのルールに従って解釈する
    /// </summary>
    internal class SchemaParseContext {

        /// <summary>
        /// 既定の型解釈ルールでコンテキストを作成します。
        /// </summary>
        /// <returns></returns>
        public static SchemaParseContext Default(XDocument xDocument) {
            var models = new IModel[] {
                new DataModel(),
                new QueryModel(),
                new CommandModel(),
                new StaticEnumModel(),
            };
            var valueMemberTypes = new IValueMemberType[] {
                new ValueMemberTypes.Word(),
            };
            return new SchemaParseContext(xDocument, models, valueMemberTypes);
        }

        private SchemaParseContext(XDocument xDocument, IModel[] models, IValueMemberType[] valueMemberTypes) {
            // スキーマ定義名重複チェック
            var appearedName = new HashSet<string>();
            var duplicates = new HashSet<string>();
            foreach (var name in models.Select(m => m.SchemaName).Concat(valueMemberTypes.Select(t => t.SchemaTypeName))) {
                if (appearedName.Contains(name)) {
                    duplicates.Add(name);
                } else {
                    appearedName.Add(name);
                }
            }
            if (duplicates.Count > 0) {
                throw new InvalidOperationException($"型名 {string.Join(", ", duplicates)} が重複しています。");
            }

            _xDocument = xDocument;
            Models = models.ToDictionary(m => m.SchemaName);
            _valueMemberTypes = valueMemberTypes.ToDictionary(m => m.SchemaTypeName);
        }

        private readonly XDocument _xDocument;
        public IReadOnlyDictionary<string, IModel> Models { get; }
        private readonly IReadOnlyDictionary<string, IValueMemberType> _valueMemberTypes;

        private const string ATTR_IS = "is";
        internal const string ATTR_NODE_TYPE = "Type";
        private const string ATTR_DISPLAY_NAME = "DisplayName";
        private const string ATTR_DB_NAME = "DbName";
        private const string ATTR_LATIN_NAME = "LatinName";

        /// <summary>
        /// 物理名
        /// </summary>
        internal string GetPhysicalName(XElement xElement) {
            return xElement.Name.LocalName;
        }
        /// <summary>
        /// 表示名称
        /// </summary>
        internal string GetDisplayName(XElement xElement) {
            return xElement.Attribute(ATTR_DISPLAY_NAME)?.Value ?? xElement.Name.LocalName;
        }
        /// <summary>
        /// DB名
        /// </summary>
        internal string GetDbName(XElement xElement) {
            return xElement.Attribute(ATTR_DB_NAME)?.Value ?? xElement.Name.LocalName;
        }
        /// <summary>
        /// ラテン名
        /// </summary>
        internal string GetLatinName(XElement xElement) {
            return xElement.Attribute(ATTR_LATIN_NAME)?.Value ?? xElement.Name.LocalName.ToHashedString();
        }

        /// <summary>
        /// 兄弟要素の中で何番目か
        /// </summary>
        internal int GetIndexInSiblings(XElement xElement) {
            return xElement.ElementsBeforeSelf().Count();
        }

        #region Find系
        /// <summary>
        /// 参照先のXML要素を返します。
        /// </summary>
        internal XElement? FindRefTo(XElement xElement) {
            var type = xElement.Attribute(ATTR_NODE_TYPE) ?? throw new InvalidOperationException();
            var xPath = $"//{type.Value.Split(':')[1]}";
            return _xDocument.XPathSelectElement(xPath);
        }
        /// <summary>
        /// 引数の集約を参照している集約を探して返します。
        /// </summary>
        internal IEnumerable<XElement> FindRefFrom(XElement xElement) {
            // まずパフォーマンスのためXPathで高速に絞り込む
            var physicalName = GetPhysicalName(xElement);
            return _xDocument.XPathSelectElements($"//*[@{ATTR_NODE_TYPE}='{NODE_TYPE_REFTO}:{physicalName}']") ?? [];
        }

        /// <summary>
        /// 集約ルートを返す。集約ルートは <see cref="XDocument.Root"/> の1つ下。
        /// </summary>
        internal XElement GetAggregateRootElement(XElement xElement) {
            var current = xElement;
            while (true) {
                if (current.Parent == null) throw new InvalidOperationException();
                if (current.Parent == _xDocument.Root) return current;

                current = current.Parent;
            }
        }
        #endregion Find系


        #region ノード種類
        private const string NODE_TYPE_CHILD = "child";
        private const string NODE_TYPE_CHILDREN = "children";
        private const string NODE_TYPE_REFTO = "ref-to";

        /// <summary>
        /// XML要素の種類
        /// </summary>
        [Flags]
        internal enum E_NodeType {
            /// <summary>集約</summary>
            Aggregate = 1 << 0,
            /// <summary>集約メンバー</summary>
            AggregateMember = 1 << 1,
            /// <summary>Child, Children, Ref</summary>
            RelationMember = 1 << 2,

            /// <summary>ルート集約</summary>
            RootAggregate = Aggregate | 1 << 3,
            /// <summary>Child</summary>
            ChildAggregate = Aggregate | RelationMember | 1 << 4,
            /// <summary>Children</summary>
            ChildrenAggregate = Aggregate | RelationMember | 1 << 5,

            /// <summary>値メンバー</summary>
            ValueMember = AggregateMember | 1 << 6,
            /// <summary>外部参照（ref-to）</summary>
            Ref = AggregateMember | RelationMember | 1 << 7,

            /// <summary>静的区分の種類</summary>
            StaticEnumType = RootAggregate | 1 << 8,
            /// <summary>静的区分の値</summary>
            StaticEnumValue = 1 << 9,

            /// <summary>
            /// 未知の値
            /// </summary>
            Unknown = 1 << 20,
        }
        /// <summary>
        /// XML要素の種類を判定する。
        /// 編集途中の不正な状態であっても入力内容検査等に使う必要があるため、
        /// XML要素が不正であっても例外を出さない。
        /// </summary>
        internal E_NodeType GetNodeType(XElement xElement) {
            // ルート要素直下に定義されている場合はルート集約
            if (xElement.Parent == _xDocument.Root) {
                return E_NodeType.RootAggregate;
            }
            // 親がenumなら静的区分の値
            if (xElement.Parent != null
             && xElement.Parent.Parent == _xDocument.Root
             && xElement.Parent.Attribute(ATTR_NODE_TYPE)?.Value == EnumDefParser.SCHEMA_NAME) {
                return E_NodeType.StaticEnumValue;
            }

            // 以降はType属性の値で区別
            var type = xElement.Attribute(ATTR_NODE_TYPE);
            if (type == null) {
                return E_NodeType.Unknown;
            }

            // Child
            if (type.Value == NODE_TYPE_CHILD) {
                return E_NodeType.ChildAggregate;
            }
            // Children
            if (type.Value == NODE_TYPE_CHILDREN) {
                return E_NodeType.ChildrenAggregate;
            }
            // RefTo
            if (type.Value.StartsWith(NODE_TYPE_REFTO)) {
                return E_NodeType.Ref;
            }
            // ValueMember
            if (TryResolveMemberType(xElement, out _)) {
                return E_NodeType.ValueMember;
            }
            return E_NodeType.Unknown;
        }
        #endregion ノード種類


        #region is属性
        private const string IS_DYNAMIC_ENUM_MODEL = "dynamic-enum";

        /// <summary>
        /// is="" 属性の内容
        /// </summary>
        internal IEnumerable<IsAttribute> ParseIsAttribute(XElement xElement) {
            var isAttributeValue = xElement.Attribute(ATTR_IS)?.Value;

            if (string.IsNullOrWhiteSpace(isAttributeValue)) yield break;

            var parsingKey = false;
            var parsingValue = false;
            var key = new List<char>();
            var value = new List<char>();
            foreach (var character in isAttributeValue) {
                // 半角スペースが登場したら属性の区切り、コロンが登場したらキーと値の区切り、と解釈する。
                if (character == ' ') {
                    if (parsingKey || parsingValue) {
                        yield return new IsAttribute {
                            Key = new string(key.ToArray()),
                            Value = new string(value.ToArray()),
                        };
                        key.Clear();
                        value.Clear();
                        parsingKey = false;
                        parsingValue = false;
                    }

                } else if (character == ':') {
                    if (!parsingValue) {
                        parsingKey = false;
                        parsingValue = true;
                    }

                } else {
                    if (parsingKey) {
                        key.Add(character);
                    } else if (parsingValue) {
                        value.Add(character);
                    } else {
                        parsingKey = true;
                        key.Add(character);
                    }
                }
            }
            if (parsingKey || parsingValue) {
                yield return new IsAttribute {
                    Key = new string(key.ToArray()),
                    Value = new string(value.ToArray()),
                };
            }
        }
        #endregion is属性


        #region ImmutableSchemaへの変換
        /// <summary>
        /// このスキーマで定義されている静的区分の種類を返します。
        /// </summary>
        /// <returns></returns>
        private IReadOnlyDictionary<string, ValueMemberTypes.StaticEnumMember> GetStaticEnumMembers() {
            return _xDocument.Root
                ?.Elements()
                .Where(el => el.Attribute(ATTR_NODE_TYPE)?.Value == EnumDefParser.SCHEMA_NAME)
                .ToDictionary(GetPhysicalName, el => new ValueMemberTypes.StaticEnumMember(el, this))
                ?? [];
        }
        /// <summary>
        /// スキーマ解釈ルールとしてあらかじめ定められた値種別および静的区分の種類の一覧を返します。
        /// </summary>
        public IEnumerable<IValueMemberType> GetValueMemberTypes() {
            // 単語型など予め登録された型
            foreach (var type in _valueMemberTypes.Values) {
                yield return type;
            }
            // 列挙体
            foreach (var type in GetStaticEnumMembers().Values) {
                yield return type;
            }
        }
        /// <summary>
        /// ValueMemberを表すXML要素の種別（日付, 数値, ...等）を判別して返します。
        /// </summary>
        internal bool TryResolveMemberType(XElement xElement, out IValueMemberType valueMemberType) {
            var type = xElement.Attribute(ATTR_NODE_TYPE);
            if (type == null) {
                valueMemberType = null!;
                return false;
            }

            // 単語型など予め登録された型
            if (_valueMemberTypes.TryGetValue(type.Value, out var vmType)) {
                valueMemberType = vmType;
                return true;
            }

            // 列挙体
            if (GetStaticEnumMembers().TryGetValue(type.Value, out var enumMember)) {
                valueMemberType = enumMember;
                return true;
            }

            // 解決できなかった
            valueMemberType = null!;
            return false;
        }
        /// <summary>
        /// ルート集約や子集約を表すXML要素を <see cref="AggregateBase"/> のインスタンスに変換します。
        /// XML要素が集約を表すもので無かった場合は例外を送出します。
        /// </summary>
        internal AggregateBase ToAggregateBase(XElement xElement, ISchemaPathNode? previous) {
            var nodeType = GetNodeType(xElement);
            if (nodeType == E_NodeType.RootAggregate) {
                return new RootAggregate(xElement, this, previous);
            }
            if (nodeType == E_NodeType.ChildAggregate) {
                return new ChildAggreagte(xElement, this, previous);
            }
            if (nodeType == E_NodeType.ChildrenAggregate) {
                return new ChildrenAggreagte(xElement, this, previous);
            }
            throw new InvalidOperationException($"集約ではありません: {xElement}");
        }
        #endregion ImmutableSchemaへの変換
    }


    /// <summary>
    /// is="" の中身。半角スペースで区切られる設定値1個分。
    /// </summary>
    internal class IsAttribute {
        internal required string Key { get; init; }
        internal required string Value { get; init; }

        public override string ToString() {
            return string.IsNullOrWhiteSpace(Value)
                ? Key
                : $"{Key}:{Value}";
        }
    }
}
