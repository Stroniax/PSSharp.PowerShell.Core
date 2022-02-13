using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Identifies a PowerShell script method for the type this attribute is applied to.
    /// </summary>
    [AttributeUsage(PSExtendedTypeDataAnalysis.PSTypeTarget, AllowMultiple = true, Inherited = false)]
    public sealed class PSScriptMethodAttribute : PSExtendedTypeDataAttribute
    {
        /// <summary>
        /// Instantiates an attribute defining a script method for the type this attribute is applied to.
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
        /// <param name="scriptText"><inheritdoc cref="ScriptText" path="/summary"/></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PSScriptMethodAttribute(string name, string scriptText)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ScriptText = scriptText ?? throw new ArgumentNullException(nameof(scriptText));
        }

        /// <summary>
        /// The name of the script method.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The script text of the method. This value is defined as <see cref="string"/> to allow this attribute
        /// to be used without a direct reference to the System.Management.Automation library.
        /// </summary>
        public string ScriptText { get; }

        internal override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var appliedToType = (Type)attributeTarget;
            return new[]
            {
                new PSScriptMethodDefinition(
                    appliedToType.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired),
                    Name,
                    ScriptText
                    )
            };
        }
    }
}
