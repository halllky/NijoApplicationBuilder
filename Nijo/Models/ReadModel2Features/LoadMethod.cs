using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 検索処理
    /// </summary>
    internal class LoadMethod {
        internal LoadMethod(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string ReactHookName =>
             $$"""
                TODO #35
                """;

        /// <summary>
        /// クライアント側から検索処理を呼び出すReact hook をレンダリングします。
        /// </summary>
        internal string RenderReactHook(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }

        /// <summary>
        /// 検索処理のASP.NET Core Controller アクションをレンダリングします。
        /// </summary>
        internal string RenderControllerAction(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }

        /// <summary>
        /// 検索処理の抽象部分をレンダリングします。
        /// </summary>
        internal string RenderAppSrvAbstractMethod(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }

        /// <summary>
        /// 検索処理の基底処理をレンダリングします。
        /// パラメータの検索条件によるフィルタリング、
        /// パラメータの並び順指定順によるソート、
        /// パラメータのskip, take によるページングを行います。
        /// </summary>
        internal string RenderAppSrvBaseMethod(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }
    }
}
