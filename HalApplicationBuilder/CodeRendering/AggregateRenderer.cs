using HalApplicationBuilder.CodeRendering.Presentation;
using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.CodeRendering.TemplateTextHelper;

namespace HalApplicationBuilder.CodeRendering {
    internal partial class AggregateRenderer : TemplateBase {

        internal AggregateRenderer(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            if (!aggregate.IsRoot())
                throw new ArgumentException($"{nameof(AggregateRenderer)} requires root aggregate.", nameof(aggregate));

            _aggregate = aggregate;

            _controller = new WebClient.Controller(_aggregate.Item, ctx);
            _create = new CreateMethod(this, ctx);
            _update = new UpdateMethod(this, ctx);
            _delete = new DeleteMethod(this, ctx);

            _ctx = ctx;
        }


        private readonly GraphNode<Aggregate> _aggregate;

        private readonly CodeRenderingContext _ctx;

        public override string FileName => $"{_aggregate.Item.DisplayName.ToFileNameSafe()}.cs";
        private const string E = "e";

        public const string GETINSTANCEKEY_METHOD_NAME = "GetInstanceKey";
        public const string GETINSTANCENAME_METHOD_NAME = "GetInstanceName";
        public const string TOKEYNAMEPAIR_METHOD_NAME = "ToKeyNamePair";

        private IEnumerable<NavigationProperty.Item> EnumerateNavigationProperties(GraphNode<Aggregate> aggregate) {
            foreach (var nav in aggregate.GetNavigationProperties()) {
                if (nav.Principal.Owner.Item == (IEFCoreEntity)aggregate.Item) yield return nav.Principal;
                if (nav.Relevant.Owner.Item == (IEFCoreEntity)aggregate.Item) yield return nav.Relevant;
            }
        }

        #region CREATE
        private readonly CreateMethod _create;
        internal class CreateMethod {
            internal CreateMethod(AggregateRenderer aggFile, CodeRenderingContext ctx) {
                _aggregate = aggFile._aggregate;
                _ctx = ctx;
            }

            private readonly GraphNode<Aggregate> _aggregate;
            private readonly CodeRenderingContext _ctx;

            internal string ArgType => _aggregate.Item.ClassName;
            internal string MethodName => $"Create{_aggregate.Item.DisplayName.ToCSharpSafe()}";
        }
        #endregion CREATE


        #region UPDATE
        private readonly UpdateMethod _update;
        internal class UpdateMethod {
            internal UpdateMethod(AggregateRenderer aggFile, CodeRenderingContext ctx) {
                _aggregate = aggFile._aggregate;
                _ctx = ctx;
            }

            private readonly GraphNode<Aggregate> _aggregate;
            private readonly CodeRenderingContext _ctx;

            internal string MethodName => $"Update{_aggregate.Item.DisplayName.ToCSharpSafe()}";

            internal string RenderDescendantsAttaching(string dbContext, string before, string after) {
                var builder = new StringBuilder();

                var descendantDbEntities = _aggregate.EnumerateDescendants().ToArray();
                for (int i = 0; i < descendantDbEntities.Length; i++) {
                    var paths = descendantDbEntities[i].PathFromEntry().ToArray();

                    // before, after それぞれの子孫インスタンスを一次配列に格納する
                    void RenderEntityArray(bool renderBefore) {
                        if (paths.Any(path => path.Terminal.IsChildrenMember())) {
                            // 子集約までの経路の途中に配列が含まれる場合
                            builder.AppendLine($"var arr{i}_{(renderBefore ? "before" : "after")} = {(renderBefore ? before : after)}");

                            var select = false;
                            foreach (var path in paths) {
                                if (select && path.Terminal.IsChildrenMember()) {
                                    builder.AppendLine($"    .SelectMany(x => x.{path.RelationName})");
                                } else if (select) {
                                    builder.AppendLine($"    .Select(x => x.{path.RelationName})");
                                } else {
                                    builder.AppendLine($"    .{path.RelationName}");
                                    if (path.Terminal.IsChildrenMember()) select = true;
                                }
                            }
                            builder.AppendLine($"    .ToArray();");

                        } else {
                            // 子集約までの経路の途中に配列が含まれない場合
                            builder.AppendLine($"var arr{i}_{(renderBefore ? "before" : "after")} = new {descendantDbEntities[i].Item.EFCoreEntityClassName}[] {{");
                            builder.AppendLine($"    {(renderBefore ? before : after)}.{paths.Select(p => p.RelationName).Join(".")},");
                            builder.AppendLine($"}};");
                        }
                    }
                    RenderEntityArray(true);
                    RenderEntityArray(false);

                    // ChangeState変更
                    builder.AppendLine($"foreach (var a in arr{i}_after) {{");
                    builder.AppendLine($"    var b = arr{i}_before.SingleOrDefault(b => b.{IEFCoreEntity.KEYEQUALS}(a));");
                    builder.AppendLine($"    if (b == null) {{");
                    builder.AppendLine($"        {dbContext}.Entry(a).State = EntityState.Added;");
                    builder.AppendLine($"    }} else {{");
                    builder.AppendLine($"        {dbContext}.Entry(a).State = EntityState.Modified;");
                    builder.AppendLine($"    }}");
                    builder.AppendLine($"}}");

                    builder.AppendLine($"foreach (var b in arr{i}_before) {{");
                    builder.AppendLine($"    var a = arr{i}_after.SingleOrDefault(a => a.{IEFCoreEntity.KEYEQUALS}(b));");
                    builder.AppendLine($"    if (a == null) {{");
                    builder.AppendLine($"        {dbContext}.Entry(b).State = EntityState.Deleted;");
                    builder.AppendLine($"    }}");
                    builder.AppendLine($"}}");
                }

                return builder.ToString();
            }
        }
        #endregion UPDATE


        #region DELETE
        private readonly DeleteMethod _delete;
        internal class DeleteMethod {
            internal DeleteMethod(AggregateRenderer aggFile, CodeRenderingContext ctx) {
                _aggregate = aggFile._aggregate;
                _ctx = ctx;
            }

            private readonly GraphNode<Aggregate> _aggregate;
            private readonly CodeRenderingContext _ctx;

            internal string MethodName => $"Delete{_aggregate.Item.DisplayName.ToCSharpSafe()}";
        }
        #endregion DELETE


        #region LIST BY KEYWORD
        private const int LIST_BY_KEYWORD_MAX = 100;
        private string ListByKeywordMethodName => $"SearchByKeyword{_aggregate.Item.DisplayName.ToCSharpSafe()}";
        private IEnumerable<ListByKeywordTargetColumn> EnumerateListByKeywordTargetColumns() {
            return _aggregate
                .GetColumns()
                .Where(col => col.IsPrimary || col.IsInstanceName)
                .Select(col => new ListByKeywordTargetColumn {
                    Name = col.PropertyName,
                    NameAsString = col.MemberType.GetCSharpTypeName().Contains("string")
                        ? col.PropertyName
                        : $"{col.PropertyName}.ToString()",
                    Path = col.Owner
                        .PathFromEntry()
                        .Select(edge => edge.RelationName)
                        .Concat(new[] { col.PropertyName })
                        .Join("."),
                    IsInstanceKey = col.IsPrimary,
                    IsInstanceName = col.IsInstanceName,
                });
        }
        private class ListByKeywordTargetColumn {
            internal required string Path { get; init; }
            internal required string Name { get; init; }
            internal required string NameAsString { get; init; }
            internal required bool IsInstanceKey { get; init; }
            internal required bool IsInstanceName { get; init; }
        }
        #endregion LIST BY KEYWORD


        #region AGGREGATE INSTANCE & CREATE COMMAND
        private string CreateCommandClassName => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}CreateCommand";
        private string CreateCommandToDbEntityMethodName => AggregateMember.TO_DB_ENTITY_METHOD_NAME;
        private string CreateCommandGetInstanceKeyMethodName => GETINSTANCENAME_METHOD_NAME;

        private IEnumerable<string> GetInstanceNameProps() {
            var useKeyInsteadOfName = _aggregate
                .GetSchalarProperties()
                .Any(p => p.CorrespondingDbColumn.IsInstanceName) == false;
            var props = useKeyInsteadOfName
                ? _aggregate
                    .GetSchalarProperties()
                    .Where(p => p.CorrespondingDbColumn.IsPrimary)
                    .ToArray()
                : _aggregate
                    .GetSchalarProperties()
                    .Where(p => p.CorrespondingDbColumn.IsInstanceName)
                    .ToArray();
            if (props.Length == 0) {
                yield return $"return string.Empty;";
            } else {
                for (int i = 0; i < props.Length; i++) {
                    var head = i == 0 ? "return " : "    + ";
                    yield return $"{head}this.{props[i].PropertyName}?.ToString()";
                }
                yield return $"    ?? string.Empty;";
            }
        }
        #endregion AGGREGATE INSTANCE & CREATE COMMAND


        #region CONTROLLER
        private readonly WebClient.Controller _controller;
        #endregion CONTROLLER
        protected override string Template() {
            var search = new Searching.SearchFeature(_aggregate.As<IEFCoreEntity>(), _ctx);
            var find = new Finding.FindFeature(_aggregate, _ctx);
            var toDbEntity = new InstanceConverting.ToDbEntityRenderer(_aggregate, _ctx);
            var fromDbEntity = new InstanceConverting.FromDbEntityRenderer(_aggregate, _ctx);

            var keyColumns = EnumerateListByKeywordTargetColumns().Where(col => col.IsInstanceKey).ToArray();
            var nameColumns = EnumerateListByKeywordTargetColumns().Where(col => col.IsInstanceName).ToArray();

            return $$"""
                #pragma warning disable CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
                #pragma warning disable CS8618 // null 非許容の変数には、コンストラクターの終了時に null 以外の値が入っていなければなりません
                #pragma warning disable IDE1006 // 命名スタイル

                {{_controller.Render()}}

                #region データ新規作成
                namespace {{_ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{_ctx.Config.EntityNamespace}};

                    partial class {{_controller.ClassName}} : ControllerBase {
                        [HttpPost("{{WebClient.Controller.CREATE_ACTION_NAME}}")]
                        public virtual IActionResult Create([FromBody] {{CreateCommandClassName}} param) {
                            if (_dbContext.{{_create.MethodName}}(param, out var created, out var errors)) {
                                return this.JsonContent(created);
                            } else {
                                return BadRequest(this.JsonContent(errors));
                            }
                        }
                    }
                }
                namespace {{_ctx.Config.EntityNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;

                    partial class {{_ctx.Config.DbContextName}} {
                        public bool {{_create.MethodName}}({{CreateCommandClassName}} command, out {{_aggregate.Item.ClassName}} created, out ICollection<string> errors) {
                            var dbEntity = command.{{CreateCommandToDbEntityMethodName}}();
                            this.Add(dbEntity);

                            try {
                                this.SaveChanges();
                            } catch (DbUpdateException ex) {
                                created = new {{_aggregate.Item.ClassName}}();
                                errors = ex.GetMessagesRecursively("  ").ToList();
                                return false;
                            }

                            var instanceKey = command.{{CreateCommandGetInstanceKeyMethodName}}().ToString();
                            var afterUpdate = this.{{find.FindMethodName}}(instanceKey);
                            if (afterUpdate == null) {
                                created = new {{_aggregate.Item.ClassName}}();
                                errors = new[] { "更新後のデータの再読み込みに失敗しました。" };
                                return false;
                            }

                            created = afterUpdate;
                            errors = new List<string>();
                            return true;
                        }
                    }
                }
                #endregion データ新規作成


                #region 一覧検索
                {{search.RenderControllerAction()}}
                {{search.RenderDbContextMethod()}}
                #endregion 一覧検索


                #region キーワード検索
                namespace {{_ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{_ctx.Config.EntityNamespace}};

                    partial class {{_controller.ClassName}} {
                        [HttpGet("{{WebClient.Controller.KEYWORDSEARCH_ACTION_NAME}}")]
                        public virtual IActionResult SearchByKeyword([FromQuery] string? keyword) {
                            var items = _dbContext.{{ListByKeywordMethodName}}(keyword);
                            return this.JsonContent(items);
                        }
                    }
                }
                namespace {{_ctx.Config.EntityNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;

                    partial class {{_ctx.Config.DbContextName}} {
                        /// <summary>
                        /// {{_aggregate.Item.DisplayName}}をキーワードで検索します。
                        /// </summary>
                        public IEnumerable<{{AggregateInstanceKeyNamePair.CLASSNAME}}> {{ListByKeywordMethodName}}(string? keyword) {
                            var query = this.{{_aggregate.Item.DbSetName}}.Select(e => new {
                {{EnumerateListByKeywordTargetColumns().SelectTextTemplate(col => $$"""
                                e.{{col.Path}},
                """)}}
                            });

                            if (!string.IsNullOrWhiteSpace(keyword)) {
                                var like = $"%{keyword.Trim().Replace("%", "\\%")}%";
                                query = query.Where(item => {{EnumerateListByKeywordTargetColumns().Select(col => $"EF.Functions.Like(item.{col.NameAsString}, like)").Join($"{Environment.NewLine}                            || ")}});
                            }

                            query = query
                                .OrderBy(item => item.{{EnumerateListByKeywordTargetColumns().Where(col => col.IsInstanceKey).First().Name}})
                                .Take({{LIST_BY_KEYWORD_MAX + 1}});

                            return query
                                .AsEnumerable()
                                .Select(item => new {{AggregateInstanceKeyNamePair.CLASSNAME}} {
                                    {{AggregateInstanceKeyNamePair.KEY}} = {{InstanceKey.CLASS_NAME}}.{{InstanceKey.CREATE}}(new object?[] {
                {{keyColumns.SelectTextTemplate(col => $$"""
                                        item.{{col.Name}},
                """)}}
                                    }).ToString(),
                                    {{AggregateInstanceKeyNamePair.NAME}} = {{(nameColumns.Any()
                                        ? nameColumns.Select(col => $"item.{col.Name}?.ToString()").Join(" + ")
                                        : keyColumns.Select(col => $"item.{col.Name}?.ToString()").Join(" + "))}},
                                });
                        }
                    }
                }
                #endregion キーワード検索


                #region 詳細検索
                {{find.RenderController()}}
                {{find.RenderEFCoreFindMethod()}}
                #endregion 詳細検索


                #region 更新
                namespace {{_ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{_ctx.Config.EntityNamespace}};

                    partial class {{_controller.ClassName}} {
                        [HttpPost("{{WebClient.Controller.UPDATE_ACTION_NAME}}")]
                        public virtual IActionResult Update({{_aggregate.Item.ClassName}} param) {
                            if (_dbContext.{{_update.MethodName}}(param, out var updated, out var errors)) {
                                return this.JsonContent(updated);
                            } else {
                                return BadRequest(this.JsonContent(errors));
                            }
                        }
                    }
                }
                namespace {{_ctx.Config.EntityNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;

                    partial class {{_ctx.Config.DbContextName}} {
                        public bool {{_update.MethodName}}({{_aggregate.Item.ClassName}} after, out {{_aggregate.Item.ClassName}} updated, out ICollection<string> errors) {
                            errors = new List<string>();
                            var key = after.{{GETINSTANCEKEY_METHOD_NAME}}().ToString();

                            {{WithIndent(find.RenderDbEntityLoading("this", "beforeDbEntity", "key", tracks: false, includeRefs: false), "            ")}}

                            if (beforeDbEntity == null) {
                                updated = new {{_aggregate.Item.ClassName}}();
                                errors.Add("更新対象のデータが見つかりません。");
                                return false;
                            }

                            var afterDbEntity = after.{{AggregateMember.TO_DB_ENTITY_METHOD_NAME}}();

                            // Attach
                            this.Entry(afterDbEntity).State = EntityState.Modified;

                            {{WithIndent(_update.RenderDescendantsAttaching("this", "beforeDbEntity", "afterDbEntity"), "            ")}}

                            try {
                                this.SaveChanges();
                            } catch (DbUpdateException ex) {
                                updated = new {{_aggregate.Item.ClassName}}();
                                foreach (var msg in ex.GetMessagesRecursively()) errors.Add(msg);
                                return false;
                            }

                            var afterUpdate = this.{{find.FindMethodName}}(key);
                            if (afterUpdate == null) {
                                updated = new {{_aggregate.Item.ClassName}}();
                                errors.Add("更新後のデータの再読み込みに失敗しました。");
                                return false;
                            }
                            updated = afterUpdate;
                            return true;
                        }
                    }
                }
                #endregion 更新


                #region 削除
                namespace {{_ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{_ctx.Config.EntityNamespace}};

                    partial class {{_controller.ClassName}} {
                        [HttpDelete("{{WebClient.Controller.DELETE_ACTION_NAME}}/{key}")]
                        public virtual IActionResult Delete(string key) {
                            if (_dbContext.{{_delete.MethodName}}(key, out var errors)) {
                                return Ok();
                            } else {
                                return BadRequest(this.JsonContent(errors));
                            }
                        }
                    }
                }
                namespace {{_ctx.Config.EntityNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;

                    partial class {{_ctx.Config.DbContextName}} {
                        public bool {{_delete.MethodName}}(string key, out ICollection<string> errors) {

                            {{WithIndent(find.RenderDbEntityLoading("this", "entity", "key", tracks: true, includeRefs: false), "            ")}}

                            if (entity == null) {
                                errors = new[] { "削除対象のデータが見つかりません。" };
                                return false;
                            }

                            this.Remove(entity);
                            try {
                                this.SaveChanges();
                            } catch (DbUpdateException ex) {
                                errors = ex.GetMessagesRecursively().ToArray();
                                return false;
                            }

                            errors = Array.Empty<string>();
                            return true;
                        }
                    }
                }
                #endregion 削除


                #region データ構造
                namespace {{_ctx.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;

                    /// <summary>
                    /// {{_aggregate.Item.DisplayName}}のデータ作成コマンドです。
                    /// </summary>
                    public partial class {{CreateCommandClassName}} {
                {{_aggregate.GetProperties().SelectTextTemplate(prop => $$"""
                        public {{prop.CSharpTypeName}} {{prop.PropertyName}} { get; set; }
                """)}}

                        {{WithIndent(toDbEntity.Render(), "        ")}}
                        /// <summary>
                        /// 主キーを返します。
                        /// </summary>
                        public {{InstanceKey.CLASS_NAME}} {{CreateCommandGetInstanceKeyMethodName}}() {
                            return {{InstanceKey.CLASS_NAME}}.{{InstanceKey.CREATE}}(new object[] {
                {{_aggregate.GetSchalarProperties().Where(p => p.CorrespondingDbColumn.IsPrimary).SelectTextTemplate(p => $$"""
                                this.{{p.PropertyName}},
                """)}}
                            });
                        }
                    }

                    /// <summary>
                    /// {{_aggregate.Item.DisplayName}}のデータ1件の詳細を表すクラスです。
                    /// </summary>
                    public partial class {{_aggregate.Item.ClassName}} : {{AggregateMember.BASE_CLASS_NAME}} {
                {{_aggregate.GetProperties().SelectTextTemplate(prop => $$"""
                        public {{prop.CSharpTypeName}} {{prop.PropertyName}} { get; set; }
                """)}}

                        {{WithIndent(fromDbEntity.Render(), "        ")}}

                        {{WithIndent(toDbEntity.Render(), "        ")}}

                        /// <summary>
                        /// 主キーを返します。
                        /// </summary>
                        public {{InstanceKey.CLASS_NAME}} {{GETINSTANCEKEY_METHOD_NAME}}() {
                            return {{InstanceKey.CLASS_NAME}}.{{InstanceKey.CREATE}}(new object[] {
                {{_aggregate.GetSchalarProperties().Where(p => p.CorrespondingDbColumn.IsPrimary).SelectTextTemplate(p => $$"""
                                this.{{p.PropertyName}},
                """)}}
                            });
                        }
                        public string {{GETINSTANCENAME_METHOD_NAME}}() {
                {{GetInstanceNameProps().SelectTextTemplate(line => $$"""
                            {{line}}
                """)}}
                        }
                        public {{AggregateInstanceKeyNamePair.CLASSNAME}} {{TOKEYNAMEPAIR_METHOD_NAME}}() {
                            return new {{AggregateInstanceKeyNamePair.CLASSNAME}} {
                                {{AggregateInstanceKeyNamePair.KEY}} = this.{{GETINSTANCEKEY_METHOD_NAME}}().ToString(),
                                {{AggregateInstanceKeyNamePair.NAME}} = this.{{GETINSTANCENAME_METHOD_NAME}}(),
                            };
                        }
                    }

                {{_aggregate.EnumerateDescendants().SelectTextTemplate(ins => $$"""
                    /// <summary>
                    /// {{ins.Item.DisplayName}}のデータ1件の詳細を表すクラスです。
                    /// </summary>
                    public partial class {{ins.Item.ClassName}} {
                {{ins.GetProperties().SelectTextTemplate(prop => $$"""
                        public {{prop.CSharpTypeName}} {{prop.PropertyName}} { get; set; }
                """)}}
                    }
                """)}}
                }

                {{search.RenderCSharpClassDef()}}

                namespace {{_ctx.Config.EntityNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.EntityFrameworkCore.Infrastructure;

                {{_aggregate.EnumerateThisAndDescendants().SelectTextTemplate(ett => $$"""
                    /// <summary>
                    /// {{ett.Item.DisplayName}}のデータベースに保存されるデータの形を表すクラスです。
                    /// </summary>
                    public partial class {{ett.Item.EFCoreEntityClassName}} {
                {{ett.GetColumns().SelectTextTemplate(col => $$"""
                        public {{col.MemberType.GetCSharpTypeName()}} {{col.PropertyName}} { get; set; }
                """)}}

                {{EnumerateNavigationProperties(ett).SelectTextTemplate(nav => $$"""
                        public virtual {{nav.CSharpTypeName}} {{nav.PropertyName}} { get; set; }
                """)}}

                        /// <summary>このオブジェクトと比較対象のオブジェクトの主キーが一致するかを返します。</summary>
                        public bool {{IEFCoreEntity.KEYEQUALS}}({{ett.Item.EFCoreEntityClassName}} entity) {
                {{ett.GetColumns().Where(c => c.IsPrimary).SelectTextTemplate(col => $$"""
                            if (entity.{{col.PropertyName}} != this.{{col.PropertyName}}) return false;
                """)}}
                            return true;
                        }
                    }
                """)}}

                    partial class {{_ctx.Config.DbContextName}} {
                {{_aggregate.EnumerateThisAndDescendants().SelectTextTemplate(ett => $$"""
                        public DbSet<{{_ctx.Config.EntityNamespace}}.{{ett.Item.EFCoreEntityClassName}}> {{ett.Item.DbSetName}} { get; set; }
                """)}}
                    }
                }
                #endregion データ構造
                """;
        }
    }
}
