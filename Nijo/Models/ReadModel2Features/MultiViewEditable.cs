using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 一括編集画面
    /// </summary>
    internal class MultiViewEditable : IReactPage {
        public string Url => throw new NotImplementedException();
        public string DirNameInPageDir => throw new NotImplementedException();
        public string ComponentPhysicalName => throw new NotImplementedException();
        public bool ShowMenu => throw new NotImplementedException();
        public string? LabelInMenu => throw new NotImplementedException();

        public SourceFile GetSourceFile() => new() {
            FileName = "multi-view-editable.tsx",
            RenderContent = ctx => {

                return $$"""
                    import React, { useCallback, useEffect, useMemo, useRef, useState, useReducer } from 'react'
                    import { useEvent } from 'react-use-event-hook'
                    import { Link, useLocation } from 'react-router-dom'
                    import { useFieldArray, FormProvider } from 'react-hook-form'
                    import * as Icon from '@heroicons/react/24/outline'
                    import { ImperativePanelHandle, Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
                    import * as Util from '../../util'
                    import * as Input from '../../input'
                    import * as Layout from '../../collection'
                    import * as AggregateType from '../../autogenerated-types'
                    import * as AggregateHook from '../../autogenerated-hooks'
                    import { {{AutoGeneratedCustomizer.USE_CONTEXT}} } from '../../autogenerated-customizer'

                    const VForm2 = Layout.VForm2

                    export default function () {


                      return (
                        <div>
                      )
                    }
                    """;
            },
        };
    }
}