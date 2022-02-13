using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Indicates that the type this attribute is applied to is a type adapter for another type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class PSTypeAdapterImplementationAttribute
        : PSExtendedTypeDataAttribute
    {
        public PSTypeAdapterImplementationAttribute(string appliesToTypeName)
        {
            AppliesToTypeName = appliesToTypeName ?? throw new ArgumentNullException(nameof(appliesToTypeName));
        }
        public PSTypeAdapterImplementationAttribute(Type appliesToType)
            : this(appliesToType?.FullName ?? throw new ArgumentNullException(nameof(appliesToType)))
        {
        }
        /// <summary>
        /// The type name that the type this attribute is applied to is a type adapter for.
        /// </summary>
        public string AppliesToTypeName { get; }
        internal override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var appliedToType = (Type)attributeTarget;
            return new PSExtendedTypeDataDefinition[]
            {
                new PSTypeAdapterDefinition(AppliesToTypeName, appliedToType.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired))
            };
        }
    }
}
