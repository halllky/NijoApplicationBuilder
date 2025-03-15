using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public static SchemaParseContext Default() {
            var valueMemberTypes = new IValueMemberType[] {
                new ValueMemberTypes.Word(),
            };
            return new SchemaParseContext(valueMemberTypes);
        }

        private SchemaParseContext(IEnumerable<IValueMemberType> valueMemberTypes) {
            // スキーマ定義名重複チェック
            var groups = valueMemberTypes
                .GroupBy(t => t.SchemaTypeName)
                .ToArray();
            var duplicates = groups
                .Where(group => group.Count() >= 2)
                .ToArray();
            if (duplicates.Length > 0) {
                throw new InvalidOperationException($"型名 {string.Join(", ", duplicates.Select(x => x.Key))} が重複しています。");
            }

            _valueMemberTypes = groups.ToDictionary(g => g.Key, g => g.Single());
        }

        private const string ATTR_IS = "is";
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
        /// <summary>
        /// 参照先のXML要素を返します。
        /// </summary>
        internal XElement? FindRefTo(XElement xElement) {
            var isAttribute = ParseIsAttribute(xElement);
            var refToAttribute = isAttribute.FirstOrDefault(attr => attr.Key == IS_REFTO);
            if (refToAttribute == null) return null;

            var xPath = $"//{refToAttribute.Value}";
            return xElement.Document?.XPathSelectElement(xPath);
        }

        /// <summary>
        /// 集約ルートを返す。集約ルートは <see cref="XDocument.Root"/> の1つ下。
        /// </summary>
        internal static XElement GetAggregateRootElement(XElement xElement) {
            var current = xElement;
            while (true) {
                if (current.Parent == null) throw new InvalidOperationException();
                if (current.Parent == xElement.Document?.Root) return current;

                current = current.Parent;
            }
        }


        #region ノード種類
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

            /// <summary>
            /// 未知の値。例えば「静的区分の値の定義」など。
            /// この値が不正かどうか、例えば「動的区分（種類）」や「値オブジェクト定義」は子を持てない
            /// といったルールを担保するのは、ルート集約と対応するモデル定義の仕事とする。
            /// </summary>
            Unknown = 1 << 20,
        }
        internal E_NodeType GetNodeType(XElement xElement) {
            // ルート要素直下に定義されている場合はルート集約
            if (xElement.Parent == xElement.Document?.Root) {
                return E_NodeType.RootAggregate;
            }

            // Child
            var isAttributes = ParseIsAttribute(xElement).ToArray();
            if (isAttributes.Any(x => x.Key == IS_CHILD)) {
                return E_NodeType.ChildAggregate;
            }
            // Children
            if (isAttributes.Any(x => x.Key == IS_CHILDREN)) {
                return E_NodeType.ChildrenAggregate;
            }

            // RefTo
            if (isAttributes.Any(x => x.Key == IS_REFTO)) {
                return E_NodeType.Ref;
            }
            // ValueMember
            if (isAttributes.Any(x => TryResolveMemberType(xElement, out _))) {
                return E_NodeType.ValueMember;
            }

            return E_NodeType.Unknown;
        }
        #endregion ノード種類


        #region is属性
        private const string IS_DATA_MODEL = "data-model";
        private const string IS_QUERY_MODEL = "query-model";
        private const string IS_COMMAND_MODEL = "command-model";
        private const string IS_STATIC_ENUM_MODEL = "enum";
        private const string IS_DYNAMIC_ENUM_MODEL = "dynamic-enum";

        private const string IS_CHILD = "child";
        private const string IS_CHILDREN = "children";
        private const string IS_REFTO = "ref-to";

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
        /// <summary>
        /// XML要素がどのモデルに属するか
        /// </summary>
        internal IModel? FindModel(XElement xElement) {
            var root = GetAggregateRootElement(xElement);
            foreach (var attr in ParseIsAttribute(root)) {
                if (attr.Key == IS_DATA_MODEL) return DataModel;
                if (attr.Key == IS_QUERY_MODEL) return QueryModel;
                if (attr.Key == IS_COMMAND_MODEL) return CommandModel;
            }
            return null;
        }
        private IModel DataModel => _dataModel ??= new DataModel();
        private IModel QueryModel => _queryModel ??= new QueryModel();
        private IModel CommandModel => _commandModel ??= new CommandModel();
        private DataModel? _dataModel;
        private QueryModel? _queryModel;
        private CommandModel? _commandModel;
        #endregion is属性


        #region ValueMember型解決
        private readonly IReadOnlyDictionary<string, IValueMemberType> _valueMemberTypes;
        internal bool TryResolveMemberType(XElement xElement, out IValueMemberType valueMemberType) {
            var isAttribute = ParseIsAttribute(xElement).ToArray();
            foreach (var attr in isAttribute) {
                if (_valueMemberTypes.TryGetValue(attr.Key, out valueMemberType!)) {
                    return true;
                }
            }
            valueMemberType = null!;
            return false;
        }
        #endregion ValueMember型解決
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
