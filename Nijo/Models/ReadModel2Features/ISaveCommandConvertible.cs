using static Nijo.Models.WriteModel2Features.DataClassForSaveBase;
using static Nijo.Models.ReadModel2Features.DataClassForDisplay;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 追加・更新・削除のいずれかのコマンドに変換可能なオブジェクト
    /// </summary>
    internal class ISaveCommandConvertible {

        internal const string INTERFACE_NAME = "ISaveCommandConvertible";
        internal const string GET_SAVE_TYPE = "GetSaveType";

        internal static SourceFile Render() => new SourceFile {
            FileName = "ISaveCommandConvertible.cs",
            RenderContent = context => {
                return $$"""
                    namespace {{context.Config.RootNamespace}};

                    /// <summary>
                    /// 追加・更新・削除のいずれかのコマンドに変換可能なオブジェクト
                    /// </summary>
                    public interface {{INTERFACE_NAME}} {
                        /// <summary>
                        /// このデータがDBに保存済みかどうか。
                        /// つまり新規作成のときはfalse, 閲覧・更新・削除のときはtrue
                        /// </summary>
                        bool {{EXISTS_IN_DB_CS}} { get; }
                        /// <summary>
                        /// 画面上で何らかの変更が加えられてから、保存処理の実行でその変更が確定するまでの間、trueになる。
                        /// </summary>
                        bool {{WILL_BE_CHANGED_CS}} { get; }
                        /// <summary>
                        /// 画面上で削除が指示されてから、保存処理の実行でその削除が確定するまでの間、trueになる。
                        /// </summary>
                        bool {{WILL_BE_DELETED_CS}} { get; }

                        /// <summary>
                        /// このオブジェクトの状態から、保存時に追加・更新・削除のうちどの処理が実行されるべきかを表す区分を返します。
                        /// </summary>
                        public {{ADD_MOD_DEL_ENUM_CS}} {{GET_SAVE_TYPE}}() {
                            if ({{WILL_BE_DELETED_CS}}) return {{ADD_MOD_DEL_ENUM_CS}}.DEL;
                            if (!{{EXISTS_IN_DB_CS}}) return {{ADD_MOD_DEL_ENUM_CS}}.ADD;
                            if ({{WILL_BE_CHANGED_CS}}) return {{ADD_MOD_DEL_ENUM_CS}}.MOD;
                            return {{ADD_MOD_DEL_ENUM_CS}}.NONE;
                        }
                    }
                    """;
            },
        };
    }
}
