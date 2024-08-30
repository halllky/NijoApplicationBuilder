using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    /// <summary>
    /// UIのカスタマイザー。
    /// 自動生成されたあとのソースコードに手を加える際に手を加えれられる箇所を定義する。
    /// 例えば一覧検索画面に任意のコマンドを追加できるようにするなど。
    /// </summary>
    internal class AutoGeneratedCustomizer : ISummarizedFile {

        internal const string USE_CONTEXT = "useCustomizerContext";
        internal const string CREATE_EMPTY_CONTEXT_VALUE = "createEmptyCustomizerContext";

        /// <summary>
        /// 生成後のソースで外から注入して、中で React context 経由で参照するコンポーネント。ValueMemberまたはRefでのみ使用
        /// </summary>
        internal const string CUSTOM_UI_COMPONENT = "CustomUiComponent";
        /// <summary>
        /// 生成後のソースで外から注入して、中で React context 経由で参照するコンポーネントの名前と型を定義します。
        /// </summary>
        /// <param name="componentName">コンポーネント名</param>
        /// <param name="valueType">value と onChange の型の名前</param>
        /// <param name="additionalProps">value, onChange, readOnly 以外にプロパティがあれば "プロパティ名: 型名" の形の配列</param>
        internal void AddCustomUi(string componentName, string valueType, IEnumerable<string> additionalProps) {
            if (_customUiComponents.TryGetValue(componentName, out var alreadyRegistered)) {
                // コンポーネント名に重複がある場合、プロパティの型が完全一致するなら無視。相違があるならエラー。
                if (alreadyRegistered.ValueType != valueType || alreadyRegistered.AdditionalProps != additionalProps) {
                    throw new InvalidOperationException($"エラー！UIコンポーネント名「{componentName}」が複数回指定されていますが、それぞれでコンポーネントの引数が異なるため同じ名前で宣言できません。");
                } else {
                    return;
                }
            } else {
                _customUiComponents.Add(componentName, (valueType, additionalProps));
            }
        }
        private readonly Dictionary<string, (string ValueType, IEnumerable<string> AdditionalProps)> _customUiComponents = new();

        /// <summary>
        /// React context 直下のメンバー名とその型を定義します。
        /// </summary>
        internal void AddMember(string sourceCode) {
            _sourceCode.Add(sourceCode);
        }
        private readonly List<string> _sourceCode = new();

        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {

            context.ReactProject.AutoGeneratedDir(dir => {
                dir.Generate(new SourceFile {
                    FileName = "autogenerated-customizer.tsx",
                    RenderContent = ctx => {

                        return $$"""
                            import React from 'react'
                            import * as Util from './util'
                            import * as Input from './input'
                            import * as Layout from './collection'
                            import * as AggregateType from './autogenerated-types'
                            import * as AggregateHook from './autogenerated-hooks'

                            /** 自動生成されたあとのソースコードに対してカスタマイズを加えられる箇所の一覧とその設定 */
                            export type AutoGeneratedCustomizer = {
                              /** Raect router のルーティング処理（クライアント側のURLとページの紐づき設定）を編集します。 */
                              modifyRoutes?: (defaultRoutes: { url: string, el: JSX.Element }[]) => ({ url: string, el: JSX.Element })[]
                            {{_sourceCode.SelectTextTemplate(source => $$"""
                              {{WithIndent(source, "  ")}}
                            """)}}
                              {{CUSTOM_UI_COMPONENT}}: {
                            {{_customUiComponents.SelectTextTemplate(kv => $$"""
                                {{kv.Key}}: (props: {
                                  value: {{kv.Value.ValueType}}
                                  onChange: (v: {{kv.Value.ValueType}}) => void
                                  readOnly: boolean | undefined
                            {{kv.Value.AdditionalProps.SelectTextTemplate(prop => $$"""
                                  {{WithIndent(prop, "      ")}}
                            """)}}
                                }) => React.ReactNode
                            """)}}
                              }
                            }

                            // ------------------------------------
                            // 上記の仕組みをアプリケーションの外から注入して中で使うのにReact context を使用する

                            export const {{CREATE_EMPTY_CONTEXT_VALUE}} = (): AutoGeneratedCustomizer => ({
                              {{CUSTOM_UI_COMPONENT}}: {
                            {{_customUiComponents.SelectTextTemplate(kv => $$"""
                                {{kv.Key}}: () => <div className="text-rose-100 bg-rose-950">このコンポーネントは未実装です。カスタムコンポーネント「{{kv.Key}}」を定義しApp.tsxのcustomizerに渡してください。</div>,
                            """)}}
                              },
                            })
                            const CustomizerContext = React.createContext<AutoGeneratedCustomizer>({{CREATE_EMPTY_CONTEXT_VALUE}}())
                            export const CustomizerContextProvider = CustomizerContext.Provider
                            export const useCustomizerContext = () => React.useContext(CustomizerContext)

                            """;
                    },
                });
            });
        }
    }
}