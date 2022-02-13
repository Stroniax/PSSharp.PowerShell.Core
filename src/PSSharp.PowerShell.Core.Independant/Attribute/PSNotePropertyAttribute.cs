using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Identifies a PowerShell NoteProperty to define on the type this attribute is applied to.
    /// </summary>
    /// <remarks>This type is not sealed so that derived classes may define a value beyond the
    /// compilation requirements for an attribute constructor.</remarks>
    [AttributeUsage(PSExtendedTypeDataAnalysis.PSTypeTarget, AllowMultiple = true, Inherited = false)]
    public class PSNotePropertyAttribute : PSExtendedTypeDataAttribute
    {
        /// <summary>
        /// Instantiates an attribute indicating that a note property should exist with the type and
        /// value provided for the type this attribute is applied.
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
        /// <param name="value"><inheritdoc cref="Value" path="/summary"/></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PSNotePropertyAttribute(string name, object? value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }
        /// <summary>
        /// The name of the PowerShell NoteProperty.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The value of the PowerShell NoteProperty.
        /// </summary>
        public object? Value { get; }
        internal sealed override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var appliedToType = (Type)attributeTarget;
            return new[]
            {
                new PSNotePropertyDefinition(
                    appliedToType.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired),
                    Name,
                    Value
                    )
            };
        }
    }
}
