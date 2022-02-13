using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Indicates that a PowerShell ScriptMethod should call the extension method that this attribute
    /// is defined on.
    /// </summary>
    public sealed class PSScriptExtensionMethodAttribute : PSExtendedTypeDataAttribute
    {
        /// <summary>
        /// Instantiate an attribute defining a script method that calls an associated extension method
        /// which this attribute is applied to.
        /// </summary>
        public PSScriptExtensionMethodAttribute()
        {
        }
        /// <summary>
        /// If set, overrides the name of the script method (instead of using the name of the method
        /// itself).
        /// </summary>
        public string? MethodName { get; set; }

        internal override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var appliedToMethod = (MethodInfo)attributeTarget;
            var script = new StringBuilder();
            script.AppendLine("[CmdletBinding()]");
            script.AppendFormat("[OutputType([{0}])]", appliedToMethod.ReturnType.FullName).AppendLine();
            script.AppendLine("param(");
            var parameters = appliedToMethod.GetParameters();
            for (int i = 1; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                script.AppendFormat("[Parameter(Mandatory)][{0}]${1}", parameter.ParameterType.FullName, parameter.Name);
                if (i < parameters.Length - 1)
                {
                    script.Append(',');
                }
                script.AppendLine();
            }
            script.AppendLine(")");
            script.AppendLine("process {");
            script.AppendFormat("[{0}]::'{1}'($this", appliedToMethod.DeclaringType?.FullName, appliedToMethod.Name);
            for (int i = 0; i < parameters.Length; i++)
            {
                script.Append(',');
                script.AppendFormat("${0}", parameters[i].Name);
            }
            script.Append(')');
            script.AppendLine();
            script.AppendLine("}");

            var extendsType = parameters[0].ParameterType;
            return new PSExtendedTypeDataDefinition[]
            {
                new PSScriptMethodDefinition(
                    extendsType.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired),
                    MethodName ?? appliedToMethod.Name,
                    script.ToString()
                    )
            };
        }
    }
}
