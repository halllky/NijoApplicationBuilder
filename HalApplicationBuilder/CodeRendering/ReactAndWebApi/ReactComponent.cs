// ------------------------------------------------------------------------------
// <auto-generated>
//     ���̃R�[�h�̓c�[���ɂ���Đ�������܂����B
//     �����^�C�� �o�[�W����: 17.0.0.0
//  
//     ���̃t�@�C���ւ̕ύX�́A�������Ȃ�����̌����ɂȂ�\��������A
//     �R�[�h���Đ��������Ǝ����܂��B
// </auto-generated>
// ------------------------------------------------------------------------------
namespace HalApplicationBuilder.CodeRendering.ReactAndWebApi
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using HalApplicationBuilder.Core;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class ReactComponent : ReactComponentBase
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write(@"
import React, { useState, useCallback } from 'react';
import { useCtrlS } from '../hooks/useCtrlS';
import { useAppContext } from '../hooks/AppContext';
import { AgGridReact } from 'ag-grid-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';
import { FieldValues, SubmitHandler, useForm } from 'react-hook-form';
import { UUID } from 'uuidjs'
import { BookmarkIcon, ChevronDownIcon, ChevronUpIcon, MagnifyingGlassIcon, PlusIcon, BookmarkSquareIcon } from '@heroicons/react/24/outline';
import { IconButton } from '../components/IconButton';
import { InlineMessageBar, BarMessage } from '../components/InlineMessageBar';

export const ");
            this.Write(this.ToStringHelper.ToStringWithCulture(MultiViewComponentName));
            this.Write(@" = () => {

    const [{ apiDomain }, dispatch] = useAppContext()
    useCtrlS(() => {
        dispatch({ type: 'pushMsg', msg: '�ۑ����܂����B' })
    })

    const [param, setParam] = useState<FieldValues>({})
    const { register, handleSubmit, reset } = useForm()
    const onSearch: SubmitHandler<FieldValues> = useCallback(data => {
        setParam(data)
    }, [])
    const onClear = useCallback((e: React.MouseEvent) => {
        reset()
        e.preventDefault()
    }, [reset])
    const { data, isFetching } = useQuery({
        queryKey: ['");
            this.Write(this.ToStringHelper.ToStringWithCulture(UseQueryKey));
            this.Write("\', JSON.stringify(param)],\r\n        queryFn: async () => {\r\n            const jso" +
                    "n = JSON.stringify(param)\r\n            const encoded = window.encodeURI(json)\r\n " +
                    "           const response = await fetch(`${apiDomain}");
            this.Write(this.ToStringHelper.ToStringWithCulture(_controller.SearchCommandApi));
            this.Write(@"?param=${encoded}`)
            if (!response.ok) throw new Error('Network response was not OK.')
            return await response.json()
        },
        onError: error => {
            dispatch({ type: 'pushMsg', msg: `ERROR!: ${JSON.stringify(error)}` })
        },
    })

    const navigate = useNavigate()
    const toCreateView = useCallback(() => {
        navigate('");
            this.Write(this.ToStringHelper.ToStringWithCulture(CreateViewUrl));
            this.Write(@"')
    }, [navigate])

    const [expanded, setExpanded] = useState(true)

    if (isFetching) return <></>

    return (
        <div className=""page-content-root"">

            <div className=""flex flex-row justify-start items-center space-x-2"">
                <div className='flex-1 flex flex-row items-center space-x-1 cursor-pointer' onClick={() => setExpanded(!expanded)}>
                    <h1 className=""text-base font-semibold select-none py-1"">
                        ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Aggregate.Item.DisplayName));
            this.Write(@"
                    </h1>
                    {expanded
                        ? <ChevronDownIcon className=""w-4"" />
                        : <ChevronUpIcon className=""w-4"" />}
                </div>
                <IconButton underline icon={PlusIcon} onClick={toCreateView}>�V�K�쐬</IconButton>
                <IconButton underline icon={BookmarkIcon}>���̌���������ۑ�</IconButton>
            </div>

            <form className={`${expanded ? '' : 'hidden'} flex flex-col space-y-1`} onSubmit={handleSubmit(onSearch)}>
");
 PushIndent("                "); 
 RenderSearchCondition(); 
 PopIndent(); 
            this.Write(@"                <div className='flex flex-row justify-start space-x-1'>
                    <IconButton fill icon={MagnifyingGlassIcon}>����</IconButton>
                    <IconButton outline onClick={onClear}>�N���A</IconButton>
                </div>
            </form>

            <div className=""ag-theme-alpine compact flex-1"">
                <AgGridReact
                    rowData={data || []}
                    columnDefs={columnDefs}
                    multiSortKey='ctrl'
                    undoRedoCellEditing
                    undoRedoCellEditingLimit={20}>
                </AgGridReact>
            </div>
        </div>
    )
}

const columnDefs = [
    {
        resizable: true,
        width: 50,
        cellRenderer: ({ data }: { data: { ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchResult.INSTANCE_KEY_PROP_NAME));
            this.Write(": string } }) => {\r\n            const encoded = window.encodeURI(data.");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchResult.INSTANCE_KEY_PROP_NAME));
            this.Write(")\r\n            return <Link to={`");
            this.Write(this.ToStringHelper.ToStringWithCulture(SingleViewUrl));
            this.Write("/${encoded}`} className=\"text-blue-400\">�ڍ�</Link>\r\n        },\r\n    },\r\n");
 foreach (var member in _searchResult.GetMembers()) { 
            this.Write("    { field: \'");
            this.Write(this.ToStringHelper.ToStringWithCulture(member.Name));
            this.Write("\', resizable: true, sortable: true, editable: true },\r\n");
 } 
            this.Write("]\r\n\r\nexport const ");
            this.Write(this.ToStringHelper.ToStringWithCulture(CreateViewComponentName));
            this.Write(@" = () => {

    const { register, handleSubmit } = useForm()
    const navigate = useNavigate()
    const [{ apiDomain },] = useAppContext()
    const [errorMessages, setErrorMessages] = useState<BarMessage[]>([])
    const onSave: SubmitHandler<FieldValues> = useCallback(async data => {
        const response = await fetch(`${apiDomain}");
            this.Write(this.ToStringHelper.ToStringWithCulture(_controller.CreateCommandApi));
            this.Write(@"`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
        })
        if (response.ok) {
            setErrorMessages([])
            const { instanceKey } = JSON.parse(await response.text())
            const encoded = window.encodeURI(instanceKey)
            navigate(`");
            this.Write(this.ToStringHelper.ToStringWithCulture(SingleViewUrl));
            this.Write(@"/${encoded}`)
        } else {
            const errors: string[] = JSON.parse(await response.text())
            setErrorMessages([...errorMessages, ...errors.map(text => ({ uuid: UUID.generate(), text }))])
        }
    }, [apiDomain, navigate, errorMessages])

    return (
        <form className=""page-content-root"" onSubmit={handleSubmit(onSave)}>
            <h1 className=""text-base font-semibold select-none py-1"">
                <Link to=""");
            this.Write(this.ToStringHelper.ToStringWithCulture(MultiViewUrl));
            this.Write("\">");
            this.Write(this.ToStringHelper.ToStringWithCulture(Aggregate.Item.DisplayName));
            this.Write("</Link>&nbsp;�V�K�쐬\r\n            </h1>\r\n            <InlineMessageBar value={errorM" +
                    "essages} onChange={setErrorMessages} />\r\n            <div className=\"flex flex-c" +
                    "ol space-y-1\">\r\n");
 PushIndent("                "); 
 RenderCreateViewContents(); 
 PopIndent(); 
            this.Write("            </div>\r\n            <IconButton fill icon={BookmarkSquareIcon} classN" +
                    "ame=\"self-start\">�ۑ�</IconButton>\r\n        </form>\r\n    )\r\n}\r\n\r\nexport const ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SingleViewComponentName));
            this.Write(@" = () => {

    const [{ apiDomain }, dispatch] = useAppContext()

    const { instanceKey } = useParams()
    const [fetched, setFetched] = useState(false)
    const defaultValues = useCallback(async () => {
        if (!instanceKey) return undefined
        const encoded = window.encodeURI(instanceKey)
        const response = await fetch(`${apiDomain}");
            this.Write(this.ToStringHelper.ToStringWithCulture(SingleViewUrl));
            this.Write(@"/${encoded}`)
        setFetched(true)
        if (response.ok) {
            const data = await response.text()
            return JSON.parse(data)
        } else {
            return undefined
        }
    }, [instanceKey, apiDomain])

    const { register, handleSubmit } = useForm({ defaultValues })
    const [errorMessages, setErrorMessages] = useState<BarMessage[]>([])
    const onSave: SubmitHandler<FieldValues> = useCallback(async data => {
        const response = await fetch(`${apiDomain}");
            this.Write(this.ToStringHelper.ToStringWithCulture(_controller.UpdateCommandApi));
            this.Write(@"`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
        })
        if (response.ok) {
            setErrorMessages([])
            dispatch({ type: 'pushMsg', msg: '�X�V���܂����B' })
        } else {
            const errors: string[] = JSON.parse(await response.text())
            setErrorMessages([...errorMessages, ...errors.map(text => ({ uuid: UUID.generate(), text }))])
        }
    }, [apiDomain, errorMessages, dispatch])

    if (!fetched) return <></>

    return (
        <form className=""page-content-root"" onSubmit={handleSubmit(onSave)}>
            <h1 className=""text-base font-semibold select-none py-1"">
                <Link to=""");
            this.Write(this.ToStringHelper.ToStringWithCulture(MultiViewUrl));
            this.Write("\">");
            this.Write(this.ToStringHelper.ToStringWithCulture(Aggregate.Item.DisplayName));
            this.Write("</Link>\r\n                &nbsp;&#047;&nbsp;\r\n                <span className=\"sel" +
                    "ect-all\">TODO:INSTANCENAME</span>\r\n            </h1>\r\n            <InlineMessage" +
                    "Bar value={errorMessages} onChange={setErrorMessages} />\r\n");
 PushIndent("                "); 
 RenderSingleViewContents(); 
 PopIndent(); 
            this.Write("            <IconButton fill icon={BookmarkSquareIcon} className=\"self-start\">�X�V<" +
                    "/IconButton>\r\n        </form>\r\n    )\r\n}\r\n");
            return this.GenerationEnvironment.ToString();
        }
    }
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public class ReactComponentBase
    {
        #region Fields
        private global::System.Text.StringBuilder generationEnvironmentField;
        private global::System.CodeDom.Compiler.CompilerErrorCollection errorsField;
        private global::System.Collections.Generic.List<int> indentLengthsField;
        private string currentIndentField = "";
        private bool endsWithNewline;
        private global::System.Collections.Generic.IDictionary<string, object> sessionField;
        #endregion
        #region Properties
        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        public System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.generationEnvironmentField == null))
                {
                    this.generationEnvironmentField = new global::System.Text.StringBuilder();
                }
                return this.generationEnvironmentField;
            }
            set
            {
                this.generationEnvironmentField = value;
            }
        }
        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public System.CodeDom.Compiler.CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errorsField == null))
                {
                    this.errorsField = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errorsField;
            }
        }
        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private System.Collections.Generic.List<int> indentLengths
        {
            get
            {
                if ((this.indentLengthsField == null))
                {
                    this.indentLengthsField = new global::System.Collections.Generic.List<int>();
                }
                return this.indentLengthsField;
            }
        }
        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return this.currentIndentField;
            }
        }
        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }
        #endregion
        #region Transform-time helpers
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (((this.GenerationEnvironment.Length == 0) 
                        || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            this.Write(textToAppend);
            this.GenerationEnvironment.AppendLine();
            this.endsWithNewline = true;
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.Write(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if ((indent == null))
            {
                throw new global::System.ArgumentNullException("indent");
            }
            this.currentIndentField = (this.currentIndentField + indent);
            this.indentLengths.Add(indent.Length);
        }
        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if ((this.indentLengths.Count > 0))
            {
                int indentLength = this.indentLengths[(this.indentLengths.Count - 1)];
                this.indentLengths.RemoveAt((this.indentLengths.Count - 1));
                if ((indentLength > 0))
                {
                    returnValue = this.currentIndentField.Substring((this.currentIndentField.Length - indentLength));
                    this.currentIndentField = this.currentIndentField.Remove((this.currentIndentField.Length - indentLength));
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            this.indentLengths.Clear();
            this.currentIndentField = "";
        }
        #endregion
        #region ToString Helpers
        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProviderField  = global::System.Globalization.CultureInfo.InvariantCulture;
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProviderField ;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProviderField  = value;
                    }
                }
            }
            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                System.Type t = objectToConvert.GetType();
                System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null))
                {
                    return objectToConvert.ToString();
                }
                else
                {
                    return ((string)(method.Invoke(objectToConvert, new object[] {
                                this.formatProviderField })));
                }
            }
        }
        private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();
        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this.toStringHelperField;
            }
        }
        #endregion
    }
    #endregion
}