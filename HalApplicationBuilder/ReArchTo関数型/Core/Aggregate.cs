using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;

namespace HalApplicationBuilder.ReArchTo関数型.Core
{
    internal class Aggregate : ValueObject
    {
        internal static Aggregate AsChild(Config config, Type underlyingType, AggregateMember parent) {
            return new Aggregate(config, underlyingType, parent);
        }

        private protected Aggregate(
            Config config,
            Type underlyingType,
            AggregateMember? parent)
        {
            _config = config;
            _underlyingType = underlyingType;
            Parent = parent;
        }
        private protected readonly Config _config;
        private protected readonly Type _underlyingType;

        internal Guid GUID => _underlyingType.GUID;
        internal string Name => _underlyingType.Name;

        internal AggregateMember? Parent { get; }

        private RootAggregate GetRoot() {
            var aggregate = (Aggregate?)this;
            while (true) {
                if (aggregate is RootAggregate root) return root;
                if (aggregate == null) throw new InvalidOperationException("ルート集約特定失敗");
                aggregate = aggregate?.Parent?.Owner;
            }
        }
        private protected IEnumerable<AggregateMember> GetMembers() {
            foreach (var prop in _underlyingType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (prop.GetCustomAttribute<NotMappedAttribute>() != null) continue;

                if (MemberImpl.SchalarValue.IsPrimitive(prop.PropertyType)) {
                    yield return new MemberImpl.SchalarValue(_config, prop, this);

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(Child<>)) {

                    var childType = prop.PropertyType.GetGenericArguments()[0];
                    var variations = prop.GetCustomAttributes<VariationAttribute>();

                    if (!childType.IsAbstract && !variations.Any())
                        yield return new MemberImpl.Child(_config, prop, this);

                    else if (childType.IsAbstract && variations.Any())
                        yield return new MemberImpl.Variation(_config, prop, this);

                    else
                        throw new InvalidOperationException($"抽象型ならバリエーション必須、抽象型でないなら{nameof(VariationAttribute)}指定不可");
                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(Children<>)) {
                    yield return new MemberImpl.Children(_config, prop, this);
                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(RefTo<>)) {
                    yield return new MemberImpl.Reference(_config, prop, this);

                } else {
                    throw new InvalidOperationException($"{Name} の {prop.Name} の型 {prop.PropertyType.Name} は非対応");
                }
            }
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
                navigations = navigations.Concat(new[] {new NavigationProperty {
                     Virtual = true,
                     CSharpTypeName = Parent.Owner.ToDbEntity().CSharpTypeName,
                     PropertyName = PARENT_NAVIGATION_PROPERTY_NAME,
                     Initializer = null,
                     IsPrincipal = false,
                     Multiplicity = NavigationProperty.E_Multiplicity.HasOneWithMany,
                     OpponentName = "参照されないので設定不要",
                     ForeignKeys = Enumerable.Empty<RenderedProperty>(), // 参照されないので設定不要
                } });
            }

            /* 被参照RefのナビゲーションプロパティはRefの方でpartialでレンダリングしているのでここには無い */

            return new RenderedEFCoreEntity {
                ClassName = Name,
                CSharpTypeName = $"{_config.EntityNamespace}.{Name}",
                DbSetName = Name,
                PrimaryKeys = pks,
                NonPrimaryKeys = nonPks,
                NavigationProperties = navigations,
            };
        }

        internal RenderedClass ToUiInstanceClass() {
            var className = Name;
            var props = GetMembers().SelectMany(m => m.ToInstanceModelMember());
            return new RenderedClass {
                ClassName = className,
                CSharpTypeName = $"{_config.MvcModelNamespace}.{className}",
                Properties = props,
            };
        }
        internal RenderedClass ToSearchConditionClass() {
            var className = $"{Name}__SearchCondition";
            var props = GetMembers().SelectMany(m => m.ToSearchConditionMember());
            return new RenderedClass {
                ClassName = className,
                CSharpTypeName = $"{_config.MvcModelNamespace}.{className}",
                Properties = props,
            };
        }
        internal RenderedClass ToSearchResultClass() {
            var className = $"{Name}__SearchResult";
            var props = GetMembers().SelectMany(m => m.ToSearchResultMember());
            return new RenderedClass {
                ClassName = className,
                CSharpTypeName = $"{_config.MvcModelNamespace}.{className}",
                Properties = props,
            };
        }

        internal System.Reflection.MethodInfo GetAutoCompleteMethod(System.Reflection.Assembly runtimeAssembly, Microsoft.EntityFrameworkCore.DbContext dbContext) {
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
        private string GetAutoCompleteSourceMethodName() => $"LoadAutoCompleteSource_{Name}";

        internal void RenderSearchCondition(CodeRendering.RenderingContext context) {

            context.Template.WriteLine($"<div class=\"flex flex-col\">");

            foreach (var member in GetMembers()) {
                context.Template.WriteLine($"    <div class=\"flex flex-col md:flex-row mb-1\">");
                context.Template.WriteLine($"        <label class=\"w-32 select-none\">");
                context.Template.WriteLine($"            {member.DisplayName}");
                context.Template.WriteLine($"        </label>");
                context.Template.WriteLine($"        <div class=\"flex-1\">");

                context.Template.PushIndent("            ");
                member.RenderMvcSearchConditionView(context);
                context.Template.PopIndent();

                context.Template.WriteLine($"        </div>");
                context.Template.WriteLine($"    </div>");
            }

            context.Template.WriteLine($"</div>");
        }

        internal void RenderAspNetMvcPartialView(CodeRendering.RenderingContext context) {

            context.Template.WriteLine($"<div class=\"flex flex-col\">");

            foreach (var member in GetMembers()) {
                context.Template.WriteLine($"    <div class=\"flex flex-col md:flex-row mb-1\">");
                context.Template.WriteLine($"        <label class=\"w-32 select-none\">");
                context.Template.WriteLine($"            {member.DisplayName}");
                context.Template.WriteLine($"        </label>");
                context.Template.WriteLine($"        <div class=\"flex-1\">");

                context.Template.PushIndent("            ");
                member.RenderAspNetMvcPartialView(context);
                context.Template.PopIndent();

                context.Template.WriteLine($"        </div>");
                context.Template.WriteLine($"    </div>");
            }

            context.Template.WriteLine($"</div>");
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
            if (pk.Length != key.ObjectValue.Length) throw new ArgumentException(null, nameof(instanceKey));
            for (int i = 0; i < pk.Length; i++) {
                pk[i].MapInstanceKeyToDbInstance(key.ObjectValue[i], dbInstance);
            }
        }
        internal void MapInstanceKeyToUiInstance(string instanceKey, object uiInstance) {
            var pk = GetMembers().Where(m => m.IsPrimary).ToArray();
            var key = Runtime.InstanceKey.FromSerializedString(instanceKey);
            if (pk.Length != key.ObjectValue.Length) throw new ArgumentException(null, nameof(instanceKey));
            for (int i = 0; i < pk.Length; i++) {
                pk[i].MapInstanceKeyToUiInstance(key.ObjectValue[i], uiInstance);
            }
        }
        internal void MapInstanceKeyToSearchResult(string instanceKey, object searchResult) {
            var pk = GetMembers().Where(m => m.IsPrimary).ToArray();
            var key = Runtime.InstanceKey.FromSerializedString(instanceKey);
            if (pk.Length != key.ObjectValue.Length) throw new ArgumentException(null, nameof(instanceKey));
            for (int i = 0; i < pk.Length; i++) {
                pk[i].MapInstanceKeyToSearchResult(key.ObjectValue[i], searchResult);
            }
        }
        internal void MapInstanceKeyToAutoCompleteItem(string instanceKey, object autoCompelteItem) {
            var pk = GetMembers().Where(m => m.IsPrimary).ToArray();
            var key = Runtime.InstanceKey.FromSerializedString(instanceKey);
            if (pk.Length != key.ObjectValue.Length) throw new ArgumentException(null, nameof(instanceKey));
            for (int i = 0; i < pk.Length; i++) {
                pk[i].MapInstanceKeyToAutoCompleteItem(key.ObjectValue[i], autoCompelteItem);
            }
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

        /// <summary>
        /// スキーマ内で集約を一意に識別するための文字列
        /// </summary>
        internal string GetUniquePath() {
            var list = new List<string>();
            var member = Parent;
            while (member != null) {
                list.Insert(0, member.DisplayName);
                member = member.Owner.Parent;
            }
            list.Insert(0, GetRoot().Name);
            return string.Join(".", list);
        }

        /// <summary>
        /// この集約が参照している集約を列挙する
        /// </summary>
        internal IEnumerable<ReferredAggregate> EnumerateRefTargetsRecursively() {
            return GetMembers()
                .Where(m => m is MemberImpl.Reference)
                .Select(m => ((MemberImpl.Reference)m).GetRefTarget());
        }

        protected override IEnumerable<object?> ValueObjectIdentifiers()
        {
            yield return Parent;
            yield return _underlyingType;
        }
    }
}

