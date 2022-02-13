using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Indicates that the method this attribute is applied to is a getter method for a
    /// code property.
    /// </summary>
    [AttributeUsage(PSExtendedTypeDataAnalysis.PSMethodTarget, AllowMultiple = true, Inherited = false)]
    public sealed class PSCodePropertyGetImplementationAttribute : PSExtendedTypeDataAttribute
    {
        /// <summary>
        /// Instantiate an attribute indicating that the property this attribute is applied to is a getter
        /// code method for the type indicated.
        /// </summary>
        /// <param name="type">The type that the code property should be applied to.</param>
        /// <param name="propertyName">The name of the code property.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public PSCodePropertyGetImplementationAttribute(Type type, string propertyName)
            : this(type?.FullName ?? throw new ArgumentNullException(nameof(type)), propertyName)
        {
        }
        /// <param name="type">The <see cref="Type.FullName"/> that the code property should be applied to.</param>
        /// <param name="propertyName">The name of the code property.</param>
        public PSCodePropertyGetImplementationAttribute(string typeName, string propertyName)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        }
        /// <summary>
        /// The <see cref="Type.FullName"/> that the code property should be applied to.
        /// </summary>
        public string TypeName { get; }
        /// <summary>
        /// The name of the code property.
        /// </summary>
        public string PropertyName { get; }

        internal override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var appliedToMethod = (MethodInfo)attributeTarget;
            return new PSExtendedTypeDataDefinition[]
            {
                new PSCodePropertyDefinition(
                    TypeName,
                    PropertyName,
                    new CodeReference(appliedToMethod.DeclaringType?.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired), appliedToMethod.Name),
                    null
                    )
            };
        }
    }
}
