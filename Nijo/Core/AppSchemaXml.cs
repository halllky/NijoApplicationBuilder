using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using Nijo.Util.DotnetEx;
using System.Linq.Expressions;
using System.Xml;

namespace Nijo.Core {
    public class AppSchemaXml {
        internal AppSchemaXml(XDocument xDocument, string projectRoot) {
            _projectRoot = projectRoot;
            _xDocument = xDocument;
        }

        private readonly string _projectRoot;
        private readonly XDocument _xDocument;

        public const string INCLUDE = "Include";
        public const string PATH = "Path";

        internal bool ConfigureBuilder(AppSchemaBuilder builder, out ICollection<string> errors) {
            if (_xDocument.Root == null) {
                errors = new List<string> { "XMLが空です。" };
                return false;
            }

            var errorList = new List<string>();

            builder.SetApplicationName(_xDocument.Root.Name.LocalName);

            void RegisterXmlElement(XDocument xDocument, string documentDirectory) {
                if (xDocument.Root == null) return;

                foreach (var xElement in xDocument.Root.Elements()) {
                    // コンフィグ
                    if (xElement.Name.LocalName == Config.XML_CONFIG_SECTION_NAME) continue;

                    // Include Path="ファイルパス"で指定された.xmlを読み込む
                    if (xElement.Name.LocalName == INCLUDE) {
                        var schemaFilePath = xElement.Attribute(XName.Get(PATH))?.Value;
                        if (schemaFilePath == null) {
                            errorList.Add($"{PATH}属性が設定されておりません。");
                            continue;
                        }
                        var path = Path.GetFullPath(Path.Combine(documentDirectory, schemaFilePath));
                        if (!File.Exists(path)) {
                            errorList.Add($"ファイルが存在しません: {path}");
                            continue;
                        }
                        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var reader = new StreamReader(stream);
                        var xmlContent = reader.ReadToEnd();
                        XDocument wDocument;
                        try {
                            wDocument = XDocument.Parse(xmlContent);
                        } catch (XmlException ex) {
                            errorList.Add($"XMLの内容が不正です: {ex.Message}");
                            continue;
                        }
                        RegisterXmlElement(wDocument, Path.GetDirectoryName(path) ?? path);
                        continue;
                    }

                    // 列挙体
                    const string ENUM = "enum";
                    var isAttr = ToKeyValues(xElement, new List<string>());
                    if (isAttr.ContainsKey(ENUM)) {
                        foreach (var kv in isAttr) {
                            if (kv.Key != ENUM) errorList.Add($"列挙体定義に属性 '{kv.Key}' を指定できません。");
                        }

                        var enumValues = new List<EnumValueOption>();
                        foreach (var innerElement in xElement.Elements()) {
                            var enumName = innerElement.Name.LocalName;
                            var strValue = innerElement.Attribute("key")?.Value;

                            int? intValue;
                            if (strValue == null) {
                                intValue = null;
                            } else if (int.TryParse(strValue, out var i)) {
                                intValue = i;
                            } else {
                                errorList.Add($"'{xElement.Name.LocalName}' の '{enumName}' の値 '{strValue}' を整数に変換できません。");
                                continue;
                            }

                            enumValues.Add(new EnumValueOption {
                                PhysicalName = enumName,
                                Value = intValue,
                                DisplayName = innerElement.Attribute("DisplayName")?.Value,
                            });
                        }

                        builder.AddEnum(xElement.Name.LocalName, enumValues);
                        continue;
                    }

                    // 集約定義
                    void HandleAggregateElementRecursively(XElement el, IEnumerable<string> parent) {
                        var path = parent.Concat(new[] { el.Name.LocalName }).ToArray();
                        var parsed = ParseAggregateXElement(el, errorList);
                        switch (parsed.ElementType) {
                            case E_XElementType.RootAggregate:
                            case E_XElementType.ChildAggregate:
                            case E_XElementType.VariationValue:
                                builder.AddAggregate(path, parsed.AggregateOption);
                                break;

                            case E_XElementType.Schalar:
                            case E_XElementType.Ref:
                                builder.AddAggregateMember(path, parsed.MemberOption);
                                break;

                            case E_XElementType.VariationContainer:
                                // variation container は集約グラフ上は存在しないものとして扱う
                                path = parent.ToArray();
                                break;

                            default:
                                break;
                        }
                        foreach (var innerElement in el.Elements()) {
                            HandleAggregateElementRecursively(innerElement, path);
                        }
                    }
                    HandleAggregateElementRecursively(xElement, Enumerable.Empty<string>());
                }
            }
            RegisterXmlElement(_xDocument, _projectRoot);
            errors = errorList;
            return errors.Count == 0;
        }

        /// <summary>
        /// 集約または集約メンバーとして解釈されたXElement
        /// </summary>
        private class ParsedXElement {
            public required XElement Source { get; init; }
            public required E_XElementType ElementType { get; init; }
            public required AggregateBuildOption AggregateOption { get; init; }
            public required AggregateMemberBuildOption MemberOption { get; init; }
        }
        private enum E_XElementType {
            RootAggregate,
            ChildAggregate,
            VariationContainer,
            VariationValue,
            Schalar,
            Ref,
        }
        public enum E_Priority {
            Force,
            IfNotSpecified,
        }

        /// <summary>
        /// is属性をstringの辞書に変換
        /// </summary>
        private static IReadOnlyDictionary<string, string> ToKeyValues(XElement element, ICollection<string> errors) {
            var isAttribute = element.Attribute("is")?.Value ?? string.Empty;
            var splitted = isAttribute.Split(' ', '　').ToArray();
            var keyValues = new Dictionary<string, string>();

            string? key = null;
            string? value = null;
            for (int i = 0; i < splitted.Length; i++) {
                if (string.IsNullOrWhiteSpace(splitted[i])) continue;

                var separated = new Queue<string>(splitted[i].Split(':'));
                while (separated.TryDequeue(out var text)) {
                    if (string.IsNullOrWhiteSpace(text)) {
                        continue;
                    } else if (key == null) {
                        key = text;
                    } else if (value == null) {
                        value = text;
                    } else {
                        errors.Add($"'{element.Name}' に':'を複数含む属性があるため '{text}' がキーか値かを判別できません。");
                        key = null;
                        value = null;
                    }
                }
                if (key != null) {
                    keyValues.Add(key, value ?? string.Empty);
                    key = null;
                    value = null;
                }
            }

            return keyValues;
        }

        /// <summary>
        /// XElementを集約または集約メンバーとして解釈する。
        /// is="" で複数の値を指定したときに設定が競合したりidを自動的に主キーと推測したりする仕様が複雑なのでそこも考慮している
        /// </summary>
        private static ParsedXElement ParseAggregateXElement(XElement element, ICollection<string> errors) {
            var keyValues = ToKeyValues(element, errors);
            var parser = new XElementOptionCollection(keyValues, errors.Add);

            // ------------------------------------------------
            // ルート集約用の属性 ここから

            parser.IfExists(NijoCodeGenerator.Models.WriteModel2.Key)
                .ElementTypeIs(E_XElementType.RootAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.Handler, NijoCodeGenerator.Models.WriteModel2.Key, E_Priority.Force);
            parser.IfExists(NijoCodeGenerator.Models.ReadModel2.Key)
                .ElementTypeIs(E_XElementType.RootAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.Handler, NijoCodeGenerator.Models.ReadModel2.Key, E_Priority.Force);
            parser.IfExists(NijoCodeGenerator.Models.CommandModel.Key)
                .ElementTypeIs(E_XElementType.RootAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.Handler, NijoCodeGenerator.Models.CommandModel.Key, E_Priority.Force);
            parser.IfExists(NijoCodeGenerator.Models.ValueObjectModel.Key)
                .ElementTypeIs(E_XElementType.RootAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.Handler, NijoCodeGenerator.Models.ValueObjectModel.Key, E_Priority.Force);

            parser.IfExists("generate-default-read-model")
                .SetAggregateOption(opt => opt.GenerateDefaultReadModel, true, E_Priority.Force);

            parser.IfExists("form-label-width")
                .SetAggregateOption(opt => opt.EstimatedLabelWidth, value => decimal.TryParse(value, out var d) ? d : throw new InvalidOperationException($"{element.Name}: form-label-width の値が数値で指定されていません。"), E_Priority.Force);

            // 非推奨。VForm2のレイアウトが変わったことによりこのオプションが無価値になったため。
            parser.IfExists("form-depth")
                .SetAggregateOption(opt => opt.FormDepth, value => int.TryParse(value, out var i) ? i : throw new InvalidOperationException($"{element.Name}: form-depth の値が数値で指定されていません。"), E_Priority.Force);

            // ------------------------------------------------
            // 子孫集約用の属性 ここから

            parser.IfExists("child")
                .ElementTypeIs(E_XElementType.ChildAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.IsArray, false, E_Priority.Force);
            parser.IfExists("children")
                .ElementTypeIs(E_XElementType.ChildAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.IsArray, true, E_Priority.Force);

            parser.IfExists("variation")
                .ElementTypeIs(E_XElementType.VariationContainer, E_Priority.Force);
            parser.IfExists("variation-item")
                .ElementTypeIs(E_XElementType.ChildAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.IsVariationGroupMember, variationKey => new() { GroupName = element.Parent?.Name.LocalName ?? string.Empty, Key = variationKey }, E_Priority.Force);

            // childと同じ。非推奨
            parser.IfExists("section")
                .ElementTypeIs(E_XElementType.ChildAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.IsArray, false, E_Priority.Force);
            // childrenと同じ。非推奨
            parser.IfExists("array")
                .ElementTypeIs(E_XElementType.ChildAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.IsArray, true, E_Priority.Force);
            // variation-itemと同じ。非推奨
            parser.IfExists("variation-key")
                .ElementTypeIs(E_XElementType.ChildAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.IsVariationGroupMember, variationKey => new() { GroupName = element.Parent?.Name.LocalName ?? string.Empty, Key = variationKey }, E_Priority.Force);

            // コマンド用のステップ属性
            parser.IfExists("step")
                .ElementTypeIs(E_XElementType.ChildAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.IsArray, false, E_Priority.Force)
                .SetAggregateOption(opt => opt.Step, v => int.TryParse(v, out var step) ? step : 0, E_Priority.Force);

            parser.IfExists("has-lifecycle")
                .SetAggregateOption(opt => opt.HasLifeCycle, true, E_Priority.Force);
            parser.IfExists("readonly")
                .SetAggregateOption(opt => opt.IsReadOnlyAggregate, true, E_Priority.Force);

            // ------------------------------------------------
            // 集約メンバー用の属性 ここから

            parser.IfExists("ref-to")
                .ElementTypeIs(E_XElementType.Ref, E_Priority.Force)
                .SetMemberOption(opt => opt.IsReferenceTo, value => value, E_Priority.Force);

            parser.IfExists("key")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.IfNotSpecified)
                .SetAggregateOption(opt => opt.IsPrimary, true, E_Priority.Force)
                .SetMemberOption(opt => opt.IsPrimary, true, E_Priority.Force)
                .SetMemberOption(opt => opt.IsRequired, true, E_Priority.Force);
            parser.IfExists("name")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.IfNotSpecified)
                .SetMemberOption(opt => opt.IsDisplayName, true, E_Priority.Force)
                .SetMemberOption(opt => opt.IsNameLike, true, E_Priority.IfNotSpecified)
                .SetMemberOption(opt => opt.MemberType, MemberTypeResolver.TYPE_WORD, E_Priority.IfNotSpecified);

            parser.IfExists("required")
                .SetAggregateOption(opt => opt.IsRequiredArray, true, E_Priority.Force)
                .SetMemberOption(opt => opt.IsRequired, true, E_Priority.Force);

            // 廃止予定 非推奨
            parser.IfExists("name-like")
                .SetMemberOption(opt => opt.IsNameLike, true, E_Priority.Force);

            parser.IfExists("uuid")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.Force)
                .SetMemberOption(opt => opt.IsPrimary, true, E_Priority.IfNotSpecified)
                .SetMemberOption(opt => opt.IsRequired, true, E_Priority.IfNotSpecified)
                .SetMemberOption(opt => opt.InvisibleInGui, true, E_Priority.IfNotSpecified)
                .SetMemberOption(opt => opt.MemberType, MemberTypeResolver.TYPE_UUID, E_Priority.Force);

            parser.IfExists("word")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.Force)
                .SetMemberOption(opt => opt.MemberType, MemberTypeResolver.TYPE_WORD, E_Priority.Force);
            parser.IfExists("sentence")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.Force)
                .SetMemberOption(opt => opt.IsDisplayName, true, E_Priority.Force)
                .SetMemberOption(opt => opt.MemberType, MemberTypeResolver.TYPE_SENTENCE, E_Priority.Force);
            parser.IfExists("int")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.Force)
                .SetMemberOption(opt => opt.MemberType, MemberTypeResolver.TYPE_INT, E_Priority.Force);

            parser.IfExists("hidden")
                .SetMemberOption(opt => opt.InvisibleInGui, true, E_Priority.Force);

            // UIコンポーネントの注入
            parser.IfExists("single-view-ui")
                .SetMemberOption(opt => opt.SingleViewCustomUiComponentName, componentName => componentName, E_Priority.Force);
            parser.IfExists("search-condition-ui")
                .SetMemberOption(opt => opt.SearchConditionCustomUiComponentName, componentName => componentName, E_Priority.Force);

            // 列挙体のUI
            parser.IfExists("combo")
                .SetAggregateOption(opt => opt.IsCombo, true, E_Priority.Force) // バリエーション用
                .SetMemberOption(opt => opt.IsCombo, true, E_Priority.Force); // EnumList用
            parser.IfExists("radio")
                .SetAggregateOption(opt => opt.IsRadio, true, E_Priority.Force) // バリエーション用
                .SetMemberOption(opt => opt.IsRadio, true, E_Priority.Force); // EnumList用

            // レイアウト
            parser.IfExists("width")
                .SetMemberOption(opt => opt.UiWidthRem, value => {
                    return new TextBoxWidth {
                        ZenHan = value.FirstOrDefault() switch {
                            'z' => TextBoxWidth.E_ZenHan.Zenkaku,
                            'h' => TextBoxWidth.E_ZenHan.Hankaku,
                            _ => throw new InvalidOperationException($"{element.Name}: widthの指定が誤りです。全角10文字なら'z10', 半角10文字なら'h10'のようにzかhをつけて指定してください。"),
                        },
                        CharCount = int.TryParse(value.AsSpan(1), out var charCount)
                            ? charCount
                            : throw new InvalidOperationException($"{element.Name}: widthの2文字目以降を数値として解釈できません。"),
                    };
                }, E_Priority.Force);
            parser.IfExists("wide")
                .SetMemberOption(opt => opt.WideInVForm, true, E_Priority.Force);

            // ------------------------------------------------
            var elementType = parser.GetElementType();
            var aggregateOption = parser.CreateAggregateOption();
            var memberOption = parser.CreateMemberOption();

            // ------------------------------------------------
            // is="" 以外の属性

            aggregateOption.DisplayName = element.Attribute(XName.Get("DisplayName"))?.Value;
            aggregateOption.DbName = element.Attribute(XName.Get("DbName"))?.Value;
            aggregateOption.LatinName = element.Attribute(XName.Get("Latin"))?.Value;

            memberOption.DisplayName = element.Attribute(XName.Get("DisplayName"))?.Value;
            memberOption.DbName = element.Attribute(XName.Get("DbName"))?.Value;

            // ------------------------------------------------
            // この時点で判断できない属性はenumかユーザー定義型とみなす
            if (parser.NotHandledKeys.Count >= 2) {
                errors.Add($"'{element.Name.LocalName}' に不明な属性が含まれています: {parser.NotHandledKeys.Join(" ")}");

            } else if (parser.NotHandledKeys.Count == 1
                && elementType == E_XElementType.Schalar
                && memberOption.MemberType == null) {

                memberOption.MemberType = parser.NotHandledKeys.Single();
            }

            // ------------------------------------------------

            return new ParsedXElement {
                Source = element,
                ElementType = elementType,
                AggregateOption = aggregateOption,
                MemberOption = memberOption,
            };
        }

        /// <summary>
        /// XMLのisに指定できる各要素の仕様を簡単に書けるようにするための仕組み
        /// </summary>
        private class XElementOptionCollection {
            public XElementOptionCollection(IReadOnlyDictionary<string, string> xmlKeyValues, Action<string> onError) {
                _xmlKeyValues = xmlKeyValues;
                _notHandledKeys = xmlKeyValues.Keys.ToHashSet();
                _onError = onError;
            }

            private readonly IReadOnlyDictionary<string, string> _xmlKeyValues;
            private readonly Action<string> _onError;
            private readonly List<(E_XElementType ElementType, E_Priority Priority)> _elementTypeCandidates = new();
            private readonly List<OptionValueSetter> _aggregateOptionCandidates = new();
            private readonly List<OptionValueSetter> _memberOptionCandidates = new();

            private readonly HashSet<string> _notHandledKeys;
            public IReadOnlySet<string> NotHandledKeys => _notHandledKeys;

            public E_XElementType GetElementType() {
                if (TryGetOne(_elementTypeCandidates, x => x.Priority, x => x.ElementType, out var item)) {
                    return item.ElementType;
                } else {
                    return E_XElementType.Schalar;
                }
            }
            public AggregateBuildOption CreateAggregateOption() {
                var option = new AggregateBuildOption();
                foreach (var group in _aggregateOptionCandidates.GroupBy(x => x.PropertyInfo)) {
                    if (!TryGetOne(group, x => x.Priority, x => x.Value, out var item)) continue;
                    var propertyInfo = group.Key;
                    propertyInfo.SetValue(option, item!.Value);
                }
                return option;
            }
            public AggregateMemberBuildOption CreateMemberOption() {
                var option = new AggregateMemberBuildOption();
                foreach (var group in _memberOptionCandidates.GroupBy(x => x.PropertyInfo)) {
                    if (!TryGetOne(group, x => x.Priority, x => x.Value, out var item)) continue;
                    var propertyInfo = group.Key;
                    propertyInfo.SetValue(option, item!.Value);
                }
                return option;
            }

            /// <summary>
            /// 優先順位の指定に従って有効な設定1個を返す
            /// </summary>
            private bool TryGetOne<TListItem, TValue>(
                IEnumerable<TListItem> list,
                Func<TListItem, E_Priority> prioritySelector,
                Func<TListItem, TValue> valueSelector,
                out TListItem? mostPrioritizedItem) {

                var items = list.Select(item => new {
                    ListItem = item,
                    Priority = prioritySelector(item),
                    Value = valueSelector(item),
                }).ToArray();

                var force = items
                    .Where(x => x.Priority == E_Priority.Force)
                    .GroupBy(x => x.Value)
                    .ToArray();
                if (force.Length >= 2) {
                    _onError("矛盾する値が指定されています。");
                    mostPrioritizedItem = default;
                    return false;
                } else if (force.Length == 1) {
                    mostPrioritizedItem = force[0].First().ListItem;
                    return true;
                }

                var ifNotSpecified = items
                    .Where(x => x.Priority == E_Priority.IfNotSpecified)
                    .ToArray();
                if (ifNotSpecified.Length >= 1) {
                    mostPrioritizedItem = ifNotSpecified[0].ListItem;
                    return true;
                }

                mostPrioritizedItem = default;
                return false;
            }

            public IOptionValueHandler IfExists(string key) {
                if (_xmlKeyValues.TryGetValue(key.ToLower(), out var value)) {
                    _notHandledKeys.Remove(key);
                    return new WhenKeyExists(this, value);
                } else {
                    return new WhenKeyNotExists();
                }
            }

            public interface IOptionValueHandler {
                IOptionValueHandler ElementTypeIs(E_XElementType elementType, E_Priority priority);
                IOptionValueHandler SetAggregateOption<T>(Expression<Func<AggregateBuildOption, T>> memberSelector, Func<string, T> getValue, E_Priority priority);
                IOptionValueHandler SetAggregateOption<T>(Expression<Func<AggregateBuildOption, T>> memberSelector, T value, E_Priority priority) => SetAggregateOption(memberSelector, _ => value, priority);
                IOptionValueHandler SetMemberOption<T>(Expression<Func<AggregateMemberBuildOption, T>> memberSelector, Func<string, T> getValue, E_Priority priority);
                IOptionValueHandler SetMemberOption<T>(Expression<Func<AggregateMemberBuildOption, T>> memberSelector, T value, E_Priority priority) => SetMemberOption(memberSelector, _ => value, priority);
            }
            public class WhenKeyNotExists : IOptionValueHandler {
                public virtual IOptionValueHandler ElementTypeIs(E_XElementType elementType, E_Priority priority) => this;
                public virtual IOptionValueHandler SetAggregateOption<T>(Expression<Func<AggregateBuildOption, T>> memberSelector, Func<string, T> getValue, E_Priority priority) => this;
                public virtual IOptionValueHandler SetAggregateOption<T>(Expression<Func<AggregateBuildOption, T>> memberSelector, T value, E_Priority priority) => SetAggregateOption(memberSelector, _ => value, priority);
                public virtual IOptionValueHandler SetMemberOption<T>(Expression<Func<AggregateMemberBuildOption, T>> memberSelector, Func<string, T> getValue, E_Priority priority) => this;
                public virtual IOptionValueHandler SetMemberOption<T>(Expression<Func<AggregateMemberBuildOption, T>> memberSelector, T value, E_Priority priority) => SetMemberOption(memberSelector, _ => value, priority);
            }
            public class WhenKeyExists : IOptionValueHandler {
                public WhenKeyExists(XElementOptionCollection collection, string xmlIsAttributeValue) {
                    _collection = collection;
                    _xmlIsAttributeValue = xmlIsAttributeValue;
                }
                private readonly XElementOptionCollection _collection;
                private readonly string _xmlIsAttributeValue;

                public IOptionValueHandler ElementTypeIs(E_XElementType elementType, E_Priority priority) {
                    _collection._elementTypeCandidates.Add((elementType, priority));
                    return this;
                }
                public IOptionValueHandler SetAggregateOption<T>(Expression<Func<AggregateBuildOption, T>> memberSelector, Func<string, T> getValue, E_Priority priority) {
                    if (memberSelector.Body is not MemberExpression memberExpression
                     || memberExpression.Member is not PropertyInfo propertyInfo
                     || propertyInfo.DeclaringType != typeof(AggregateBuildOption))
                        throw new InvalidOperationException($"{nameof(SetAggregateOption)}の第1引数に指定できるのは{nameof(AggregateBuildOption)}のプロパティ指定の式のみ");

                    _collection._aggregateOptionCandidates.Add(new OptionValueSetter {
                        PropertyInfo = propertyInfo,
                        Value = getValue(_xmlIsAttributeValue),
                        Priority = priority,
                    });
                    return this;
                }
                public IOptionValueHandler SetMemberOption<T>(Expression<Func<AggregateMemberBuildOption, T>> memberSelector, Func<string, T> getValue, E_Priority priority) {
                    if (memberSelector.Body is not MemberExpression memberExpression
                     || memberExpression.Member is not PropertyInfo propertyInfo
                     || propertyInfo.DeclaringType != typeof(AggregateMemberBuildOption))
                        throw new InvalidOperationException($"{nameof(SetAggregateOption)}の第1引数に指定できるのは{nameof(AggregateMemberBuildOption)}のプロパティ指定の式のみ");

                    _collection._memberOptionCandidates.Add(new OptionValueSetter {
                        PropertyInfo = propertyInfo,
                        Value = getValue(_xmlIsAttributeValue),
                        Priority = priority,
                    });
                    return this;
                }
            }

            private class OptionValueSetter {
                public required PropertyInfo PropertyInfo { get; init; }
                public required object? Value { get; init; }
                public required E_Priority Priority { get; init; }
            }
        }
    }
}
