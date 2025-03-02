using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.QueryModelModules {
    /// <summary>
    /// ReadModelの画面表示用データ
    /// </summary>
    internal class DisplayData {

        internal DisplayData(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;


        internal const string BASE_CLASS_NAME = "DisplayDataClassBase";

        /// <summary>C#クラス名</summary>
        internal string CsClassName => $"{_aggregate.PhysicalName}DisplayData";
        /// <summary>TypeScript型名</summary>
        internal string TsTypeName => $"{_aggregate.PhysicalName}DisplayData";

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
        /// 追加・更新・削除のタイミングが親要素と異なるか否か
        /// </summary>
        internal bool HasLifeCycle => _aggregate is RootAggregate || _aggregate is ChildrenAggreagte || Aggregate.Item.Options.HasLifeCycle;
        /// <summary>
        /// 楽観排他制御用のバージョンをもつかどうか
        /// </summary>
        internal bool HasVersion => Aggregate.IsRoot() || Aggregate.Item.Options.HasLifeCycle;


        /// <summary>
        /// C#の基底クラスをレンダリングします。
        /// </summary>
        internal static SourceFile RenderBaseClass() {
            return new() {
                FileName = "DisplayDataClassBase.cs",
                RenderContent = context => {
                    return $$"""
                    /// <summary>
                    /// 画面表示用データの基底クラス
                    /// </summary>
                    public abstract partial class {{BASE_CLASS_NAME}} {
                    }
                    """;
                },
            };
        }


        internal virtual string RenderCSharpDeclaring(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }

        internal virtual string RenderTypeScriptType(CodeRenderingContext ctx) {
            throw new NotImplementedException();
        }
    }
}
