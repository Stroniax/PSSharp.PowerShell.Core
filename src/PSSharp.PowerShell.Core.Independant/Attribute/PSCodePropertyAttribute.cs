using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    [AttributeUsage(PSExtendedTypeDataAnalysis.PSTypeTarget, AllowMultiple = true, Inherited = false)]
    public sealed class PSCodePropertyAttribute : PSExtendedTypeDataAttribute
    {
        /// <summary>
        /// Instantiates an attribute that defines a code property for the type this attribute
        /// is defined on.
        /// </summary>
        /// <param name="name">The name of the code property.</param>
        /// <param name="referencedTypeNameGet">The type name that defines the property getter method.
        /// A value must be provided if the setter reference is null.</param>
        /// <param name="referencedMethodNameGet">The name of the getter method defined by the type
        /// <paramref name="referencedTypeNameGet"/>.
        /// If <paramref name="referencedTypeNameGet"/> is <see langword="null"/>, this value must
        /// also be <see langword="null"/>.
        /// A value must be provided if the setter reference is null.</param>
        /// <param name="referencedTypeNameSet">The type name that defines the property setter method.
        /// A value must be provided if the getter reference is null.</param>
        /// <param name="referencedMethodNameSet">The name of the setter method defiend by the type
        /// <paramref name="referencedTypeNameSet"/>.
        /// If <paramref name="referencedTypeNameSet"/> is <see langword="null"/>, this value must
        /// also be <see langword="null"/>.
        /// A value must be provided if the getter reference is null.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public PSCodePropertyAttribute(
            string name,
            string? referencedTypeNameGet,
            string? referencedMethodNameGet,
            string? referencedTypeNameSet,
            string? referencedMethodNameSet
            )
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));

            // Fail if any partial reference was provided
            if ((referencedTypeNameGet is null
                != referencedMethodNameGet is null)
                ||
                (referencedTypeNameSet is null
                != referencedMethodNameSet is null))
            {
                throw new ArgumentException(Errors.CodeMethodPartialReference);
            }
            if (referencedTypeNameGet != null
                && referencedMethodNameGet != null)
            {
                GetCodeReference = new CodeReference(referencedTypeNameGet, referencedMethodNameGet);
            }
            if (referencedTypeNameSet != null
                && referencedMethodNameSet != null)
            {
                SetCodeReference = new CodeReference(referencedTypeNameSet, referencedMethodNameSet);
            }
            if ((GetCodeReference ?? SetCodeReference) is null)
            {
                throw new InvalidOperationException(Errors.GetOrSetRequired);
            }
        }
        /// <summary>
        /// Instantiates an attribute that defines a code property for the type this attribute
        /// is defined on.
        /// </summary>
        /// <param name="name">The name of the code property.</param>
        /// <param name="referencedTypeGet">The type that defines the property getter method.
        /// A value must be provided if the setter reference is null.</param>
        /// <param name="referencedMethodNameGet">The name of the getter method defined by the type
        /// <paramref name="referencedTypeGet"/>.
        /// If <paramref name="referencedTypeGet"/> is <see langword="null"/>, this value must
        /// also be <see langword="null"/>.
        /// A value must be provided if the setter reference is null.</param>
        /// <param name="referencedTypeSet">The type that defines the property setter method.
        /// A value must be provided if the getter reference is null.</param>
        /// <param name="referencedMethodNameSet">The name of the setter method defiend by the type
        /// <paramref name="referencedTypeSet"/>.
        /// If <paramref name="referencedTypeSet"/> is <see langword="null"/>, this value must
        /// also be <see langword="null"/>.
        /// A value must be provided if the getter reference is null.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public PSCodePropertyAttribute(string name, Type referencedTypeGet, string referencedMethodNameGet, Type referencedTypeSet, string referencedMethodNameSet)
            : this(
                  name,
                  referencedTypeGet?.FullName ?? throw new ArgumentNullException(nameof(referencedTypeGet)),
                  referencedMethodNameGet,
                  referencedTypeSet?.FullName ?? throw new ArgumentNullException(nameof(referencedTypeSet)),
                  referencedMethodNameSet
                  )
        {
        }
        /// <inheritdoc cref="PSCodePropertyAttribute(string, string, string, string, string)"/>
        public PSCodePropertyAttribute(string name, string referencedTypeNameGet, string referencedMethodNameGet)
            : this(name, referencedTypeNameGet, referencedMethodNameGet, null, null)
        {
        }
        /// <inheritdoc cref="PSCodePropertyAttribute(string, Type, string, Type, string)"/>
        public PSCodePropertyAttribute(string name, Type referencedTypeGet, string referencedMethodNameGet)
            : this(name, referencedTypeGet?.FullName ?? throw new ArgumentNullException(nameof(referencedTypeGet)), referencedMethodNameGet)
        {
        }
        /// <summary>
        /// The name of the code property.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// A weak reference to the getter method for this code property.
        /// </summary>
        public CodeReference? GetCodeReference { get; }
        /// <summary>
        /// A weak reference to the setter method for this code property.
        /// </summary>
        public CodeReference? SetCodeReference { get; }

        internal override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var appliedToType = (Type)attributeTarget;
            return new PSExtendedTypeDataDefinition[]
            {
                new PSCodePropertyDefinition(
                    appliedToType.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired),
                    Name,
                    GetCodeReference,
                    SetCodeReference
                    )
            };
        }
    }
}
