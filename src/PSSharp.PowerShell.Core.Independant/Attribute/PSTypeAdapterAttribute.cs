using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Indicates that the type indicated by <see cref="TypeAdapterTypeName"/> is a PowerShell TypeAdapter for
    /// instances of the type this attribute is applied to.
    /// </summary>
    public sealed class PSTypeAdapterAttribute : PSExtendedTypeDataAttribute
    {
        /// <summary>
        /// Instantiate an attribute indicating a type adapter to associate with the type this attribute is defined on.
        /// </summary>
        /// <param name="typeAdapterTypeName">The <see cref="Type.FullName"/> of the type that implements
        /// PSTypeAdapter to provide members for instances of the type this attribute is applied to.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public PSTypeAdapterAttribute(string typeAdapterTypeName)
        {
            TypeAdapterTypeName = typeAdapterTypeName ?? throw new ArgumentNullException(nameof(typeAdapterTypeName));
        }
        /// <summary>
        /// Instantiate an attribute indicating a type adapter to associate with the type this attribute is defined on.
        /// </summary>
        /// <param name="typeAdapterType">The <see cref="Type"/> that implements PSTypeAdapter to provide
        /// members for instances of the type this attribute is applied to.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public PSTypeAdapterAttribute(Type typeAdapterType)
            : this(typeAdapterType?.FullName ?? throw new ArgumentNullException(nameof(typeAdapterType)))
        {
        }
        /// <summary>
        /// The <see cref="Type.FullName"/> of the type that implements PSTypeAdapter to provide
        /// members for instances of the type this attribute is applied to.
        /// </summary>
        public string TypeAdapterTypeName { get; }

        internal override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var appliedToType = (Type)attributeTarget;
            return new PSExtendedTypeDataDefinition[]
            {
                new PSTypeAdapterDefinition(appliedToType.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired), TypeAdapterTypeName)
            };
        }
    }
}
