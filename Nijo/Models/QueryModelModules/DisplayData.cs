using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.QueryModelModules {
    /// <summary>
    /// ReadModelの画面表示用データ
    /// </summary>
    internal class DisplayData : IInstancePropertyOwnerMetadata {

        internal DisplayData(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;


        /// <summary>C#クラス名</summary>
        internal string CsClassName => $"{_aggregate.PhysicalName}DisplayData";
        /// <summary>C#クラス名（values）</summary>
        internal string CsValuesClassName => $"{_aggregate.PhysicalName}DisplayDataValues";
        /// <summary>TypeScript型名</summary>
        internal string TsTypeName => $"{_aggregate.PhysicalName}DisplayData";

        /// <summary>画面上で独自の追加削除のライフサイクルを持つかどうか</summary>
        internal virtual bool HasLifeCycle => true;
        /// <summary>楽観排他制御用のバージョンを持つかどうか</summary>
        internal virtual bool HasVersion => _aggregate is RootAggregate;

        /// <summary>値が格納されるプロパティの名前（C#）</summary>
        internal const string VALUES_CS = "Values";
        /// <summary>値が格納されるプロパティの名前（TypeScript）</summary>
        internal const string VALUES_TS = "values";
        /// <summary>値クラス名</summary>
        internal string ValueCsClassName => $"{CsClassName}Values";

        /// <summary>メッセージ用構造体 C#クラス名</summary>
        internal string MessageDataCsClassName => $"{CsClassName}Messages";

        /// <summary>読み取り専用か否かが格納されるプロパティの名前（C#）</summary>
        internal const string READONLY_CS = "ReadOnly";
        /// <summary>読み取り専用か否かが格納されるプロパティの名前（TypeScript）</summary>
        internal const string READONLY_TS = "readOnly";
        /// <summary>全項目が読み取り専用か否か（C#）</summary>
        internal const string ALL_READONLY_CS = "AllReadOnly";
        /// <summary>全項目が読み取り専用か否か（TypeScript）</summary>
        internal const string ALL_READONLY_TS = "allReadOnly";
        /// <summary>メッセージ用構造体 C#クラス名</summary>
        internal string ReadOnlyDataCsClassName => $"{CsClassName}ReadOnly";

        /// <summary>
        /// 通常、保存時に追加・更新・削除のどの処理となるかは
        /// <see cref="EXISTS_IN_DB_TS"/>, <see cref="WILL_BE_CHANGED_TS"/>, <see cref="WILL_BE_DELETED_TS"/>
        /// から計算されるが、強制的に追加または更新または削除いずれかの処理を走らせたい場合に指定されるプロパティ
        /// </summary>
        internal const string ADD_MOD_DEL_CS = "AddModDel";
        /// <inheritdoc cref="ADD_MOD_DEL_CS"/>
        internal const string ADD_MOD_DEL_TS = "addModDel";

        /// <summary>このデータがDBに保存済みかどうか（C#）。つまり新規作成のときはfalse, 閲覧・更新・削除のときはtrue</summary>
        internal const string EXISTS_IN_DB_CS = "ExistsInDatabase";
        /// <summary>このデータがDBに保存済みかどうか（TypeScript）。つまり新規作成のときはfalse, 閲覧・更新・削除のときはtrue</summary>
        internal const string EXISTS_IN_DB_TS = "existsInDatabase";

        /// <summary>画面上で何らかの変更が加えられてから、保存処理の実行でその変更が確定するまでの間、trueになる（C#）</summary>
        internal const string WILL_BE_CHANGED_CS = "WillBeChanged";
        /// <summary>画面上で何らかの変更が加えられてから、保存処理の実行でその変更が確定するまでの間、trueになる（TypeScript）</summary>
        internal const string WILL_BE_CHANGED_TS = "willBeChanged";

        /// <summary>画面上で削除が指示されてから、保存処理の実行でその削除が確定するまでの間、trueになる（C#）</summary>
        internal const string WILL_BE_DELETED_CS = "WillBeDeleted";
        /// <summary>画面上で削除が指示されてから、保存処理の実行でその削除が確定するまでの間、trueになる（TypeScript）</summary>
        internal const string WILL_BE_DELETED_TS = "willBeDeleted";

        /// <summary>楽観排他制御用のバージョニング情報をもつプロパティの名前（C#側）</summary>
        internal const string VERSION_CS = "Version";
        /// <summary>楽観排他制御用のバージョニング情報をもつプロパティの名前（TypeScript側）</summary>
        internal const string VERSION_TS = "version";

        /// <summary>追加・更新・削除のいずれかの区分を返すメソッドの名前</summary>
        internal const string GET_SAVE_TYPE = "GetSaveType";


        /// <summary>
        /// valuesの中に宣言されるメンバーを列挙する。
        /// </summary>
        internal IEnumerable<IDisplayDataMember> GetOwnMembers() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is ValueMember vm) {
                    yield return new DisplayDataValueMember(vm);

                } else if (member is RefToMember refTo) {
                    yield return new DisplayDataRefMember(refTo);

                }
            }
        }

        /// <summary>
        /// 子要素を列挙する。
        /// </summary>
        internal IEnumerable<DisplayDataDescendant> GetChildMembers() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is ChildAggregate child) {
                    yield return new DisplayDataChildDescendant(child);

                } else if (member is ChildrenAggregate children) {
                    yield return new DisplayDataChildrenDescendant(children);

                }
            }
        }

        IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
            var ownMembers = GetOwnMembers().Cast<IInstancePropertyMetadata>();
            var childMemberes = GetChildMembers().Cast<IInstancePropertyMetadata>();
            return ownMembers.Concat(childMemberes);
        }


        #region レンダリング
        internal static string RenderCSharpRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var tree = rootAggregate
                .EnumerateThisAndDescendants()
                .Select(agg => agg switch {
                    RootAggregate root => new DisplayData(root),
                    ChildAggregate child => new DisplayDataChildDescendant(child),
                    ChildrenAggregate children => new DisplayDataChildrenDescendant(children),
                    _ => throw new InvalidOperationException(),
                });

            return $$"""
                #region 画面表示用データ
                {{tree.SelectTextTemplate(disp => $$"""
                {{disp.RenderCSharpDeclaring(ctx)}}
                """)}}
                #endregion 画面表示用データ
                """;
        }
        internal static string RenderTypeScriptRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var tree = rootAggregate
                .EnumerateThisAndDescendants()
                .Select(agg => agg switch {
                    RootAggregate root => new DisplayData(root),
                    ChildAggregate child => new DisplayDataChildDescendant(child),
                    ChildrenAggregate children => new DisplayDataChildrenDescendant(children),
                    _ => throw new InvalidOperationException(),
                });

            return $$"""
                //#region 画面表示用データ
                {{tree.SelectTextTemplate(disp => $$"""
                {{disp.RenderTypeScriptType(ctx)}}
                """)}}
                //#endregion 画面表示用データ
                """;
        }

        private string RenderCSharpDeclaring(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}}の画面表示用データ。
                /// </summary>
                public partial class {{CsClassName}} {
                    /// <summary>{{_aggregate.DisplayName}}自身が持つ値</summary>
                    [JsonPropertyName("{{VALUES_TS}}")]
                    public {{CsValuesClassName}} {{VALUES_CS}} { get; set; } = new();
                {{GetChildMembers().SelectTextTemplate(c => $$"""
                    [JsonPropertyName("{{c.PhysicalName}}")]
                    public {{WithIndent(c.CsClassNameAsMember, "    ")}} {{c.PhysicalName}} { get; set; } = new();
                """)}}
                {{If(HasLifeCycle, () => $$"""

                    /// <summary>このデータがDBに保存済みかどうか</summary>
                    [JsonPropertyName("{{EXISTS_IN_DB_TS}}")]
                    public bool {{EXISTS_IN_DB_CS}} { get; set; }
                    /// <summary>このデータに更新がかかっているかどうか</summary>
                    [JsonPropertyName("{{WILL_BE_CHANGED_TS}}")]
                    public bool {{WILL_BE_CHANGED_CS}} { get; set; }
                    /// <summary>このデータが更新確定時に削除されるかどうか</summary>
                    [JsonPropertyName("{{WILL_BE_DELETED_TS}}")]
                    public bool {{WILL_BE_DELETED_CS}} { get; set; }
                """)}}
                {{If(HasVersion, () => $$"""
                    /// <summary>楽観排他制御用のバージョニング情報</summary>
                    [JsonPropertyName("{{VERSION_TS}}")]
                    public int? {{VERSION_CS}} { get; set; }
                """)}}

                    /// <summary>どの項目が読み取り専用か</summary>
                    [JsonPropertyName("{{READONLY_TS}}")]
                    public {{ReadOnlyDataCsClassName}} {{READONLY_CS}} { get; set; } = new();
                }

                /// <summary>
                /// <see cref="{{CsClassName}}/> の{{VALUES_CS}}の型
                /// </summary>
                public partial class {{CsValuesClassName}} {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                    {{WithIndent(m.RenderCsDeclaration(), "    ")}}
                """)}}
                }

                /// <summary>
                /// <see cref="{{CsClassName}}/> の{{READONLY_CS}}の型
                /// </summary>
                public partial class {{ReadOnlyDataCsClassName}} {
                    /// <summary>{{_aggregate.DisplayName}}全体が読み取り専用か否か</summary>
                    [JsonPropertyName("{{ALL_READONLY_TS}}")]
                    public bool {{ALL_READONLY_CS}} { get; set; }
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                    /// <summary>{{member.DisplayName}}が読み取り専用か否か</summary>
                    public bool {{member.PhysicalName}} { get; set; }
                """)}}
                }
                """;
        }

        private string RenderTypeScriptType(CodeRenderingContext ctx) {
            return $$"""
                /** {{_aggregate.DisplayName}}の画面表示用データ。 */
                export type {{TsTypeName}} = {
                  /** 値 */
                  {{VALUES_TS}}: {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{WithIndent(m.RenderTsDeclaration(), "  ")}}
                """)}}
                  }
                {{GetChildMembers().SelectTextTemplate(member => $$"""
                  /** {{member.DisplayName}} */
                  {{member.PhysicalName}}: {{member.TsTypeNameAsMember}}
                """)}}

                {{If(HasLifeCycle, () => $$"""
                  /** このデータがDBに保存済みかどうか */
                  {{EXISTS_IN_DB_TS}}: boolean
                  /** このデータに更新がかかっているかどうか */
                  {{WILL_BE_CHANGED_TS}}: boolean
                  /** このデータが更新確定時に削除されるかどうか */
                  {{WILL_BE_DELETED_TS}}: boolean
                """)}}
                {{If(HasVersion, () => $$"""
                  /** 楽観排他制御用のバージョニング情報 */
                  {{VERSION_TS}}: number | undefined
                """)}}
                  /** どの項目が読み取り専用か */
                  {{READONLY_TS}}: {
                    /** {{_aggregate.DisplayName}}全体が読み取り専用か否か */
                    {{ALL_READONLY_TS}}?: boolean
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                    /** {{member.DisplayName}}が読み取り専用か否か */
                    {{member.PhysicalName}}?: boolean
                """)}}
                  }
                }
                """;
        }
        #endregion レンダリング


        #region Valuesの中に定義されるメンバー
        internal interface IDisplayDataMember : IUiConstraintValue, IInstancePropertyMetadata {
            string PhysicalName { get; }
            UiConstraint.E_Type UiConstraintType { get; }

            string RenderCsDeclaration();
            string RenderTsDeclaration();

            string RenderNewObjectCreation();
        }

        internal class DisplayDataValueMember : IDisplayDataMember, IInstanceValuePropertyMetadata {
            internal DisplayDataValueMember(ValueMember vm) {
                Member = vm;
            }
            internal ValueMember Member { get; }

            public string PhysicalName => Member.PhysicalName;
            public string DisplayName => Member.DisplayName;
            public UiConstraint.E_Type UiConstraintType => Member.Type.UiConstraintType;

            IValueMemberType IInstanceValuePropertyMetadata.Type => Member.Type;
            ISchemaPathNode IInstancePropertyMetadata.MappingKey => Member;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;

            public bool IsRequired => Member.IsKey || Member.IsRequired;
            public string? CharacterType => Member.CharacterType;
            public int? MaxLength => Member.MaxLength;
            public int? TotalDigit => Member.TotalDigit;
            public int? DecimalPlace => Member.DecimalPlace;

            public string RenderCsDeclaration() {
                return $$"""
                    /// <summary>{{Member.DisplayName}}</summary>
                    public {{Member.Type.CsDomainTypeName}}? {{PhysicalName}} { get; set; }
                    """;
            }
            public string RenderTsDeclaration() {
                return $$"""
                    {{PhysicalName}}?: {{Member.Type.TsTypeName}}
                    """;
            }

            public string RenderNewObjectCreation() {
                return "undefined";
            }
        }

        internal class DisplayDataRefMember : IDisplayDataMember, IInstanceStructurePropertyMetadata {
            internal DisplayDataRefMember(RefToMember refTo) {
                Member = refTo;
                RefEntry = new DisplayDataRef.Entry(refTo.RefTo);
            }
            internal RefToMember Member { get; }
            internal DisplayDataRef.Entry RefEntry;

            public string PhysicalName => Member.PhysicalName;
            public string DisplayName => Member.DisplayName;
            public UiConstraint.E_Type UiConstraintType => UiConstraint.E_Type.MemberConstraintBase;

            public bool IsRequired => Member.IsKey || Member.IsRequired;
            public string? CharacterType => null;
            public int? MaxLength => null;
            public int? TotalDigit => null;
            public int? DecimalPlace => null;

            public string RenderCsDeclaration() {
                return $$"""
                    /// <summary>{{Member.DisplayName}}</summary>
                    public {{RefEntry.CsClassName}} {{PhysicalName}} { get; set; } = new();
                    """;
            }
            public string RenderTsDeclaration() {
                return $$"""
                    {{PhysicalName}}: {{RefEntry.TsTypeName}}
                    """;
            }

            public string RenderNewObjectCreation() {
                return $"{RefEntry.TsNewObjectFunction}()";
            }

            internal IEnumerable<DisplayDataRef.IRefDisplayDataMember> GetMembers() {
                return RefEntry.GetMembers();
            }

            ISchemaPathNode IInstancePropertyMetadata.MappingKey => Member;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();
        }
        #endregion Valuesの中に定義されるメンバー


        #region UI用の制約定義
        internal string UiConstraintTypeName => $"{_aggregate.PhysicalName}ConstraintType";
        internal string UiConstraingValueName => $"{_aggregate.PhysicalName}Constraints";
        internal string RenderUiConstraintType(CodeRenderingContext ctx) {
            if (_aggregate is not RootAggregate) throw new InvalidOperationException();

            return $$"""
                /** {{_aggregate.DisplayName}}の各メンバーの制約の型 */
                type {{UiConstraintTypeName}} = {
                  {{WithIndent(RenderMembers(this), "  ")}}
                }
                """;

            static string RenderMembers(DisplayData displayData) {
                return $$"""
                    {{VALUES_TS}}: {
                    {{displayData.GetOwnMembers().SelectTextTemplate(m => $$"""
                      {{m.PhysicalName}}: Util.{{m.UiConstraintType}}
                    """)}}
                    }
                    {{displayData.GetChildMembers().SelectTextTemplate(desc => $$"""
                    {{desc.PhysicalName}}: {
                      {{WithIndent(RenderMembers(desc), "  ")}}
                    }
                    """)}}
                    """;
            }
        }
        internal string RenderUiConstraintValue(CodeRenderingContext ctx) {
            if (_aggregate is not RootAggregate) throw new InvalidOperationException();

            return $$"""
                /** {{_aggregate.DisplayName}}の各メンバーの制約の具体的な値 */
                export const {{UiConstraingValueName}}: {{UiConstraintTypeName}} = {
                  {{WithIndent(RenderMembers(this), "  ")}}
                }
                """;

            static string RenderMembers(DisplayData displayData) {
                return $$"""
                    {{VALUES_TS}}: {
                    {{displayData.GetOwnMembers().SelectTextTemplate(m => $$"""
                      {{m.PhysicalName}}: {
                    {{If(m.IsRequired, () => $$"""
                        {{UiConstraint.MEMBER_REQUIRED}}: true,
                    """)}}
                    {{If(m.CharacterType != null, () => $$"""
                        {{UiConstraint.MEMBER_CHARACTER_TYPE}}: {{m.CharacterType}},
                    """)}}
                    {{If(m.MaxLength != null, () => $$"""
                        {{UiConstraint.MEMBER_MAX_LENGTH}}: {{m.MaxLength}},
                    """)}}
                    {{If(m.TotalDigit != null, () => $$"""
                        {{UiConstraint.MEMBER_TOTAL_DIGIT}}: {{m.TotalDigit}},
                    """)}}
                    {{If(m.DecimalPlace != null, () => $$"""
                        {{UiConstraint.MEMBER_DECIMAL_PLACE}}: {{m.DecimalPlace}},
                    """)}}
                      },
                    """)}}
                    },
                    {{displayData.GetChildMembers().SelectTextTemplate(desc => $$"""
                    {{desc.PhysicalName}}: {
                      {{WithIndent(RenderMembers(desc), "  ")}}
                    },
                    """)}}
                    """;
            }
        }
        #endregion UI用の制約定義


        #region TypeScript新規オブジェクト作成関数
        /// <summary>
        /// TypeScriptの新規オブジェクト作成関数の名前
        /// </summary>
        internal string TsNewObjectFunction => $"createNew{TsTypeName}";

        internal static string RenderTsNewObjectFunctionRecursively(RootAggregate rootAggregate, CodeRenderingContext ctx) {
            var tree = rootAggregate
                .EnumerateThisAndDescendants()
                .Select(agg => agg switch {
                    RootAggregate root => new DisplayData(root),
                    ChildAggregate child => new DisplayDataChildDescendant(child),
                    ChildrenAggregate children => new DisplayDataChildrenDescendant(children),
                    _ => throw new InvalidOperationException(),
                });

            return $$"""
                //#region 画面表示用データ新規作成用関数
                {{tree.SelectTextTemplate(disp => $$"""
                {{disp.RenderTypeScriptObjectCreationFunction(ctx)}}
                """)}}
                //#endregion 画面表示用データ新規作成用関数
                """;
        }
        private string RenderTypeScriptObjectCreationFunction(CodeRenderingContext ctx) {
            return $$"""
                /** {{_aggregate.DisplayName}}の画面表示用データの新しいインスタンスを作成します。 */
                export const {{TsNewObjectFunction}} = (): {{TsTypeName}} => ({
                  {{VALUES_TS}}: {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                    {{m.PhysicalName}}: {{m.RenderNewObjectCreation()}},
                """)}}
                  },
                {{If(HasLifeCycle, () => $$"""
                  {{EXISTS_IN_DB_TS}}: false,
                  {{WILL_BE_CHANGED_TS}}: true,
                  {{WILL_BE_DELETED_TS}}: false,
                """)}}
                {{If(HasVersion, () => $$"""
                  {{VERSION_TS}}: undefined,
                """)}}
                  {{READONLY_TS}}: {},
                {{GetChildMembers().SelectTextTemplate(c => $$"""
                  {{c.PhysicalName}}: {{c.RenderNewObjectCreation()}},
                """)}}
                })
                """;
        }
        #endregion TypeScript新規オブジェクト作成関数


        #region Valuesの外に定義されるメンバー（Child, Children）
        internal abstract class DisplayDataDescendant : DisplayData {
            internal DisplayDataDescendant(AggregateBase aggregate) : base(aggregate) { }

            internal string PhysicalName => _aggregate.PhysicalName;
            internal string DisplayName => _aggregate.DisplayName;
            internal abstract string CsClassNameAsMember { get; }
            internal abstract string TsTypeNameAsMember { get; }

            internal abstract string RenderNewObjectCreation();
        }

        internal class DisplayDataChildDescendant : DisplayDataDescendant, IInstanceStructurePropertyMetadata {
            internal DisplayDataChildDescendant(ChildAggregate child) : base(child) {
                _child = child;
            }
            private readonly ChildAggregate _child;

            internal override string CsClassNameAsMember => CsClassName;
            internal override string TsTypeNameAsMember => TsTypeName;
            internal override bool HasLifeCycle => _child.HasLifeCycle;

            internal override string RenderNewObjectCreation() {
                return $"{TsNewObjectFunction}()";
            }

            ISchemaPathNode IInstancePropertyMetadata.MappingKey => _child;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;
        }

        internal class DisplayDataChildrenDescendant : DisplayDataDescendant, IInstanceStructurePropertyMetadata {
            internal DisplayDataChildrenDescendant(ChildrenAggregate children) : base(children) { }

            internal override string CsClassNameAsMember => $"List<{CsClassName}>";
            internal override string TsTypeNameAsMember => $"{TsTypeName}[]";
            internal override bool HasLifeCycle => true;

            internal override string RenderNewObjectCreation() {
                return "[]";
            }

            ISchemaPathNode IInstancePropertyMetadata.MappingKey => _aggregate;
            bool IInstanceStructurePropertyMetadata.IsArray => true;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;
        }
        #endregion Valuesの外に定義されるメンバー（Child, Children）
    }
}
