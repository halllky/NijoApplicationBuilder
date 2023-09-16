using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.Core.AppSchemaBuilder;
using System.Xml.Linq;
using System.IO;
using HalApplicationBuilder.DotnetEx;

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
                    .Select(inner => IsAttributeParser.Parse(inner, errorListLocal))
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

                    builder.AddAggregate(path, options => {

                        if (el.ElementType == E_XElementType.ChildAggregate && el.IsMultipleChildAggregate) {
                            options.IsArray();
                        }
                        if (el.ElementType == E_XElementType.VariationValue) {
                            options.IsPrimary(el.IsKey);
                            options.IsVariationGroupMember(
                                groupName: el.Source.Parent?.Name.LocalName ?? string.Empty,
                                key: el.Source.Attribute(VARIATION_KEY)?.Value ?? string.Empty);
                        }
                    });
                }
                foreach (var member in members) {
                    if (member.ElementType == E_XElementType.Schalar
                     || member.ElementType == E_XElementType.Ref) {

                        builder.AddAggregateMember(path.Concat(new[] { member.Source.Name.LocalName }), option => {
                            option.IsPrimary(member.IsKey);
                            option.IsDisplayName(member.IsName);
                            option.IsRequired(member.IsRequired);
                            option.MemberType(member.AggregateMemberTypeName);

                            if (member.ElementType == E_XElementType.Ref) {
                                option.IsReferenceTo(member.RefTargetName);
                            }
                        });
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
                var isAttr = IsAttributeParser.ToKeyValues(xElement, new List<string>());
                if (isAttr.ContainsKey(ENUM)) {
                    foreach (var kv in isAttr) {
                        if (kv.Key != ENUM) errorList.Add($"列挙体定義に属性 '{kv.Key}' を指定できません。");
                    }
                    builder.AddEnum(xElement.Name.LocalName, options => {
                        foreach (var innerElement in xElement.Elements()) {
                            var enumName = innerElement.Name.LocalName;
                            var strValue = innerElement.Attribute("key")?.Value;
                            if (strValue == null) {
                                options.AddMember(enumName);
                            } else if (int.TryParse(strValue, out var intValue)) {
                                options.AddMember(enumName, intValue);
                            } else {
                                errorList.Add($"'{xElement.Name.LocalName}' の '{enumName}' の値 '{strValue}' を整数に変換できません。");
                            }
                        }
                    });
                    continue;
                }

                // 集約定義
                var parsed = IsAttributeParser.Parse(xElement, errorList);
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
            public required bool IsMultipleChildAggregate { get; init; }
            public required bool IsKey { get; init; }
            public required bool IsName { get; init; }
            public required bool IsRequired { get; init; }
            public required string AggregateMemberTypeName { get; init; }

            public required string RefTargetName { get; init; }
        }
        private enum E_XElementType {
            RootAggregate,
            ChildAggregate,
            VariationContainer,
            VariationValue,
            Schalar,
            Ref,
        }

        /// <summary>
        /// is="" で複数の値を指定したときに設定が競合したりidを自動的に主キーと推測したりする仕様が複雑なのでそれを簡略化するための仕組み
        /// </summary>
        private abstract class IsAttributeParser {
            /// <summary>
            /// XElementを集約または集約メンバーとして解釈する
            /// </summary>
            public static ParsedXElement Parse(XElement element, ICollection<string> errors) {

                // 各値のハンドラを決定
                var keyValues = ToKeyValues(element, errors);
                var attributeTypes = Enumerate().ToDictionary(attr => attr.GetType().GetCustomAttribute<IsAttribute>()!.Value);
                var handlers = new HashSet<IsAttributeParser>();
                foreach (var kv in keyValues) {
                    IsAttributeParser? handler = null;
                    if (attributeTypes.TryGetValue(kv.Key.ToLower(), out handler)) {
                        handler.Value = kv.Value;
                    } else {
                        handler = new OtherAttr { Value = kv.Key };
                    }

                    if (handlers.Contains(handler)) {
                        errors.Add($"'{element.Name}' に '{kv.Key}' が複数指定されています。");
                        continue;
                    }
                    handlers.Add(handler);
                }

                // 各指定間で矛盾がないかを調べて返す
                var isAttribute = element.Attribute(IS)?.Value ?? string.Empty;
                bool specified;
                var elementType = Parse(handlers.Select(h => h.ElementType), out specified, err => errors.Add($"'{element.Name.LocalName}' 種別の指定でエラー: {err} ('{isAttribute}')"));
                if (!specified) elementType = E_XElementType.Schalar;

                var multiple = Parse(handlers.Select(h => h.IsMultipleChildAggregate), out specified, err => errors.Add($"'{element.Name.LocalName}' エラー: {err} ('{isAttribute}')"));
                if (!specified) multiple = false;

                var isKey = Parse(handlers.Select(h => h.IsKey), out specified, err => errors.Add($"'{element.Name.LocalName}' キーか否かの指定でエラー: {err} ('{isAttribute}')"));
                if (!specified) isKey = false;

                var isName = Parse(handlers.Select(h => h.IsName), out specified, err => errors.Add($"'{element.Name.LocalName}' 表示名称か否かの指定でエラー: {err} ('{isAttribute}')"));
                if (!specified) isName = false;

                var isRequired = Parse(handlers.Select(h => h.IsRequired), out specified, err => errors.Add($"'{element.Name.LocalName}' 必須か否かの指定でエラー: {err} ('{isAttribute}')"));
                if (!specified) isRequired = false;

                var memberTypeName = Parse(handlers.Select(h => h.AggregateMemberTypeName), out specified, err => errors.Add($"'{element.Name.LocalName}' 型名の指定でエラー: {err} ('{isAttribute}')")) ?? string.Empty;
                if (!specified) memberTypeName = string.Empty;

                return new ParsedXElement {
                    Source = element,
                    ElementType = elementType,
                    IsKey = isKey,
                    IsName = isName,
                    IsRequired = isRequired,
                    AggregateMemberTypeName = memberTypeName,
                    IsMultipleChildAggregate = multiple,
                    RefTargetName = elementType == E_XElementType.Ref
                        ? handlers.OfType<RefToAttr>().First().Value
                        : string.Empty,
                };
            }
            /// <summary>
            /// is属性をstringの辞書に変換
            /// </summary>
            public static IReadOnlyDictionary<string, string> ToKeyValues(XElement element, ICollection<string> errors) {
                var isAttribute = element.Attribute(IS)?.Value ?? string.Empty;
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
            /// 各指定値の優先順位を考慮して値を決定する。
            /// </summary>
            private static T? Parse<T>(IEnumerable<(T, E_Priority)?> values, out bool specified, Action<string> onError) {
                var notNullValues = values
                    .Where(x => x != null)
                    .Cast<(T, E_Priority)>()
                    .ToArray();

                var force = notNullValues
                    .Where(x => x.Item2 == E_Priority.Force)
                    .GroupBy(x => x.Item1)
                    .ToArray();
                if (force.Length >= 2) {
                    onError("矛盾する値が指定されています。");
                    specified = true;
                    return default;
                } else if (force.Length == 1) {
                    specified = true;
                    return force.Single().Key;
                }

                var ifNotSpecified = notNullValues
                    .Where(x => x.Item2 == E_Priority.IfNotSpecified)
                    .ToArray();
                if (ifNotSpecified.Length >= 1) {
                    specified = true;
                    return ifNotSpecified.First().Item1;
                }

                specified = false;
                return default;
            }

            public enum E_Priority {
                Force,
                IfNotSpecified,
            }

            protected virtual ValueTuple<E_XElementType, E_Priority>? ElementType => null;
            protected virtual (bool, E_Priority)? IsMultipleChildAggregate => null;
            protected virtual (bool, E_Priority)? IsKey => null;
            protected virtual (bool, E_Priority)? IsName => null;
            protected virtual (bool, E_Priority)? IsRequired => null;
            protected virtual (string, E_Priority)? AggregateMemberTypeName => null;
            protected string Value { get; set; } = string.Empty;

            private const string IS = "is";

            /// <summary>
            /// どの予約語にも合致しないもの。enumの名前ないしユーザー定義型とみなす
            /// </summary>
            private class OtherAttr : IsAttributeParser {
                protected override (string, E_Priority)? AggregateMemberTypeName => (Value, E_Priority.Force);
            }

            #region 新しい属性があればここに追加
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            private sealed class IsAttribute : Attribute {
                public IsAttribute(string value) {
                    Value = value;
                }
                public string Value { get; }
            }
            private static IEnumerable<IsAttributeParser> Enumerate() {
                yield return new MasterDataAttr();
                yield return new ObjectAttr();
                yield return new ArrayAttr();
                yield return new VariationAttr();
                yield return new RefToAttr();
                yield return new KeyAttr();
                yield return new NameAttr();
                yield return new IdAttr();
                yield return new WordAttr();
                yield return new SentenceAttr();
            }

            [Is("master-data")]
            private class MasterDataAttr : IsAttributeParser {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.RootAggregate, E_Priority.Force);
            }
            [Is("object")]
            private class ObjectAttr : IsAttributeParser {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.ChildAggregate, E_Priority.Force);
                protected override (bool, E_Priority)? IsMultipleChildAggregate => (false, E_Priority.Force);
            }
            [Is("array")]
            private class ArrayAttr : IsAttributeParser {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.ChildAggregate, E_Priority.Force);
                protected override (bool, E_Priority)? IsMultipleChildAggregate => (true, E_Priority.Force);
            }
            [Is("variation")]
            private class VariationAttr : IsAttributeParser {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.VariationContainer, E_Priority.Force);
            }
            [Is("ref-to")]
            private class RefToAttr : IsAttributeParser {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Ref, E_Priority.Force);
            }

            [Is("key")]
            private class KeyAttr : IsAttributeParser {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.IfNotSpecified);
                protected override (bool, E_Priority)? IsKey => (true, E_Priority.Force);
                protected override (bool, E_Priority)? IsRequired => (true, E_Priority.Force);
            }
            [Is("name")]
            private class NameAttr : IsAttributeParser {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.IfNotSpecified);
                protected override (bool, E_Priority)? IsName => (true, E_Priority.Force);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_WORD, E_Priority.IfNotSpecified);
            }

            [Is("id")]
            private class IdAttr : IsAttributeParser {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.Force);
                protected override (bool, E_Priority)? IsKey => (true, E_Priority.IfNotSpecified);
                protected override (bool, E_Priority)? IsRequired => (true, E_Priority.IfNotSpecified);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_ID, E_Priority.Force);
            }
            [Is("word")]
            private class WordAttr : IsAttributeParser {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.Force);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_WORD, E_Priority.Force);
            }
            [Is("sentence")]
            private class SentenceAttr : IsAttributeParser {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.Force);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_SENTENCE, E_Priority.Force);
            }
            #endregion 新しい属性があればここに追加
        }
    }
}
