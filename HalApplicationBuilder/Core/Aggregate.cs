using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.CodeRendering;
using System.Text;
using HalApplicationBuilder.CodeRendering.EFCore;

namespace HalApplicationBuilder.Core
{
    internal partial class Aggregate : ValueObject
    {
        internal static Aggregate AsChild(Config config, IAggregateDefine setting, AggregateMember parent) {
            return new Aggregate(config, setting, parent);
        }

        private protected Aggregate(Config config, IAggregateDefine def, AggregateMember? parent) {
            _config = config;
            Def = def;
            Parent = parent;
        }
        private protected readonly Config _config;
        internal IAggregateDefine Def { get; }

        internal Guid GetGuid() => new HashedString(GetUniquePath()).Guid;
        internal string GetDisplayName() => Def.DisplayName;
        internal string GetCSharpSafeName() => $"{GetDisplayName().ToCSharpSafe()}_{GetGuid().ToString().ToCSharpSafe()}";
        internal string GetFileSafeName() => new HashedString(GetUniquePath()).ToFileSafe();

        internal AggregateMember? Parent { get; }
        internal IEnumerable<Aggregate> GetDescendants() {
            foreach (var child in GetMembers().SelectMany(m => m.GetChildAggregates())) {
                yield return child;
                foreach (var grandChild in child.GetDescendants()) {
                    yield return grandChild;
                }
            }
        }

        private protected IEnumerable<AggregateMember> GetMembers() {
            return Def.GetMembers(this);
        }

        internal const string PARENT_NAVIGATION_PROPERTY_NAME = "Parent";
        internal RenderedEFCoreEntity ToDbEntity() {
            var props = GetMembers()
                .SelectMany(m => m.ToDbEntityMember(), (m, prop) => new { m.IsPrimary, prop });

            var pks = props
                .Where(p => p.IsPrimary && p.prop is not NavigationProperty)
                .Select(p => p.prop);
            var nonPks = props
                .Where(p => !p.IsPrimary && p.prop is not NavigationProperty)
                .Select(p => p.prop);
            var navigations = props
                .Where(p => p.prop is NavigationProperty)
                .Select(p => p.prop)
                .Cast<NavigationProperty>();


            if (Parent != null) {
                // 親の主キー
                var parentPK = Parent.Owner.ToDbEntity().PrimaryKeys.Select(ppk => new RenderedParentPkProperty {
                    CSharpTypeName = ppk.CSharpTypeName,
                    PropertyName = $"Parent_{ppk.PropertyName}",
                });
                pks = parentPK.Union(pks);

                // 親へのナビゲーションプロパティ
                navigations = navigations.Concat(new[] { new NavigationProperty {
                     Virtual = true,
                     CSharpTypeName = Parent.Owner.ToDbEntity().CSharpTypeName,
                     PropertyName = PARENT_NAVIGATION_PROPERTY_NAME,
                     Initializer = null,
                     OnModelCreating = null,
                } });
            }

            /* 被参照RefのナビゲーションプロパティはRefの方でpartialでレンダリングしているのでここには無い */

            return new RenderedEFCoreEntity {
                ClassName = GetCSharpSafeName(),
                CSharpTypeName = $"{_config.EntityNamespace}.{GetCSharpSafeName()}",
                DbSetName = GetCSharpSafeName(),
                PrimaryKeys = pks,
                NonPrimaryKeys = nonPks,
                NavigationProperties = navigations,
            };
        }

        internal RenderedClass ToUiInstanceClass() {
            var props = GetMembers().SelectMany(m => m.ToInstanceModelMember());
            return new RenderedClass {
                ClassName = GetCSharpSafeName(),
                CSharpTypeName = $"{_config.MvcModelNamespace}.{GetCSharpSafeName()}",
                Properties = props,
            };
        }
        internal RenderedClass ToSearchConditionClass() {
            var className = $"{GetCSharpSafeName()}__SearchCondition";
            var props = GetMembers().SelectMany(m => m.ToSearchConditionMember());
            return new RenderedClass {
                ClassName = className,
                CSharpTypeName = $"{_config.MvcModelNamespace}.{className}",
                Properties = props,
            };
        }
        internal RenderedClass ToSearchResultClass() {
            var className = $"{GetCSharpSafeName()}__SearchResult";
            var props = GetMembers().SelectMany(m => m.ToSearchResultMember());
            return new RenderedClass {
                ClassName = className,
                CSharpTypeName = $"{_config.MvcModelNamespace}.{className}",
                Properties = props,
            };
        }

        internal MethodInfo GetAutoCompleteMethod(Assembly runtimeAssembly, Microsoft.EntityFrameworkCore.DbContext dbContext) {
            var dbContextType = dbContext.GetType();
            var method = dbContextType.GetMethod(GetAutoCompleteSourceMethodName());
            if (method == null) throw new InvalidOperationException($"{dbContextType.Name} にメソッド {GetAutoCompleteSourceMethodName()} が存在しません。");
            return method;
        }
        internal CodeRendering.EFCore.AutoCompleteSourceDTO BuildAutoCompleteSourceMethod() {
            var dbEntity = ToDbEntity();
            var dto = new CodeRendering.EFCore.AutoCompleteSourceDTO {
                DbSetName = dbEntity.DbSetName,
                EntityClassName = dbEntity.CSharpTypeName,
                MethodName = GetAutoCompleteSourceMethodName(),
            };
            return dto;
        }
        private string GetAutoCompleteSourceMethodName() => $"LoadAutoCompleteSource_{GetCSharpSafeName()}";

        /// <summary>
        /// メンバーのうち最も長い名前を持つものの文字数から、Tailwind CSS におけるflex basisの値を算出する
        /// </summary>
        /// <returns>CSSクラス名</returns>
        private string CalculateFlexBasis() {
            var maxByteCount = GetMembers()
                .DefaultIfEmpty()
                .Max(member => member!.DisplayName.Length);
            return $"basis-{(maxByteCount + 1) * 4}";
        }
        internal void RenderReactSearchCondition(RenderingContext context) {
            var flexBasis = CalculateFlexBasis();
            foreach (var member in GetMembers()) {
                context.Template.WriteLine($"<div className=\"self-stretch flex flex-row\">");
                context.Template.WriteLine($"    <label className=\"{flexBasis} text-sm select-none\">");
                context.Template.WriteLine($"        {member.DisplayName}");
                context.Template.WriteLine($"    </label>");
                context.Template.WriteLine($"    <div className=\"flex-1\">");

                context.Template.PushIndent("        ");
                member.RenderReactSearchCondition(context);
                context.Template.PopIndent();

                context.Template.WriteLine($"    </div>");
                context.Template.WriteLine($"</div>");
            }
        }
        internal void RenderReactComponent(RenderingContext context) {
            var flexBasis = CalculateFlexBasis();
            foreach (var member in GetMembers()) {
                context.Template.WriteLine($"<div className=\"self-stretch flex flex-row\">");
                context.Template.WriteLine($"    <label className=\"{flexBasis} text-sm select-none\">");
                context.Template.WriteLine($"        {member.DisplayName}");
                context.Template.WriteLine($"    </label>");
                context.Template.WriteLine($"    <div className=\"flex-1\">");

                context.Template.PushIndent("        ");
                member.RenderReactComponent(context);
                context.Template.PopIndent();

                context.Template.WriteLine($"    </div>");
                context.Template.WriteLine($"</div>");
            }
        }

        internal Runtime.InstanceKey CreateInstanceKeyFromDbInstnace(object dbInstance) {
            var values = GetMembers()
                .Where(m => m.IsPrimary)
                .Select(m => m.GetInstanceKeyFromDbInstance(dbInstance));
            return Runtime.InstanceKey.FromObjects(values);
        }
        internal Runtime.InstanceKey CreateInstanceKeyFromUiInstnace(object uiInstance) {
            var values = GetMembers()
                .Where(m => m.IsPrimary)
                .Select(m => m.GetInstanceKeyFromUiInstance(uiInstance));
            return Runtime.InstanceKey.FromObjects(values);
        }
        internal Runtime.InstanceKey CreateInstanceKeyFromSearchResult(object searchResult) {
            var values = GetMembers()
                .Where(m => m.IsPrimary)
                .Select(m => m.GetInstanceKeyFromSearchResult(searchResult));
            return Runtime.InstanceKey.FromObjects(values);
        }
        internal Runtime.InstanceKey CreateInstanceKeyFromAutoCompleteItem(object autoCompelteItem) {
            var values = GetMembers()
                .Where(m => m.IsPrimary)
                .Select(m => m.GetInstanceKeyFromAutoCompleteItem(autoCompelteItem));
            return Runtime.InstanceKey.FromObjects(values);
        }

        internal void MapInstanceKeyToDbInstance(string instanceKey, object dbInstance) {
            var pk = GetMembers().Where(m => m.IsPrimary).ToArray();
            var key = Runtime.InstanceKey.FromSerializedString(instanceKey);

            if (key != null && pk.Length != key.ObjectValue.Length)
                throw new ArgumentException(null, nameof(instanceKey));

            for (int i = 0; i < pk.Length; i++) {
                pk[i].MapInstanceKeyToDbInstance(key?.ObjectValue[i], dbInstance);
            }
        }
        internal void MapInstanceKeyToUiInstance(string instanceKey, object uiInstance) {
            var pk = GetMembers().Where(m => m.IsPrimary).ToArray();
            var key = Runtime.InstanceKey.FromSerializedString(instanceKey);

            if (key != null && pk.Length != key.ObjectValue.Length)
                throw new ArgumentException(null, nameof(instanceKey));

            for (int i = 0; i < pk.Length; i++) {
                pk[i].MapInstanceKeyToUiInstance(key?.ObjectValue[i], uiInstance);
            }
        }
        internal void MapInstanceKeyToSearchResult(string instanceKey, object searchResult) {
            var pk = GetMembers().Where(m => m.IsPrimary).ToArray();
            var key = Runtime.InstanceKey.FromSerializedString(instanceKey);

            if (key != null && pk.Length != key.ObjectValue.Length)
                throw new ArgumentException(null, nameof(instanceKey));

            for (int i = 0; i < pk.Length; i++) {
                pk[i].MapInstanceKeyToSearchResult(key?.ObjectValue[i], searchResult);
            }
        }
        internal void MapInstanceKeyToAutoCompleteItem(string instanceKey, object autoCompelteItem) {
            var pk = GetMembers().Where(m => m.IsPrimary).ToArray();
            var key = Runtime.InstanceKey.FromSerializedString(instanceKey);

            if (key != null && pk.Length != key.ObjectValue.Length)
                throw new ArgumentException(null, nameof(instanceKey));

            for (int i = 0; i < pk.Length; i++) {
                pk[i].MapInstanceKeyToAutoCompleteItem(key?.ObjectValue[i], autoCompelteItem);
            }
        }

        internal Runtime.InstanceKey CreateEmptyInstanceKey() {
            return Runtime.InstanceKey.Empty(ToDbEntity().PrimaryKeys.Count());
        }

        internal void MapUiToDb(object uiInstance, object dbInstance, Runtime.IInstanceConvertingContext context) {
            foreach (var member in GetMembers()) {
                member.MapUiToDb(uiInstance, dbInstance, context);
            }
        }
        internal void MapDbToUi(object dbInstance, object uiInstance, Runtime.IInstanceConvertingContext context) {
            foreach (var member in GetMembers()) {
                member.MapDbToUi(dbInstance, uiInstance, context);
            }
        }

        internal const string UNIQUE_PATH_SEPARATOR = "/";
        /// <summary>
        /// スキーマ内で集約を一意に識別するための文字列
        /// </summary>
        internal string GetUniquePath() {
            var list = new List<string>();
            if (Parent == null) {
                list.Add(GetDisplayName());
            } else {
                var memberPath = Parent.GetMemberPath();
                list.Add(memberPath.Root.GetDisplayName());
                list.AddRange(memberPath.Path.Select(m => m.DisplayName));
            }
            return string.Join(
                UNIQUE_PATH_SEPARATOR,
                list.Select(name => System.Web.HttpUtility.HtmlEncode(name)));
        }

        internal Serialized.AggregateJson ToJson() {
            return new Serialized.AggregateJson {
                Guid = GetGuid(),
                Name = GetDisplayName(),
                Members = GetMembers().Select(m => m.ToJson()).ToArray(),
            };
        }

        /// <summary>
        /// この集約が参照している集約を列挙する
        /// </summary>
        internal IEnumerable<ReferenceRelation> EnumerateRefTargetsRecursively() {
            return GetMembers()
                .Where(m => m is MemberImpl.Reference)
                .Select(m => new ReferenceRelation((MemberImpl.Reference)m));
        }

        protected override IEnumerable<object?> ValueObjectIdentifiers()
        {
            yield return Parent;
            yield return Def.DisplayName;
        }

        public override string ToString()
        {
            return GetUniquePath();
        }
    }
}

