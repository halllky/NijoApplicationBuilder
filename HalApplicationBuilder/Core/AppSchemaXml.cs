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
    internal static class AppSchemaXml {
        internal static string GetPath(string projectRoot) {
            return Path.Combine(projectRoot, "halapp.xml");
        }
        internal static XDocument Load(string projectRoot) {
            var xmlFullPath = GetPath(projectRoot);
            using var stream = IO.OpenFileWithRetry(xmlFullPath);
            using var reader = new StreamReader(stream);
            var xmlContent = reader.ReadToEnd();
            var xDocument = XDocument.Parse(xmlContent);
            return xDocument;
        }

        internal static bool AddXml(this AppSchemaBuilder builder, string projectRoot, out ICollection<string> errors) {
            var xDocument = Load(projectRoot);
            if (xDocument.Root == null) throw new FormatException($"Xml doesn't have contents.");

            var errorList = new List<string>();

            void Handle(ParsedXElement el, AggregatePath? parent) {
                const string VARIATION_KEY = "variation-key";
                var errorListLocal = new HashSet<string>();

                // パス組み立て
                AggregatePath aggregatePath;
                if (parent == null) {
                    if (!AggregatePath.TryCreate(new[] { el.Source.Name.LocalName }, out aggregatePath, out var err)) errorListLocal.Add(err);
                } else {
                    if (!parent.TryCreateChild(el.Source.Name.LocalName, out aggregatePath, out var err)) errorListLocal.Add(err);
                }

                // バリデーション
                var members = el.Source
                    .Elements()
                    .Select(inner => Attributes.Parse(inner, errorListLocal))
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
                var schalarMembers = members
                    .Where(member => member.ElementType == E_XElementType.Schalar)
                    .Select(member => new SchalarMemberDef {
                        Name = member.Source.Name.LocalName,
                        Type = member.AggregateMemberTypeName,
                        IsPrimary = member.IsKey,
                        IsInstanceName = member.IsName,
                        Optional = !member.IsRequired,
                    });

                if (el.ElementType == E_XElementType.RootAggregate) {
                    builder.AddAggregate(new AggregateDef {
                        FullPath = aggregatePath,
                        Members = schalarMembers.ToList(),
                    });
                } else if (el.ElementType == E_XElementType.ChildAggregate) {
                    if (el.IsMultipleChildAggregate) {
                        builder.AddChildrenAggregate(new ChildrenDef {
                            Name = el.Source.Name.LocalName,
                            Members = schalarMembers.ToList(),
                            OwnerFullPath = parent!.Value,
                        });
                    } else {
                        builder.AddChildAggregate(new ChildDef {
                            Name = el.Source.Name.LocalName,
                            Members = schalarMembers.ToList(),
                            OwnerFullPath = parent!.Value,
                        });
                    }
                } else if (el.ElementType == E_XElementType.VariationValue) {
                    builder.AddVariationAggregate(new VariationDef {
                        Name = el.Source.Name.LocalName,
                        Members = schalarMembers.ToList(),
                        VariationContainer = el.Source.Parent?.Name.LocalName ?? string.Empty,
                        VariationSwitch = el.Source.Attribute(VARIATION_KEY)?.Value ?? string.Empty,
                        OwnerFullPath = parent!.Value,
                        IsPrimary = el.IsKey,
                        IsInstanceName = el.IsName,
                        Optional = !el.IsRequired,
                    });
                }

                var refMembers = members.Where(m => m.ElementType == E_XElementType.Ref);
                foreach (var member in refMembers) {
                    builder.AddReference(new ReferenceDef {
                        Name = member.Source.Name.LocalName,
                        OwnerFullPath = aggregatePath.Value,
                        IsPrimary = member.IsKey,
                        IsInstanceName = member.IsName,
                        IsRequired = member.IsRequired,
                        TargetFullPath = member.RefTargetName,
                    });
                }

                // 再帰
                var descendants = members
                    .Where(member => member.ElementType == E_XElementType.ChildAggregate
                                  || member.ElementType == E_XElementType.VariationValue);
                foreach (var item in descendants) {
                    Handle(item, aggregatePath);
                }
            }

            builder.SetApplicationName(xDocument.Root.Name.LocalName);

            foreach (var xElement in xDocument.Root.Elements()) {
                if (xElement.Name.LocalName == Config.XML_CONFIG_SECTION_NAME) continue;
                var parsed = Attributes.Parse(xElement, errorList);
                Handle(parsed, parent: null);
            }
            errors = errorList;
            return errors.Count == 0;
        }

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
        private abstract class Attributes {
            public static ParsedXElement Parse(XElement element, ICollection<string> errors) {

                // stringの辞書に変換
                var isAttribute = element.Attribute("is")?.Value ?? string.Empty;
                var splitted = isAttribute.Split(' ', '　');
                var keyValues = new Dictionary<string, string>();
                foreach (var item in splitted) {
                    var separated = item.Split(':');
                    if (separated.Length >= 3) {
                        errors.Add($"'{element.Name}' の '{item}' が':'を複数含んでいます。");
                        continue;
                    }
                    var key = separated[0];
                    var value = separated.Length >= 2 ? separated[1] : string.Empty;
                    keyValues.Add(key, value);
                }

                // 各値のハンドラを決定
                var attributeTypes = Enumerate().ToDictionary(attr => attr.GetType().GetCustomAttribute<IsAttribute>()!.Value);
                var handlers = new HashSet<Attributes>();
                foreach (var kv in keyValues) {
                    if (!attributeTypes.TryGetValue(kv.Key.ToLower(), out var handler)) {
                        errors.Add($"'{element.Name}' の '{kv.Key}' は認識できない属性です。");
                        continue;
                    }
                    if (handlers.Contains(handler)) {
                        errors.Add($"'{element.Name}' に '{kv.Key}' が複数指定されています。");
                        continue;
                    }
                    handler.Value = kv.Value;
                    handlers.Add(handler);
                }

                // 各指定間で矛盾がないかを調べて返す
                bool specified;
                var elementType = Parse(handlers.Select(h => h.ElementType), out specified, err => errors.Add($"'{element.Name.LocalName}' 種別の指定でエラー: {err} ('{isAttribute}')"));
                if (!specified) errors.Add("の種別が不明です。");

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

            #region 新しい属性があればここに追加
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            private sealed class IsAttribute : Attribute {
                public IsAttribute(string value) {
                    Value = value;
                }
                public string Value { get; }
            }
            private static IEnumerable<Attributes> Enumerate() {
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
            private class MasterDataAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.RootAggregate, E_Priority.Force);
            }
            [Is("object")]
            private class ObjectAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.ChildAggregate, E_Priority.Force);
                protected override (bool, E_Priority)? IsMultipleChildAggregate => (false, E_Priority.Force);
            }
            [Is("array")]
            private class ArrayAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.ChildAggregate, E_Priority.Force);
                protected override (bool, E_Priority)? IsMultipleChildAggregate => (true, E_Priority.Force);
            }
            [Is("variation")]
            private class VariationAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.VariationContainer, E_Priority.Force);
            }
            [Is("ref-to")]
            private class RefToAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Ref, E_Priority.Force);
            }

            [Is("key")]
            private class KeyAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.IfNotSpecified);
                protected override (bool, E_Priority)? IsKey => (true, E_Priority.Force);
                protected override (bool, E_Priority)? IsRequired => (true, E_Priority.Force);
            }
            [Is("name")]
            private class NameAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.IfNotSpecified);
                protected override (bool, E_Priority)? IsName => (true, E_Priority.Force);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_WORD, E_Priority.IfNotSpecified);
            }

            [Is("id")]
            private class IdAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.Force);
                protected override (bool, E_Priority)? IsKey => (true, E_Priority.IfNotSpecified);
                protected override (bool, E_Priority)? IsRequired => (true, E_Priority.IfNotSpecified);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_ID, E_Priority.Force);
            }
            [Is("word")]
            private class WordAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.Force);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_WORD, E_Priority.Force);
            }
            [Is("sentence")]
            private class SentenceAttr : Attributes {
                protected override (E_XElementType, E_Priority)? ElementType => (E_XElementType.Schalar, E_Priority.Force);
                protected override (string, E_Priority)? AggregateMemberTypeName => (MemberTypeResolver.TYPE_SENTENCE, E_Priority.Force);
            }
            #endregion 新しい属性があればここに追加
        }
    }
}
