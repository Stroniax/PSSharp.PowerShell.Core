using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Identifies a PowerShell code method to apply to the type this attribute is applied to.
    /// </summary>
    [AttributeUsage(PSExtendedTypeDataAnalysis.PSTypeTarget, AllowMultiple = true, Inherited = false)]
    public sealed class PSCodeMethodAttribute : PSExtendedTypeDataAttribute
    {
        public PSCodeMethodAttribute(string methodName, string referencedTypeName, string referencedMethodName)
        {
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            ReferencedTypeName = referencedTypeName ?? throw new ArgumentNullException(nameof(referencedTypeName));
            ReferencedMethodName = referencedMethodName ?? throw new ArgumentNullException(nameof(referencedMethodName));
        }
        public PSCodeMethodAttribute(string methodName, Type referencedType, string referencedMethodName)
            : this(
                  methodName,
                  referencedType?.FullName ?? throw new ArgumentNullException(nameof(referencedType)),
                  referencedMethodName
                  )
        {
        }
        /// <summary>
        /// The name of the code method to define on this type.
        /// </summary>
        public string MethodName { get; }
        /// <summary>
        /// The name of the type that contains the method invoked by this code method.
        /// </summary>
        public string ReferencedTypeName { get; }
        /// <summary>
        /// The name of the method invoked by this code method.
        /// </summary>
        public string ReferencedMethodName { get; }

        internal override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var appliedToType = (Type)attributeTarget;
            return new PSExtendedTypeDataDefinition[]
            {
                new PSCodeMethodDefinition(
                    appliedToType.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired),
                    MethodName,
                    new CodeReference(ReferencedTypeName, ReferencedMethodName)
                    )
            };
        }
    }
}
