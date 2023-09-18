using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using HalApplicationBuilder.DotnetEx;
using System.Linq.Expressions;

namespace HalApplicationBuilder.Core {
    public class AppSchemaXml {
        internal AppSchemaXml(string projectRoot) {
            _projectRoot = projectRoot;
        }

        private readonly string _projectRoot;

        public string GetPath() {
            return Path.Combine(_projectRoot, "halapp.xml");
        }

        public XDocument Load() {
            var xmlFullPath = GetPath();
            using var stream = IO.OpenFileWithRetry(xmlFullPath);
            using var reader = new StreamReader(stream);
            var xmlContent = reader.ReadToEnd();
            var xDocument = XDocument.Parse(xmlContent);
            return xDocument;
        }

        internal bool ConfigureBuilder(AppSchemaBuilder builder, out ICollection<string> errors) {
            var xDocument = Load();
            if (xDocument.Root == null) {
                errors = new List<string> { "XMLが空です。" };
                return false;
            }

            var errorList = new List<string>();

            void Handle(ParsedXElement el, IEnumerable<string> ancestors) {
                const string VARIATION_KEY = "variation-key";
                var errorListLocal = new HashSet<string>();
                var path = ancestors.Concat(new[] { el.Source.Name.LocalName }).ToArray();

                // バリデーション
                var members = el.Source
                    .Elements()
                    .Select(inner => ParseAggregateXElement(inner, errorListLocal))
                    .ToArray();

                if (el.ElementType == E_XElementType.VariationContainer) {
                    var duplicates = el.Source
                        .Elements()
                        .Select(e => e.Attribute(VARIATION_KEY)?.Value)
                        .Where(str => str != null)
                        .Select(str => int.TryParse(str, out var i) ? i : (int?)null)
                        .GroupBy(i => i)
                        .Where(group => group.Key != null && group.Count() >= 2);
                    foreach (var group in duplicates) {
                        errorListLocal.Add($"Value of '{VARIATION_KEY}' of child of '{el.Source.Name.LocalName}' duplicates: {group.Key}");
                    }
                    foreach (var innerElement in el.Source.Elements()) {
                        if (innerElement.Attribute(VARIATION_KEY) == null) {
                            errorListLocal.Add($"Aggregate define '{innerElement.Name}' must have '{VARIATION_KEY}' attribute.");
                            continue;
                        }
                    }
                }

                errorList.AddRange(errorListLocal);

                // 登録
                if (el.ElementType == E_XElementType.RootAggregate
                 || el.ElementType == E_XElementType.ChildAggregate
                 || el.ElementType == E_XElementType.VariationValue) {

                    builder.AddAggregate(path, el.AggregateOption);
                }
                foreach (var member in members) {
                    if (member.ElementType == E_XElementType.Schalar
                     || member.ElementType == E_XElementType.Ref) {

                        builder.AddAggregateMember(path.Concat(new[] { member.Source.Name.LocalName }), member.MemberOption);
                    }
                }

                // 再帰
                foreach (var member in members) {
                    if (member.ElementType == E_XElementType.ChildAggregate
                     || member.ElementType == E_XElementType.VariationValue) {

                        Handle(member, path);
                    }
                }
            }

            builder.SetApplicationName(xDocument.Root.Name.LocalName);

            foreach (var xElement in xDocument.Root.Elements()) {
                // コンフィグ
                if (xElement.Name.LocalName == Config.XML_CONFIG_SECTION_NAME) continue;

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
                        if (strValue == null) {
                            enumValues.Add(new EnumValueOption { Name = enumName });
                        } else if (int.TryParse(strValue, out var intValue)) {
                            enumValues.Add(new EnumValueOption { Name = enumName, Value = intValue });
                        } else {
                            errorList.Add($"'{xElement.Name.LocalName}' の '{enumName}' の値 '{strValue}' を整数に変換できません。");
                        }
                    }

                    builder.AddEnum(xElement.Name.LocalName, enumValues);
                    continue;
                }

                // 集約定義
                var parsed = ParseAggregateXElement(xElement, errorList);
                Handle(parsed, Enumerable.Empty<string>());
            }
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

            parser.IfExists("master-data")
                .ElementTypeIs(E_XElementType.RootAggregate, E_Priority.Force);

            parser.IfExists("object")
                .ElementTypeIs(E_XElementType.ChildAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.IsArray, false, E_Priority.Force);
            parser.IfExists("array")
                .ElementTypeIs(E_XElementType.ChildAggregate, E_Priority.Force)
                .SetAggregateOption(opt => opt.IsArray, true, E_Priority.Force);
            parser.IfExists("variation")
                .ElementTypeIs(E_XElementType.VariationContainer, E_Priority.Force);
            parser.IfExists("ref-to")
                .ElementTypeIs(E_XElementType.Ref, E_Priority.Force)
                .SetMemberOption(opt => opt.IsReferenceTo, value => value, E_Priority.Force);

            parser.IfExists("key")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.IfNotSpecified)
                .SetMemberOption(opt => opt.IsPrimary, true, E_Priority.Force)
                .SetMemberOption(opt => opt.IsRequired, true, E_Priority.Force);
            parser.IfExists("name")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.IfNotSpecified)
                .SetMemberOption(opt => opt.IsDisplayName, true, E_Priority.Force)
                .SetMemberOption(opt => opt.MemberType, MemberTypeResolver.TYPE_WORD, E_Priority.IfNotSpecified);
            parser.IfExists("id")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.Force)
                .SetMemberOption(opt => opt.IsPrimary, true, E_Priority.IfNotSpecified)
                .SetMemberOption(opt => opt.IsRequired, true, E_Priority.IfNotSpecified)
                .SetMemberOption(opt => opt.MemberType, MemberTypeResolver.TYPE_ID, E_Priority.Force);
            parser.IfExists("word")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.Force)
                .SetMemberOption(opt => opt.MemberType, MemberTypeResolver.TYPE_WORD, E_Priority.Force);
            parser.IfExists("sentence")
                .ElementTypeIs(E_XElementType.Schalar, E_Priority.Force)
                .SetMemberOption(opt => opt.IsDisplayName, true, E_Priority.Force)
                .SetMemberOption(opt => opt.MemberType, MemberTypeResolver.TYPE_SENTENCE, E_Priority.Force);

            var elementType = parser.GetElementType();
            var aggregateOption = parser.CreateAggregateOption();
            var memberOption = parser.CreateMemberOption();

            // この時点で判断できない属性はenumかユーザー定義型とみなす
            if (parser.NotHandledKeys.Count >= 2) {
                errors.Add($"'{element.Name.LocalName}' に不明な属性が含まれています: {parser.NotHandledKeys.Join(" ")}");

            } else if (parser.NotHandledKeys.Count == 1
                && elementType == E_XElementType.Schalar
                && memberOption.MemberType == null) {

                memberOption.MemberType = parser.NotHandledKeys.Single();
            }

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

            public OptionValueSetterFactory IfExists(string key) {
                if (_xmlKeyValues.TryGetValue(key.ToLower(), out var value)) {
                    _notHandledKeys.Remove(key);
                    return new WhenKeyExists(this, value);
                } else {
                    return new OptionValueSetterFactory();
                }
            }
            public class OptionValueSetterFactory {
                public virtual OptionValueSetterFactory ElementTypeIs(E_XElementType elementType, E_Priority priority) => this;
                public virtual OptionValueSetterFactory SetAggregateOption<T>(Expression<Func<AggregateBuildOption, T>> memberSelector, Func<string, T> getValue, E_Priority priority) => this;
                public virtual OptionValueSetterFactory SetAggregateOption<T>(Expression<Func<AggregateBuildOption, T>> memberSelector, T value, E_Priority priority) => SetAggregateOption(memberSelector, _ => value, priority);
                public virtual OptionValueSetterFactory SetMemberOption<T>(Expression<Func<AggregateMemberBuildOption, T>> memberSelector, Func<string, T> getValue, E_Priority priority) => this;
                public virtual OptionValueSetterFactory SetMemberOption<T>(Expression<Func<AggregateMemberBuildOption, T>> memberSelector, T value, E_Priority priority) => SetMemberOption(memberSelector, _ => value, priority);
            }
            public class WhenKeyExists : OptionValueSetterFactory {
                public WhenKeyExists(XElementOptionCollection collection, string xmlIsAttributeValue) {
                    _collection = collection;
                    _xmlIsAttributeValue = xmlIsAttributeValue;
                }
                private readonly XElementOptionCollection _collection;
                private readonly string _xmlIsAttributeValue;

                public override OptionValueSetterFactory ElementTypeIs(E_XElementType elementType, E_Priority priority) {
                    _collection._elementTypeCandidates.Add((elementType, priority));
                    return this;
                }
                public override OptionValueSetterFactory SetAggregateOption<T>(Expression<Func<AggregateBuildOption, T>> memberSelector, Func<string, T> getValue, E_Priority priority) {
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
                public override OptionValueSetterFactory SetMemberOption<T>(Expression<Func<AggregateMemberBuildOption, T>> memberSelector, Func<string, T> getValue, E_Priority priority) {
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
