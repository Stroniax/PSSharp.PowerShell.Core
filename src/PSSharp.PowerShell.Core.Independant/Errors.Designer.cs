﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PSSharp {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Errors {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Errors() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PSSharp.PowerShell.Core.Independant.Errors", typeof(Errors).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The awaitable operation failed..
        /// </summary>
        internal static string AwaitableJobError {
            get {
                return ResourceManager.GetString("AwaitableJobError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A partial reference to a code method was provided (the referenced type name or referenced method name, but not both). Both values must be defined..
        /// </summary>
        internal static string CodeMethodPartialReference {
            get {
                return ResourceManager.GetString("CodeMethodPartialReference", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The code reference could not be identified. The referenced type may not be imported into the application domain, or the method may not meet the CodeReference criteria..
        /// </summary>
        internal static string CodeReferenceNotFound {
            get {
                return ResourceManager.GetString("CodeReferenceNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value passed to the ArgumentCompleter is not implemented..
        /// </summary>
        internal static string DynamicParameterCompleterNotImplemented {
            get {
                return ResourceManager.GetString("DynamicParameterCompleterNotImplemented", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value passed to the ArgumentCompleter of type {0} is not implemented..
        /// </summary>
        internal static string DynamicParameterCompleterNotImplementedInterpolated {
            get {
                return ResourceManager.GetString("DynamicParameterCompleterNotImplementedInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The argument does not match any of the valid types for this parameter. The value must be assignable to one of the following types: {0}..
        /// </summary>
        internal static string EitherTypeValidationNotMatchedInterpolated {
            get {
                return ResourceManager.GetString("EitherTypeValidationNotMatchedInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected to get a result from the task but the result could not be retrieved. Exception when attempting to capture the result of &quot;await task&quot;: {0}.
        /// </summary>
        internal static string ExpectedTaskAwaitResultInterpolated {
            get {
                return ResourceManager.GetString("ExpectedTaskAwaitResultInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type data member must have at least one getter or setter..
        /// </summary>
        internal static string GetOrSetRequired {
            get {
                return ResourceManager.GetString("GetOrSetRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The job has already been started..
        /// </summary>
        internal static string JobAlreadyStarted {
            get {
                return ResourceManager.GetString("JobAlreadyStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The job must be running for the attempted operation to be executed..
        /// </summary>
        internal static string JobNotRunning {
            get {
                return ResourceManager.GetString("JobNotRunning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The job cannot be resumed unless it is Suspended..
        /// </summary>
        internal static string JobStatusNotResumable {
            get {
                return ResourceManager.GetString("JobStatusNotResumable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The provided value may not be a collection or bitwise combination of values..
        /// </summary>
        internal static string NoEnumeratedEnumValidation {
            get {
                return ResourceManager.GetString("NoEnumeratedEnumValidation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The provided object is not awaitable. The object must contain all required members to be awaitable - extension methods cannot be supported because awaiting the value is executed at runtime instead of compile time. If necessary, create a task-like wrapper for the object that provides the necessary members..
        /// </summary>
        internal static string NotAwaitable {
            get {
                return ResourceManager.GetString("NotAwaitable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This error should never occur with a production version of the module. Please report the bug to the module developer..
        /// </summary>
        internal static string NotImplementedHelpMessage {
            get {
                return ResourceManager.GetString("NotImplementedHelpMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The parameter set is not implemented..
        /// </summary>
        internal static string ParameterSetNotImplemented {
            get {
                return ResourceManager.GetString("ParameterSetNotImplemented", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The parameter set {0} is not implemented..
        /// </summary>
        internal static string ParameterSetNotImplementedInterpolated {
            get {
                return ResourceManager.GetString("ParameterSetNotImplementedInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The maximum retry attempt count has been reached..
        /// </summary>
        internal static string PingJobMaximumRetry {
            get {
                return ResourceManager.GetString("PingJobMaximumRetry", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The maximum retry attempt count {0} has been reached..
        /// </summary>
        internal static string PingJobMaximumRetryInterpolated {
            get {
                return ResourceManager.GetString("PingJobMaximumRetryInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The job state was not expected when determining a new job state or continuing the ping activity..
        /// </summary>
        internal static string PingJobUnhandledStateForContinuation {
            get {
                return ResourceManager.GetString("PingJobUnhandledStateForContinuation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The job state {0} was not expected when determining a new job state or continuing the ping activity..
        /// </summary>
        internal static string PingJobUnhandledStateForContinuationInterpolated {
            get {
                return ResourceManager.GetString("PingJobUnhandledStateForContinuationInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The CodeMethod requires a PSObject with a base value of type [{0}]..
        /// </summary>
        internal static string PSCodeMethodInvalidSourceInterpolated {
            get {
                return ResourceManager.GetString("PSCodeMethodInvalidSourceInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The input value is missing one or more expected members..
        /// </summary>
        internal static string RequiredShapeError {
            get {
                return ResourceManager.GetString("RequiredShapeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The method &apos;{0}&apos; with return type [{1}] and parameters {2} is not found..
        /// </summary>
        internal static string RequiredShapeMethodInterpolated {
            get {
                return ResourceManager.GetString("RequiredShapeMethodInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The property &apos;{0}&apos; of type [{1}] is not found..
        /// </summary>
        internal static string RequiredShapePropertyInterpolated {
            get {
                return ResourceManager.GetString("RequiredShapePropertyInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The script block is expected to have a single variable assignment expression..
        /// </summary>
        internal static string ScriptBlockVariableAssignmentExpected {
            get {
                return ResourceManager.GetString("ScriptBlockVariableAssignmentExpected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load type data for one or more type definitions. See the inner exception(s) for details..
        /// </summary>
        internal static string TypeDataAggregateLoadException {
            get {
                return ResourceManager.GetString("TypeDataAggregateLoadException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type data {0} is already defined for type [{1}]..
        /// </summary>
        internal static string TypeDataDefinedInterpolated {
            get {
                return ResourceManager.GetString("TypeDataDefinedInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The conversion to TypeData from type data definition {0} is not implemented..
        /// </summary>
        internal static string TypeDataDefinitionToTypeDataNotImplementedInterpolated {
            get {
                return ResourceManager.GetString("TypeDataDefinitionToTypeDataNotImplementedInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load type data for definition {0}. {1}.
        /// </summary>
        internal static string TypeDataLoadExceptionInterpolated {
            get {
                return ResourceManager.GetString("TypeDataLoadExceptionInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type data member {0} is already a defined {1} for type {2}..
        /// </summary>
        internal static string TypeDataMemberDefinedInterpolated {
            get {
                return ResourceManager.GetString("TypeDataMemberDefinedInterpolated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type may not have a null FullName value..
        /// </summary>
        internal static string TypeFullNameRequired {
            get {
                return ResourceManager.GetString("TypeFullNameRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type must be derived from [System.Management.Automation.Language.Ast]..
        /// </summary>
        internal static string TypeNotDerivedFromAst {
            get {
                return ResourceManager.GetString("TypeNotDerivedFromAst", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The provided type argument does not represent a discriminated union type..
        /// </summary>
        internal static string TypeNotDiscriminatedUnion {
            get {
                return ResourceManager.GetString("TypeNotDiscriminatedUnion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attempted to stop the job but no cancellation action was provided. The job will be stopped, but the awaitable action will continue..
        /// </summary>
        internal static string UnstoppableAwaitJob {
            get {
                return ResourceManager.GetString("UnstoppableAwaitJob", resourceCulture);
            }
        }
    }
}