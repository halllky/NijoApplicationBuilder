using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HalApplicationBuilder.Core.Definition {
    internal class JsonDefine : IAggregateDefine {
        internal static IEnumerable<IAggregateDefine> Create(Config config, string json) {
            var schema = JsonSerializer.Deserialize<Serialized.AppSchemaJson>(json);
            if (schema == null) throw new FormatException($"集約定義のJSONの形式が不正です。");
            return Create(config, schema);
        }
        internal static IEnumerable<IAggregateDefine> Create(Config config, Serialized.AppSchemaJson schema) {
            if (schema.Aggregates == null) throw new FormatException($"集約定義のJSONの形式が不正です。");
            foreach (var rootAggregate in schema.Aggregates) {
                yield return new JsonDefine(config, rootAggregate, schema);
            }
        }

        private JsonDefine(Config config, Serialized.AggregateJson aggregate, Serialized.AppSchemaJson schema) {
            _config = config;
            _aggregate = aggregate;
            _schema = schema;
        }
        private readonly Config _config;
        private readonly Serialized.AggregateJson _aggregate;
        private readonly Serialized.AppSchemaJson _schema;

        public string DisplayName {
            get {
                if (_aggregate.Name == null)
                    throw new FormatException($"nameが見つかりません。");
                if (string.IsNullOrWhiteSpace(_aggregate.Name))
                    throw new FormatException($"nameが空です。");
                return _aggregate.Name;
            }
        }

        private Aggregate GetAggregateByUniquePath(string uniquePath) {
            var found = Create(_config, _schema)
                .Select(def => new RootAggregate(_config, def))
                .SelectMany(root => root.GetDescendantsAndSelf())
                .SingleOrDefault(aggregate => aggregate.GetUniquePath() == uniquePath);
            if (found == null)
                throw new InvalidOperationException($"'{uniquePath}' の集約が見つかりません。");
            return found;
        }

        public IEnumerable<AggregateMember> GetMembers(Aggregate owner) {
            if (_aggregate.Members == null)
                throw new FormatException($"membersが見つかりません。");

            foreach (var member in _aggregate.Members!) {
                if (string.IsNullOrWhiteSpace(member.Name))
                    throw new FormatException($"nameが空です。");
                if (string.IsNullOrWhiteSpace(member.Kind))
                    throw new FormatException($"kindが空です。");

                var displayName = member.Name;
                var isPrimary = member.IsPrimary == true;
                var isNullable = member.IsNullable == true;

                var schalarType = MemberImpl.SchalarValue.TryParseTypeName(member.Kind);
                if (schalarType != null) {
                    yield return new MemberImpl.SchalarValue(_config, displayName, isPrimary, owner, schalarType, isNullable);

                } else if (member.Kind == MemberImpl.Reference.JSON_KEY) {
                    if (string.IsNullOrWhiteSpace(member.RefTarget)) throw new FormatException($"refTargetが空です。");
                    var getRefTarget = () => GetAggregateByUniquePath(member.RefTarget);
                    yield return new MemberImpl.Reference(_config, displayName, isPrimary, owner, getRefTarget);

                } else if (member.Kind == MemberImpl.Child.JSON_KEY) {
                    if (member.Child == null) throw new FormatException($"childが空です。");
                    var child = new JsonDefine(_config, member.Child, _schema);
                    yield return new MemberImpl.Child(_config, displayName, isPrimary, owner, child);

                } else if (member.Kind == MemberImpl.Children.JSON_KEY) {
                    if (member.Children == null) throw new FormatException($"childrenが空です。");
                    var children = new JsonDefine(_config, member.Children, _schema);
                    yield return new MemberImpl.Children(_config, displayName, isPrimary, owner, children);

                } else {
                    throw new FormatException($"不正な種類です: {member.Kind}");
                }
            }
        }
    }
}

