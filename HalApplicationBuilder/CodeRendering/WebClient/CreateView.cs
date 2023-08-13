﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン: 17.0.0.0
//  
//     このファイルへの変更は、正しくない動作の原因になる可能性があり、
//     コードが再生成されると失われます。
// </auto-generated>
// ------------------------------------------------------------------------------
namespace HalApplicationBuilder.CodeRendering.WebClient
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using HalApplicationBuilder.Core;
    using HalApplicationBuilder.CodeRendering.Presentation;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class CreateView : CreateViewBase
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write(@"import { useState, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { FieldValues, SubmitHandler, useForm } from 'react-hook-form';
import { BookmarkSquareIcon } from '@heroicons/react/24/outline';
import { IconButton } from '../../components/IconButton';
import { InlineMessageBar, BarMessage } from '../../components/InlineMessageBar';
import { useHttpRequest } from '../../hooks/useHttpRequest';
import { useAppContext } from ""../../hooks/AppContext""
import { ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_instance.Item.TypeScriptTypeName));
            this.Write(" as DataDetail } from \'../../");
            this.Write(this.ToStringHelper.ToStringWithCulture(types.ImportName));
            this.Write(@"'

export default function () {

    const { register, handleSubmit } = useForm()
    const navigate = useNavigate()
    const { post } = useHttpRequest()
    const [, dispatch] = useAppContext()
    const [errorMessages, setErrorMessages] = useState<BarMessage[]>([])
    const onSave: SubmitHandler<FieldValues> = useCallback(async data => {
        const response = await post<DataDetail>(`");
            this.Write(this.ToStringHelper.ToStringWithCulture(GetCreateCommandApi()));
            this.Write("`, data)\r\n        if (response.ok) {\r\n            dispatch({ type: \'pushMsg\', msg" +
                    ": `${response.data.");
            this.Write(this.ToStringHelper.ToStringWithCulture(AggregateInstanceBase.INSTANCE_NAME));
            this.Write("}を作成しました。` })\r\n            setErrorMessages([])\r\n            const encoded = wind" +
                    "ow.encodeURI(response.data.");
            this.Write(this.ToStringHelper.ToStringWithCulture(AggregateInstanceBase.INSTANCE_KEY));
            this.Write("!)\r\n            navigate(`");
            this.Write(this.ToStringHelper.ToStringWithCulture(GetSingleViewUrl()));
            this.Write(@"/${encoded}`)
        } else {
            setErrorMessages([...errorMessages, ...response.errors])
        }
    }, [post, navigate, errorMessages, setErrorMessages, dispatch])

    return (
        <form className=""page-content-root"" onSubmit={handleSubmit(onSave)}>
            <h1 className=""text-base font-semibold select-none py-1"">
                <Link to=""");
            this.Write(this.ToStringHelper.ToStringWithCulture(GetMultiViewUrl()));
            this.Write("\">");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregate.Item.DisplayName));
            this.Write("</Link>&nbsp;新規作成\r\n            </h1>\r\n            <InlineMessageBar value={errorM" +
                    "essages} onChange={setErrorMessages} />\r\n            <div className=\"flex flex-c" +
                    "ol space-y-1 p-1 bg-neutral-200\">\r\n");
 foreach (var prop in _instance.GetSchalarProperties(_ctx.Config)) { 
            this.Write("                <div className=\"flex\">\r\n                    <div className=\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(PropNameWidth));
            this.Write("\">\r\n                        <span className=\"text-sm select-none\">\r\n             " +
                    "               ");
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.PropertyName));
            this.Write("\r\n                        </span>\r\n                    </div>\r\n                  " +
                    "  <div className=\"flex-1\">\r\n");
 foreach (var line in RenderForm(prop)) { 
            this.Write("                        ");
            this.Write(this.ToStringHelper.ToStringWithCulture(line));
            this.Write("\r\n");
 } 
            this.Write("                    </div>\r\n                </div>\r\n");
 } 
            this.Write("            </div>\r\n            <IconButton fill icon={BookmarkSquareIcon} classN" +
                    "ame=\"self-start\">保存</IconButton>\r\n        </form>\r\n    )\r\n}\r\n");
            return this.GenerationEnvironment.ToString();
        }
    }
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public class CreateViewBase
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
