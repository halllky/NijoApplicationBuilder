using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using Nijo.ImmutableSchema;
using Nijo.Models.DataModelModules;
using Nijo.Util.DotnetEx;
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
            Aggregate = aggregate;
        }
        internal AggregateBase Aggregate { get; }


        /// <summary>C#クラス名</summary>
        internal string CsClassName => $"{Aggregate.PhysicalName}DisplayData";
        /// <summary>C#クラス名（values）</summary>
        internal string CsValuesClassName => $"{Aggregate.PhysicalName}DisplayDataValues";
        /// <summary>TypeScript型名</summary>
        internal string TsTypeName => $"{Aggregate.PhysicalName}DisplayData";

        /// <summary>画面上で独自の追加削除のライフサイクルを持つかどうか</summary>
        internal virtual bool HasLifeCycle => true;
        /// <summary>楽観排他制御用のバージョンを持つかどうか</summary>
        internal virtual bool HasVersion => Aggregate is RootAggregate;

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

        public const string TO_CREATE_COMMAND = "ToCreateCommand";
        public const string TO_UPDATE_COMMAND = "ToUpdateCommand";
        public const string TO_DELETE_COMMAND = "ToDeleteCommand";

        /// <summary>
        /// 子孫要素でなく自身のメンバーはこのオブジェクトの中に列挙される
        /// </summary>
        internal ValuesContainer Values => _values ??= new ValuesContainer(Aggregate);
        private ValuesContainer? _values;

        /// <summary>
        /// 子要素を列挙する。
        /// </summary>
        internal IEnumerable<DisplayDataDescendant> GetChildMembers() {
            foreach (var member in Aggregate.GetMembers()) {
                if (member is ChildAggregate child) {
                    yield return new DisplayDataChildDescendant(child);

                } else if (member is ChildrenAggregate children) {
                    yield return new DisplayDataChildrenDescendant(children);

                }
            }
        }

        IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() {
            yield return Values;

            foreach (var childMember in GetChildMembers()) {
                yield return (IInstancePropertyMetadata)childMember;
            }
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

            // SaveCommandへの変換メソッドを生成するかどうかの判定
            bool shouldGenerateSaveCommandMethods = Aggregate is RootAggregate root && root.Model is Nijo.Models.DataModel;

            return $$"""
                /// <summary>
                /// {{Aggregate.DisplayName}}の画面表示用データ。
                /// </summary>
                public partial class {{CsClassName}} {
                    /// <summary>{{Aggregate.DisplayName}}自身が持つ値</summary>
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
                {{If(shouldGenerateSaveCommandMethods, () => $$"""

                    {{WithIndent(RenderConvertingToSaveCommand(), "    ")}}
                """)}}
                }

                /// <summary>
                /// <see cref="{{CsClassName}}/> の{{VALUES_CS}}の型
                /// </summary>
                public partial class {{CsValuesClassName}} {
                {{Values.GetMembers().SelectTextTemplate(m => $$"""
                    {{WithIndent(m.RenderCsDeclaration(), "    ")}}
                """)}}
                }

                /// <summary>
                /// <see cref="{{CsClassName}}/> の{{READONLY_CS}}の型
                /// </summary>
                public partial class {{ReadOnlyDataCsClassName}} {
                    /// <summary>{{Aggregate.DisplayName}}全体が読み取り専用か否か</summary>
                    [JsonPropertyName("{{ALL_READONLY_TS}}")]
                    public bool {{ALL_READONLY_CS}} { get; set; }
                {{Values.GetMembers().SelectTextTemplate(member => $$"""
                    /// <summary>{{member.DisplayName}}が読み取り専用か否か</summary>
                    public bool {{member.PropertyName}} { get; set; }
                """)}}
                }
                """;
        }

        private string RenderTypeScriptType(CodeRenderingContext ctx) {
            return $$"""
                /** {{Aggregate.DisplayName}}の画面表示用データ。 */
                export type {{TsTypeName}} = {
                  /** 値 */
                  {{VALUES_TS}}: {
                {{Values.GetMembers().SelectTextTemplate(m => $$"""
                    {{WithIndent(m.RenderTsDeclaration(), "    ")}}
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
                    /** {{Aggregate.DisplayName}}全体が読み取り専用か否か */
                    {{ALL_READONLY_TS}}?: boolean
                {{Values.GetMembers().SelectTextTemplate(member => $$"""
                    /** {{member.DisplayName}}が読み取り専用か否か */
                    {{member.PropertyName}}?: boolean
                """)}}
                  }
                }
                """;
        }
        #endregion レンダリング


        #region Values
        /// <summary>
        /// Valuesオブジェクトそのもの
        /// </summary>
        internal class ValuesContainer : IInstanceStructurePropertyMetadata {
            public ValuesContainer(AggregateBase aggregate) {
                _aggregate = aggregate;
            }
            private readonly AggregateBase _aggregate;

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => ISchemaPathNode.Empty;
            string IInstancePropertyMetadata.PropertyName => VALUES_CS;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstanceStructurePropertyMetadata.CsType => new DisplayData(_aggregate).ValueCsClassName;

            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();

            internal IEnumerable<IDisplayDataMemberInValues> GetMembers() {
                foreach (var member in _aggregate.GetMembers()) {
                    if (member is ValueMember vm) {
                        yield return new DisplayDataValueMember(vm);

                    } else if (member is RefToMember refTo) {
                        yield return new DisplayDataRefMember(refTo);

                    }
                }
            }
        }
        /// <summary>
        /// Valuesオブジェクトの中のメンバー
        /// </summary>
        internal interface IDisplayDataMemberInValues : IUiConstraintValue, IInstancePropertyMetadata {
            UiConstraint.E_Type UiConstraintType { get; }

            string RenderCsDeclaration();
            string RenderTsDeclaration();

            string RenderNewObjectCreation();
        }
        /// <summary>
        /// Valuesオブジェクトの中のValueMember
        /// </summary>
        internal class DisplayDataValueMember : IDisplayDataMemberInValues, IInstanceValuePropertyMetadata {
            internal DisplayDataValueMember(ValueMember vm) {
                Member = vm;
            }
            internal ValueMember Member { get; }

            public string PropertyName => Member.PhysicalName;
            public string DisplayName => Member.DisplayName;
            public UiConstraint.E_Type UiConstraintType => Member.Type.UiConstraintType;

            IValueMemberType IInstanceValuePropertyMetadata.Type => Member.Type;
            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Member;
            string IInstancePropertyMetadata.PropertyName => PropertyName;

            public bool IsRequired => Member.IsKey || Member.IsRequired;
            public string? CharacterType => Member.CharacterType;
            public int? MaxLength => Member.MaxLength;
            public int? TotalDigit => Member.TotalDigit;
            public int? DecimalPlace => Member.DecimalPlace;

            public string RenderCsDeclaration() {
                return $$"""
                    /// <summary>{{Member.DisplayName}}</summary>
                    public {{Member.Type.CsDomainTypeName}}? {{PropertyName}} { get; set; }
                    """;
            }
            public string RenderTsDeclaration() {
                return $$"""
                    {{PropertyName}}?: {{Member.Type.TsTypeName}}
                    """;
            }

            public string RenderNewObjectCreation() {
                return "undefined";
            }
        }
        /// <summary>
        /// Valuesオブジェクトの中のRefTo
        /// </summary>
        internal class DisplayDataRefMember : IDisplayDataMemberInValues, IInstanceStructurePropertyMetadata {
            internal DisplayDataRefMember(RefToMember refTo) {
                Member = refTo;
                RefEntry = new DisplayDataRef.Entry(refTo.RefTo);
            }
            internal RefToMember Member { get; }
            internal DisplayDataRef.Entry RefEntry;

            public string PropertyName => Member.PhysicalName;
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
                    public {{RefEntry.CsClassName}} {{PropertyName}} { get; set; } = new();
                    """;
            }
            public string RenderTsDeclaration() {
                return $$"""
                    {{PropertyName}}: {{RefEntry.TsTypeName}}
                    """;
            }

            public string RenderNewObjectCreation() {
                return $"{RefEntry.TsNewObjectFunction}()";
            }

            internal IEnumerable<DisplayDataRef.IRefDisplayDataMember> GetMembers() {
                return RefEntry.GetMembers();
            }

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Member;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.PropertyName => PropertyName;
            string IInstanceStructurePropertyMetadata.CsType => RefEntry.CsClassName;
            IEnumerable<IInstancePropertyMetadata> IInstancePropertyOwnerMetadata.GetMembers() => GetMembers();
        }
        #endregion Values


        #region UI用の制約定義
        internal string UiConstraintTypeName => $"{Aggregate.PhysicalName}ConstraintType";
        internal string UiConstraingValueName => $"{Aggregate.PhysicalName}Constraints";
        internal string RenderUiConstraintType(CodeRenderingContext ctx) {
            if (Aggregate is not RootAggregate) throw new InvalidOperationException();

            return $$"""
                /** {{Aggregate.DisplayName}}の各メンバーの制約の型 */
                type {{UiConstraintTypeName}} = {
                  {{WithIndent(RenderMembers(this), "  ")}}
                }
                """;

            static string RenderMembers(DisplayData displayData) {
                return $$"""
                    {{VALUES_TS}}: {
                    {{displayData.Values.GetMembers().SelectTextTemplate(m => $$"""
                      {{m.PropertyName}}: Util.{{m.UiConstraintType}}
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
            if (Aggregate is not RootAggregate) throw new InvalidOperationException();

            return $$"""
                /** {{Aggregate.DisplayName}}の各メンバーの制約の具体的な値 */
                export const {{UiConstraingValueName}}: {{UiConstraintTypeName}} = {
                  {{WithIndent(RenderMembers(this), "  ")}}
                }
                """;

            static string RenderMembers(DisplayData displayData) {
                return $$"""
                    {{VALUES_TS}}: {
                    {{displayData.Values.GetMembers().SelectTextTemplate(m => $$"""
                      {{m.PropertyName}}: {
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
                /** {{Aggregate.DisplayName}}の画面表示用データの新しいインスタンスを作成します。 */
                export const {{TsNewObjectFunction}} = (): {{TsTypeName}} => ({
                  {{VALUES_TS}}: {
                {{Values.GetMembers().SelectTextTemplate(m => $$"""
                    {{m.PropertyName}}: {{m.RenderNewObjectCreation()}},
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

            internal string PhysicalName => Aggregate.PhysicalName;
            internal string DisplayName => Aggregate.DisplayName;
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

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => _child;
            bool IInstanceStructurePropertyMetadata.IsArray => false;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;
            string IInstanceStructurePropertyMetadata.CsType => CsClassName;
        }

        internal class DisplayDataChildrenDescendant : DisplayDataDescendant, IInstanceStructurePropertyMetadata {
            internal DisplayDataChildrenDescendant(ChildrenAggregate children) : base(children) {
                ChildrenAggregate = children;
            }

            internal ChildrenAggregate ChildrenAggregate { get; }

            internal override string CsClassNameAsMember => $"List<{CsClassName}>";
            internal override string TsTypeNameAsMember => $"{TsTypeName}[]";
            internal override bool HasLifeCycle => true;

            internal override string RenderNewObjectCreation() {
                return "[]";
            }

            ISchemaPathNode IInstancePropertyMetadata.SchemaPathNode => Aggregate;
            bool IInstanceStructurePropertyMetadata.IsArray => true;
            string IInstancePropertyMetadata.PropertyName => PhysicalName;
            string IInstanceStructurePropertyMetadata.CsType => CsClassName;
        }
        #endregion Valuesの外に定義されるメンバー（Child, Children）


        #region SaveCommandへの変換
        private string RenderConvertingToSaveCommand() {
            var createCommand = new SaveCommand(Aggregate, SaveCommand.E_Type.Create);
            var udpateCommand = new SaveCommand(Aggregate, SaveCommand.E_Type.Update);
            var deleteCommand = new SaveCommand(Aggregate, SaveCommand.E_Type.Delete);

            var right = new Variable("this", this);
            var dict = right
                .Create1To1PropertiesRecursively()
                .ToDictionary(x => x.Metadata.SchemaPathNode.ToMappingKey());

            return $$"""
                /// <summary>
                /// このインスタンスを <see cref="{{createCommand.CsClassName}}"/> に変換します。
                /// </summary>
                public {{createCommand.CsClassNameCreate}} {{TO_CREATE_COMMAND}}() {
                    return new {{createCommand.CsClassNameCreate}} {
                        {{WithIndent(RenderBody(createCommand, right, dict), "        ")}}
                    };
                }
                /// <summary>
                /// このインスタンスを <see cref="{{udpateCommand.CsClassName}}"/> に変換します。
                /// </summary>
                public {{udpateCommand.CsClassNameUpdate}} {{TO_UPDATE_COMMAND}}() {
                    return new {{udpateCommand.CsClassNameUpdate}} {
                        {{WithIndent(RenderBody(udpateCommand, right, dict), "        ")}}
                        {{SaveCommand.VERSION}} = this.{{VERSION_CS}},
                    };
                }
                /// <summary>
                /// このインスタンスを <see cref="{{deleteCommand.CsClassName}}"/> に変換します。
                /// </summary>
                public {{deleteCommand.CsClassNameDelete}} {{TO_DELETE_COMMAND}}() {
                    return new {{deleteCommand.CsClassNameDelete}} {
                        {{WithIndent(RenderBody(deleteCommand, right, dict), "        ")}}
                        {{SaveCommand.VERSION}} = this.{{VERSION_CS}},
                    };
                }
                """;

            static IEnumerable<string> RenderBody(IInstancePropertyOwnerMetadata left, IInstancePropertyOwner right, IReadOnlyDictionary<SchemaNodeIdentity, IInstanceProperty> rigthMembers) {

                foreach (var member in left.GetMembers()) {
                    if (member is IInstanceValuePropertyMetadata vp) {
                        var rightPath = rigthMembers.TryGetValue(member.SchemaPathNode.ToMappingKey(), out var source)
                            ? $"{source.Root.Name}.{source.GetPathFromInstance().Select(p => p.Metadata.PropertyName).Join("?.")}"
                            : "null";
                        yield return $$"""
                            {{member.PropertyName}} = {{rightPath}},
                            """;

                    } else if (member is IInstanceStructurePropertyMetadata sp) {

                        if (sp.IsArray) {
                            var arrayPath = rigthMembers.TryGetValue(sp.SchemaPathNode.ToMappingKey(), out var source)
                                ? $"{source.Root.Name}.{source.GetPathFromInstance().Select(p => p.Metadata.PropertyName).Join("?.")}"
                              : throw new InvalidOperationException($"右辺にChildrenのXElementが無い: {sp.DisplayName}");

                            // 辞書に、ラムダ式内部で右辺に使用できるプロパティを加える
                            var dict2 = new Dictionary<SchemaNodeIdentity, IInstanceProperty>(rigthMembers);
                            var loopVar = new Variable(((ChildrenAggregate)sp.SchemaPathNode).GetLoopVarName(), (IInstancePropertyOwnerMetadata)source.Metadata);
                            foreach (var descendant in loopVar.Create1To1PropertiesRecursively()) {
                                dict2.Add(descendant.Metadata.SchemaPathNode.ToMappingKey(), descendant);
                            }

                            yield return $$"""
                                {{member.PropertyName}} = {{arrayPath}}?.Select({{loopVar.Name}} => new {{sp.CsType}}() {
                                    {{WithIndent(RenderBody(sp, loopVar, dict2), "    ")}}
                                }).ToList() ?? [],
                                """;

                        } else {
                            yield return $$"""
                                {{member.PropertyName}} = new() {
                                    {{WithIndent(RenderBody(sp, right, rigthMembers), "    ")}}
                                },
                                """;
                        }

                    }
                }
            }
        }
        #endregion SaveCommandへの変換
    }
}
