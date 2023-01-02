﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HalApplicationBuilder.AspNetMvc {
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System;
    
    
    public partial class MvcModelsTemplate : MvcModelsTemplateBase {
        
        public virtual string TransformText() {
            this.GenerationEnvironment = null;
            
            #line 6 ""
            this.Write("\nnamespace ");
            
            #line default
            #line hidden
            
            #line 7 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(Namespace));
            
            #line default
            #line hidden
            
            #line 7 ""
            this.Write(" {\n    using System;\n    using System.Collections.Generic;\n\n");
            
            #line default
            #line hidden
            
            #line 11 ""
 /* 検索条件DTO */ 
            
            #line default
            #line hidden
            
            #line 12 ""
 foreach (var classDef in SearchConditionClasses) { 
            
            #line default
            #line hidden
            
            #line 13 ""
            this.Write("    public class ");
            
            #line default
            #line hidden
            
            #line 13 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(classDef.ClassName));
            
            #line default
            #line hidden
            
            #line 13 ""
            this.Write(" {\n");
            
            #line default
            #line hidden
            
            #line 14 ""
 foreach (var prop in classDef.Properties) { 
            
            #line default
            #line hidden
            
            #line 15 ""
            this.Write("        public ");
            
            #line default
            #line hidden
            
            #line 15 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.CSharpTypeName));
            
            #line default
            #line hidden
            
            #line 15 ""
            this.Write(" ");
            
            #line default
            #line hidden
            
            #line 15 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.PropertyName));
            
            #line default
            #line hidden
            
            #line 15 ""
            this.Write(" { get; set; }");
            
            #line default
            #line hidden
            
            #line 15 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.Initializer == null ? "" : $" = {prop.Initializer};"));
            
            #line default
            #line hidden
            
            #line 15 ""
            this.Write("\n");
            
            #line default
            #line hidden
            
            #line 16 ""
 }
            
            #line default
            #line hidden
            
            #line 17 ""
            this.Write("    }\n");
            
            #line default
            #line hidden
            
            #line 18 ""
 }
            
            #line default
            #line hidden
            
            #line 19 ""
            this.Write("\n");
            
            #line default
            #line hidden
            
            #line 20 ""
 /* 検索結果DTO */ 
            
            #line default
            #line hidden
            
            #line 21 ""
 foreach (var classDef in SearchResultClasses) { 
            
            #line default
            #line hidden
            
            #line 22 ""
            this.Write("    public class ");
            
            #line default
            #line hidden
            
            #line 22 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(classDef.ClassName));
            
            #line default
            #line hidden
            
            #line 22 ""
            this.Write(" {\n");
            
            #line default
            #line hidden
            
            #line 23 ""
 foreach (var prop in classDef.Properties) { 
            
            #line default
            #line hidden
            
            #line 24 ""
            this.Write("        public ");
            
            #line default
            #line hidden
            
            #line 24 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.CSharpTypeName));
            
            #line default
            #line hidden
            
            #line 24 ""
            this.Write(" ");
            
            #line default
            #line hidden
            
            #line 24 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.PropertyName));
            
            #line default
            #line hidden
            
            #line 24 ""
            this.Write(" { get; set; }");
            
            #line default
            #line hidden
            
            #line 24 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.Initializer == null ? "" : $" = {prop.Initializer};"));
            
            #line default
            #line hidden
            
            #line 24 ""
            this.Write("\n");
            
            #line default
            #line hidden
            
            #line 25 ""
 }
            
            #line default
            #line hidden
            
            #line 26 ""
            this.Write("    }\n");
            
            #line default
            #line hidden
            
            #line 27 ""
 }
            
            #line default
            #line hidden
            
            #line 28 ""
            this.Write("\n");
            
            #line default
            #line hidden
            
            #line 29 ""
 /* シングルビューDTO */ 
            
            #line default
            #line hidden
            
            #line 30 ""
 foreach (var classDef in InstanceClasses) { 
            
            #line default
            #line hidden
            
            #line 31 ""
            this.Write("    public class ");
            
            #line default
            #line hidden
            
            #line 31 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(classDef.ClassName));
            
            #line default
            #line hidden
            
            #line 31 ""
            this.Write(" {\n");
            
            #line default
            #line hidden
            
            #line 32 ""
 foreach (var prop in classDef.Properties) { 
            
            #line default
            #line hidden
            
            #line 33 ""
            this.Write("        public ");
            
            #line default
            #line hidden
            
            #line 33 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.CSharpTypeName));
            
            #line default
            #line hidden
            
            #line 33 ""
            this.Write(" ");
            
            #line default
            #line hidden
            
            #line 33 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.PropertyName));
            
            #line default
            #line hidden
            
            #line 33 ""
            this.Write(" { get; set; }");
            
            #line default
            #line hidden
            
            #line 33 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.Initializer == null ? "" : $" = {prop.Initializer};"));
            
            #line default
            #line hidden
            
            #line 33 ""
            this.Write("\n");
            
            #line default
            #line hidden
            
            #line 34 ""
 }
            
            #line default
            #line hidden
            
            #line 35 ""
            this.Write("    }\n");
            
            #line default
            #line hidden
            
            #line 36 ""
 }
            
            #line default
            #line hidden
            
            #line 37 ""
            this.Write("\n}");
            
            #line default
            #line hidden
            return this.GenerationEnvironment.ToString();
        }
        
        public virtual void Initialize() {
        }
    }
    
    public class MvcModelsTemplateBase {
        
        private global::System.Text.StringBuilder builder;
        
        private global::System.Collections.Generic.IDictionary<string, object> session;
        
        private global::System.CodeDom.Compiler.CompilerErrorCollection errors;
        
        private string currentIndent = string.Empty;
        
        private global::System.Collections.Generic.Stack<int> indents;
        
        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();
        
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session {
            get {
                return this.session;
            }
            set {
                this.session = value;
            }
        }
        
        public global::System.Text.StringBuilder GenerationEnvironment {
            get {
                if ((this.builder == null)) {
                    this.builder = new global::System.Text.StringBuilder();
                }
                return this.builder;
            }
            set {
                this.builder = value;
            }
        }
        
        protected global::System.CodeDom.Compiler.CompilerErrorCollection Errors {
            get {
                if ((this.errors == null)) {
                    this.errors = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errors;
            }
        }
        
        public string CurrentIndent {
            get {
                return this.currentIndent;
            }
        }
        
        private global::System.Collections.Generic.Stack<int> Indents {
            get {
                if ((this.indents == null)) {
                    this.indents = new global::System.Collections.Generic.Stack<int>();
                }
                return this.indents;
            }
        }
        
        public ToStringInstanceHelper ToStringHelper {
            get {
                return this._toStringHelper;
            }
        }
        
        public void Error(string message) {
            this.Errors.Add(new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message));
        }
        
        public void Warning(string message) {
            global::System.CodeDom.Compiler.CompilerError val = new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message);
            val.IsWarning = true;
            this.Errors.Add(val);
        }
        
        public string PopIndent() {
            if ((this.Indents.Count == 0)) {
                return string.Empty;
            }
            int lastPos = (this.currentIndent.Length - this.Indents.Pop());
            string last = this.currentIndent.Substring(lastPos);
            this.currentIndent = this.currentIndent.Substring(0, lastPos);
            return last;
        }
        
        public void PushIndent(string indent) {
            this.Indents.Push(indent.Length);
            this.currentIndent = (this.currentIndent + indent);
        }
        
        public void ClearIndent() {
            this.currentIndent = string.Empty;
            this.Indents.Clear();
        }
        
        public void Write(string textToAppend) {
            this.GenerationEnvironment.Append(textToAppend);
        }
        
        public void Write(string format, params object[] args) {
            this.GenerationEnvironment.AppendFormat(format, args);
        }
        
        public void WriteLine(string textToAppend) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendLine(textToAppend);
        }
        
        public void WriteLine(string format, params object[] args) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendFormat(format, args);
            this.GenerationEnvironment.AppendLine();
        }
        
        public class ToStringInstanceHelper {
            
            private global::System.IFormatProvider formatProvider = global::System.Globalization.CultureInfo.InvariantCulture;
            
            public global::System.IFormatProvider FormatProvider {
                get {
                    return this.formatProvider;
                }
                set {
                    if ((value != null)) {
                        this.formatProvider = value;
                    }
                }
            }
            
            public string ToStringWithCulture(object objectToConvert) {
                if ((objectToConvert == null)) {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                global::System.Type type = objectToConvert.GetType();
                global::System.Type iConvertibleType = typeof(global::System.IConvertible);
                if (iConvertibleType.IsAssignableFrom(type)) {
                    return ((global::System.IConvertible)(objectToConvert)).ToString(this.formatProvider);
                }
                global::System.Reflection.MethodInfo methInfo = type.GetMethod("ToString", new global::System.Type[] {
                            iConvertibleType});
                if ((methInfo != null)) {
                    return ((string)(methInfo.Invoke(objectToConvert, new object[] {
                                this.formatProvider})));
                }
                return objectToConvert.ToString();
            }
        }
    }
}