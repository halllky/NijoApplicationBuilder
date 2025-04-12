using Nijo.CodeGenerating;
using Nijo.Models.QueryModelModules;
using Nijo.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.ImmutableSchema {
    /// <summary>
    /// モデルの属性の種類。
    /// 単語型、日付型、整数型、…など
    /// </summary>
    public interface IValueMemberType {
        #region SchemaParsing
        /// <summary>
        /// XMLスキーマ定義でこの型を指定するときの型名
        /// </summary>
        string SchemaTypeName { get; }

        /// <summary>
        /// モデルの指定内容の検証を行ないます。
        /// </summary>
        public void Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError);
        #endregion SchemaParsing


        #region CodeGenerating
        /// <summary>
        /// この型の名前がソースコード中に表れるときの物理名
        /// </summary>
        string TypePhysicalName { get; }

        /// <summary>
        /// C#型名（ドメインロジック用）
        /// </summary>
        string CsDomainTypeName { get; }
        /// <summary>
        /// C#型名（EFCoreやJSONとの変換に用いられるプリミティブ型）
        /// </summary>
        string CsPrimitiveTypeName { get; }
        /// <summary>
        /// TypeScript型名
        /// </summary>
        string TsTypeName { get; }

        /// <summary>
        /// 型に由来する生成ソースがある場合はここで登録する
        /// </summary>
        void RegisterDependencies(IMultiAggregateSourceFileManager ctx);
        /// <summary>
        /// 型に由来する生成ソースがある場合はここでレンダリングする
        /// </summary>
        /// </summary>
        void RenderStaticSources(CodeRenderingContext ctx);

        /// <summary>
        /// QueryModelでの検索時の振る舞い。
        /// パスワードなど検索条件に指定することができない型の場合はこれがnullになる。
        /// </summary>
        ValueMemberSearchBehavior? SearchBehavior { get; }
        /// <summary>
        /// UI上の制約がとりうる型
        /// </summary>
        UiConstraint.E_Type UiConstraintType { get; }

        /// <summary>
        /// ダミーデータ生成処理の既定の処理をレンダリングします。
        /// <code>return /* 値 */;</code> の式を返してください。
        /// </summary>
        string RenderCreateDummyDataValueBody(CodeRenderingContext ctx);
        #endregion CodeGenerating
    }


    /// <summary>
    /// QueryModelで検索条件に指定することが可能な <see cref="IValueMemberType"/> について、検索の振る舞い。
    /// </summary>
    public class ValueMemberSearchBehavior {
        /// <summary>
        /// 検索条件のフィルタリングの属性の型（C#）
        /// </summary>
        public required string FilterCsTypeName { get; init; }
        /// <summary>
        /// 検索条件のフィルタリングの属性の型（TypeScript）
        /// </summary>
        public required string FilterTsTypeName { get; init; }
        /// <summary>
        /// 検索条件オブジェクトの新規作成関数でこの型のメンバーがとる空の値。
        /// 検索条件がObjectの場合など、必ずしもundefinedを初期値としてよいわけではないため、型ごとに指定させている。
        /// </summary>
        public required Func<string> RenderTsNewObjectFunctionValue { get; init; }
        /// <summary>
        /// EFCoreを使ったWhere句付加処理のレンダリング
        /// </summary>
        public required Func<FilterStatementRenderingContext, string> RenderFiltering { get; init; }
    }

    public class FilterStatementRenderingContext {
        /// <summary>メンバー情報</summary>
        public required ValueMember Member { get; init; }
        /// <summary>EFCoreのクエリの変数の名前。検索処理はこの変数に再代入を繰り返して実装していく。</summary>
        public required string Query { get; init; }
        /// <summary>検索条件オブジェクトの変数名</summary>
        public required string SearchCondition { get; init; }
        /// <inheritdoc cref="CodeGenerating.CodeRenderingContext"/>
        public required CodeRenderingContext CodeRenderingContext { get; init; }
    }
}

namespace Nijo {
    using Nijo.ImmutableSchema;

    internal static class IValueMemberTypeExtension {

        /// <summary>
        /// (string?) のようなC#のキャスト処理をレンダリングする
        /// </summary>
        internal static string RenderCastToPrimitiveType(this IValueMemberType vmType, bool notNull = false) {
            return vmType.CsDomainTypeName == vmType.CsPrimitiveTypeName
                ? string.Empty
                : $"({vmType.CsPrimitiveTypeName}{(notNull ? "" : "?")})";
        }

        /// <summary>
        /// (XxxxxID?) のようなC#のキャスト処理をレンダリングする
        /// </summary>
        internal static string RenderCastToDomainType(this IValueMemberType vmType, bool notNull = false) {
            return vmType.CsDomainTypeName == vmType.CsPrimitiveTypeName
                ? string.Empty
                : $"({vmType.CsDomainTypeName}{(notNull ? "" : "?")})";
        }
    }
}
